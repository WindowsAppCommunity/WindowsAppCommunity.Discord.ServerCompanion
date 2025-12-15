using System.Text.Json.Serialization;
using WindowsAppCommunity.Discord.ServerCompanion.Settings;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// JSON serialization context for rate limiter settings.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, MessageBucket>))]
[JsonSerializable(typeof(HashSet<ulong>))]
[JsonSerializable(typeof(MessageBucket))]
[JsonSerializable(typeof(MessageMetadata))]
[JsonSerializable(typeof(List<MessageMetadata>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(DateTimeOffset))]
public partial class RateLimitSerializerContext : JsonSerializerContext
{
}
