using Ipfs.Http;
using Ipfs;
using OwlCore.Kubo;
using Remora.Discord.Extensions.Embeds;
using Remora.Rest.Core;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk;
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

    /// <summary>
    /// Resolves a user by name directly from the keystore.
    /// </summary>
    /// <param name="userKeystore">The keystore to search for the user in.</param>
    /// <param name="userCid">The name of the user to find.</param>
    /// <param name="client">The client to user for retrieving data.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>If found, a <see cref="ManagedUserMap"/> containing up-to-date User data and the ipns keys used to retrieve it.</returns>
    internal static async Task<(ManagedUserMap? UserMap, Cid? ResolvedUserCid)> GetUserByIpnsCidAsync(this UserKeystore userKeystore, Cid userCid, IpfsClient client, CancellationToken cancellationToken)
    {
        var userMap = userKeystore.ManagedUsers.FirstOrDefault(p => p.IpnsCid == userCid);
        if (userMap is null)
            return (null, null);

        var userRes = await userMap.IpnsCid.ResolveDagCidAsync<User>(client, nocache: true, cancellationToken);
        if (userRes.Result is null)
            return (userMap, userRes.ResultCid);

        // Hydrate cached user data
        userMap.User = userRes.Result;

        // Return user map data
        return (userMap, userRes.ResultCid);
    }

    internal static EmbedBuilder ToEmbedBuilder(this User user)
    {
        var builder = new EmbedBuilder()
            .WithAuthor(user.Name)
            .WithThumbnailUrl($"https://ipfs.io/ipfs/{user.Icon}?filename=image.png");

        if (!string.IsNullOrWhiteSpace(user.MarkdownAboutMe))
            builder.WithDescription(user.MarkdownAboutMe);

        var emailConnection = user.Connections.OfType<EmailConnection>().FirstOrDefault();
        if (emailConnection is not null)
            builder.AddField("Email", emailConnection.Email, inline: true);

        if (user.Links.Length > 0)
            builder.AddField("Links", string.Join("\n", user.Links.Select(x => $"[{x.Name}]({x.Url})")));

        if (user.Projects.Length > 0)
            builder.AddField("Projects Cids", string.Join("\n", user.Projects.Select(x => x.ToString())));

        if (user.Publishers.Length > 0)
            builder.AddField("Subpublishers", string.Join("\n", user.Publishers.Select(x => x.ToString())));

        return builder;
    }
}
