using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Storage;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Keystore;

public class UserKeystore(IModifiableFolder folder) : SettingsBase(folder, SettingsSerializer.Singleton)
{
    public List<ManagedUserMap> ManagedUsers
    {
        get => GetSetting(() => new List<ManagedUserMap>());
        set => SetSetting(value);
    }
}

/// <summary>
/// Represents a single user managed by the community.
/// </summary>
/// <param name="User">The resolved user data.</param>
/// <param name="IpnsCid">A CID that can be used to resolve the user data</param>
public record ManagedUserMap(User User, Cid IpnsCid)
{
    public User User { get; set; } = User;
}