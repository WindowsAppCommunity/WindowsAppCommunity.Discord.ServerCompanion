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
    
    private async IAsyncEnumerable<string> ComputeHashesAsync(CancellationToken ct)
    {
        // Would love to wrap the whole body in a try-catch, but you can't yield-return from inside one

        Stream? videoStream = null;
        
        try
        {
            videoStream = await VideoFile.OpenReadAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            OwlCore.Diagnostics.Logger.LogError($"Exception while computing hash for {VideoFile.Id}", ex);
        }
        
        if (videoStream is null)
            yield break;
        
        await using (videoStream)
        {
            _tmpFolder = new SystemFolder(Path.GetTempPath());
            Process? ffmpegProc = null;
            
            var imageFolderName = typeof(CrossChannelRateLimitResponder).Namespace ?? ThrowHelper.ThrowArgumentNullException<string>();
            _imageFolder = (SystemFolder)await _tmpFolder.CreateFolderAsync(imageFolderName, overwrite: true, cancellationToken: ct);
            
            ct.ThrowIfCancellationRequested();

            const string ffmpegPath = "/usr/bin/ffmpeg";
            var arguments = $"-hwaccel auto -i pipe:0 -r 1 -f image2 {Path.Join(_imageFolder.Path, "frame-%03d.jpeg")}";
            ffmpegProc = await ProcessHelpers.StartExecutableAsync(ffmpegPath, videoStream, arguments, ct);
        
            // CancellationTokenSource ffmpegCancellationSource = new();
            // ffmpegProc.Exited += FfmpegProcOnExited;
            //
            // void FfmpegProcOnExited(object? sender, EventArgs e)
            // {
            //     throw new NotImplementedException();
            // }

            await foreach (var imageFile in GetNewFilesAsync(_imageFolder, ct))
            {
                string? hash = null;
            
                try
                {
                    hash = videoStream.ComputePerceptualHash();
                }
                catch (Exception ex)
                {
                    OwlCore.Diagnostics.Logger.LogError($"Exception while computing hash for {VideoFile.Id}", ex);
                }
            
                if (hash is not null)
                    yield return hash;
            }
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