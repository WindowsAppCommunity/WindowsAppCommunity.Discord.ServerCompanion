using Ipfs.Http;
using OwlCore.Nomad.Kubo;
using OwlCore.Storage.System.IO;

public class WacsdkCommandConfig
{
    public CancellationToken CancellationToken { get; set; }
    public KuboOptions KuboOptions { get; set; }
    public IpfsClient Client { get; set; }
    public SystemFolder RepositoryStorage { get; set; }
}