using System.Security.Cryptography;
using System.Text;

namespace WindowsAppCommunity.Discord.ServerCompanion.Extensions;

/// <summary>
/// Extension methods for hashing message text content.
/// </summary>
public static class MessageHashExtensions
{
    /// <summary>
    /// Normalizes text content for consistent hashing (lowercase, trim whitespace).
    /// </summary>
    /// <param name="content">The text content to normalize.</param>
    /// <returns>Normalized text content.</returns>
    public static string NormalizeText(this string content)
    {
        return content.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Generates a SHA256 hash of the normalized text content.
    /// </summary>
    /// <param name="content">The text content to hash.</param>
    /// <returns>Hex string representation of the hash.</returns>
    public static string ComputeTextHash(this string content)
    {
        var normalized = content.NormalizeText();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
