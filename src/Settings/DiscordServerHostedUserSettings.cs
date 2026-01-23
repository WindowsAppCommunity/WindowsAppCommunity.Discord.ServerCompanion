using OwlCore.ComponentModel;
using OwlCore.Storage;

using WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// Manages the settings for a user who has registered and onboarded with a server-hosted node.
/// </summary>
public class DiscordServerHostedUserSettings(IModifiableFolder folder) : SettingsBase(folder, new SystemTextSettingsSerializer(DiscordServerHostedUserSettingsSerializerContext.Default))
{
    /// <summary>
    /// Gets a value indicating whether registration has occurred.
    /// </summary>
    public bool NeverRegistered => RegisteredAt.Count == 0;

    /// <summary>
    /// Gets a value indicating whether user has a registration that hasn't been unregistered yet.  
    /// </summary>
    public bool RegistrationActive => !NeverRegistered && RegisteredAt.Max() > (UnregisteredAt.Any() ? UnregisteredAt.Max() : DateTimeOffset.MinValue);

    /// <summary>
    /// The Discord ID representing the user whose preferences are being stored.
    /// </summary>
    public string DiscordId
    {
        get => GetSetting(() => string.Empty);
        set => SetSetting(value);
    }

    /// <summary>
    /// Whether or not the user has accepted the privacy policy for offered services.
    /// </summary>
    public bool AcceptedPrivacyPolicy
    {
        get => GetSetting(() => false);
        set => SetSetting(value);
    }

    /// <summary>
    /// Whether or not the user has accepted the terms of service for offered services.
    /// </summary>
    public bool AcceptedTermsOfService
    {
        get => GetSetting(() => false);
        set => SetSetting(value);
    }

    /// <summary>
    /// The amount of time that the user would like their data to be retained by the server after unregistering or leaving. 
    /// </summary>
    /// <remarks>
    /// The default of 30 days acts as a grace period for users who are linked to you to clean up their published data,
    /// which prevents client slowing down from attempting to resolve an unpublished ipns address.
    /// </remarks>
    public TimeSpan DataRetentionPeriod
    {
        get => GetSetting(() => TimeSpan.FromDays(30));
        set => SetSetting(value);
    }

    /// <summary>
    /// The DateTime offsets when the user has completed registration or re-registration .
    /// </summary>
    public List<DateTimeOffset> RegisteredAt
    {
        get => GetSetting(() => new List<DateTimeOffset>());
        set => SetSetting(value);
    }

    /// <summary>
    /// The Datetime offsets when the user has unregistered or left the server.
    /// </summary>
    public List<DateTimeOffset> UnregisteredAt
    {
        get => GetSetting(() => new List<DateTimeOffset>());
        set => SetSetting(value);
    }

    /// <summary>
    /// The DateTime offsets when the user has left the server.
    /// </summary>
    /// <remarks>
    /// The user may choose to manually unregister before leaving.
    /// See also <seealso cref="UnregisteredAt"/>. 
    /// </remarks>
    public List<DateTimeOffset> LeftServerAt
    {
        get => GetSetting(() => new List<DateTimeOffset>());
        set => SetSetting(value);
    }

    /// <summary>
    /// Pending registration data.
    /// </summary>
    public ServerHostedRegisterInitialUserProperties PendingRegistrationData
    {
        get => GetSetting(() => new ServerHostedRegisterInitialUserProperties());
        set => SetSetting(value);
    }
}