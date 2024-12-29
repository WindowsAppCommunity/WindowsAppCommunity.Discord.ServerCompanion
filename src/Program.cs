using CommunityToolkit.Diagnostics;
using Ipfs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OwlCore.Diagnostics;
using OwlCore.Kubo;
using OwlCore.Nomad;
using OwlCore.Nomad.Extensions;
using OwlCore.Storage.System.IO;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Gateway.Results;
using Remora.Discord.Interactivity.Extensions;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion;
using WindowsAppCommunity.Discord.ServerCompanion.Autocomplete;
using WindowsAppCommunity.Discord.ServerCompanion.Commands;
using WindowsAppCommunity.Discord.ServerCompanion.Interactivity;
using WindowsAppCommunity.Sdk.AppModels;
using WindowsAppCommunity.Sdk.Models;
using WindowsAppCommunity.Sdk.Nomad.Kubo;

// Cancellation setup
var cancellationSource = new CancellationTokenSource();
var cancelTok = cancellationSource.Token;

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

AppDomain.CurrentDomain.ProcessExit += (object? sender, EventArgs e) =>
{
    cancellationSource.Cancel();
};

// Logging setup
Logger.MessageReceived += Logger_MessageReceived;

void Logger_MessageReceived(object? sender, LoggerMessageEventArgs e) => Console.WriteLine($"{DateTime.UtcNow:O} [{e.Level}] [Thread {Thread.CurrentThread.ManagedThreadId}] L{e.CallerLineNumber} {Path.GetFileName(e.CallerFilePath)} {e.CallerMemberName} {e.Exception} {e.Message}");

// Config setup
var isDebug =
#if DEBUG
    true;
#else
    false;
#endif

var env = isDebug ? "dev" : "prod";

var configProvider = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build()
    .Providers
    .First();

configProvider.TryGet($"{env}:DiscordBotToken", out var botToken);
configProvider.TryGet($"{env}:GuildId", out var guildId);

ArgumentNullException.ThrowIfNullOrEmpty(botToken);
ArgumentNullException.ThrowIfNullOrEmpty(guildId);

// Service setup
var config = new ServerCompanionConfig(botToken, guildId);

var appData = new SystemFolder(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
var serverCompanionData = (SystemFolder)await appData.CreateFolderAsync("WindowsAppCommunity.Discord.ServerCompanion", overwrite: false, cancelTok);
var kuboRepo = (SystemFolder)await serverCompanionData.CreateFolderAsync(".ipfs", overwrite: false, cancelTok);

// Kubo setup (for ipfs access)
var kubo = new KuboBootstrapper(kuboRepo.Path)
{
    RoutingMode = DhtRoutingMode.AutoClient,
    LaunchConflictMode = BootstrapLaunchConflictMode.Attach,
};

await kubo.StartAsync(cancelTok);
var ipfsClient = kubo.Client;

Logger.LogInformation("Kubo is running and ready to use");


var kuboOptions = new KuboOptions
{
    ShouldPin = false,
    IpnsLifetime = TimeSpan.FromDays(1),
    UseCache = false,
};

// Get or create root community publisher event stream
var communityKey = await ipfsClient.GetOrCreateKeyAsync("WindowsAppCommunity", x => new KuboNomadEventStream { Id = x.Id, Entries = [], Label = "Windows App Community" }, kuboOptions.IpnsLifetime, cancellationToken: cancelTok);

// Get all other managed event streams
var allKeys = await ipfsClient.Key.ListAsync(cancelTok);

// Find all nomad event streams
var sources = new List<KuboNomadEventStream>();
foreach (var key in allKeys)
{
    try
    {
        var (result, resultCid) = await ipfsClient.ResolveDagCidAsync<KuboNomadEventStream>(key.Id, nocache: !kuboOptions.UseCache, cancellationToken: cancelTok);
        if (result is not null)
            sources.Add(result);
    }
    catch
    {
        // ignored, timeout or key value is not nomad event stream.
    }
}

var listeningEventStreamHandlers = new List<ISharedEventStreamHandler<Cid, KuboNomadEventStream, KuboNomadEventStreamEntry>>();
var communityPublisher = new ModifiablePublisherAppModel(listeningEventStreamHandlers)
{
    KuboOptions = kuboOptions,
    Client = ipfsClient,
    Id = communityKey.Id,
    LocalEventStreamKeyName = communityKey.Name,
    Sources = sources,
};

await foreach (var eventStreamEntry in communityPublisher.AdvanceSharedEventStreamAsync(ContentPointerToStreamEntryAsync, cancelTok))
{
    // Event handler id and event stream entry id should match.
    Logger.LogInformation($"Event applied to handler {eventStreamEntry.Id}: {eventStreamEntry.TimestampUtc} {eventStreamEntry.Content}");
    cancelTok.ThrowIfCancellationRequested();
}

Logger.LogInformation($"Root publisher {communityPublisher.Inner.Name} is ready");

async Task<KuboNomadEventStreamEntry> ContentPointerToStreamEntryAsync(Cid cid, CancellationToken token)
{
    var (streamEntry, streamEntryCid) = await ipfsClient.ResolveDagCidAsync<KuboNomadEventStreamEntry>(cid, nocache: !communityPublisher.KuboOptions.UseCache, token);
    Guard.IsNotNull(streamEntry);
    return streamEntry;
}

// Service setup and init
var services = new ServiceCollection()
    .AddSingleton(config)
    .AddSingleton(userKeystore)
    .AddSingleton(projectKeystore)
    .AddSingleton(publisherKeystore)
    .AddSingleton(ipfsClient)
    .AddDiscordGateway(_ => botToken)
    .AddDiscordCommands(enableSlash: true)
    .AddInteractivity()
    .AddInteractionGroup<MyInteractions>()
    .AddCommands()
        .AddCommandTree()
            .WithCommandGroup<SampleCommandGroup>()
            .WithCommandGroup<ProjectCommandGroup>()
            .WithCommandGroup<PublisherCommandGroup>()
            .WithCommandGroup<UserCommandGroup>()
            .Finish()
    .AddResponder<PingPongResponder>()
    .Configure<DiscordGatewayClientOptions>(g => g.Intents |= GatewayIntents.MessageContents)
    .AddAutocompleteProvider<SampleAutoCompleteProvider>()
    .AddAutocompleteProvider<ProjectCategoryAutoCompleteProvider>()
    .AddAutocompleteProvider<UserAutoCompleteProvider>()
    .BuildServiceProvider();

var log = services.GetRequiredService<ILogger<Program>>();
var gatewayClient = services.GetRequiredService<DiscordGatewayClient>();
var slashService = services.GetRequiredService<SlashService>();

await slashService.UpdateSlashCommandsAsync(new Remora.Rest.Core.Snowflake(ulong.Parse(guildId)), ct: cancelTok);

var runResult = await gatewayClient.RunAsync(cancelTok);

switch (runResult.Error)
{
    case null:
        break;
    case ExceptionError exe:
        log.LogError
        (
            exe.Exception,
            "Exception during gateway connection: {ExceptionMessage}",
            exe.Message
        );

        break;
    case GatewayWebSocketError:
    case GatewayDiscordError:
        log.LogError("Gateway error: {Message}", runResult.Error.Message);
        break;
    default:
        log.LogError("Unknown error: {Message}", runResult.Error.Message);
        break;
}

Console.WriteLine("Shutting down");