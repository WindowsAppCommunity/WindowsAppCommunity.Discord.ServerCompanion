using Ipfs.Http;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Keystore;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;


public partial class PublishersCommandGroup
{
    [Group("subpublisher")]
    public class SubpublisherCommandGroup : CommandGroup
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IInteractionContext _context;
        private readonly PublisherKeystore _publisherKeystore;
        private readonly UserKeystore _userKeystore;
        private readonly IpfsClient _client;

        public SubpublisherCommandGroup(IInteractionContext context, IFeedbackService feedbackService, PublisherKeystore publisherKeystore, UserKeystore userKeystore, IpfsClient client)
        {
            _context = context;
            _feedbackService = feedbackService;
            _publisherKeystore = publisherKeystore;
            _userKeystore = userKeystore;
            _client = client;
        }

/*        [Command("add")]
        [Description("Adds a publisher as owned and managed by another publisher.")]
        public async Task<IResult> AddSubpublisherAsync()
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

        [Command("remove")]
        [Description("Removes a publisher owned and managed by another publisher.")]
        public async Task<IResult> RemoveSubpublisherAsync()
        {
            try
            {

            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }*/
    }
}