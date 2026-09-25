using SixLabors.ImageSharp.Formats;
using System.Diagnostics.CodeAnalysis;
using NeoSolve.ImageSharp.AVIF;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// AVIF format detector that works under a full ImageSharp configuration,
/// such as Configuration.Default.Clone(). The NeoSolve AVIFImageFormatDetector
/// guard requires header.Length to be no more than 12, which only holds when
/// the global MaxHeaderSize is 12 or less. Under the default configuration
/// the global MaxHeaderSize is the max of all detector HeaderSize values, so
/// the span handed to each detector is wider than 12 and that guard is always
/// false, which is why AVIF never gets detected. This detector instead requires
/// at least 12 bytes, matching the built-in convention header.Length >= HeaderSize.
public class AvifImageFormatDetector : IImageFormatDetector
{
    public int HeaderSize => 12;

    public bool TryDetectFormat(ReadOnlySpan<byte> header, [NotNullWhen(true)] out IImageFormat format)
    {
        bool isAVIF = header.Length >= HeaderSize && IsAvif(header);
        format = isAVIF ? AVIFFormat.Instance : null;
        return isAVIF;
    }

    private static bool IsAvif(ReadOnlySpan<byte> header)
    {
        // ftyp box type at byte offset 4
        if (header[4] != 'f' || header[5] != 't' || header[6] != 'y' || header[7] != 'p')
        {
            return false;
        }
        // avif brand at byte offset 8
        return header[8] == 'a' && header[9] == 'v' && header[10] == 'i' && header[11] == 'f';
    }
}
