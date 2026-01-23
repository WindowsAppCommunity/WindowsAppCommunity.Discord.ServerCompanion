using OwlCore.Nomad.Kubo;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// Handles lifecycle and storage of multiple Nomad repositories.
/// </summary>
public interface IWacNomadRepoGroupRepository : INomadKuboRepository<WacNomadRepoGroupRepositoryItem, WacNomadRepoGroupRepositoryItem, string>
{
}
