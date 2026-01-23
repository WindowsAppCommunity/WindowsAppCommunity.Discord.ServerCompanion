using System.Drawing;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Rest.Core;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WindowsAppCommunity.Discord.ServerCompanion.Responders;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// Extension methods for <see cref="IWacNomadRepoGroupRepository"/>.
/// </summary>
public static class WacNomadRepoGroupRepositoryExtensions
{
    public static async Task<WacNomadRepoGroupRepositoryItem> GetOrCreateWacNomadRepoAsync(this IWacNomadRepoGroupRepository wacNomadRepoGroupRepository, string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WacNomadRepoGroupRepositoryItem repoItem;
        try
        {
            repoItem = await wacNomadRepoGroupRepository.GetAsync($"{userId}", cancellationToken);
        }
        catch (FileNotFoundException)
        {
            repoItem = await wacNomadRepoGroupRepository.CreateAsync($"{userId}", cancellationToken);
        }

        return repoItem;
    }

    public static async Task<IResult> EnsureGuildHostPublisherExists(this IWacNomadRepoGroupRepository wacNomadRepoGroupRepository, ICommandContext commandContext, IInteractionContext interactionContext, IDiscordRestInteractionAPI interactionApi, IDiscordRestGuildAPI guildApi, IDiscordRestUserAPI userApi, IDiscordRestChannelAPI channelApi, DiscordServerHostingSettings serverHostingSettings, ReactionTracker reactionTracker, ServerHostedRegisterInitialUserProperties registrationInitialValues, CancellationToken cancellationToken)
    {
        if (!commandContext.TryGetGuildID(out var guildId))
            return await interactionApi.CreateInteractionFailureResponseAsync(new PublisherNotFoundError("Could not determine guild ID for publisher retrieval"), interactionContext, cancellationToken);

        if (!commandContext.TryGetUserID(out var userId))
            return await interactionApi.CreateInteractionFailureResponseAsync(new UserNotFoundError("Could not determine user's Discord ID"), interactionContext, cancellationToken);

        try
        {
            _ = await wacNomadRepoGroupRepository.GetAsync(guildId.ToString(), cancellationToken);
            return Result.Success;
        }
        catch (FileNotFoundException)
        {
            // Remove existing registration if exists
            var alreadyHeldRegistration = serverHostingSettings.HeldRegistrations.FirstOrDefault(x => x.FromDiscordId == userId.ToString());
            if (alreadyHeldRegistration is not null)
                serverHostingSettings.HeldRegistrations.Remove(alreadyHeldRegistration);

            // Store latest registration preferences
            serverHostingSettings.HeldRegistrations.Add(new HeldRegistration(userId.ToString(), registrationInitialValues, DateTimeOffset.UtcNow));

            // Get guild owner
            var guild = await guildApi.GetGuildAsync(guildId, ct: cancellationToken);

            // Get user who requested registration

            // Open DM with guild owner
            var ownerDm = await userApi.CreateDMAsync(guild.Entity.OwnerID, cancellationToken);
            var sentMessage = await channelApi.CreateMessageAsync(ownerDm.Entity.ID, embeds: new List<Embed>([
                new Embed
                {
                    Title = "Member registration requested",
                    Description = $"""
                    Member <@{userId}> of the "{guild.Entity.Name}" Discord server has requested to register as a community member, but this server has not been set up for hosting community data.
                    ---
                    Before any members can use this server to pair or host their community data, the server itself must be set up as a publisher and configured to allow server-hosting of wacsdk data.
                    To set up this server for hosting, please run the `/register` command with the server's name as the `name` parameter:
                    ```
                    /register [name | {guild.Entity.Name}]
                    ```
                    - The data retention period parameter will be ignored during server-hosted SDK setup.
                    - You **will not** be able to unregister or request deletion of the publisher which represents this Discord server without running our CLI locally.
                    """,
                },
                new Embed
                {
                    Title = "Warning!",
                    Description = $"""
                    - **SERVER-HOSTED SETUPS ARE >>NOT<< THE RECOMMENDED DEFAULT FOR ONBOARDING NEW USERS**
                    - DO NOT PROCEED WITHOUT UNDERSTANDING AND ACCOMMODATING THE IMPLICATIONS AND RISKS OF HOSTING UN-REVOKABLE ASYMMETRIC PRIVATE KEYS THAT DON'T BELONG TO YOU.

                    - We recommend waiting for the launch of self-host support before setting up this bot in your own server. 
                    - Please refer to documentation for [`WindowsAppCommunity.Sdk`](https://github.com/WindowsAppCommunity/WindowsAppCommunity.Sdk) and [`OwlCore.Nomad.Kubo`](https://github.com/Arlodotexe/OwlCore.Nomad.Kubo/) and/or contact maintainers directly in the [Windows App Community](https://windowsappcommunity.com/discord/) for questions.
                    """,
                    Footer = new EmbedFooter("To silence notices for registration requests, react with 🔕"),
                    Colour = Color.Goldenrod,
                }

            ]), flags: serverHostingSettings.RegistrationRequestsServerOwnerDmMuted ? MessageFlags.SuppressNotifications : default);

            var msgsToListenForReactions = new List<Snowflake> { sentMessage.Entity.ID };

            reactionTracker.MessageReactionAdded += async (s, e) =>
            {
                if (!msgsToListenForReactions.Contains(e.MessageID))
                    return;

                if (e.Emoji.Name.HasValue && e.Emoji.Name.Value == "🔕" && !serverHostingSettings.RegistrationRequestsServerOwnerDmMuted)
                {
                    var silenceMsg = await channelApi.CreateMessageAsync(ownerDm.Entity.ID, "Confirmed, notifications for registration requests will be silent from now on. To un-silence these, react with 🔔");
                    serverHostingSettings.RegistrationRequestsServerOwnerDmMuted = true;
                    msgsToListenForReactions.Add(silenceMsg.Entity.ID);
                }

                if (e.Emoji.Name.HasValue && e.Emoji.Name.Value == "🔔" && serverHostingSettings.RegistrationRequestsServerOwnerDmMuted)
                {
                    var unsilenceMsg = await channelApi.CreateMessageAsync(ownerDm.Entity.ID, "Confirmed, notifications for registration requests will no longer be silent. To silence again, react with 🔕");
                    serverHostingSettings.RegistrationRequestsServerOwnerDmMuted = false;
                    msgsToListenForReactions.Add(unsilenceMsg.Entity.ID);
                }

                await serverHostingSettings.SaveAsync(cancellationToken);
            };

            return await interactionApi.CreateInteractionFailureResponseAsync(new GenericResultError($"""
            Thank you for your interest, but this server has not been set up for hosting community data.

            {(serverHostingSettings.HeldRegistrations.Count == 0 ? "No" : serverHostingSettings.HeldRegistrations.Count)} user{(serverHostingSettings.HeldRegistrations.Count == 1 ? " has" : "s have")} requested to register with this server.
            
            Your request has been held and forwarded to the server owner with setup instructions. Your request will be processed and you'll be notified if/when the server owner completes setup.
            """), interactionContext, cancellationToken);
        }
    }
}