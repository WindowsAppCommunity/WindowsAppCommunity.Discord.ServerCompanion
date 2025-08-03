using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion.Services
{
    public interface INomadRepoService
    {
        public Task<(RepositoryContainer, WacsdkCommandConfig, WacsdkNomadSettings)> GetNomadRepoAsync(string repoId, string knownId, CancellationToken token);
        public WacsdkCommandConfig Config { get; }

    }
}