#!/usr/bin/env dotnet

#:project ../src/WindowsAppCommunity.Discord.ServerCompanion.csproj

using CommunityToolkit.Diagnostics;
using OwlCore.Diagnostics;
using OwlCore.Storage;
using OwlCore.Storage.System.IO;
using WindowsAppCommunity.Discord.ServerCompanion;

// Logging setup
Logger.MessageReceived += Logger_MessageReceived;

void Logger_MessageReceived(object? sender, LoggerMessageEventArgs e) => Console.WriteLine($"{DateTime.UtcNow:O} [{e.Level}] [Thread {Thread.CurrentThread.ManagedThreadId}] L{e.CallerLineNumber} {Path.GetFileName(e.CallerFilePath)} {e.CallerMemberName} {e.Exception} {e.Message}");

AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => Logger.LogError(e.ExceptionObject?.ToString() ?? "Error message not found", e.ExceptionObject as Exception);
AppDomain.CurrentDomain.FirstChanceException += (object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e) => Logger.LogError(e.Exception?.ToString() ?? "Error message not found", e.Exception);
TaskScheduler.UnobservedTaskException += (object? sender, UnobservedTaskExceptionEventArgs e) => Logger.LogError(e.Exception?.ToString() ?? "Error message not found", e.Exception);

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
    var ffmpegPath = args[2] ?? "/usr/bin/ffmpeg";

    SystemFile videoFile = new(videoPath);
    await using PerceptualVideoHasher hasher = new(videoFile, ffmpegPath) { WorkingFolder = new SystemFolder(Path.GetTempPath()) };

    // TODO: Select different hash functions based on `algorithm` argument
    var hashes = hasher.ComputeHashesAsync();
    
    var hashNum = 0;
    await foreach (var hash in hashes)
        Console.WriteLine($"#{++hashNum}: {hash}");
    
    return 0;
}
