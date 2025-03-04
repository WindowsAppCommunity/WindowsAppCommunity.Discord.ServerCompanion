using System.ComponentModel;
using System.Drawing;
using System.Security;
using Polly;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;
using NotFoundError = WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors.NotFoundError;

namespace WindowsAppCommunity.Discord.ServerCompanion;



[Group("commands")]
public class PortalCommandGroup(IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ICommandContext context) : Remora.Commands.Groups.CommandGroup
{
    [Command("sample-command")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> TestInteractivity()
    {
        try
        {
            var response = new InteractionResponse
           (
               InteractionCallbackType.Modal,
               new
               (
                   new InteractionModalCallbackData
                   (
                       CustomIDHelpers.CreateModalID("modal"),
                       "Register User",
                       new[]
                       {
                        new ActionRowComponent
                        (
                            new[]
                            {
                                new TextInputComponent
                                (
                                    "project-name",
                                    TextInputStyle.Short,
                                    "Project Name",
                                    1,
                                    32,
                                    true,
                                    default,
                                    "Project name here"
                                ),
                            }
                        ),
                        new ActionRowComponent(
                            new []{

                                 new TextInputComponent
                                (
                                    "project-description",
                                    TextInputStyle.Paragraph,
                                    "Project Description",
                                    1,
                                    2000,
                                    true,
                                    default,
                                    "Project description here"
                                ),
                            }
                        ),
                        new ActionRowComponent(
                            new[]
                                {
                              new TextInputComponent
                                (
                                    "project-description2",
                                    TextInputStyle.Paragraph,
                                    "Project Description",
                                    1,
                                    2000,
                                    true,
                                    default,
                                    "Project description here"
                                )
                             })
                       }
                   )
               )
           );

            var result = await interactionAPI.CreateInteractionResponseAsync
            (
                interactionContext.Interaction.ID,
                interactionContext.Interaction.Token,
                response,
                ct: this.CancellationToken
            );

            if (!result.IsSuccess)
            {
                await feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{result.Error}");
                return result;
            }

            return result;
        }
        catch (Exception ex)
        {
            await feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }


    [Command("portal")]
    public async Task<IResult> PortalAsync(string destinationChannelName)
    {
        if (!context.TryGetChannelID(out var sourceChannelId))
            return Result.FromError(new InvalidOperationError("Could not determine the source channel."));

        var sourceChannelResult = await channelApi.GetChannelAsync(sourceChannelId);
        if (!sourceChannelResult.IsSuccess)
            return Result.FromError(sourceChannelResult.Error);

        if (sourceChannelResult.Entity.GuildID == null)
            return Result.FromError(new NotFoundError("This command can only be used inside a server."));

        var guildId = sourceChannelResult.Entity.GuildID.Value;

        var channelsResult = await guildApi.GetGuildChannelsAsync(guildId);
        if (!channelsResult.IsSuccess)
            return Result.FromError(channelsResult.Error);

        var destinationChannel = channelsResult.Entity
            .FirstOrDefault(c => string.Equals(c.Name.Value, destinationChannelName, StringComparison.OrdinalIgnoreCase));

        if (destinationChannel == null)
            return Result.FromError(new NotFoundError($"Channel '{destinationChannelName}' not found."));

        var destinationChannelId = destinationChannel.ID;

        if (sourceChannelId == destinationChannelId)
            return Result.FromError(new InvalidOperationError("You're already in that channel!"));

        if (!context.TryGetUserID(out var userId))
            return Result.FromError(new InvalidOperationError("Could not determine the user ID."));

        var userPermissionSet = await guildApi.GetGuildMemberAsync(guildId, userId);
        if (!userPermissionSet.IsSuccess)
            return Result.FromError(userPermissionSet.Error);

        var botPermissions = userPermissionSet.Entity.Roles
            .Select(roleId => guildApi.GetGuildRolesAsync(guildId, CancellationToken.None).Result.Entity.First(role => role.ID == roleId).Permissions);

        DiscordPermission permissions = default;

        foreach (var set in botPermissions)
        {
            if (set.HasPermission(DiscordPermission.SendMessages))
            {
                permissions = DiscordPermission.SendMessages;
                break;
            }
        }

        if (permissions == default)
            return Result.FromError(new InvalidOperationError("Bot does not have permission to send messages in the destination channel."));

        var sourceChannelMention = $"<#{sourceChannelId}>";
        var destinationChannelMention = $"<#{destinationChannelId}>";

        var outPortalEmbed = new Embed(
            Title: "🌌 Portal Created! 🌌",
            Description: $"Click below to travel to {destinationChannelMention}",
            Colour: new Optional<Color>(Color.Teal),
            Thumbnail: new Optional<IEmbedThumbnail>(new EmbedThumbnail(Url: "https://cdn.discordapp.com/attachments/642818541426573344/960339465891631186/b.png"))
        );

        var sourceMessage = await channelApi.CreateMessageAsync(
            sourceChannelId,
            embeds: new[] { outPortalEmbed }
        );

        if (!sourceMessage.IsSuccess)
            return Result.FromError(sourceMessage.Error);

        var inPortalEmbed = new Embed(
            Title: "🌌 Return Portal Created! 🌌",
            Description: $"Click below to return to {sourceChannelMention}",
            Colour: new Optional<Color>(Color.Gold),
            Thumbnail: new Optional<IEmbedThumbnail>(new EmbedThumbnail(Url: "https://cdn.discordapp.com/attachments/642818541426573344/960339466185224212/o.png"))
        );

        var destinationMessage = await channelApi.CreateMessageAsync(
            destinationChannelId,
            embeds: new[] { inPortalEmbed }
        );

        return destinationMessage.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(destinationMessage.Error);
    }
}
