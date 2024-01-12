using System.Diagnostics;
using Ipfs.Http;
using OwlCore.Kubo;
using OwlCore.Storage;
using OwlCore.Storage.SystemIO;

namespace WinAppCommunity.Discord.ServerCompanion.Tests;

public class TestFixture()
{
    public static IpfsClient? Client { get; private set; }
    
    public static KuboBootstrapper? Bootstrapper { get; private set; }

    [AssemblyInitialize]
    public async Task Setup()
    {
        OwlCore.Diagnostics.Logger.MessageReceived += (sender, args) => Debug.WriteLine(args.Message);

        var workingFolder = await CreateWorkingFolder();
        
        Bootstrapper = await CreateNodeAsync(workingFolder, "node1", 5034, 8034);

        Client = new IpfsClient(Bootstrapper.ApiUri.OriginalString);
    }

    [AssemblyCleanup]
    public async Task Teardown()
    {
        Bootstrapper?.Dispose();
    }

    private async Task<KuboBootstrapper> CreateNodeAsync(SystemFolder workingDirectory, string nodeName, int apiPort, int gatewayPort)
    {
        var nodeRepo = (SystemFolder)await workingDirectory.CreateFolderAsync(nodeName, overwrite: true);

        var node = new KuboBootstrapper(nodeRepo.Path)
        {
            ApiUri = new Uri($"http://127.0.0.1:{apiPort}"),
            GatewayUri = new Uri($"http://127.0.0.1:{gatewayPort}"),
            BinaryWorkingFolder = workingDirectory,
        };
        
        OwlCore.Diagnostics.Logger.LogInformation($"Starting node {nodeName}\n");

        await node.StartAsync();
        
        Assert.IsNotNull(node.Process);
        return node;
    }

    private async Task<SystemFolder> CreateWorkingFolder()
    {
        var tempFolder = new SystemFolder(Path.GetTempPath());

        var testTempRoot = (SystemFolder)await tempFolder.CreateFolderAsync("WinAppCommunity.Discord.ServerCompanion", overwrite: false);
        await RemoveReadOnlyFromAllFilesRecursive(testTempRoot);
        
        return (SystemFolder)await tempFolder.CreateFolderAsync("WinAppCommunity.Discord.ServerCompanion", overwrite: true);
    }

    private async Task RemoveReadOnlyFromAllFilesRecursive(SystemFolder rootFolder)
    {
        await foreach (SystemFile file in rootFolder.GetFilesAsync())
            file.Info.Attributes = FileAttributes.Normal;

        await foreach (SystemFolder folder in rootFolder.GetFoldersAsync())
            RemoveReadOnlyFromAllFilesRecursive(folder);
    }
}