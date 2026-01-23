using OwlCore.Storage;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// A repository which manages the lifecycle of <see cref="DiscordServerHostedUserSettings"/> multiple instances.
/// </summary>
/// <param name="folder"></param>
public class DiscordServerHostedUserSettingsRepository(IModifiableFolder folder) : StorageHostedRepository<DiscordServerHostedUserSettings>(folder), IDiscordServerHostedUserSettingsRepository
{
    /// <inheritdoc/>
    public override async Task<DiscordServerHostedUserSettings> FolderToItem(IModifiableFolder folder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var settings = new DiscordServerHostedUserSettings(folder);
        await settings.LoadAsync(cancellationToken);

        return settings;
    }

    /// <inheritdoc/>
    public override IModifiableFolder ItemToFolder(DiscordServerHostedUserSettings item) => item.Folder;
}
