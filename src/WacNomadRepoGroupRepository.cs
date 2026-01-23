using Ipfs.CoreApi;
using OwlCore.Nomad.Kubo;
using OwlCore.Storage;
using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// A repository which manages the lifecycle of Nomad entity repositories.
/// </summary>
public class WacNomadRepoGroupRepository(IModifiableFolder folder) : StorageHostedRepository<WacNomadRepoGroupRepositoryItem>(folder), IWacNomadRepoGroupRepository
{
    /// <summary>
    /// The client used to access ipfs. 
    /// </summary>
    public required ICoreApi Client { get; init; }

    /// <summary>
    /// Options for storing, retrieving and publish ipfs data.
    /// </summary>
    public required IKuboOptions KuboOptions { get; init; }

    /// <inheritdoc/>
    public override IModifiableFolder ItemToFolder(WacNomadRepoGroupRepositoryItem item) => item.Settings.Folder;

    /// <inheritdoc/>
    public override async Task<WacNomadRepoGroupRepositoryItem> FolderToItem(IModifiableFolder folder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var repoSettings = new WacsdkNomadSettings(folder);
        await repoSettings.LoadAsync(cancellationToken);

        var repositoryContainer = new WacEntityRepositoryGroup(KuboOptions, Client,
            repoSettings.ManagedKeys,
            repoSettings.ManagedUserConfigs,
            repoSettings.ManagedProjectConfigs,
            repoSettings.ManagedPublisherConfigs
        );

        var item = new WacNomadRepoGroupRepositoryItem()
        {
            RepositoryGroup = repositoryContainer,
            Settings = repoSettings,
        };

        return item;
    }
}
