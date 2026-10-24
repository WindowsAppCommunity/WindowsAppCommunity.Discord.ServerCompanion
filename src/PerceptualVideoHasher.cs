using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using OwlCore.Storage;
using OwlCore.Storage.System.IO;
using WindowsAppCommunity.Discord.ServerCompanion.Responders;

namespace WindowsAppCommunity.Discord.ServerCompanion;

public class PerceptualVideoHasher(IFile videoFile) : IAsyncDisposable
{
    private SystemFolder? _tmpFolder = null;
    private SystemFolder? _imageFolder = null;
    
    public IFile VideoFile => videoFile;
    
    public async IAsyncEnumerable<string> ComputeHashesAsync(CancellationToken ct = default)
    {
        // Would love to wrap the whole body in a try-catch, but you can't yield-return from inside one

        await using var videoStream = await VideoFile.OpenReadAsync(cancellationToken: ct);
        
        _tmpFolder = new SystemFolder(Path.GetTempPath());
            
        var imageFolderName = typeof(CrossChannelRateLimitResponder).Namespace ?? ThrowHelper.ThrowArgumentNullException<string>();
        _imageFolder = (SystemFolder)await _tmpFolder.CreateFolderAsync(imageFolderName, overwrite: true, cancellationToken: ct);
            
        ct.ThrowIfCancellationRequested();

        const string ffmpegPath = "/usr/bin/ffmpeg";
        var startFrame = 0;
        var endFrame = 1000;
        var frameInterval = 10;
        var frameSize = 8;
        var arguments = $"-hwaccel auto -i pipe:0 -f image2 -filter:v \"trim=start_frame={startFrame}:end_frame={endFrame}, select=not(mod(n\\,{frameInterval})), format=pix_fmts=gray8\" -s {frameSize}x{frameSize} -fps_mode vfr {Path.Join(_imageFolder.Path, "frame-%03d.jpeg")}";
        var ffmpegProc = await ProcessHelpers.StartExecutableAsync(ffmpegPath, videoStream, arguments, ct);
        
        CancellationTokenSource ffmpegCancellationSource = new();
        ffmpegProc.Exited += FfmpegProcOnExited;
            
        void FfmpegProcOnExited(object? sender, EventArgs e)
        {
            ffmpegCancellationSource.Cancel();
        }

        await foreach (var imageFile in GetNewFilesAsync(_imageFolder, ct))
        {
            await using var stream = await imageFile.OpenReadAsync(cancellationToken: ct);
            var hash = stream.ComputePerceptualHash();
            
            if (hash is not null)
                yield return hash;
        }
    }

    private static async IAsyncEnumerable<IFile> GetNewFilesAsync(IMutableFolder folder, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        TaskCompletionSource<IFile> tcs = new();

        void OnNewImageFile(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var newFile = e.NewItems?.Cast<IFile>().FirstOrDefault();
            if (newFile is not null)
                tcs.SetResult(newFile);
        }

        await using var imageFolderWatcher = await folder.GetFolderWatcherAsync(ct);
        imageFolderWatcher.CollectionChanged += OnNewImageFile;

        while (true)
        {
            if (ct.IsCancellationRequested)
                break;
            
            yield return await tcs.Task.WaitAsync(ct);
            tcs = new();
        }
                    
        imageFolderWatcher.CollectionChanged -= OnNewImageFile;
    }

    public async ValueTask DisposeAsync()
    {
        if (_imageFolder is not null && _tmpFolder is not null)
            await _tmpFolder.DeleteAsync(_imageFolder);
    }
}