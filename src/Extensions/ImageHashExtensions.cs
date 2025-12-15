using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace WindowsAppCommunity.Discord.ServerCompanion.Extensions;

/// <summary>
/// Extension methods for perceptual image hashing.
/// </summary>
public static class ImageHashExtensions
{
    /// <summary>
    /// Computes a perceptual hash of an image from a stream.
    /// Uses 8x8 resize → grayscale → average → bits → hex hash algorithm.
    /// </summary>
    /// <param name="imageStream">The stream containing the image data.</param>
    /// <returns>Hex string representation of the perceptual hash, or null if image invalid/corrupt.</returns>
    public static string? ComputePerceptualHash(this Stream imageStream)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageStream);
            
            // Resize to 8x8 pixels
            image.Mutate(x => x.Resize(8, 8));
            
            // Convert to grayscale
            image.Mutate(x => x.Grayscale());
            
            // Calculate mean pixel value
            var pixelValues = new List<byte>();
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var pixel = image[x, y];
                    // Grayscale: R=G=B, so we can use any channel
                    pixelValues.Add(pixel.R);
                }
            }
            
            var mean = pixelValues.Average(b => (double)b);
            
            // Generate hash: 1 if pixel > mean, 0 otherwise
            var bits = new StringBuilder();
            foreach (var value in pixelValues)
            {
                bits.Append(value > mean ? '1' : '0');
            }
            
            // Convert 64 bits to hex string (8 bytes)
            var hashBytes = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                var byteString = bits.ToString(i * 8, 8);
                hashBytes[i] = Convert.ToByte(byteString, 2);
            }
            
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch
        {
            // Return null for corrupt/invalid images
            return null;
        }
    }
}
