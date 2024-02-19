using Remora.Rest.Core;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Extensions;

/// <summary>
/// Extension methods for interacting with a <see cref="User"/>.
/// </summary>
internal static class UserExtensions
{
    /// <summary>
    /// Gets a user by the discordId on their account.
    /// </summary>
    /// <param name="keystore">The keystore to retrieve the user from.</param>
    /// <param name="discordId">The discordId of the user to retrieve.</param>
    /// <returns></returns>
    public static async Task<User?> GetUserByDiscordId(this UserKeystore keystore, Snowflake discordId)
    {
        return keystore.ManagedUsers.FirstOrDefault(um => 
            um.User.Connections
                .OfType<DiscordConnection>()
                .FirstOrDefault()?.DiscordId == discordId.ToString()
            )?.User;
    }
}