using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Storage;

namespace WinAppCommunity.Discord.ServerCompanion.Keystore;

public class PublisherKeystore : SettingsBase
{
    public PublisherKeystore(IModifiableFolder folder)
        : base(folder, SettingsSerializer.Singleton)
    {
    }

    public List<Cid> ManagedIpfsKeys
    {
        get => GetSetting(() => new List<Cid>());
        set => SetSetting(value);
    }
}