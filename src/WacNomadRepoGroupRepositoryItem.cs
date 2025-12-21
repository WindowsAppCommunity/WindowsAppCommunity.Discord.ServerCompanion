using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// A container for entity repository group instance and its' corresponding settings instance. 
/// </summary>
public class WacNomadRepoGroupRepositoryItem
{
    /// <summary>
    /// The settings instance for this entity group's Nomad repository.
    /// </summary>
    public required WacsdkNomadSettings Settings { get; init; }

    /// <summary>
    /// An instance containing usable repositories for managing user, project and publisher. 
    /// </summary>
    public required WacEntityRepositoryGroup RepositoryGroup { get; init; }
}
