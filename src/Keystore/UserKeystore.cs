using OwlCore.ComponentModel;
using OwlCore.Storage;
using WinAppCommunity.Discord.ServerCompanion.Commands;

namespace WinAppCommunity.Discord.ServerCompanion.Keystore;

public class UserKeystore : SettingsBase
{
    public UserKeystore(IModifiableFolder folder)
        : base(folder, SettingsSerializer.Singleton)
    {
    }

    public List<ManagedUserMap> ManagedUsers
    {
        get => GetSetting(() => new List<ManagedUserMap>());
        set => SetSetting(value);
    }
}