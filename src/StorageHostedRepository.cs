using System.Runtime.CompilerServices;
using OwlCore.Nomad.Kubo;
using OwlCore.Storage;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// An abstract class for managing item lifecycle in a repository backed by storage.
/// </summary>
/// <typeparam name="T">The type of the item being managed by this repository.</typeparam>
public abstract class StorageHostedRepository<T>(IModifiableFolder dataFolder) : INomadKuboRepository<T, T, string>
{
    /// <summary>
    /// Runtime storage of known and created instances. 
    /// </summary>
    protected List<T> RuntimeItems { get; init; } = new();

    /// <summary>
    /// The folder where data for the repos are stored are sub-folders. 
    /// </summary>
    public IModifiableFolder DataFolder { get; init; } = dataFolder;

    /// <inheritdoc/>
    public event EventHandler<T[]>? ItemsAdded;

    /// <inheritdoc/>
    public event EventHandler<T[]>? ItemsRemoved;

    /// <summary>
    /// From an existing repository item, returns the underlying storage folder. 
    /// </summary>
    /// <param name="item">The existing repository item.</param>
    /// <returns>The underlying storage folder.</returns>
    public abstract IModifiableFolder ItemToFolder(T item);

    /// <summary>
    /// Given a modifiable folder to store data, returns a ready-to-use repository item instance.
    /// </summary>
    /// <param name="folder">The underlying storage folder.</param>
    /// <returns>A task representing the asynchronous operation whose value is the repository item instance.</returns>
    public abstract Task<T> FolderToItem(IModifiableFolder folder, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public async Task<T> CreateAsync(string repoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingRuntimeItem = RuntimeItems.FirstOrDefault(x => ItemToFolder(x).Name == repoId);
        if (existingRuntimeItem is not null)
            throw new InvalidOperationException("Repository with the provided ID already exists and cannot be re-created");

        var existingRetrievedItem = await GetAsync(cancellationToken).FirstOrDefaultAsync(x => ItemToFolder(x).Name == repoId, cancellationToken);
        if (existingRetrievedItem is not null)
            throw new InvalidOperationException("Repository with the provided ID already exists and cannot be re-created");

        var itemStorage = (IModifiableFolder)await DataFolder.CreateFolderAsync(repoId, overwrite: false, cancellationToken);
        var item = await FolderToItem(itemStorage, cancellationToken);

        RuntimeItems.Add(item);
        ItemsAdded?.Invoke(this, [item]);
        return item;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(T item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await DataFolder.DeleteAsync((IStorableChild)ItemToFolder(item), cancellationToken);
        ItemsRemoved?.Invoke(this, [item]);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<T> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var item in RuntimeItems)
            yield return item;

        await foreach (IModifiableFolder repoStorage in DataFolder.GetFoldersAsync(cancellationToken).Where(x => RuntimeItems.All(y => ItemToFolder(y).Id != x.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = await FolderToItem(repoStorage, cancellationToken);
            RuntimeItems.Add(item);
            yield return item;
        }
    }

    /// <inheritdoc/>
    public async Task<T> GetAsync(string repoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var existingRuntimeItem = RuntimeItems.FirstOrDefault(x => ItemToFolder(x).Name == repoId);
        if (existingRuntimeItem is not null)
            return existingRuntimeItem;

        var repoStorage = (IModifiableFolder)await DataFolder.GetFirstByNameAsync(repoId, cancellationToken);
        var item = await FolderToItem(repoStorage, cancellationToken);

        RuntimeItems.Add(item);
        return item;
    }
}
