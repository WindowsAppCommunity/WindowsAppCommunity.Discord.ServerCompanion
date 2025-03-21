﻿using System.ComponentModel;
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
    [Command("portal")]
    public async Task<IResult> PortalAsync(string destinationChannelName)
    {
        try
        {
            if (!context.TryGetChannelID(out var sourceChannelId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the source channel.");

            var sourceChannelResult = await channelApi.GetChannelAsync(sourceChannelId);
            if (!sourceChannelResult.IsSuccess)
                return await feedbackService.SendContextualErrorAsync(sourceChannelResult.Error.Message);

            if (sourceChannelResult.Entity.GuildID == default)
                return await feedbackService.SendContextualErrorAsync("This command can only be used inside a server.");

            var guildId = sourceChannelResult.Entity.GuildID.Value;

            var channelsResult = await guildApi.GetGuildChannelsAsync(guildId);
            if (!channelsResult.IsSuccess)
                return await feedbackService.SendContextualErrorAsync(channelsResult.Error.Message);

            var destinationChannel = channelsResult.Entity
                .FirstOrDefault(c => string.Equals(c.Name.Value, destinationChannelName, StringComparison.OrdinalIgnoreCase));

            if (destinationChannel == null)
                return await feedbackService.SendContextualErrorAsync($"Channel '{destinationChannelName}' not found.");

            var destinationChannelId = destinationChannel.ID;

            if (sourceChannelId == destinationChannelId)
                return await feedbackService.SendContextualErrorAsync("You're already in that channel!");

            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            var userPermissionSet = await guildApi.GetGuildMemberAsync(guildId, userId);
            if (!userPermissionSet.IsSuccess)
                return await feedbackService.SendContextualErrorAsync(userPermissionSet.Error.Message);

            var userpermissions = userPermissionSet.Entity.Roles
                .Select(roleId => guildApi.GetGuildRolesAsync(guildId, CancellationToken.None).Result.Entity.First(role => role.ID == roleId).Permissions);

            DiscordPermission permissions = default;

            foreach (var set in userpermissions)
            {
                if (set.HasPermission(DiscordPermission.ViewChannel))
                {
                    if (set.HasPermission(DiscordPermission.SendMessages))
                    {
                        permissions = DiscordPermission.SendMessages;
                        break;
                    }
                }
            }

            if (permissions == default)
                return await feedbackService.SendContextualErrorAsync("You aren't allowed to open a portal there.");

            var sourceChannelMention = $"<#{sourceChannelId}>";
            var destinationChannelMention = $"<#{destinationChannelId}>";
            var userIdMention = $"<@{userId}>";

            var sourceMessage = await channelApi.CreateMessageAsync(
                sourceChannelId,
                $"Portal creation initiated to {destinationChannelMention}"
            );

            if (!sourceMessage.IsSuccess)
                return await feedbackService.SendContextualErrorAsync(sourceMessage.Error.Message);

            var sourceMessageLink = $"https://discord.com/channels/{guildId}/{sourceChannelId}/{sourceMessage.Entity.ID}";

            var destinationMessage = await channelApi.CreateMessageAsync(
                destinationChannelId,
                string.Empty,
                embeds: new[] { new Embed(
                Description: $"{userIdMention} opened the portal from {sourceChannelMention}!\n[Go back through the portal]({sourceMessageLink})",
                Colour: new Optional<Color>(Color.Teal),
                Thumbnail: new Optional<IEmbedThumbnail>(new EmbedThumbnail(Url: "https://cdn.discordapp.com/attachments/642818541426573344/960339465891631186/b.png")))}
            );

            var destinationMessageLink = $"https://discord.com/channels/{guildId}/{destinationChannelId}/{destinationMessage.Entity.ID}";

            var inPortalEmbed = new Embed(
                    Description: $"A portal to {destinationChannelMention} was opened!\n[Enter the portal]({destinationMessageLink})",
                    Colour: new Optional<Color>(Color.Gold),
                    Thumbnail: new Optional<IEmbedThumbnail>(new EmbedThumbnail(Url: "https://cdn.discordapp.com/attachments/642818541426573344/960339466185224212/o.png"))
                );

            sourceMessage = await channelApi.EditMessageAsync(
                sourceChannelId,
                sourceMessage.Entity.ID,
                string.Empty,
                embeds: new[] { inPortalEmbed }
            );

            if (!destinationMessage.IsSuccess)
                return await feedbackService.SendContextualErrorAsync(destinationMessage.Error.Message);

            return Result.FromSuccess();
        }
        catch (Exception ex)
        {
            return await feedbackService.SendContextualErrorAsync(ex.Message);
        }
    }
}
