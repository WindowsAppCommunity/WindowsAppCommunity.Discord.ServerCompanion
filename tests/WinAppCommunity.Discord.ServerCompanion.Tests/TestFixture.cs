using System.Diagnostics;
using Ipfs.Http;
using OwlCore.Kubo;
using OwlCore.Storage;
using OwlCore.Storage.SystemIO;

namespace WinAppCommunity.Discord.ServerCompanion.Tests;

public class TestFixture()
{
    /// <summary>
    /// A client that enables access to ipfs.
    /// </summary>
    public static IpfsClient? Client { get; private set; }

    /// <summary>
    /// The bootstrapper that was used to create the <see cref="Client"/>.
    /// </summary>
    public static KuboBootstrapper? Bootstrapper { get; private set; }

    /// <summary>
    /// Sets up the test fixture.
    /// </summary>
    [AssemblyInitialize]
    public async Task Setup()
    {
        OwlCore.Diagnostics.Logger.MessageReceived += (sender, args) => Debug.WriteLine(args.Message);

        var workingFolder = await SafeCreateWorkingFolder();

        Bootstrapper = await CreateNodeAsync(workingFolder, "node1", 5034, 8034);

        Client = new IpfsClient(Bootstrapper.ApiUri.OriginalString);
    }

    /// <summary>
    /// Tears down the test fixture.
    /// </summary>
    [AssemblyCleanup]
    public async Task Teardown()
    {
        Bootstrapper?.Dispose();
    }

    /// <summary>
    /// Creates a Kubo node with the provided <paramref name="apiPort"/> and <paramref name="gatewayPort"/>, downloading and bootstrapping as needed.
    /// </summary>
    /// <param name="workingDirectory">The directory where Kubo will be downloaded to and executed in.</param>
    /// <param name="nodeRepoName">A unique name for this node's ipfs repo.</param>
    /// <param name="apiPort">The port number to use for the Kubo RPC API.</param>
    /// <param name="gatewayPort">The port number to use for the locally hosted Ipfs Http Gateway.</param>
    /// <returns>An instance of <see cref="KuboBootstrapper"/> that has been started and is ready to use.</returns>
    private async Task<KuboBootstrapper> CreateNodeAsync(SystemFolder workingDirectory, string nodeRepoName, int apiPort, int gatewayPort)
    {
        var nodeRepo = (SystemFolder)await workingDirectory.CreateFolderAsync(nodeRepoName, overwrite: true);

        var node = new KuboBootstrapper(nodeRepo.Path)
        {
            ApiUri = new Uri($"http://127.0.0.1:{apiPort}"),
            GatewayUri = new Uri($"http://127.0.0.1:{gatewayPort}"),
            BinaryWorkingFolder = workingDirectory,
        };

        OwlCore.Diagnostics.Logger.LogInformation($"Starting node {nodeRepoName}\n");

        await node.StartAsync();

        Assert.IsNotNull(node.Process);
        return node;
    }

    /// <summary>
    /// Creates a temp folder for the test fixture to work in, safely unlocking and removing existing files if needed.
    /// </summary>
    /// <returns>The folder that was created.</returns>
    private async Task<SystemFolder> SafeCreateWorkingFolder(string name = "WinAppCommunity.Discord.ServerCompanion")
    {
        var tempFolder = new SystemFolder(Path.GetTempPath());

        var testTempRoot = (SystemFolder)await tempFolder.CreateFolderAsync(name, overwrite: false);
        await SetAllFileAttributesRecursive(testTempRoot, attributes => attributes &~ FileAttributes.ReadOnly);

        return (SystemFolder)await tempFolder.CreateFolderAsync(name, overwrite: true); 
    }

    /// <summary>
    /// Changes the file attributes of all files in all subfolders of the provided <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="rootFolder">The folder to set file permissions in.</param>
    /// <param name="transform">This function is provided the current file attributes, and should return the new file attributes.</param>
    private async Task SetAllFileAttributesRecursive(SystemFolder rootFolder, Func<FileAttributes, FileAttributes> transform)
    {
        await foreach (SystemFile file in rootFolder.GetFilesAsync())
            file.Info.Attributes = transform(file.Info.Attributes);

        await foreach (SystemFolder folder in rootFolder.GetFoldersAsync())
            await SetAllFileAttributesRecursive(folder, transform);
    }
}