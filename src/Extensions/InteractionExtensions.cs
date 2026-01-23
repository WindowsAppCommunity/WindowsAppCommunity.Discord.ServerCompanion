using System.Drawing;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace WindowsAppCommunity.Discord.ServerCompanion;

public static class InteractionExtensions
{
    public static async Task<Result> CreateInteractionSuccessResponseAsync(this IDiscordRestInteractionAPI interactionApi, string successMessage, IInteractionContext interactionContext, CancellationToken cancellationToken)
    {
        var successEmbed = new Embed
        {
            Description = successMessage,
            Colour = Color.SpringGreen,
        };

        return await interactionApi.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, new(
                new InteractionMessageCallbackData(
                    Flags: MessageFlags.Ephemeral,
                    Embeds: new List<Embed>([successEmbed]))
                )
            ),
            ct: cancellationToken
        );
    }

    public static async Task<Result> CreateInteractionFailureResponseAsync(this IDiscordRestInteractionAPI interactionApi, IResultError error, IInteractionContext interactionContext, CancellationToken cancellationToken)
    {
        return await interactionApi.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, new(
                new InteractionMessageCallbackData(
                    Flags: MessageFlags.Ephemeral,
                    Embeds: new List<Embed>([error.ToEmbed()])
                )
            )),
            ct: cancellationToken
        );
    }

    public static async Task<Result> CreateInteractionFailureResponseAsync(this IDiscordRestInteractionAPI interactionApi, Exception exception, IInteractionContext interactionContext, CancellationToken cancellationToken)
    {
        return await interactionApi.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, new(
                new InteractionMessageCallbackData(
                    Flags: MessageFlags.Ephemeral,
                    Embeds: new List<Embed>([exception.ToMessageEmbed(), exception.ToStackTraceEmbed()])
                )
            )),
            ct: cancellationToken
        );
    }
}