using Ipfs.Http;
using OwlCore.Extensions;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Extensions;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk.Models;
using User = WinAppCommunity.Sdk.Models.User;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;

public partial class UserCommandGroup
{
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
                return await ipnsCid.UpdateIpnsDataAsync<User>(user => user.Icon = icon, finalStatus: $"User icon updated", dataLabel: "User", _context, _interactionAPI, _client);
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