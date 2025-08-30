using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OwlCore.Diagnostics;
using OwlCore.Storage;
using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion.Services
{
    public class NomadRepoService : INomadRepoService
    {
        public WacsdkCommandConfig Config { get; }

        public NomadRepoService(WacsdkCommandConfig config)
        {
            Config = config;
        }

        public async Task<(RepositoryContainer, WacsdkCommandConfig, WacsdkNomadSettings)> GetNomadRepoAsync(string repoId, string knownId, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            var thisRepoStorage = (IModifiableFolder)await Config.RepositoryStorage.CreateFolderAsync(repoId, overwrite: false);

            Logger.LogInformation($"Getting repo store with ID {repoId} at {thisRepoStorage.GetType().Name} {thisRepoStorage.Id}");
            var repoSettings = new WacsdkNomadSettings(thisRepoStorage);
            await repoSettings.LoadAsync();

            var repositoryContainer = new RepositoryContainer(Config.KuboOptions, Config.Client, repoSettings.ManagedKeys, repoSettings.ManagedUserConfigs, repoSettings.ManagedProjectConfigs, repoSettings.ManagedPublisherConfigs);

            return (repositoryContainer, Config, repoSettings);
        }

        public async Task<string> CreateRepoAsync(string repoId, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            var thisRepoStorage = (IModifiableFolder)await Config.RepositoryStorage.CreateFolderAsync(repoId, overwrite: false);

            return thisRepoStorage.Id;
        }

        public async Task<List<string>> ListRepoAsync(CancellationToken token)
        {
            var repoIds = new List<string>();
            Logger.LogInformation($"Listing repositories");
            await foreach (var item in Config.RepositoryStorage.GetFoldersAsync(Config.CancellationToken))
            {
                repoIds.Add(item.Id);
            }

            return repoIds;
        }

        public async Task<bool> DeleteRepoAsync(string repoId, CancellationToken token)
        {
            var existingItem = await Config.RepositoryStorage.GetFirstByNameAsync(repoId);

            if (existingItem == null)
                return false;

            await Config.RepositoryStorage.DeleteAsync(existingItem);
            Logger.LogInformation($"Deleted repo store with ID {repoId} at {existingItem.Id}");

            return true;
        }
    }
}
