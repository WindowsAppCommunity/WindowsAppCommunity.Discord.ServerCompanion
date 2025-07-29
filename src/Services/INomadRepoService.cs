using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion.Services
{
    public interface INomadRepoService
    {
        public Task<(RepositoryContainer, WacsdkCommandConfig, WacsdkNomadSettings)> GetNomadRepo(string repoId,string knownId);
        public WacsdkCommandConfig Config { get; }

    }
}