using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Kubo;
using OwlCore.Storage;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Keystore;

public class PublisherKeystore(IModifiableFolder folder) : SettingsBase(folder, SettingsSerializer.Singleton)
{
    public List<ManagedPublisherMap> ManagedPublishers
    {
        get => GetSetting(() => new List<ManagedPublisherMap>());
        set => SetSetting(value);
    }
}

/// <summary>
/// Represents a single publisher managed by the community.
/// </summary>
/// <param name="Publisher">The resolved user data.</param>
/// <param name="IpnsCid">A CID that can be used to resolve the user data</param>
public record ManagedPublisherMap(Publisher Publisher, Cid IpnsCid)
{
    public Publisher Publisher { get; set; } = Publisher;
}