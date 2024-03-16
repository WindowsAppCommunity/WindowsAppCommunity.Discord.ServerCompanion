using Ipfs.Http;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Extensions;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;

public partial class PublisherCommandGroup
{
    [Group("edit")]
    [Description("Edit the details of a publisher.")]
    public class EditPublisherCommandGroup : CommandGroup
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IInteractionContext _context;
        private readonly IDiscordRestInteractionAPI _interactionAPI;
        private readonly IpfsClient _client;

        /// <summary>
        /// Creates a new instance of<see cref="EditPublisherCommandGroup"/>.
        /// </summary>
        public EditPublisherCommandGroup(IInteractionContext context, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IpfsClient client)
        {
            _feedbackService = feedbackService;
            _context = context;
            _interactionAPI = interactionAPI;
            _client = client;
        }

        [Command("name")]
        [Description("Edit the name of a publisher")]
        public async Task<IResult> EditNameAsync([Description("The cid of the publisher to edit.")] string ipnsCid, [Description("The new name for the publisher")] string name)
        {
            try
            {
                return await ipnsCid.UpdateIpnsDataAsync<Publisher>(publisher => publisher.Name = name, finalStatus: $"Publisher name updated to {name}", dataLabel: "Publisher", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }

        [Command("description")]
        public async Task<IResult> EditDescriptionAsync([Description("The ipns cid of the publisher to edit.")] string ipnsCid, [Description("The new description for the publisher")] string description)
        {
            try
            {
                return await ipnsCid.UpdateIpnsDataAsync<Publisher>(publisher => publisher.Description = description, finalStatus: "Updated publisher description", dataLabel: "Publisher", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }

        [Command("icon")]
        public async Task<IResult> EditIconAsync([Description("The ipns cid of the publisher to edit.")] string ipnsCid, [Description("The new cid for the publisher icon")] string icon)
        {
            try
            {
                return await ipnsCid.UpdateIpnsDataAsync<Publisher>(publisher => publisher.Icon = icon, finalStatus: $"Publisher icon updated", dataLabel: "Publisher", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }

        [Command("accentcolor")]
        public async Task<IResult> EditAccentColorAsync([Description("The cid of the publisher to edit.")] string ipnsCid, [Description("The new hex-encoded accent color for the publisher, or no value.")] string? accentColor)
        {
            try
            {
                return await ipnsCid.UpdateIpnsDataAsync<Publisher>(publisher => publisher.AccentColor = accentColor, finalStatus: $"Publisher accent color updated", dataLabel: "Publisher", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }

        [Command("contactemail")]
        public async Task<IResult> EditContactEmailAsync([Description("The cid of the publisher to edit.")] string ipnsCid, [Description("The new public contact email for the publisher, or no value.")] string contactEmail)
        {
            try
            {
                return await ipnsCid.UpdateIpnsDataAsync<Publisher>(publisher => publisher.ContactEmail = new EmailConnection(contactEmail), finalStatus: $"Publisher email updated to {contactEmail}", dataLabel: "Publisher", _context, _interactionAPI, _client);
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }
    }
}