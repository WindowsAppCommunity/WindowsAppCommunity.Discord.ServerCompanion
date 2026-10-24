using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using OwlCore.Diagnostics;
using OwlCore.Extensions;
using OwlCore.Storage;
using OwlCore.Storage.System.IO;
using WindowsAppCommunity.Discord.ServerCompanion.Responders;

namespace WindowsAppCommunity.Discord.ServerCompanion;

public class PerceptualVideoHasher(IFile videoFile) : IAsyncDisposable
{
    private SystemFolder? _imageFolder = null;
    private SemaphoreSlim _folderWatcherSemaphore = new(1, 1);

    /// <summary>
    /// A folder for ffmpeg to output frames to and for internal code to consume from for computing image hashes per frame.
    /// </summary>
    /// <remarks>
    /// All folders and files created in this folder during processing are cleaned up before processing ends. 
    /// </remarks>
    public required SystemFolder WorkingFolder { get; init; }

    /// <summary>
    /// The binary file for ffmpeg.
    /// </summary>
    public required SystemFile FfmpegBinary { get; init; }

    /// <summary>
    /// The video file being processed by this <see cref="PerceptualVideoHasher"/> instance.
    /// </summary>
    public IFile VideoFile => videoFile;

    /// <summary>
    /// For the <see cref="VideoFile"/> in this instance, extract the image frames using FFMPEG and compute the hashes of those images.
    /// </summary>
    /// <param name="ct">A token that cancels the ongoing operation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="string"/> that each represent a single image hash. Yielded in the order they arrive on disk from ffmpeg.</returns>
    public async IAsyncEnumerable<string> ComputeHashesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Setup image output folder for ffmpeg
        _imageFolder = (SystemFolder)await WorkingFolder.CreateFolderAsync($"{Guid.NewGuid()}", overwrite: true, cancellationToken: ct);

        // Keep reference to task completion source that we can both:
        // 1) set data on from inside the event handler when it arrives, and
        // 2) reset the tsc from within the async enumerator after waiting for for the event to set some data and after yielding it.
        var perceptualHashCompletionSource = new PerceptualHashCompletionSource();

        // Watch folder for new files
        await using var imageFolderWatcher = await _imageFolder.GetFolderWatcherAsync(ct);
        imageFolderWatcher.CollectionChanged += async (object? sender, NotifyCollectionChangedEventArgs e) =>
        {
            Logger.LogInformation($"Folder watcher raised CollectionChanged handler");

            // 'Removes' or 'renames' should not happen during this ffmpeg processing in this folder, may indicate code flow or storage separation issues.
            // TODO ignore removes IF we delete files incrementally during processing
            Guard.IsTrue(e.Action is NotifyCollectionChangedAction.Add);
            Guard.IsNotNull(e.NewItems);

            // Don't add data while old data is being flushed.
            using (await _folderWatcherSemaphore.DisposableWaitAsync())
            {
                // We expect one file to appear at a time, but support multiple appearing at a time.
                var newFiles = e.NewItems.Cast<IFile>().ToArray();
                perceptualHashCompletionSource.LatestFiles = newFiles;

                // Log and release lock
                foreach (var newFile in newFiles)
                    Logger.LogInformation($"Added {newFile.Id}");
            }
        };

        // Video frame extraction
        // ffmpeg params
        var startFrame = 0;
        var endFrame = 1000;
        var frameInterval = 10;
        var frameSize = 8;

        // ffmpeg proc args
        var arguments = $"-hwaccel auto -i pipe:0 -f image2 -filter:v \"trim=start_frame={startFrame}:end_frame={endFrame}, select=not(mod(n\\,{frameInterval})), format=pix_fmts=gray8\" -s {frameSize}x{frameSize} -fps_mode vfr {Path.Join(_imageFolder.Path, "frame-%03d.jpeg")}";

        // Open video
        Logger.LogInformation($"Opening {VideoFile.Id}");
        await using var videoStream = await VideoFile.OpenReadAsync(ct);

        // Start ffmpeg
        Logger.LogInformation($"Starting {FfmpegBinary.Path} {arguments}, frame extraction to {_imageFolder.Path}");
        var ffmpegProc = await ProcessHelpers.StartExecutableAsync(FfmpegBinary.Path, videoStream, arguments, ct);
        Guard.IsFalse(ffmpegProc.HasExited);

        // Listen for stderr
        ffmpegProc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => Logger.LogError(e.Data ?? "Null or empty error data");

        // Linked token source to handle externally forced process exits
        CancellationTokenSource ffmpegCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ffmpegProc.Exited += (object? sender, EventArgs e) => ffmpegCancellationSource.Cancel();

        // Wait for new image files to arrive from ffmpeg
        Logger.LogInformation($"Enumerating task completions");
        await foreach (var imageFile in GetNewFilesUntilExitAsync(ffmpegProc, perceptualHashCompletionSource, ct))
        {
            // Open extracted frame
            await using var stream = await imageFile.OpenReadAsync(cancellationToken: ct);

            // Compute perceptual image hash
            string? hash = null;
            try
            {
                hash = stream.ComputePerceptualHash();
            }
            catch (Exception ex) { Logger.LogError(ex.Message, ex); }

            // Yield if no error
            if (hash is not null)
                yield return hash;
        }
    }

    private async IAsyncEnumerable<IFile> GetNewFilesUntilExitAsync(Process proc, PerceptualHashCompletionSource completionSource, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        while (!proc.HasExited && !ct.IsCancellationRequested)
        {
            // Don't flush while new data is being added.
            using (await _folderWatcherSemaphore.DisposableWaitAsync(ct))
            {
                // Yield any new files
                if (completionSource.LatestFiles.Length > 0)
                    foreach (var newlyAddedFile in completionSource.LatestFiles)
                        yield return newlyAddedFile;

                completionSource.LatestFiles = [];
            }
        }
    }

    /// <summary>
    // Keep reference to task completion source that we can both:
    // 1) set data on from inside the event handler when it arrives, and
    // 2) reset the tsc from within the async enumerator after waiting for for the event to set some data and after yielding it.
    /// </summary>
    private record PerceptualHashCompletionSource
    {
        public IFile[] LatestFiles { get; set; } = [];
    }

    public async ValueTask DisposeAsync()
    {
        if (_imageFolder is not null)
            await WorkingFolder.DeleteAsync(_imageFolder);
    }
}