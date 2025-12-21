using System.Runtime.CompilerServices;
using Ipfs.CoreApi;
using OwlCore.Nomad.Kubo;
using OwlCore.Storage;
using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// A repository which manages the lifecycle of Nomad entity repositories.
/// </summary>
public class WacNomadRepoGroupRepository : IWacNomadRepoGroupRepository
{
    private List<WacNomadRepoGroupRepositoryItem> _runtimeItems = new();

    /// <summary>
    /// The folder where data for the entity repos are stored are sub-folders. 
    /// </summary>
    public required IModifiableFolder DataFolder { get; init; }

    /// <summary>
    /// The client used to access ipfs. 
    /// </summary>
    public required ICoreApi Client { get; init; }

    /// <summary>
    /// Options for storing, retrieving and publish ipfs data.
    /// </summary>
    public required IKuboOptions KuboOptions { get; init; }

    /// <inheritdoc/>
    public event EventHandler<WacNomadRepoGroupRepositoryItem[]>? ItemsAdded;

    /// <inheritdoc/>
    public event EventHandler<WacNomadRepoGroupRepositoryItem[]>? ItemsRemoved;

    /// <inheritdoc/>
    public async Task<WacNomadRepoGroupRepositoryItem> CreateAsync(string repoId, CancellationToken cancellationToken)
    {
        var existingRuntimeItem = _runtimeItems.FirstOrDefault(x => x.Settings.Folder.Name == repoId);
        if (existingRuntimeItem is not null)
            throw new InvalidOperationException("Repository with the provided ID already exists and cannot be re-created");

        var existingRetrievedItem = await GetAsync(cancellationToken).FirstOrDefaultAsync(x => x.Settings.Folder.Name == repoId, cancellationToken);
        if (existingRetrievedItem is not null)
            throw new InvalidOperationException("Repository with the provided ID already exists and cannot be re-created");

        var repoStorage = (IModifiableFolder)await DataFolder.CreateFolderAsync(repoId, overwrite: false, cancellationToken);
        var repoSettings = new WacsdkNomadSettings(repoStorage);
        await repoSettings.LoadAsync(cancellationToken);

        var repositoryContainer = new WacEntityRepositoryGroup(KuboOptions, Client,
            repoSettings.ManagedKeys,
            repoSettings.ManagedUserConfigs,
            repoSettings.ManagedProjectConfigs,
            repoSettings.ManagedPublisherConfigs
        );

        var item = new WacNomadRepoGroupRepositoryItem
        {
            RepositoryGroup = repositoryContainer,
            Settings = repoSettings,
        };

        _runtimeItems.Add(item);
        ItemsAdded?.Invoke(this, [item]);
        return item;
    }

    public async Task DeleteAsync(WacNomadRepoGroupRepositoryItem item, CancellationToken cancellationToken)
    {
        await DataFolder.DeleteAsync((IChildFolder)item.Settings.Folder, cancellationToken);
        ItemsRemoved?.Invoke(this, [item]);
    }

    public async IAsyncEnumerable<WacNomadRepoGroupRepositoryItem> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var item in _runtimeItems)
            yield return item;

        await foreach (IModifiableFolder repoStorage in DataFolder.GetFoldersAsync(cancellationToken).Where(x => _runtimeItems.All(y => y.Settings.Folder.Id != x.Id)))
        {
            var repoSettings = new WacsdkNomadSettings(repoStorage);
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

            _runtimeItems.Add(item);
        }
    }

    public async Task<WacNomadRepoGroupRepositoryItem> GetAsync(string repoId, CancellationToken cancellationToken)
    {
        var existingRuntimeItem = _runtimeItems.FirstOrDefault(x => x.Settings.Folder.Name == repoId);
        if (existingRuntimeItem is not null)
            return existingRuntimeItem;

        var repoStorage = (IModifiableFolder)await DataFolder.GetFirstByNameAsync(repoId, cancellationToken);
        var repoSettings = new WacsdkNomadSettings(repoStorage);
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

        _runtimeItems.Add(item);
        return item;
    }
}
