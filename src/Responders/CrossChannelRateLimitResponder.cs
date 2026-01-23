using System.Drawing;
using System.Text.RegularExpressions;
using OwlCore.Storage.System.Net.Http;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace WindowsAppCommunity.Discord.ServerCompanion.Responders;

/// <summary>
/// Responder that detects duplicate messages across channels and applies rate limiting actions.
/// </summary>
public partial class CrossChannelRateLimitResponder : IResponder<IMessageCreate>
{
    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly IDiscordRestGuildAPI _guildAPI;
    private readonly RateLimitSettings _settings;
    private readonly ServerCompanionConfig _config;

    [GeneratedRegex(@"https?://[^\s]+\.(?:png|jpg|jpeg|gif|webp)", RegexOptions.IgnoreCase)]
    private static partial Regex ImageUrlPattern();

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossChannelRateLimitResponder"/> class.
    /// </summary>
    public CrossChannelRateLimitResponder(
        IDiscordRestChannelAPI channelAPI,
        IDiscordRestGuildAPI guildAPI,
        RateLimitSettings settings,
        ServerCompanionConfig config)
    {
        _channelAPI = channelAPI;
        _guildAPI = guildAPI;
        _settings = settings;
        _config = config;
    }

    /// <inheritdoc />
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        try
        {
            // Skip if not in a guild
            if (!gatewayEvent.GuildID.HasValue)
                return Result.FromSuccess();

            var guildId = gatewayEvent.GuildID.Value;
            var channelId = gatewayEvent.ChannelID;
            var messageId = gatewayEvent.ID;
            var userId = gatewayEvent.Author.ID;

            // Check channel exemption
            if (_settings.ExemptChannelIds.Contains(channelId.Value))
                return Result.FromSuccess();

            // Check user exemption
            if (_settings.ExemptUserIds.Contains(userId.Value))
                return Result.FromSuccess();

            // Check role exemption (if member data available)
            if (gatewayEvent.Member.HasValue)
            {
                var memberRoles = gatewayEvent.Member.Value.Roles;
                if (memberRoles.HasValue && memberRoles.Value.Any(roleId => _settings.ExemptRoleIds.Contains(roleId.Value)))
                    return Result.FromSuccess();
            }

            // Cleanup expired buckets
            _settings.CleanupExpiredBuckets();

            // Collect content hashes
            var hashes = new List<string>();

            // Hash text content
            if (!string.IsNullOrWhiteSpace(gatewayEvent.Content))
            {
                var textHash = gatewayEvent.Content.ComputeTextHash();
                hashes.Add(textHash);
            }

            // Hash direct image attachments
            foreach (var attachment in gatewayEvent.Attachments)
            {
                if (attachment.ContentType.HasValue && attachment.ContentType.Value.StartsWith("image/"))
                {
                    var imageHash = await ComputeImageHashFromUrlAsync(attachment.Url, ct);
                    if (imageHash != null)
                        hashes.Add(imageHash);
                }
            }

            // Hash images from URLs in message content
            if (!string.IsNullOrWhiteSpace(gatewayEvent.Content))
            {
                var imageUrls = ImageUrlPattern().Matches(gatewayEvent.Content);
                foreach (Match match in imageUrls)
                {
                    var imageHash = await ComputeImageHashFromUrlAsync(match.Value, ct);
                    if (imageHash != null)
                        hashes.Add(imageHash);
                }
            }

            // Track all hashes
            var buckets = _settings.MessageBuckets;
            var now = DateTimeOffset.UtcNow;
            var metadata = new MessageMetadata(channelId, messageId, userId, now);

            foreach (var hash in hashes)
            {
                if (!buckets.TryGetValue(hash, out var bucket))
                {
                    bucket = new MessageBucket(new List<MessageMetadata> { metadata }, now);
                    buckets[hash] = bucket;
                }
                else
                {
                    // Debounce: reset TTL on duplicate add
                    bucket.Messages.Add(metadata);
                    buckets[hash] = bucket with { LastUpdated = now };
                }

                // Check threshold
                if (bucket.Messages.Count > _settings.DuplicateThreshold)
                {
                    await HandleThresholdExceededAsync(hash, bucket, guildId, ct);
                }
            }

            _settings.MessageBuckets = buckets;
            await _settings.SaveAsync(ct);

            return Result.FromSuccess();
        }
        catch (Exception ex)
        {
            // Log error but don't fail gateway processing
            Console.WriteLine($"[CrossChannelRateLimitResponder] Error: {ex.Message}");
            return Result.FromSuccess();
        }
    }

    private async Task<string?> ComputeImageHashFromUrlAsync(string url, CancellationToken ct)
    {
        try
        {
            var httpFile = new HttpFile(new Uri(url));
            using var stream = await httpFile.OpenStreamAsync(cancellationToken: ct);
            return stream.ComputePerceptualHash();
        }
        catch
        {
            return null;
        }
    }

    private async Task HandleThresholdExceededAsync(string hash, MessageBucket bucket, Snowflake guildId, CancellationToken ct)
    {
        try
        {
            // Action 1: Mute user
            var userId = bucket.Messages.First().UserId;
            var muteDuration = TimeSpan.FromMinutes(_settings.MuteDurationMinutes);
            var muteUntil = DateTimeOffset.UtcNow + muteDuration;

            await _guildAPI.ModifyGuildMemberAsync(
                guildId,
                userId,
                communicationDisabledUntil: new Optional<DateTimeOffset?>(muteUntil),
                ct: ct);

            // Action 2: Delete messages
            var messagesByChannel = bucket.Messages.GroupBy(m => m.ChannelId);
            foreach (var group in messagesByChannel)
            {
                var messageIds = group.Select(m => m.MessageId).ToList();
                
                // Bulk delete if possible (max 100 messages, must be <14 days old)
                if (messageIds.Count > 1 && messageIds.Count <= 100)
                {
                    await _channelAPI.BulkDeleteMessagesAsync(group.Key, messageIds, ct: ct);
                }
                else
                {
                    // Individual deletion fallback
                    foreach (var messageId in messageIds)
                    {
                        await _channelAPI.DeleteMessageAsync(group.Key, messageId, ct: ct);
                    }
                }
            }

            // Action 3: Notify staff
            var channelsMentioned = string.Join(", ", bucket.Messages.Select(m => $"<#{m.ChannelId}>").Distinct());
            var embed = new Embed(
                Title: "⚠️ Cross-Channel Spam Detected",
                Description: $"User <@{userId}> exceeded duplicate message threshold.\n\n" +
                            $"**Violation Count:** {bucket.Messages.Count} duplicates\n" +
                            $"**Channels Affected:** {channelsMentioned}\n" +
                            $"**Actions Taken:**\n" +
                            $"- User muted for {_settings.MuteDurationMinutes} minutes\n" +
                            $"- {bucket.Messages.Count} duplicate messages deleted",
                Colour: Color.Red,
                Timestamp: DateTimeOffset.UtcNow);

            foreach (var notificationChannelId in _settings.NotificationChannelIds)
            {
                await _channelAPI.CreateMessageAsync(
                    new Snowflake(notificationChannelId),
                    embeds: new[] { embed },
                    ct: ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CrossChannelRateLimitResponder] Error handling threshold exceeded: {ex.Message}");
        }
    }
}
