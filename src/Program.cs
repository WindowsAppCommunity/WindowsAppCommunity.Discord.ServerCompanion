using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OwlCore.Diagnostics;
using OwlCore.Kubo;
using OwlCore.Nomad.Kubo;
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
using WindowsAppCommunity.Discord.ServerCompanion.Commands.User;
using WindowsAppCommunity.Discord.ServerCompanion.Interactivity;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Project;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Publisher;
using Ipfs.CoreApi;

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

var appDataFolder = new SystemFolder(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
var windowsAppCommunityFolder = (SystemFolder)await appDataFolder.CreateFolderAsync("WindowsAppCommunity", overwrite: false, cancelTok);
var wacDiscordFolder = (SystemFolder)await windowsAppCommunityFolder.CreateFolderAsync("Discord", overwrite: false, cancelTok);
var serverCompanionDataFolder = (SystemFolder)await wacDiscordFolder.CreateFolderAsync("ServerCompanion", overwrite: false, cancelTok);

// Bootstrap and start Kubo
var kuboRepoFolder = (SystemFolder)await serverCompanionDataFolder.CreateFolderAsync(".ipfs", overwrite: false);
var kubo = new KuboBootstrapper(kuboRepoFolder.Path)
{
    GatewayUri = new Uri("http://127.0.0.1:8025"),
    ApiUri = new Uri("http://127.0.0.1:5025"),
    GatewayUriMode = ConfigMode.OverwriteExisting,
    ApiUriMode = ConfigMode.OverwriteExisting,
    LaunchConflictMode = BootstrapLaunchConflictMode.Attach,
    RoutingMode = DhtRoutingMode.Auto,
};

await kubo.StartAsync();

var kuboOptions = new KuboOptions
{
    IpnsLifetime = TimeSpan.FromDays(1),
    ShouldPin = false,
    UseCache = false,
};

// Nomad entity data repo manager
var nomadEntityRepoGroupRepositoryDataFolder = (SystemFolder)await serverCompanionDataFolder.CreateFolderAsync("Nomad", overwrite: false, cancelTok);
var wacNomadEntityRepoGroupRepository = new WacNomadRepoGroupRepository
{
    Client = kubo.Client,
    KuboOptions = kuboOptions,
    DataFolder = nomadEntityRepoGroupRepositoryDataFolder,
};

// Service setup and init  
var services = new ServiceCollection()
  .AddSingleton(kubo)
  .AddSingleton<ICoreApi>(kubo.Client)
  .AddSingleton<IKuboOptions>(kuboOptions)
  .AddSingleton(wacNomadEntityRepoGroupRepository)  
  .AddDiscordGateway(_ => botToken)
  .AddDiscordCommands(enableSlash: true)
  .AddInteractivity()
  .AddInteractionGroup<MyInteractions>()
  .AddCommands()
      .AddCommandTree()
          .WithCommandGroup<PortalCommandGroup>()
          .WithCommandGroup<SampleCommandGroup>()
          //.WithCommandGroup<UserCommandGroup>()
          //.WithCommandGroup<ProjectCommandGroup>()
          //.WithCommandGroup<PublisherCommandGroup>()
          .Finish()
  .AddResponder<PingPongResponder>()
  .Configure<DiscordGatewayClientOptions>(g => g.Intents |= GatewayIntents.MessageContents)
  .AddAutocompleteProvider<SampleAutoCompleteProvider>()
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