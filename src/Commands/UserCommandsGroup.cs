using CommunityToolkit.Diagnostics;
using Ipfs.Http;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Extensions.Embeds;
using Remora.Results;
using System.ComponentModel;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCommands"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="context">The context of the invoked interaction.</param>
    /// <param name="userKeystore">A keystore that stores all known user keys.</param>
    /// <param name="client">The client to use when interacting with IPFS.</param>
    public UserCommands(IInteractionContext context, IFeedbackService feedbackService, UserKeystore userKeystore, IpfsClient client)
    {
        _context = context;
        _feedbackService = feedbackService;
        _userKeystore = userKeystore;
        _client = client;
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

            // Create user
            var user = new User(name, connections.ToArray());

            // Get CID of new user object
            var cid = await _client.Dag.PutAsync(user);

            // Create ipns address
            // Use name "temp" to create key
            var key = await _client.Key.CreateAsync(name: "temp", "ed25519", 4096);

            // Rename key name to the key id
            var renamed = await _client.Key.RenameAsync("temp", $"{key.Id}");

            // Publish data to ipns
            await _client.Name.PublishAsync(cid, $"{renamed.Id}");

            // Save new renamed
            _userKeystore.ManagedUsers.Add(new(user, key.Id));
            await _userKeystore.SaveAsync();

            return (Result)await _feedbackService.SendContextualSuccessAsync($"User registration successful. Welcome to the community, <@{discordId}>!");
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }

    [Command("get")]
    [Description("Displays your user profile")]
    public async Task<IResult> GetUserProfileAsync([AutocompleteProvider("autocomplete::users")] string userName)
    {
        try
        {
            var managedUser = _userKeystore.ManagedUsers.FirstOrDefault(x => x.User.Name == userName);
            if (managedUser is null)
            {
                var result = (Result)new UserProfileNotFoundError();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? "User not found");
                return result;
            }

            managedUser.User = await managedUser.IpnsCid.ResolveIpnsDagAsync<User>(_client, CancellationToken.None);

            Guard.IsNotNullOrWhiteSpace(managedUser.User.Name);

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(managedUser.User.Name);

            if (!string.IsNullOrWhiteSpace(managedUser.User.MarkdownAboutMe))
                embedBuilder = embedBuilder.WithDescription(managedUser.User.MarkdownAboutMe);

            if (managedUser.User.Connections.OfType<EmailConnection>().FirstOrDefault() is EmailConnection emailConnection)
            {
                var embedWithFieldResult = embedBuilder.AddField("Contact email", emailConnection.Email, inline: true);
                if (!embedWithFieldResult.IsSuccess)
                {
                    await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{embedWithFieldResult.Error}");
                    return embedWithFieldResult;
                }

                embedBuilder = embedWithFieldResult.Entity;
            }

            var embedBuildResult = embedBuilder.Build();
            if (!embedBuildResult.IsSuccess)
                return embedBuildResult;

            return await _feedbackService.SendContextualEmbedAsync(embedBuildResult.Entity);
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }

    [Command("view")]
    [Description("Displays your user profile")]
    public async Task<IResult> GetProfileAsync([AutocompleteProvider("autocomplete::cid")] string cid)
    {
        try
        {
            var discordUser = _context.Interaction.Member.Value.User;
            var discordId = discordUser.Value.ID;

            var managedUser = _userKeystore.ManagedUsers.FirstOrDefault(x => x.IpnsCid == cid);
            if (managedUser is null)
            {
                var result = (Result)new UserProfileNotFoundError();
                await _feedbackService.SendContextualErrorAsync(result.Error?.Message ?? "User not found");
                return result;
            }

            managedUser.User = await managedUser.IpnsCid.ResolveIpnsDagAsync<User>(_client, CancellationToken.None);

            Guard.IsNotNullOrWhiteSpace(managedUser.User.Name);

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(managedUser.User.Name);

            if (!string.IsNullOrWhiteSpace(managedUser.User.MarkdownAboutMe))
                embedBuilder = embedBuilder.WithDescription(managedUser.User.MarkdownAboutMe);

            var emailConnection = managedUser.User.Connections.OfType<EmailConnection>().FirstOrDefault();
            if (emailConnection is not null)
            {
                var embedWithFieldResult = embedBuilder.AddField("Contact email", emailConnection.Email, inline: true);
                if (!embedWithFieldResult.IsSuccess)
                {
                    await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{embedWithFieldResult.Error}");
                    return embedWithFieldResult;
                }

                embedBuilder = embedWithFieldResult.Entity;
            }

            var embedBuildResult = embedBuilder.Build();
            if (!embedBuildResult.IsSuccess)
                return embedBuildResult;

            return await _feedbackService.SendContextualEmbedAsync(embedBuildResult.Entity);
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }
}