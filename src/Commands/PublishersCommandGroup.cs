using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Extensions;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Extensions.Embeds;
using Remora.Results;
using System.ComponentModel;
using System.Drawing;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Extensions;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;

/// <summary>
/// Command group for interacting with publisher data.
/// </summary>
[Group("publisher")]
public partial class PublishersCommandGroup : CommandGroup
{
    private readonly IFeedbackService _feedbackService;
    private readonly IInteractionContext _context;
    private readonly PublisherKeystore _publisherKeystore;
    private readonly UserKeystore _userKeystore;
    private readonly IDiscordRestInteractionAPI _interactionAPI;
    private readonly IpfsClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishersCommandGroup"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="context">The context of the invoked interaction.</param>
    /// <param name="publisherKeystore">A keystore that stores all known publisher keys.</param>
    /// <param name="userKeystore">A keystore that stores all known user keys.</param>
    /// <param name="client">The client to use when interacting with IPFS.</param>
    public PublishersCommandGroup(IInteractionContext context, IFeedbackService feedbackService, PublisherKeystore publisherKeystore, UserKeystore userKeystore, IDiscordRestInteractionAPI interactionAPI, IpfsClient client)
    {
        _context = context;
        _feedbackService = feedbackService;
        _publisherKeystore = publisherKeystore;
        _userKeystore = userKeystore;
        _interactionAPI = interactionAPI;
        _client = client;
    }

    [Command("register")]
    [Description("Register a publisher.")]
    public async Task<IResult> RegisterPublisherAsync(
        [Description("The name of the publisher.")]
        string name,

        [Description("A brief description of the publisher")]
        string description,

        [Description("An icon that reprents that publisher")]
        Cid? icon = null,

        [Description("Optional. An accent color for the publisher")]
        string? accentColor = null,

        [Description("Optional. A public email address that can be used to contact this publisher.")]
        string? contactEmail = null)
    {
        try
        {
            var discordId = _context.Interaction.Member.Value.User.Value.ID;

            var userMap = await _userKeystore.GetUserMapByDiscordId(discordId);
            if (userMap is null)
            {
                var result = (Result)new UserNotFoundError();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }

            // Check if publisher name is already registered
            var existingPublisherByName = _publisherKeystore.ManagedPublishers.FirstOrDefault(p => p.Publisher.Name == name);
            if (existingPublisherByName is not null)
            {
                var result = (Result)new PublisherAlreadyRegistered();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }

            // Validate accentColor
            if (accentColor is not null)
            {
                // Prepend with missing # if needed
                if (!accentColor.StartsWith("#"))
                    accentColor = $"#{accentColor}";

                // Restrict to only 6 characters
                if (accentColor.TrimStart('#').Length != 6)
                {
                    var result = (Result)new GenericResultError("Invalid accent color length");
                    await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                    return result;
                }
            }

            var embedBuilder = new EmbedBuilder()
                .WithColour(Color.YellowGreen)
                .WithTitle($"Registering publisher {name}")
                .WithCurrentTimestamp();

            var embeds = embedBuilder.WithDescription("Loading").Build().GetEntityOrThrowError().IntoList();
            var followUpRes = await _interactionAPI.CreateFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, embeds: new(embeds));
            var followUpMsg = followUpRes.GetEntityOrThrowError();

            // Create data
            var contactEmailConnection = contactEmail is null ? null : new EmailConnection(contactEmail);

            var publisher = new Publisher(name, description, owner: userMap.IpnsCid, icon: icon, accentColor, contactEmailConnection);
            var newPublisherCid = await _client.Dag.PutAsync(publisher);

            // Create publisher ipns
            embeds = embedBuilder.WithDescription("Creating new ipns keys for publisher").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            var key = await _client.Key.CreateKeyWithNameOfIdAsync();

            // Publish publisher to ipns
            embeds = embedBuilder.WithDescription("Publishing new publisher to ipns").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            await _client.Name.PublishAsync(newPublisherCid, key.Id);

            // Modify user to contain new publisher.
            userMap.User.Publishers = [.. userMap.User.Publishers, .. new[] { newPublisherCid }];

            // Add modified user data to ipfs
            embeds = embedBuilder.WithDescription("Adding publisher to user profile").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            var newUserCid = await _client.Dag.PutAsync(userMap.User);

            // Publisher user to ipns
            embeds = embedBuilder.WithDescription("Publishing user update to ipns").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            await _client.Name.PublishAsync(newUserCid, userMap.IpnsCid);

            // Save publisher to keystore
            embeds = embedBuilder.WithDescription("Saving publisher in keystore").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            _publisherKeystore.ManagedPublishers.Add(new(publisher, key.Id));
            await _publisherKeystore.SaveAsync();

            // TODO: Create embed for displaying publisher data.
            // temp: return simple message with publisher data
            var returnMessage = $"Publisher registration successful.\nPublisher name: {publisher.Name}\nPublisher description: {publisher.Description}\nPublisher CID: {newPublisherCid}\nPublisher IPNS: {key.Id}\nPublisher icon: {publisher.Icon}\nPublisher accent color: {publisher.AccentColor}\nPublisher contact email: {publisher.ContactEmail?.Email}";

            embeds = embedBuilder.WithDescription(returnMessage).WithColour(Color.Green).Build().GetEntityOrThrowError().IntoList();
            return (Result)await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }

    [Command("view")]
    [Description("View publisher details.")]
    public async Task<IResult> ViewPublisherAsync([Description("The name of the publisher.")] string ipnsCid)
    {
        try
        {
            Cid cid = ipnsCid;

            var embedBuilder = new EmbedBuilder()
                .WithColour(Color.YellowGreen)
                .WithTitle("Publisher registration")
                .WithCurrentTimestamp();

            var embeds = embedBuilder.WithDescription("Loading").Build().GetEntityOrThrowError().IntoList();
            var followUpRes = await _interactionAPI.CreateFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, embeds: new(embeds));
            var followUpMsg = followUpRes.GetEntityOrThrowError();

            // Resolve publisher data
            embeds = embedBuilder.WithDescription("Resolving publisher data").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            var publisherRes = await cid.ResolveIpnsDagAsync<Publisher>(_client, CancellationToken.None);
            var publisher = publisherRes.Result;

            if (publisher is null)
            {
                var result = (Result)new PublisherNotFoundError();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }

            // Create embed for displaying publisher data.
            var publisherEmbedBuilder = publisher.ToEmbedBuilder();

            publisherEmbedBuilder.AddField("IPNS CID", cid.ToString(), inline: true);

            embeds = publisherEmbedBuilder.Build().GetEntityOrThrowError().IntoList();
            return (Result)await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }
}