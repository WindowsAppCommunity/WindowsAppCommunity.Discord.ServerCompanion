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
using System.Xml.Linq;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
using WinAppCommunity.Discord.ServerCompanion.Extensions;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;

public partial class PublishersCommandGroup
{
    [Group("edit")]
    [Description("Edit the details of a publisher.")]
    public class EditPublisherCommandGroup(
        IInteractionContext context,
        IFeedbackService feedbackService,
        PublisherKeystore publisherKeystore,
        UserKeystore userKeystore,
        IDiscordRestInteractionAPI interactionAPI,
        IpfsClient client) : CommandGroup
    {
        private readonly IFeedbackService _feedbackService = feedbackService;
        private readonly IInteractionContext _context = context;
        private readonly PublisherKeystore _publisherKeystore = publisherKeystore;
        private readonly UserKeystore _userKeystore = userKeystore;
        private readonly IDiscordRestInteractionAPI _interactionAPI = interactionAPI;
        private readonly IpfsClient _client = client;

        private async Task<IResult> UpdatePublisherAsync(string ipnsCid, Action<Publisher> transform, string finalStatus)
        {
            Cid cid = ipnsCid;

            var embedBuilder = new EmbedBuilder()
                .WithColour(Color.YellowGreen)
                .WithTitle("Updating publisher")
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

            embeds = embedBuilder.WithTitle($"Updating publisher {publisher.Name}").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));

            // Update data
            transform(publisher);

            embeds = embedBuilder.WithDescription("Saving publisher data").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            var res = await SaveRegisteredPublisherAsync(publisher, cid);
                
            embeds = embedBuilder.WithDescription(finalStatus).WithColour(Color.Green).Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));

            return res;
        }

        private async Task<IResult> SaveRegisteredPublisherAsync(Publisher publisher, Cid cid)
        {
            // Get keystore entry
            var keystorePublisherMap = _publisherKeystore.ManagedPublishers.FirstOrDefault(x => x.IpnsCid == cid);
            if (keystorePublisherMap is null)
            {
                var result = (Result)new PublisherNotFoundError();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }

            // Update keystore entry
            keystorePublisherMap.Publisher = publisher;
            await _publisherKeystore.SaveAsync();

            // Publish to ipns
            var newPublisherCid = await _client.Dag.PutAsync(publisher);
            await _client.Name.PublishAsync(newPublisherCid, keystorePublisherMap.IpnsCid);

            return Result.FromSuccess();
        }

        [Command("name")]
        [Description("Edit the name of a publisher")]
        public async Task<IResult> EditNameAsync([Description("The cid of the publisher to edit.")] string ipnsCid, [Description("The new name for the publisher")] string name)
        {
            try
            {
                return await UpdatePublisherAsync(ipnsCid, publisher => publisher.Name = name, $"Publisher name updated to {name}");
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
                return await UpdatePublisherAsync(ipnsCid, publisher => publisher.Description = description, "Updated publisher description");
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
                return await UpdatePublisherAsync(ipnsCid, publisher => publisher.Icon = icon, $"Publisher icon updated");
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
                return await UpdatePublisherAsync(ipnsCid, publisher => publisher.AccentColor = accentColor, $"Publisher icon updated");
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
                return await UpdatePublisherAsync(ipnsCid, publisher => publisher.ContactEmail = new EmailConnection(contactEmail), $"Publisher email updated to {contactEmail}");
            }
            catch (Exception ex)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
                return (Result)new UnhandledExceptionError(ex);
            }
        }
    }
}