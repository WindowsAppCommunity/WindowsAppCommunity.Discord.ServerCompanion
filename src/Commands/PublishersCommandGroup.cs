using System.ComponentModel;
using CommunityToolkit.Diagnostics;
using Ipfs.Http;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Extensions;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
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
    public async Task<IResult> RegisterPublisherAsync()
    {
        try
        {
            var discordId = _context.Interaction.Member.Value.User.Value.ID;

            var user = _userKeystore.GetUserByDiscordId(discordId);
            if (user is null)
            {
                var result = (Result)new PublisherAlreadyRegistered()();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }

            var existingPublisher = _publisherKeystore.ManagedPublishers.FirstOrDefault(x => x.Publisher.Connections.OfType<DiscordConnection>().Any(o => o.DiscordId == discordId.ToString()));
            if (existingPublisher is not null)
            {
                var result = (Result)new PublisherAlreadyRegistered();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }


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