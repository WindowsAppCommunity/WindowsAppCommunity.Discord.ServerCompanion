using Ipfs;
using Ipfs.Http;
using Remora.Discord.Extensions.Embeds;
using System.Drawing;
using OwlCore.Kubo;
using WindowsAppCommunity.Discord.ServerCompanion.Keystore;
using WindowsAppCommunity.Sdk.Models;

namespace WindowsAppCommunity.Discord.ServerCompanion.Extensions;

internal static class PublisherExtensions
{
    /// <summary>
    /// Resolves a publisher by name directly from the keystore.
    /// </summary>
    /// <param name="publisherKeystore">The keystore to search for the publisher in.</param>
    /// <param name="name">The name of the publisher to find.</param>
    /// <param name="client">The client to user for retrieving data.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing task.</param>
    /// <returns>If found, a <see cref="ManagedPublisherMap"/> containing up-to-date Publisher data and the ipns keys used to retrieve it.</returns>
    internal static async Task<(ManagedPublisherMap? PublisherMap, Cid? ResolvedPublisherCid)> GetPublisherByNameAsync(this PublisherKeystore publisherKeystore, string name, IpfsClient client, CancellationToken cancellationToken)
    {
        var publisherMap = publisherKeystore.ManagedPublishers.FirstOrDefault(p => p.Publisher.Name == name);
        if (publisherMap is null)
            return (null, null);

        var publisherRes = await publisherMap.IpnsCid.ResolveDagCidAsync<Publisher>(client, nocache: false, cancellationToken);
        if (publisherRes.Result is null)
            return (publisherMap, publisherRes.ResultCid);

        // Hydrate cached publisher data
        publisherMap.Publisher = publisherRes.Result;

        // Return publisher map data
        return (publisherMap, publisherRes.ResultCid);
    }

    internal static EmbedBuilder ToEmbedBuilder(this Publisher publisher)
    {
        var builder = new EmbedBuilder()
            .WithAuthor(publisher.Name)
            .WithDescription(publisher.Description)
            .WithThumbnailUrl($"https://ipfs.io/ipfs/{publisher.Icon}?filename=image.png");

        builder.AddField("Owner", publisher.Owner);

        if (publisher.ContactEmail is not null)
            builder.AddField("Email", publisher.ContactEmail.Email);

        if (publisher.AccentColor is not null)
            builder.WithColour(ColorTranslator.FromHtml(publisher.AccentColor));

        if (publisher.Links.Length > 0)
            builder.AddField("Links", string.Join("\n", publisher.Links.Select(x => $"[{x.Name}]({x.Url})")));

        if (publisher.Projects.Length > 0)
            builder.AddField("Projects", string.Join("\n", publisher.Projects.Select(x => x.ToString())));

        if (publisher.ChildPublishers.Length > 0)
            builder.AddField("ChildPublishers", string.Join("\n", publisher.ChildPublishers.Select(x => x.ToString())));

        if (publisher.ParentPublishers.Length > 0)
            builder.AddField("ParentPublishers", string.Join("\n", publisher.ParentPublishers.Select(x => x.ToString())));

        return builder;
    }
}