using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.Http;
using OwlCore.Extensions;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
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
using User = WinAppCommunity.Sdk.Models.User;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;

[Group("user")]
public class UserCommands : CommandGroup
{
    private readonly IFeedbackService _feedbackService;
    private readonly IInteractionContext _context;
    private readonly UserKeystore _userKeystore;
    private readonly IpfsClient _client;
    private readonly IDiscordRestInteractionAPI _interactionAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCommands"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="context">The context of the invoked interaction.</param>
    /// <param name="userKeystore">A keystore that stores all known user keys.</param>
    /// <param name="client">The client to use when interacting with IPFS.</param>
    public UserCommands(IInteractionContext context, IFeedbackService feedbackService, UserKeystore userKeystore, IpfsClient client, IDiscordRestInteractionAPI interactionApi)
    {
        _context = context;
        _feedbackService = feedbackService;
        _userKeystore = userKeystore;
        _client = client;
        _interactionAPI = interactionApi;
    }

    [Command("register")]
    [Description("Register yourself as a community member")]
    public async Task<IResult> RegisterUserAsync([Description("The name to display on your profile")] string name, [Description("Optional public contact email")] string? contactEmail = null)
    {
        try
        {
            var discordId = _context.Interaction.Member.Value.User.Value.ID;

            var existingUser = _userKeystore.ManagedUsers.FirstOrDefault(x => x.User.Connections.OfType<DiscordConnection>().Any(o => o.DiscordId == discordId.ToString()));
            if (existingUser is not null)
            {
                var result = (Result)new UserAlreadyRegistered();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? ThrowHelper.ThrowArgumentNullException<string>());
                return result;
            }

            // Setup connections
            // Discord connection is required for users to operate within Discord bot.
            var connections = new List<ApplicationConnection>
            {
                new DiscordConnection(discordId.ToString()),
            };

            // Add optional email
            if (!string.IsNullOrWhiteSpace(contactEmail))
                connections.Add(new EmailConnection(contactEmail));

            var embedBuilder = new EmbedBuilder()
                .WithColour(Color.YellowGreen)
                .WithTitle($"Registering user {name}")
                .WithCurrentTimestamp();

            var embeds = embedBuilder.WithDescription("Loading").Build().GetEntityOrThrowError().IntoList();
            var followUpRes = await _interactionAPI.CreateFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, embeds: new(embeds));
            var followUpMsg = followUpRes.GetEntityOrThrowError();

            // Create user
            var user = new User(name, connections.ToArray());


            // Get CID of new user object
            embeds = embedBuilder.WithDescription("Get new user CID").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            var cid = await _client.Dag.PutAsync(user);


            // Create ipns address
            embeds = embedBuilder.WithDescription("Creating ipns address").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            var key = await _client.Key.CreateKeyWithNameOfIdAsync();

            // Publish data to ipns
            embeds = embedBuilder.WithDescription("Publishing data to ipns address").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            await _client.Name.PublishAsync(cid, $"{key.Id}");

            // Save new renamed user
            embeds = embedBuilder.WithDescription("Finalizing").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
            _userKeystore.ManagedUsers.Add(new(user, key.Id));
            await _userKeystore.SaveAsync();

            return await _feedbackService.SendContextualSuccessAsync($"User registration successful. Welcome to the community, <@{discordId}>!\nIpnsCid {key.Id}");
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }

    [Command("view")]
    [Description("Displays your user profile")]
    public async Task<IResult> GetProfileAsync([Description("Enter user CID")] string ipnsCid)
    {
        try
        {
            Cid cid = ipnsCid;

            var embedBuilder = new EmbedBuilder()
                .WithColour(Color.YellowGreen)
                .WithTitle("Displays user profile")
                .WithCurrentTimestamp();
            var embeds = embedBuilder.WithDescription("Loading").Build().GetEntityOrThrowError().IntoList();
            var followUpRes = await _interactionAPI.CreateFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, embeds: new(embeds));
            var followUpMsg = followUpRes.GetEntityOrThrowError();

            var discordUser = _context.Interaction.Member.Value.User;
            var discordId = discordUser.Value.ID;

            var managedUser = _userKeystore.ManagedUsers.FirstOrDefault(x => x.IpnsCid == cid);
            if (managedUser is null)
            {
                var result = (Result)new UserNotFoundError();

                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? "User not found");
                return result;
            }

            embeds = embedBuilder.WithDescription("Resolving ipns D.A.G async").Build().GetEntityOrThrowError().IntoList();
            await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));

            var userRes = await managedUser.IpnsCid.ResolveIpnsDagAsync<User>(_client, CancellationToken.None);
            if (userRes.Result is null)
            {
                var result = (Result)new UserNotFoundError();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? "User not found");
                return result;
            }

            var user = userRes.Result;
            managedUser.User = user;

            Guard.IsNotNullOrWhiteSpace(managedUser.User.Name);

            if (!string.IsNullOrWhiteSpace(managedUser.User.MarkdownAboutMe))
                embedBuilder = embedBuilder.WithDescription(managedUser.User.MarkdownAboutMe);


            var userEmbed = user.ToEmbedBuilder(managedUser);
            userEmbed.AddField("IPNS CID", cid.ToString(), inline: true);

            embeds = userEmbed.Build().GetEntityOrThrowError().IntoList();
            return await _interactionAPI.EditFollowupMessageAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, followUpMsg.ID, embeds: new(embeds));
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }

    [Command("list")]
    [Description("Displays your user profile")]
    public async Task<IResult> GetUsersAsync([Description("The name to filter the result")] string name,
     [Description("Page number")] int number,
     [Description("Limit of the page")] int limit)
    {
        try
        {
            // TODO: WIP
            var discordUser = _context.Interaction.Member.Value.User;
            var discordId = discordUser.Value.ID;

            var managedUser = _userKeystore.ManagedUsers.ToList();
            if (managedUser is null)
            {
                var result = (Result)new UserNotFoundError();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? "User not found");
                return result;
            }


            //  managedUser.User = await managedUser.IpnsCid.ResolveIpnsDagAsync<User>(_client, CancellationToken.None);

            // Guard.IsNotNullOrWhiteSpace(managedUser.User.Name);

            //     var embedBuilder = new EmbedBuilder()
            //       .WithAuthor(managedUser.User.Name);

            //  if (!string.IsNullOrWhiteSpace(managedUser.User.MarkdownAboutMe))
            //    embedBuilder = embedBuilder.WithDescription(managedUser.User.MarkdownAboutMe);

            //  var emailConnection = managedUser.User.Connections.OfType<EmailConnection>().FirstOrDefault();
            // if (emailConnection is not null)
            // {
            //     var embedWithFieldResult = embedBuilder.AddField("Contact email", emailConnection.Email, inline: true);
            //     if (!embedWithFieldResult.IsSuccess)
            //     {
            //         await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{embedWithFieldResult.Error}");
            //         return embedWithFieldResult;
            //     }

            //     embedBuilder = embedWithFieldResult.Entity;
            // }

            // var embedBuildResult = embedBuilder.Build();
            // if (!embedBuildResult.IsSuccess)
            //     return embedBuildResult;

            return await _feedbackService.SendContextualEmbedAsync(null);
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }
}