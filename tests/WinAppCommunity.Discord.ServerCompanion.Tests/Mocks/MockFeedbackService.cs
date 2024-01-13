using OwlCore.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Feedback.Themes;
using Remora.Rest.Core;
using Remora.Results;
using System.Drawing;

namespace WinAppCommunity.Discord.ServerCompanion.Tests.Mocks;

public class MockFeedbackService : IFeedbackService
{
    public MockFeedbackService(IUser messageAuthor)
    {
        MessageAuthor = messageAuthor;
    }

    public IFeedbackTheme Theme => throw new NotImplementedException();

    public bool HasEditedOriginalMessage => false;

    public IUser MessageAuthor { get; }

    public bool HasInteractionContext() => true;

    public Task<Result<IMessage>> SendAsync(Snowflake channel, Optional<string> content = default, Optional<IReadOnlyList<IEmbed>> embeds = default, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = content.OrDefault() ?? string.Empty,
            Embeds = embeds.OrDefault() ?? new List<IEmbed>(),
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IMessage>>(message);
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendContentAsync(Snowflake channel, string contents, Color color, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IMessage>> SendContextualAsync(Optional<string> content = default, Optional<IReadOnlyList<IEmbed>> embeds = default, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = content.OrDefault() ?? string.Empty,
            Embeds = embeds.OrDefault() ?? new List<IEmbed>(),
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IMessage>>(message);
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendContextualContentAsync(string contents, Color color, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IMessage>> SendContextualEmbedAsync(Embed embed, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = string.Empty,
            Embeds = embed.IntoList(),
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IMessage>>(message);
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendContextualErrorAsync(string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendContextualInfoAsync(string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendContextualMessageAsync(FeedbackMessage message, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var mockMessage = new MockMessage
        {
            Content = message.Message,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(mockMessage.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendContextualNeutralAsync(string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendContextualSuccessAsync(string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendContextualWarningAsync(string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IMessage>> SendEmbedAsync(Snowflake channel, Embed embed, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = string.Empty,
            Embeds = embed.IntoList(),
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IMessage>>(message);
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendErrorAsync(Snowflake channel, string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendInfoAsync(Snowflake channel, string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendMessageAsync(Snowflake channel, FeedbackMessage message, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var mockMessage = new MockMessage
        {
            Content = message.Message,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(mockMessage.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendNeutralAsync(Snowflake channel, string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IMessage>> SendPrivateAsync(Snowflake user, Optional<string> content = default, Optional<IReadOnlyList<IEmbed>> embeds = default, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = content.OrDefault() ?? string.Empty,
            Embeds = embeds.OrDefault() ?? new List<IEmbed>(),
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IMessage>>(message);
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendPrivateContentAsync(Snowflake user, string contents, Color color, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IMessage>> SendPrivateEmbedAsync(Snowflake user, Embed embed, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = string.Empty,
            Embeds = embed.IntoList(),
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IMessage>>(message);
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendPrivateErrorAsync(Snowflake user, string contents, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendPrivateInfoAsync(Snowflake user, string contents, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendPrivateMessageAsync(Snowflake user, FeedbackMessage message, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var mockMessage = new MockMessage
        {
            Content = message.Message,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(mockMessage.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendPrivateNeutralAsync(Snowflake user, string contents, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendPrivateSuccessAsync(Snowflake user, string contents, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendPrivateWarningAsync(Snowflake user, string contents, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendSuccessAsync(Snowflake channel, string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }

    public Task<Result<IReadOnlyList<IMessage>>> SendWarningAsync(Snowflake channel, string contents, Snowflake? target = null, FeedbackMessageOptions? options = null, CancellationToken ct = default)
    {
        var message = new MockMessage
        {
            Content = contents,
            Author = MessageAuthor,
        };

        return Task.FromResult<Result<IReadOnlyList<IMessage>>>(message.IntoList());
    }
}
