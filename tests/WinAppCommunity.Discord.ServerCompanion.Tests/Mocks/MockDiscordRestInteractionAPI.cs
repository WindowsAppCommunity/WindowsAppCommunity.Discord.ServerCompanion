using CommunityToolkit.Diagnostics;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace WinAppCommunity.Discord.ServerCompanion.Tests.Mocks;

public class MockDiscordRestInteractionAPI : IDiscordRestInteractionAPI
{
    private readonly List<IMessage> _messages = new();

    public Task<Result> CreateInteractionResponseAsync(Snowflake interactionID, string interactionToken, IInteractionResponse response,
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = new(), CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task<Result<IMessage>> GetOriginalInteractionResponseAsync(Snowflake applicationID, string interactionToken,
        CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task<Result<IMessage>> EditOriginalInteractionResponseAsync(Snowflake applicationID, string token, Optional<string?> content = new(),
        Optional<IReadOnlyList<IEmbed>?> embeds = new(), Optional<IAllowedMentions?> allowedMentions = new(), Optional<IReadOnlyList<IMessageComponent>?> components = new(),
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>?> attachments = new(), CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task<Result> DeleteOriginalInteractionResponseAsync(Snowflake applicationID, string token,
        CancellationToken ct = new())
    {
        throw new NotImplementedException();
    }

    public Task<Result<IMessage>> CreateFollowupMessageAsync(Snowflake applicationID, string token, Optional<string> content = new(),
        Optional<bool> isTTS = new(), Optional<IReadOnlyList<IEmbed>> embeds = new(), Optional<IAllowedMentions> allowedMentions = new(),
        Optional<IReadOnlyList<IMessageComponent>> components = new(), Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = new(), Optional<MessageFlags> flags = new(),
        CancellationToken ct = new())
    {
        var message = new MockMessage
        {
            Content = content.OrDefault() ?? string.Empty,
            Embeds = embeds.OrDefault() ?? new List<IEmbed>(),
            Author = new MockUser() { ID = applicationID },
        };

        _messages.Add(message);

        return Task.FromResult<Result<IMessage>>(message);
    }

    public Task<Result<IMessage>> GetFollowupMessageAsync(Snowflake applicationID, string token, Snowflake messageID,
        CancellationToken ct = new())
    {
        var targetMsg = _messages.FirstOrDefault(x => x.ID == messageID && x.ApplicationID == applicationID);
        Guard.IsNotNull(targetMsg);

        return Task.FromResult(Result<IMessage>.FromSuccess(targetMsg));
    }

    public Task<Result<IMessage>> EditFollowupMessageAsync(Snowflake applicationID, string token, Snowflake messageID,
        Optional<string?> content = new(), Optional<IReadOnlyList<IEmbed>?> embeds = new(), Optional<IAllowedMentions?> allowedMentions = new(),
        Optional<IReadOnlyList<IMessageComponent>?> components = new(), Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>?> attachments = new(),
        CancellationToken ct = new())
    {
        var targetMsg = _messages.FirstOrDefault(x => x.ID == messageID && x.ApplicationID == applicationID);
        Guard.IsNotNull(targetMsg);

        if (content is { HasValue: true, Value: not null })
            ((MockMessage)targetMsg).Content = content.Value;

        if (embeds is { HasValue: true, Value: not null })
            ((MockMessage)targetMsg).Embeds = embeds.Value;

        return Task.FromResult(Result<IMessage>.FromSuccess(targetMsg));
    }

    public Task<Result> DeleteFollowupMessageAsync(Snowflake applicationID, string token, Snowflake messageID,
        CancellationToken ct = new())
    {
        var targetMsg = _messages.FirstOrDefault(x => x.ID == messageID && x.ApplicationID == applicationID);
        Guard.IsNotNull(targetMsg);

        _messages.Remove(targetMsg);

        return Task.FromResult(Result.FromSuccess());
    }
}