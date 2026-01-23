using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Rest.Core;
using System.Collections.Concurrent;

namespace WindowsAppCommunity.Discord.ServerCompanion.Responders;

/// <summary>
/// Tracks pending reaction waiters keyed by the message ID.
/// </summary>
public class ReactionTracker
{
    // Stores pending reaction waiters keyed by the message ID.
    private readonly ConcurrentDictionary<Snowflake, TaskCompletionSource<IMessageReactionAdd>> _pendingReactions = new();

    /// <summary>
    /// Waits asynchronously for a reaction to be added to the specified message.
    /// </summary>
    /// <param name="messageId">The ID of the message to monitor.</param>
    /// <param name="timeout">Maximum time to wait for a reaction.</param>
    /// <returns>
    /// The <see cref="IMessageReactionAdd"/> event if a reaction is received within the timeout;
    /// otherwise <c>null</c>.
    /// </returns>
    public async Task<IMessageReactionAdd?> WaitForReactionAsync(Snowflake messageId, TimeSpan timeout)
    {
        // Create a task that will complete when a reaction is received.
        var tcs = new TaskCompletionSource<IMessageReactionAdd>();
        // Register the waiter for this message ID.
        _pendingReactions[messageId] = tcs;

        // Await either the reaction or the timeout.
        var completedTask = await Task.WhenAny(
            tcs.Task,
            Task.Delay(timeout)
        );

        // Clean up the pending entry.
        _pendingReactions.TryRemove(messageId, out _);

        // Return the reaction if it arrived before the timeout; otherwise null.
        return completedTask == tcs.Task ? tcs.Task.Result : null;
    }

    public event EventHandler<IMessageReactionAdd>? MessageReactionAdded;

    /// <summary>
    /// Called when a reaction is added to a message. If a waiter is pending for that message,
    /// completes the corresponding task.
    /// </summary>
    /// <param name="reaction">The reaction event that was received.</param>
    public void ProcessReaction(IMessageReactionAdd reaction)
    {
        // If a waiter is registered for this message, complete it with the reaction.
        if (_pendingReactions.TryRemove(reaction.MessageID, out var tcs))
        {
            tcs.SetResult(reaction);
        }

        MessageReactionAdded?.Invoke(this, reaction);
    }
}
