using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Storage;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Keystore;

public class ProjectKeystore(IModifiableFolder folder) : SettingsBase(folder, SettingsSerializer.Singleton)
{
    public List<ManagedProjectMap> ManagedIpfsKeys
    {
        get => GetSetting(() => new List<ManagedProjectMap>());
        set => SetSetting(value);
    }
}

/// <summary>
/// Represents a single project managed by the community.
/// </summary>
/// <param name="Project">The resolved project data.</param>
/// <param name="IpnsCid">A CID that can be used to resolve the project data</param>
public record ManagedProjectMap(Project Project, Cid IpnsCid)
{
    public Project Project { get; set; } = Project;
}