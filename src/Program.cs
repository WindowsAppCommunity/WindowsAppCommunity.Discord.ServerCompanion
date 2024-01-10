using Ipfs.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OwlCore.Kubo;
using OwlCore.Storage.Memory;
using OwlCore.Storage.SystemIO;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Gateway.Results;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands;
using WinAppCommunity.Discord.ServerCompanion.Keystore;

// Cancellation setup
var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

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

var config = new ServerCompanionConfig(botToken, guildId);

var userKeystore = new UserKeystore(new MemoryFolder("", nameof(UserKeystore)));
var projectKeystore = new ProjectKeystore(new MemoryFolder("", nameof(UserKeystore)));
var publisherKeystore = new PublisherKeystore(new MemoryFolder("", nameof(UserKeystore)));

var tempFolder = new SystemFolder(Path.GetTempPath());
var repoPath = (SystemFolder)await tempFolder.CreateFolderAsync(".ipfs", overwrite: false);

var kubo = new KuboBootstrapper(repoPath.Path)
{
    ApiUri = new Uri("http://127.0.0.1:5021"),
    GatewayUri= new Uri("http://127.0.0.1:8021"),
};

await kubo.StartAsync();

var ipfsClient = new IpfsClient(kubo.ApiUri.OriginalString);

// Service setup and init
{
    var services = new ServiceCollection()
        .AddSingleton(config)
        .AddSingleton(userKeystore)
        .AddSingleton(projectKeystore)
        .AddSingleton(publisherKeystore)
        .AddSingleton(ipfsClient)
        .AddDiscordGateway(_ => botToken)
        .AddDiscordCommands(enableSlash: true)
        .AddCommands()
            .AddCommandTree()
                .WithCommandGroup<UserCommands>()
                .Finish()
        .AddResponder<PingPongResponder>()
        .Configure<DiscordGatewayClientOptions>(g => g.Intents |= GatewayIntents.MessageContents)
        .BuildServiceProvider();

    var log = services.GetRequiredService<ILogger<Program>>();

    var gatewayClient = services.GetRequiredService<DiscordGatewayClient>();
    var slashService = services.GetRequiredService<SlashService>();

    await slashService.UpdateSlashCommandsAsync(new Remora.Rest.Core.Snowflake(ulong.Parse(guildId)), ct: cancellationSource.Token);

    var runResult = await gatewayClient.RunAsync(cancellationSource.Token);

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
}

Console.WriteLine("Shutting down");
