using Remora.Discord.Extensions.Embeds;
using Remora.Rest.Core;
using Remora.Results;
using System.Drawing;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk.Models;
using ManagedUserMap = WinAppCommunity.Discord.ServerCompanion.Keystore.ManagedUserMap;

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
    internal static async Task<ManagedUserMap?> GetUserMapByDiscordId(this UserKeystore keystore, Snowflake discordId)
    {
        return keystore.ManagedUsers.FirstOrDefault(um =>
            um.User.Connections
                .OfType<DiscordConnection>()
                .FirstOrDefault()?.DiscordId == discordId.ToString()
            );
    }

    internal static EmbedBuilder ToEmbedBuilder(this User user, ManagedUserMap managedUser)
    {
        var builder = new EmbedBuilder()
            .WithAuthor(user?.Name ?? string.Empty);

        var emailConnection = managedUser.User.Connections.OfType<EmailConnection>().FirstOrDefault();

        if (emailConnection is not null)
            builder.AddField("Email", emailConnection.Email, inline: true);

        builder.AddField("Name", user?.Name ?? string.Empty);

        if (user.Links.Length > 0)
            builder.AddField("Links", string.Join("\n", user.Links.Select(x => $"[{x.Name}]({x.Url})")));

        if (user.Projects.Length > 0)
            builder.AddField("Projects Cids", string.Join("\n", user.Projects.Select(x => x.ToString())));

        if (user.Publishers.Length > 0)
            builder.AddField("Subpublishers", string.Join("\n", user.Publishers.Select(x => x.ToString())));


        return builder;
    }
}
