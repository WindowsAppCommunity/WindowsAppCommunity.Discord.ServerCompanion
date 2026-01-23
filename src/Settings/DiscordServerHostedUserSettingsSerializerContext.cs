using System.Text.Json.Serialization;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// JSON serialization context for <see cref="DiscordServerHostedUserSettings"/>.
/// </summary>
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(List<DateTimeOffset>))]
[JsonSerializable(typeof(ServerHostedRegisterInitialUserProperties))]
public partial class DiscordServerHostedUserSettingsSerializerContext : JsonSerializerContext
{
}
