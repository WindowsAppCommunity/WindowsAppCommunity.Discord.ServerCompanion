using Ipfs.Http;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using OwlCore.Kubo.Extensions;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WindowsAppCommunity.Discord.ServerCompanion.Extensions;
using WindowsAppCommunity.Sdk.Models;
using User = WindowsAppCommunity.Sdk.Models.User;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands;

public partial class UserCommandGroup
{
    [Group("publishers")]
    [Description("Add or remove publishers from a user")]
    public class UserPublishersCommandGroup : CommandGroup
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IInteractionContext _context;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly IpfsClient _client;

        /// <summary>
        /// Creates a new instance of<see cref="UserPublishersCommandGroup"/>.
        /// </summary>
        public UserPublishersCommandGroup(IInteractionContext context, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IpfsClient client)
        {
            _feedbackService = feedbackService;
            _context = context;
            _interactionAPI = interactionAPI;
            _client = client;
        }

        [Command("add")]
        [Description("Adds a publisher to a user's profile")]
        public async Task<IResult> AddPublisherAsync([Description("The cid of the user to modify.")] string userCid, [Description("The cid of the publisher to add.")] string publisherCid)
        {
            try
            {


                return await userCid.UpdateIpnsDataAsync<User>(user => user.Name = name, finalStatus: $"User name updated to {name}", dataLabel: "User", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }

        [Command("add")]
        [Description("Adds a publisher to a user's profile")]
        public async Task<IResult> RemovePublisherAsync([Description("The cid of the user to modify.")] string userCid, [Description("The cid of the publisher to remove.")] string publisherCid)
        {

        }
    }

    [Group("edit")]
    [Description("Edit the details of a user.")]
    public class EditUserCommandGroup : CommandGroup
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IInteractionContext _context;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly IpfsClient _client;

        /// <summary>
        /// Creates a new instance of<see cref="EditUserCommandGroup"/>.
        /// </summary>
        public EditUserCommandGroup(IInteractionContext context, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IpfsClient client)
        {
            _feedbackService = feedbackService;
            _context = context;
            _interactionAPI = interactionAPI;
            _client = client;
        }

        [Command("name")]
        [Description("Edit the name of a user")]
        public async Task<IResult> EditNameAsync([Description("The cid of the user to edit.")] string ipnsCid, [Description("The new name for the user")] string name)
        {
            try
            {
                return await ipnsCid.UpdateIpnsDataAsync<User>(user => user.Name = name, finalStatus: $"User name updated to {name}", dataLabel: "User", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }

        // TODO: Use full multi-line modal for this
        [Command("aboutme")]
        public async Task<IResult> EditDescriptionAsync([Description("The ipns cid of the user to edit.")] string ipnsCid, [Description("The new markdown for the user")] string markdownAboutMe)
        {
            try
            {
                return await ipnsCid.UpdateIpnsDataAsync<User>(user => user.MarkdownAboutMe = markdownAboutMe, finalStatus: "Updated user description", dataLabel: "User", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }

        [Command("icon")]
        public async Task<IResult> EditIconAsync([Description("The ipns cid of the user to edit.")] string ipnsCid, [Description("The new cid for the user icon")] string icon)
        {
            try
            {

                return await _client.TransformIpnsDagAsync<User>(ipnsCid, user => user.Icon = icon, finalStatus: $"User icon updated", dataLabel: "User", _context, _interactionAPI);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }

        [Command("contactemail")]
        public async Task<IResult> EditContactEmailAsync([Description("The cid of the user to edit.")] string ipnsCid, [Description("The new public contact email for the user, or no value.")] string contactEmail)
        {
            try
            {
                return await ipnsCid.UpdateIpnsDataAsync<User>((user) =>
                {
                    user.Connections = [.. user.Connections.Where(x => x is not EmailConnection), new EmailConnection(contactEmail)];
                }, finalStatus: $"User email updated to {contactEmail}", dataLabel: "User", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }
    }
}