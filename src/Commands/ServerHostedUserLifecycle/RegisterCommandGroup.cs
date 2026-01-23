using System.ComponentModel;
using CommunityToolkit.Diagnostics;
using Ipfs.CoreApi;
using OwlCore.Diagnostics;
using OwlCore.Nomad.Kubo;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Interactivity;
using Remora.Discord.Commands.Attributes;
using WindowsAppCommunity.Sdk.Nomad;
using Remora.Rest.Core;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;
using System.Drawing;
using WindowsAppCommunity.Discord.ServerCompanion.Responders;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands;

public partial class RegisterCommandGroup(
    ICommandContext CommandContext,
    IInteractionContext InteractionContext,
    IFeedbackService Feedback,
    IDiscordServerHostedUserSettingsRepository HostedUserSettingsRepo,
    ICoreApi Client,
    IKuboOptions KuboOptions,
    IWacNomadRepoGroupRepository WacNomadRepoGroupRepository,
    IDiscordRestInteractionAPI InteractionApi,
    IDiscordRestGuildAPI GuildApi,
    IDiscordRestUserAPI UserApi,
    IDiscordRestChannelAPI ChannelApi,
    DiscordServerHostingSettings ServerHostingSettings,
    ReactionResponder ReactionResponder 
) : Remora.Commands.Groups.CommandGroup
{
    public static readonly EmbedFooter WacEmbedFooter = new EmbedFooter("Windows App Community", "https://cdn.discordapp.com/emojis/1295806709255770267.png?size=96");

    public static Embed HostingTermsEmbed { get; } = new Embed
    {
        Title = "Hosting Terms",
        Description = """
        We offer to host your data on our dedicated server, enabling access to your own isolated instance of [`WindowsAppCommunity.Sdk`](https://github.com/WindowsAppCommunity/WindowsAppCommunity.Sdk/) via Discord slash commands on our Server Companion bot.
        
        Any user, project and publisher data you create with us will be uniquely identifiable by its own [IPNS](https://docs.ipfs.tech/concepts/ipns/) public key and published to the public IPFS ["Amino" DHT](https://docs.ipfs.tech/concepts/glossary/#amino-dht) on your behalf. In addition to being accessible through Discord itself, it will be accessible through most [public gateways](https://docs.ipfs.tech/concepts/public-utilities/#public-ipfs-gateways) (including our hosted community gateways) and through any individual device running an [ipfs](https://docs.ipfs.tech/install) node. 
        
        We promise to operate on your behalf only when you request it or in accordance with your preferences, and to protect your data to the best of our ability.
        
        By registering through our Discord bot, you acknowledge the additional risk and reward that centralization offers for malicious actors. If you'd like to help contribute towards the launch of finished p2p self-host support, stay tuned to our upcoming Appathon events in the Windows App Community for opportunities to contribute.
        """,
        Footer = WacEmbedFooter,
        Type = EmbedType.Article,
    };

    public static Embed PrivacyPolicyNoticeEmbed { get; } = new Embed
    {
        Title = "Privacy Policy",
        Description = "To continue, please confirm that you have read and agree to our [Privacy Policy](https://windowsappcommunity.com/privacy-policy) and that you are at least 13 years of age.",
        Footer = WacEmbedFooter,
    };

    [Command("register")]
    [Description("Onboard our hosted collaboration services")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> Register([Description("The display name for your user profile")] string name, [Description("Duration in days your data is kept after you leave or unregister. Default (recommended) is 30.")] int dataRetentionPeriod = 30)
    {
        try
        {
            if (!CommandContext.TryGetUserID(out var userId))
                return await InteractionApi.CreateInteractionFailureResponseAsync(new UserNotFoundError("Could not determine user's Discord ID"), InteractionContext, this.CancellationToken);

            var registrationInitialValues = new ServerHostedRegisterInitialUserProperties(name);

            var hostPublisherCheck = await WacNomadRepoGroupRepository.EnsureGuildHostPublisherExists(CommandContext, InteractionContext, InteractionApi, GuildApi, UserApi, ChannelApi, ServerHostingSettings, ReactionResponder.Tracker, registrationInitialValues, CancellationToken);
            if (!hostPublisherCheck.IsSuccess)
                return hostPublisherCheck;

            var cancellationToken = this.CancellationToken;
            var settings = await GetOrCreateSettingsAsync($"{userId}", cancellationToken);
            settings.PendingRegistrationData = registrationInitialValues;
            settings.DataRetentionPeriod = TimeSpan.FromDays(dataRetentionPeriod);

            // First time user
            if (settings.NeverRegistered)
            {
                return await NewUserRegistrationAsync(settings, cancellationToken);
            }

            // Returning user
            else if (!settings.RegistrationActive)
            {
                return await ReactivateUserRegistrationAsync(settings, cancellationToken);
            }

            // Already registered, not unregistered.
            else
            {
                return await InteractionApi.CreateInteractionSuccessResponseAsync("You're already registered!", InteractionContext, this.CancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message, ex);
            return await InteractionApi.CreateInteractionFailureResponseAsync(ex, InteractionContext, this.CancellationToken);
        }
    }

    private async Task<IResult> NewUserRegistrationAsync(DiscordServerHostedUserSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var buttons = new ActionRowComponent(new[]
        {
            new ButtonComponent(ButtonComponentStyle.Success, "Agree", CustomID: CustomIDHelpers.CreateButtonID("register-agree")),
            new ButtonComponent(ButtonComponentStyle.Danger, "Decline", CustomID: CustomIDHelpers.CreateButtonID("register-decline"))
        });

        var result = await InteractionApi.CreateInteractionResponseAsync(
            InteractionContext.Interaction.ID,
            InteractionContext.Interaction.Token,
            new InteractionResponse(
                InteractionCallbackType.ChannelMessageWithSource,
                new(new InteractionMessageCallbackData(Flags: MessageFlags.Ephemeral, Embeds: new[] { HostingTermsEmbed, PrivacyPolicyNoticeEmbed }, Components: new[] { buttons }))
            ),
            ct: this.CancellationToken
        );

        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result.Error);
    }

    private async Task<IResult> ReactivateUserRegistrationAsync(DiscordServerHostedUserSettings settings, CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(settings.DiscordId);
        throw new NotImplementedException();
    }

    private async Task<DiscordServerHostedUserSettings> GetOrCreateSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DiscordServerHostedUserSettings settings;
        try
        {
            settings = await HostedUserSettingsRepo.GetAsync($"{userId}", cancellationToken);
        }
        catch (FileNotFoundException)
        {
            settings = await HostedUserSettingsRepo.CreateAsync($"{userId}", cancellationToken);
        }

        return settings;
    }
}

/// <summary>
/// Handles button interactions for the registration flow.
/// </summary>
public class RegisterInteractionGroup(IDiscordServerHostedUserSettingsRepository ServerSettingsRepo, IWacNomadRepoGroupRepository WacNomadRepoGroupRepository, IInteractionCommandContext Context, IDiscordRestInteractionAPI InteractionApi) : InteractionGroup
{
    [Button("register-agree")]
    public async Task<Result> OnAgreeAsync()
    {
        if (!Context.TryGetGuildID(out var guildId))
            return Result.FromError(new PublisherNotFoundError("Could not determine guild ID for publisher retrieval"));

        var serverHostWacPublisherRepoItem = await WacNomadRepoGroupRepository.GetAsync(guildId.ToString(), this.CancellationToken);
        var serverHostWacPublisher = await serverHostWacPublisherRepoItem.RepositoryGroup.PublisherRepository.GetAsync(guildId.ToString(), this.CancellationToken);

        var discordUser = Context.Interaction.Member.HasValue
            ? Context.Interaction.Member.Value.User.Value
            : Context.Interaction.User.Value;

        var userId = discordUser.ID.ToString();

        // Get server-hosted settings instance for this user
        await UpdateStatus("Getting server-hosted user settings", userId);
        var serverSettings = await ServerSettingsRepo.GetAsync(userId, this.CancellationToken);
        var displayName = serverSettings.PendingRegistrationData.DisplayName;

        // Apply server-hosted user registration settings
        await UpdateStatus("Applying server-hosted user settings", displayName);
        serverSettings.DiscordId = $"{userId}";
        serverSettings.AcceptedTermsOfService = true;
        serverSettings.AcceptedPrivacyPolicy = true;
        serverSettings.RegisteredAt.Add(DateTimeOffset.Now);

        // Open wac entity nomad repository and backing settings
        await UpdateStatus($"Getting wac entity repository and setting for Discord user {serverSettings.DiscordId}", displayName);
        var wacNomadRepoItem = await WacNomadRepoGroupRepository.GetOrCreateWacNomadRepoAsync(serverSettings.DiscordId, this.CancellationToken);

        // Create new wac sdk user
        await UpdateStatus($"Creating new WAC user for Discord user {serverSettings.DiscordId}", displayName);
        var wacUser = await wacNomadRepoItem.RepositoryGroup.UserRepository.CreateAsync(new UserCreateParam(KnownId: serverSettings.DiscordId), this.CancellationToken);

        // Apply initial property values from `/register` command input
        await UpdateStatus($"Applying initial values", displayName, wacUser.Id);
        await wacUser.UpdateNameAsync(serverSettings.PendingRegistrationData.DisplayName, this.CancellationToken);

        // Publish Nomad event stream and roaming key to ipns
        await UpdateStatus($"Publishing new user to ipns. This may take a minute.", displayName, wacUser.Id);
        await wacUser.FlushAsync(this.CancellationToken);

        // Persist created/published Nomad ipns keys to settings storage
        await UpdateStatus($"Saving Nomad settings for created wac entities", displayName, wacUser.Id);
        await wacNomadRepoItem.Settings.SaveAsync(this.CancellationToken);

        // Cleanup temp registration data
        await UpdateStatus($"Resetting temp registration data", displayName, wacUser.Id);
        serverSettings.ResetSetting(nameof(serverSettings.PendingRegistrationData));

        // Persist server-hosted user registration settings
        await UpdateStatus($"Saving server-hosted user registration settings", displayName, wacUser.Id);
        await serverSettings.SaveAsync(this.CancellationToken);

        return (Result)await InteractionApi.EditOriginalInteractionResponseAsync(
            Context.Interaction.ApplicationID,
            Context.Interaction.Token,
            content: "Welcome! You have successfully registered.",
            embeds: Array.Empty<IEmbed>(),
            components: Array.Empty<IMessageComponent>(),
            ct: this.CancellationToken
        );

        async Task UpdateStatus(string statusMessage, string displayName, string? wacUserId = null)
        {
            var statusEmbed = new Embed
            {
                Title = "Setup in progress",
                Description = statusMessage,
                Footer = new EmbedFooter($"{RegisterCommandGroup.WacEmbedFooter.Text}: {displayName}\n-# {(wacUserId is { } ipnsId ? $"{ipnsId}" : new Optional<string>())}", RegisterCommandGroup.WacEmbedFooter.IconUrl),
                Colour = serverHostWacPublisher.AccentColor is { } accent ? ColorTranslator.FromHtml(accent) : new Optional<Color>(),
            };

            await InteractionApi.EditOriginalInteractionResponseAsync(Context.Interaction.ApplicationID, Context.Interaction.Token,
                embeds: new List<Embed>([RegisterCommandGroup.HostingTermsEmbed, RegisterCommandGroup.PrivacyPolicyNoticeEmbed, statusEmbed]),
                components: Array.Empty<IMessageComponent>(),
                ct: this.CancellationToken
            );
        }
    }

    [Button("register-decline")]
    public async Task<Result> OnDeclineAsync()
    {
        return (Result)await InteractionApi.EditOriginalInteractionResponseAsync(
            Context.Interaction.ApplicationID,
            Context.Interaction.Token,
            content: "Registration declined.",
            embeds: Array.Empty<IEmbed>(),
            components: Array.Empty<IMessageComponent>(),
            ct: this.CancellationToken
        );
    }


}