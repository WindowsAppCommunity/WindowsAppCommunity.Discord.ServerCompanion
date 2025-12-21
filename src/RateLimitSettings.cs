using OwlCore.ComponentModel;
using OwlCore.Storage;
using Remora.Rest.Core;

namespace WindowsAppCommunity.Discord.ServerCompanion.Settings;

/// <summary>
/// Settings class for cross-channel spam rate limiter configuration and runtime tracking.
/// </summary>
public class RateLimitSettings : SettingsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitSettings"/> class.
    /// </summary>
    /// <param name="folder">The folder to store settings in.</param>
    /// <param name="settingSerializer">The serializer to use for settings.</param>
    public RateLimitSettings(IModifiableFolder folder, IAsyncSerializer<Stream> settingSerializer)
        : base(folder, settingSerializer)
    {
    }

    /// <summary>
    /// Gets or sets the runtime tracking dictionary mapping content hashes to message buckets.
    /// </summary>
    public Dictionary<string, MessageBucket> MessageBuckets
    {
        get => GetSetting(() => new Dictionary<string, MessageBucket>());
        set => SetSetting(value);
    }

    /// <summary>
    /// Gets or sets the number of duplicate messages required to trigger rate limit action.
    /// </summary>
    public int DuplicateThreshold
    {
        get => GetSetting(() => 2);
        set => SetSetting(value);
    }

    /// <summary>
    /// Gets or sets the time window in minutes for tracking duplicates (TTL with debounce).
    /// </summary>
    public int TimeWindowMinutes
    {
        get => GetSetting(() => 30);
        set => SetSetting(value);
    }

    /// <summary>
    /// Gets or sets the duration in minutes to mute users who exceed the threshold.
    /// </summary>
    public int MuteDurationMinutes
    {
        get => GetSetting(() => 60);
        set => SetSetting(value);
    }

    /// <summary>
    /// Gets or sets the set of channel IDs where spam detection is disabled.
    /// </summary>
    public HashSet<ulong> ExemptChannelIds
    {
        get => GetSetting(() => new HashSet<ulong>());
        set => SetSetting(value);
    }

    /// <summary>
    /// Gets or sets the set of role IDs that can crosspost freely.
    /// </summary>
    public HashSet<ulong> ExemptRoleIds
    {
        get => GetSetting(() => new HashSet<ulong>());
        set => SetSetting(value);
    }

    /// <summary>
    /// Gets or sets the set of user IDs that can crosspost freely.
    /// </summary>
    public HashSet<ulong> ExemptUserIds
    {
        get => GetSetting(() => new HashSet<ulong>());
        set => SetSetting(value);
    }

    /// <summary>
    /// Gets or sets the set of channel IDs where staff notifications are sent.
    /// </summary>
    public HashSet<ulong> NotificationChannelIds
    {
        get => GetSetting(() => new HashSet<ulong>());
        set => SetSetting(value);
    }

    /// <summary>
    /// Cleans up expired message buckets based on the current time window.
    /// </summary>
    public void CleanupExpiredBuckets()
    {
        var buckets = MessageBuckets;
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-TimeWindowMinutes);
        
        var expiredHashes = buckets
            .Where(kvp => kvp.Value.LastUpdated < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var hash in expiredHashes)
        {
            buckets.Remove(hash);
        }

        if (expiredHashes.Count > 0)
        {
            MessageBuckets = buckets;
        }
    }
}

/// <summary>
/// Represents a bucket of duplicate messages for a specific content hash.
/// </summary>
public record MessageBucket(List<MessageMetadata> Messages, DateTimeOffset LastUpdated);

/// <summary>
/// Represents metadata for a tracked message.
/// </summary>
public record MessageMetadata(Snowflake ChannelId, Snowflake MessageId, Snowflake UserId, DateTimeOffset Timestamp);
