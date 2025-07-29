using OwlCore.ComponentModel;
using OwlCore.Kubo;
using OwlCore.Nomad.Kubo;
using OwlCore.Storage;
using WindowsAppCommunity.Discord.ServerCompanion.Services;

public class WacsdkNomadSettings : SettingsBase
{
    public WacsdkNomadSettings(IModifiableFolder folder)
        : base(folder, NewtonsoftSerializer.Singleton)
    {
    }

    public List<Key> ManagedKeys
    {
        get => GetSetting<List<Key>>(() => []);
        set => SetSetting(value);
    }

    public List<NomadKuboEventStreamHandlerConfig<WindowsAppCommunity.Sdk.Models.User>> ManagedUserConfigs
    {
        get => GetSetting<List<NomadKuboEventStreamHandlerConfig<WindowsAppCommunity.Sdk.Models.User>>>(() => []);
        set => SetSetting(value);
    }

    public List<NomadKuboEventStreamHandlerConfig<WindowsAppCommunity.Sdk.Models.Project>> ManagedProjectConfigs
    {
        get => GetSetting<List<NomadKuboEventStreamHandlerConfig<WindowsAppCommunity.Sdk.Models.Project>>>(() => []);
        set => SetSetting(value);
    }

    public List<NomadKuboEventStreamHandlerConfig<WindowsAppCommunity.Sdk.Models.Publisher>> ManagedPublisherConfigs
    {
        get => GetSetting<List<NomadKuboEventStreamHandlerConfig<WindowsAppCommunity.Sdk.Models.Publisher>>>(() => []);
        set => SetSetting(value);
    }

    public override async Task LoadAsync(CancellationToken? cancellationToken = null)
    {
        cancellationToken?.ThrowIfCancellationRequested();

        await base.LoadAsync(cancellationToken);

        // Roaming value is used as the initial value for the event stream handlers.
        // Clear it out to ensure we start event playback from a clean slate.
        foreach (var config in ManagedUserConfigs)
        {
            config.RoamingValue = new WindowsAppCommunity.Sdk.Models.User
            {
                Sources = [],
            };

            config.LocalValue = null;
            config.ResolvedEventStreamEntries = null;
        }

        foreach (var config in ManagedProjectConfigs)
        {
            config.RoamingValue = new WindowsAppCommunity.Sdk.Models.Project
            {
                Sources = [],
            };
            config.LocalValue = null;
            config.ResolvedEventStreamEntries = null;
        }

        foreach (var config in ManagedPublisherConfigs)
        {
            config.RoamingValue = new WindowsAppCommunity.Sdk.Models.Publisher
            {
                Sources = [],
            };
            config.LocalValue = null;
            config.ResolvedEventStreamEntries = null;
        }
    }
}
