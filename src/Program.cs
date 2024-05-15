using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OwlCore.Diagnostics;
using OwlCore.Kubo;
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
using WinAppCommunity.Discord.ServerCompanion;
using WinAppCommunity.Discord.ServerCompanion.Autocomplete;
using WinAppCommunity.Discord.ServerCompanion.Commands;
using WinAppCommunity.Discord.ServerCompanion.Interactivity;
using WinAppCommunity.Discord.ServerCompanion.Keystore;

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
var serverCompanionData = (SystemFolder)await appData.CreateFolderAsync("WinAppCommunity.Discord.ServerCompanion", overwrite: false, cancelTok);
var kuboRepo = (SystemFolder)await serverCompanionData.CreateFolderAsync(".ipfs", overwrite: false, cancelTok);
var keystoreData = (SystemFolder)await serverCompanionData.CreateFolderAsync("keystore", overwrite: false, cancelTok);

var userKeystore = new UserKeystore(keystoreData);
var projectKeystore = new ProjectKeystore(keystoreData);
var publisherKeystore = new PublisherKeystore(keystoreData);

await userKeystore.LoadAsync(cancelTok);
await projectKeystore.LoadAsync(cancelTok);
await publisherKeystore.LoadAsync(cancelTok);

// Kubo setup (for ipfs access)
var kubo = new KuboBootstrapper(kuboRepo.Path)
{
    RoutingMode = DhtRoutingMode.AutoClient,
    LaunchConflictMode = BootstrapLaunchConflictMode.Attach,
};

await kubo.StartAsync(cancelTok);
var ipfsClient = kubo.Client;

Logger.LogInformation("Kubo is running and ready to use");

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