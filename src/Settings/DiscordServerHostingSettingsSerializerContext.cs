using System.Text.Json.Serialization;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// JSON serialization context for <see cref="DiscordServerHostingSettings"/>.
/// </summary>
[JsonSerializable(typeof(string))]
public partial class DiscordServerHostingSettingsSerializerContext : JsonSerializerContext
{
}
