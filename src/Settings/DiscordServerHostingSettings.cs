using OwlCore.ComponentModel;
using OwlCore.Storage;

using WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// Manages the settings for the entire server-hosted node.
/// </summary>
public class DiscordServerHostingSettings(IModifiableFolder folder) : SettingsBase(folder, new SystemTextSettingsSerializer(DiscordServerHostingSettingsSerializerContext.Default))
{
    /// <summary>
    /// Registration requests that have been held due to missing server configurations.
    /// </summary>
    public List<HeldRegistration> HeldRegistrations
    {
        get => GetSetting(() => new List<HeldRegistration>());
        set => SetSetting(value);
    }

    /// <summary>
    /// Gets a value that indicates that the server owner has muted notifications regarding held registration requests.
    /// </summary>
    public bool RegistrationRequestsServerOwnerDmMuted
    {
        get => GetSetting(() => false);
        set => SetSetting(value);
    }
}

/// <summary>
/// A record representing a user who requested registration but was unable to proceed due to the server owner not setting up hosting.
/// </summary>
public record HeldRegistration(string FromDiscordId, ServerHostedRegisterInitialUserProperties InitialUserProperties, DateTimeOffset RequestedAt);