using Ipfs.Http;
using OwlCore.Kubo;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Remora.Discord.Commands.Services;
using Remora.Discord.Extensions.Embeds;
using WinAppCommunity.Discord.ServerCompanion.Commands;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Commands;

[Group("user")]
public class UserCommands : CommandGroup
{
    private readonly FeedbackService _feedbackService;
    private readonly UserKeystore _userKeystore;
    private readonly IpfsClient _client;
    private readonly IInteractionContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCommands"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="context">The context of the invoked interaction.</param>
    public UserCommands(IInteractionContext context, FeedbackService feedbackService, UserKeystore userKeystore, IpfsClient client)
    {
        _context = context;
        _feedbackService = feedbackService;
        _userKeystore = userKeystore;
        _client = client;
    }

    [Command("register")]
    [Description("Register yourself as a community member")]
    public async Task<IResult> RegisterUserAsync(string name, string? contactEmail)
    {
        try
        {
            var discordId = _context.Interaction.Member.Value.User.Value.ID;

            // Setup connections
            // Discord connection is required for operation within Discord bot.
            var connections = new List<ApplicationConnection>
            {
                new DiscordConnection(discordId.ToString()),
            };

            if (!string.IsNullOrWhiteSpace(contactEmail))
                connections.Add(new EmailConnection(contactEmail));

            // Create user
            var user = new User(name, connections);

            // Get CID of new user object
            var cid = await user.GetCidAsync(_client, CancellationToken.None);

            // Create ipns address
            // Use name "temp" to create key
            var key = await _client.Key.CreateAsync(name: "temp", "ed25519", 4096);

            var peer = new Peer { Id = key.Id };
            // Rename key name to the key id
            var renamed = await _client.Key.RenameAsync("temp", $"{peer.PublicKey}");
            
            // Publish data to ipns
            await _client.Name.PublishAsync(cid, $"{renamed.Id}");

            // Save new renamed
            _userKeystore.ManagedUsers.Add(new(user, $"{renamed.Id}"));
            await _userKeystore.SaveAsync();

            return (Result)await _feedbackService.SendContextualInfoAsync("Ran to completion");

        }
        catch (Exception ex)
        {
            return (Result)await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
        }
    }

    [Command("profile")]
    [Description("Displays your user profile")]
    public async Task<IResult> GetProfileAsync()
    {
        var discordUser = _context.Interaction.Member.Value.User;
        var discordId = discordUser.Value.ID;

        var managedUser = _userKeystore.ManagedUsers.FirstOrDefault(x =>
            x.User.Connections.Any(o => o is DiscordConnection discordConnection && discordConnection.discordId == $"{discordId}"));

        if (managedUser is null)
        {
            return (Result)await _feedbackService.SendContextualErrorAsync("You're not registered");
        }

        managedUser.User = await managedUser.IpnsCid.ResolveIpnsDagAsync<User>(_client, CancellationToken.None);

        Guard.IsNotNullOrWhiteSpace(managedUser.User.Name);

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(managedUser.User.Name);

        if (!string.IsNullOrWhiteSpace(managedUser.User.MarkdownAboutMe))
            embedBuilder = embedBuilder.WithDescription(managedUser.User.MarkdownAboutMe);

        return (Result)await _feedbackService.SendContextualEmbedAsync(embedBuilder.Build().Entity);
    }
}

/// <summary>
/// Represents a single user managed by the community.
/// </summary>
/// <param name="User">The resolved user data.</param>
/// <param name="IpnsCid">A CID that can be used to resolve the user data</param>
public record ManagedUserMap(User User, Cid IpnsCid)
{
    public User User { get; set; } = User;
}