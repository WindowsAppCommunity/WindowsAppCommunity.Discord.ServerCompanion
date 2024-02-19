using System.ComponentModel;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Extensions;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;

/// <summary>
/// Command group for interacting with publisher data.
/// </summary>
[Group("publishers")]
public class PublishersCommandGroup : CommandGroup
{
    private readonly IFeedbackService _feedbackService;
    private readonly IInteractionContext _context;
    private readonly PublisherKeystore _publisherKeystore;
    private readonly UserKeystore _userKeystore;
    private readonly IpfsClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishersCommandGroup"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="context">The context of the invoked interaction.</param>
    /// <param name="publisherKeystore">A keystore that stores all known publisher keys.</param>
    /// <param name="userKeystore">A keystore that stores all known user keys.</param>
    /// <param name="client">The client to use when interacting with IPFS.</param>
    public PublishersCommandGroup(IInteractionContext context, IFeedbackService feedbackService, PublisherKeystore publisherKeystore, UserKeystore userKeystore, IpfsClient client)
    {
        _context = context;
        _feedbackService = feedbackService;
        _publisherKeystore = publisherKeystore;
        _userKeystore = userKeystore;
        _client = client;
    }

    [Command("register")]
    [Description("Register a publisher.")]
    public async Task<IResult> RegisterPublisherAsync(
        [Description("The name of the publisher.")]
        string name,

        [Description("A breif description of the publisher")]
        string description,

        [Description("An icon that reprents that publisher")]
        Cid icon,

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
                var result = (Result)new PublisherAlreadyRegistered();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }

            var existingPublisherByName = _publisherKeystore.ManagedPublishers.FirstOrDefault(p => p.Publisher.Name == name);
            if (existingPublisherByName is not null)
            {
                var result = (Result)new PublisherAlreadyRegistered();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }

            var contactEmailConnection = contactEmail is null ? null : new EmailConnection(contactEmail);

            var newPublisher = new Publisher(name, description, icon, accentColor, links: [], projects: [], contactEmailConnection);
            var newPublisherCid = await _client.Dag.PutAsync(newPublisher);

            // Create publisher ipns
            var key = await _client.Key.CreateKeyWithNameOfIdAsync();

            // Publish publisher to ipns
            await _client.Name.PublishAsync(newPublisherCid, key.Id);

            // Modify user to contain new publisher.
            userMap.User.Publishers = [.. userMap.User.Publishers, .. new[] { newPublisherCid }];

            // Add modified user data to ipfs
            var newUserCid = await _client.Dag.PutAsync(userMap.User);

            // Publisher user to ipns
            await _client.Name.PublishAsync(newUserCid, userMap.IpnsCid);

            // TODO: Create embed for displaying publisher data.
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }

    [Command("view")]
    [Description("View publisher details.")]
    public async Task<IResult> ViewPublisherAsync()
    {
        try
        {

        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }

    [Command("edit")]
    [Description("Edit the details of a publisher.")]
    public async Task<IResult> EditPublisherAsync()
    {
        try
        {

        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }
}