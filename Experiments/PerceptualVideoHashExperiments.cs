#!/usr/bin/env dotnet

#:project ../src/WindowsAppCommunity.Discord.ServerCompanion.csproj

using CommunityToolkit.Diagnostics;
using OwlCore.Storage;
using OwlCore.Storage.System.IO;
using WindowsAppCommunity.Discord.ServerCompanion;

Guard.HasSizeGreaterThanOrEqualTo(args, 2);

var command = args[0];
var otherArgs = args[1..];

return command.ToLowerInvariant() switch
{
    "vhash" => await ComputeVideoHash(otherArgs),
    _ => -1
};

async Task<int> ComputeVideoHash(string[] args)
{
    Guard.HasSizeGreaterThanOrEqualTo(args, 2);
    var algorithm = args[0];
    var videoPath = args[1];

    SystemFile videoFile = new(videoPath);
    await using PerceptualVideoHasher hasher = new(videoFile);

    // TODO: Select different hash functions based on `algorithm` argument
    var hashes = hasher.ComputeHashesAsync();
    
    var hashNum = 0;
    await foreach (var hash in hashes)
        Console.WriteLine($"#{++hashNum}: {hash}");
    
    return 0;
}
