using OwlCore.Nomad.Kubo;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// Handles lifecycle and storage of multiple <see cref="DiscordServerHostedUserSettings"/> .
/// </summary>
public interface IDiscordServerHostedUserSettingsRepository : INomadKuboRepository<DiscordServerHostedUserSettings, DiscordServerHostedUserSettings, string>
{
}
