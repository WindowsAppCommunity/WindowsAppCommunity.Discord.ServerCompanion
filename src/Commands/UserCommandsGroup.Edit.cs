using Ipfs.Http;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using WinAppCommunity.Discord.ServerCompanion.Keystore;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;

public partial class UserCommands
{
    public class EditUserCommands : CommandGroup
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IInteractionContext _context;
        private readonly UserKeystore _userKeystore;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly IpfsClient _client;

        public EditUserCommands(IInteractionContext context, IFeedbackService feedbackService,
            UserKeystore userKeystore, IDiscordRestInteractionAPI interactionAPI, IpfsClient client)
        {
            _feedbackService = feedbackService;
            _context = context;
            _userKeystore = userKeystore;
            _interactionAPI = interactionAPI;
            _client = client;
        }


    }
}