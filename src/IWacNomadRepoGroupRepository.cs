using OwlCore.ComponentModel;
using OwlCore.Nomad.Kubo;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// Handles lifecycle and storage of multiple Nomad repositories.
/// </summary>
public interface IWacNomadRepoGroupRepository : IRepository<WacNomadRepoGroupRepositoryItem, WacNomadRepoGroupRepositoryItem, string>
{
}
