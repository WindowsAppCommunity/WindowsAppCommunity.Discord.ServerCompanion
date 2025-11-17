using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion.Services
{
    public interface INomadRepoService
    {
        public Task<(RepositoryContainer, WacsdkCommandConfig)> GetNomadRepoAsync(string repoId, string knownId, CancellationToken token);
        Task<string> CreateRepoAsync(string repoId, CancellationToken token);

        Task<List<string>> ListRepoAsync(CancellationToken token);

        Task<bool> DeleteRepoAsync(string repoId,CancellationToken token);

    }
}