using System.Drawing;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Extensions;
using Polly;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Extensions.Embeds;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Extensions;

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

        var publisherRes = await publisherMap.IpnsCid.ResolveIpnsDagAsync<Publisher>(client, cancellationToken);
        if (publisherRes.Result is null)
            return (publisherMap, publisherRes.ResultCid);

        // Hydrate cached publisher data
        publisherMap.Publisher = publisherRes.Result;

        // Return publisher map data
        return (publisherMap, publisherRes.ResultCid);
    }

    public static async Task<IResult> UpdatePublisherAsync(this PublisherKeystore publisherKeystore, string ipnsCid, Action<Publisher> transform, string finalStatus, IInteractionContext context, IDiscordRestInteractionAPI interactionAPI, IpfsClient client)
    {
        Cid cid = ipnsCid;

        var embedBuilder = new EmbedBuilder()
            .WithColour(Color.YellowGreen)
            .WithTitle("Updating publisher")
            .WithCurrentTimestamp();

        var embeds = embedBuilder.WithDescription("Loading").Build().GetEntityOrThrowError().IntoList();
        var followUpRes = await interactionAPI.CreateFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, embeds: new(embeds));
        var followUpMsg = followUpRes.GetEntityOrThrowError();

        // Resolve publisher data
        embeds = embedBuilder.WithDescription("Resolving publisher data").Build().GetEntityOrThrowError().IntoList();
        await interactionAPI.EditFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
        var publisherRes = await cid.ResolveIpnsDagAsync<Publisher>(client, CancellationToken.None);
        var publisher = publisherRes.Result;

        if (publisher is null)
        {
            var result = (Result)new PublisherNotFoundError();
            embeds = embedBuilder.WithColour(Color.Red).WithTitle("An error occurred").WithDescription(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>()).Build().GetEntityOrThrowError().IntoList();
            await interactionAPI.EditFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            return result;
        }

        embeds = embedBuilder.WithTitle($"Updating publisher {publisher.Name}").Build().GetEntityOrThrowError().IntoList();
        await interactionAPI.EditFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));

        // Update data
        transform(publisher);

        // Save data
        embeds = embedBuilder.WithDescription("Saving publisher data").Build().GetEntityOrThrowError().IntoList();
        await interactionAPI.EditFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
        var res = await publisher.SaveRegisteredPublisherAsync(cid, client, publisherKeystore);

        embeds = embedBuilder.WithDescription(finalStatus).WithColour(Color.Green).Build().GetEntityOrThrowError().IntoList();
        await interactionAPI.EditFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
        return res;
    }

    public static async Task<IResult> SaveRegisteredPublisherAsync(this Publisher publisher, Cid cid, IpfsClient client, PublisherKeystore publisherKeystore)
    {
        // Get keystore entry
        var keystorePublisherMap = publisherKeystore.ManagedPublishers.FirstOrDefault(x => x.IpnsCid == cid);
        if (keystorePublisherMap is null)
        {
            var result = (Result)new PublisherNotFoundError();
            return result;
        }

        // Update keystore entry
        keystorePublisherMap.Publisher = publisher;
        await publisherKeystore.SaveAsync();

        // Publish to ipns
        var newPublisherCid = await client.Dag.PutAsync(publisher);
        await client.Name.PublishAsync(newPublisherCid, keystorePublisherMap.IpnsCid);

        return Result.FromSuccess();
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

        if (publisher.Subpublishers.Length > 0)
            builder.AddField("Subpublishers", string.Join("\n", publisher.Subpublishers.Select(x => x.ToString())));

        return builder;
    }
}