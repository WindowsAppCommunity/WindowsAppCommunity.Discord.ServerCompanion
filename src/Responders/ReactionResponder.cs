using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace WindowsAppCommunity.Discord.ServerCompanion.Responders;

/// <summary>
/// Handles reactions to messages.
/// </summary>
public class ReactionResponder(ReactionTracker Tracker) : IResponder<IMessageReactionAdd>
{
    /// <summary>
    /// Gets the reaction tracker.
    /// </summary>
    public ReactionTracker Tracker { get; } = Tracker;

    /// <summary>
    /// Responds to a message reaction event.
    /// </summary>
    /// <param name="gatewayEvent">The message reaction event.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> RespondAsync(
        IMessageReactionAdd gatewayEvent,
        CancellationToken ct = default)
    {
        Tracker.ProcessReaction(gatewayEvent);
        return Result.FromSuccess();
    }
}