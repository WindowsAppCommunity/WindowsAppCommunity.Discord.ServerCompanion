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

public class PortalCommandGroup(IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ICommandContext context) : Remora.Commands.Groups.CommandGroup
{
    [Command("portal")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> PortalAsync(IChannel destChannel)
    {
        try
        {
            if (!context.TryGetChannelID(out var sourceChannelId))
            {
                await interactionAPI.CreateInteractionResponseAsync(
                    interactionContext.Interaction.ID,
                    interactionContext.Interaction.Token,
                    new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
                        new(new InteractionMessageCallbackData(Content: "Could not determine the source channel.", Flags: MessageFlags.Ephemeral))),
                    ct: this.CancellationToken);
                return Result.FromSuccess();
            }

            var sourceChannelResult = await channelApi.GetChannelAsync(sourceChannelId);
            if (!sourceChannelResult.IsSuccess)
            {
                await interactionAPI.CreateInteractionResponseAsync(
                    interactionContext.Interaction.ID,
                    interactionContext.Interaction.Token,
                    new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
                        new(new InteractionMessageCallbackData(Content: sourceChannelResult.Error.Message, Flags: MessageFlags.Ephemeral))),
                    ct: this.CancellationToken);
                return Result.FromSuccess();
            }

            if (sourceChannelResult.Entity.GuildID == default)
            {
                await interactionAPI.CreateInteractionResponseAsync(
                    interactionContext.Interaction.ID,
                    interactionContext.Interaction.Token,
                    new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
                        new(new InteractionMessageCallbackData(Content: "This command can only be used inside a server.", Flags: MessageFlags.Ephemeral))),
                    ct: this.CancellationToken);
                return Result.FromSuccess();
            }

            var guildId = sourceChannelResult.Entity.GuildID.Value;

            var destinationChannelId = destChannel.ID;

            if (sourceChannelId == destinationChannelId)
            {
                await interactionAPI.CreateInteractionResponseAsync(
                    interactionContext.Interaction.ID,
                    interactionContext.Interaction.Token,
                    new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
                        new(new InteractionMessageCallbackData(Content: "You're already in that channel!", Flags: MessageFlags.Ephemeral))),
                    ct: this.CancellationToken);
                return Result.FromSuccess();
            }

            if (!context.TryGetUserID(out var userId))
            {
                await interactionAPI.CreateInteractionResponseAsync(
                    interactionContext.Interaction.ID,
                    interactionContext.Interaction.Token,
                    new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
                        new(new InteractionMessageCallbackData(Content: "Could not determine the user ID.", Flags: MessageFlags.Ephemeral))),
                    ct: this.CancellationToken);
                return Result.FromSuccess();
            }

            var sourceChannelMention = $"<#{sourceChannelId}>";
            var destinationChannelMention = $"<#{destinationChannelId}>";
            var userIdMention = $"<@{userId}>";

            // Create destination portal message first
            var destinationMessage = await channelApi.CreateMessageAsync(
                destinationChannelId,
                string.Empty,
                embeds: new[] { new Embed(
                Description: $"{userIdMention} opened the portal from {sourceChannelMention}!\n[Go back through the portal]()",
                Colour: new Optional<Color>(Color.Teal),
                Thumbnail: new Optional<IEmbedThumbnail>(new EmbedThumbnail(Url: "https://cdn.discordapp.com/attachments/642818541426573344/960339465891631186/b.png")))}
            );

            if (!destinationMessage.IsSuccess)
            {
                await interactionAPI.CreateInteractionResponseAsync(
                    interactionContext.Interaction.ID,
                    interactionContext.Interaction.Token,
                    new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
                        new(new InteractionMessageCallbackData(Content: destinationMessage.Error.Message, Flags: MessageFlags.Ephemeral))),
                    ct: this.CancellationToken);
                return Result.FromSuccess();
            }

            var destinationMessageLink = $"https://discord.com/channels/{guildId}/{destinationChannelId}/{destinationMessage.Entity.ID}";

            // Create source portal embed as interaction response
            var sourcePortalEmbed = new Embed(
                Description: $"A portal to {destinationChannelMention} was opened!\n[Enter the portal]({destinationMessageLink})",
                Colour: new Optional<Color>(Color.Gold),
                Thumbnail: new Optional<IEmbedThumbnail>(new EmbedThumbnail(Url: "https://cdn.discordapp.com/attachments/642818541426573344/960339466185224212/o.png"))
            );

            // Update the interaction response with the actual embed
            var interactionResponse = await interactionAPI.CreateInteractionResponseAsync(
                interactionContext.Interaction.ID,
                interactionContext.Interaction.Token,
                new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, new(new InteractionMessageCallbackData(Embeds: new[] { sourcePortalEmbed }))),
                ct: this.CancellationToken
            );

            if (!interactionResponse.IsSuccess)
            {
                // Clean up destination message if interaction response fails
                await channelApi.DeleteMessageAsync(destinationChannelId, destinationMessage.Entity.ID);
                return (Result)interactionResponse;
            }

            // Get the interaction response message to create back link
            var originalResponse = await interactionAPI.GetOriginalInteractionResponseAsync(
                interactionContext.Interaction.ApplicationID,
                interactionContext.Interaction.Token,
                ct: this.CancellationToken
            );

            if (!originalResponse.IsSuccess)
                return Result.FromSuccess(); // Portal created but couldn't update back link

            var sourceMessageLink = $"https://discord.com/channels/{guildId}/{sourceChannelId}/{originalResponse.Entity.ID}";

            // Update destination message with correct back link
            await channelApi.EditMessageAsync(
                destinationChannelId,
                destinationMessage.Entity.ID,
                string.Empty,
                embeds: new[] { new Embed(
                    Description: $"{userIdMention} opened the portal from {sourceChannelMention}!\n[Go back through the portal]({sourceMessageLink})",
                    Colour: new Optional<Color>(Color.Teal),
                    Thumbnail: new Optional<IEmbedThumbnail>(new EmbedThumbnail(Url: "https://cdn.discordapp.com/attachments/642818541426573344/960339465891631186/b.png")))
                }
            );

            return Result.FromSuccess();
        }
        catch (Exception ex)
        {
            // Try to send error response if interaction not yet acknowledged
            try
            {
                await interactionAPI.CreateInteractionResponseAsync(
                    interactionContext.Interaction.ID,
                    interactionContext.Interaction.Token,
                    new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
                        new(new InteractionMessageCallbackData(Content: $"An error occurred: {ex.Message}", Flags: MessageFlags.Ephemeral))),
                    ct: this.CancellationToken);
            }
            catch
            {
                // Interaction already acknowledged, can't send error
            }
            return Result.FromSuccess();
        }
    }
}
