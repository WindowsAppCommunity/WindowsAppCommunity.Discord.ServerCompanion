using System.ComponentModel;
using Ipfs.Http;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Keystore;

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
    private readonly IpfsClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishersCommandGroup"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="context">The context of the invoked interaction.</param>
    /// <param name="publisherKeystore">A keystore that stores all known publisher keys.</param>
    /// <param name="client">The client to use when interacting with IPFS.</param>
    public PublishersCommandGroup(IInteractionContext context, IFeedbackService feedbackService, PublisherKeystore publisherKeystore, IpfsClient client)
    {
        _context = context;
        _feedbackService = feedbackService;
        _publisherKeystore = publisherKeystore;
        _client = client;
    }

    [Command("register")]
    [Description("Register a publisher.")]
    public async Task<IResult> RegisterPublisherAsync()
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