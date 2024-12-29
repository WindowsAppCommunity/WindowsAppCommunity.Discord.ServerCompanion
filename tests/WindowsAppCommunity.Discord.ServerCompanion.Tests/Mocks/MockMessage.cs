using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace WindowsAppCommunity.Discord.ServerCompanion.Tests.Mocks;

public class MockMessage : IMessage
{
    public Snowflake ID { get; } = Snowflake.CreateTimestampSnowflake();

    public Snowflake ChannelID => throw new NotImplementedException();

    public required IUser Author { get; init; }

    public required string Content { get; set; }

    public DateTimeOffset Timestamp => throw new NotImplementedException();

    public DateTimeOffset? EditedTimestamp => throw new NotImplementedException();

    public bool IsTTS => throw new NotImplementedException();

    public bool MentionsEveryone => throw new NotImplementedException();

    public IReadOnlyList<IUser> Mentions { get; } = new List<IUser>();

    public IReadOnlyList<Snowflake> MentionedRoles { get; } = new List<Snowflake>();

    public Optional<IReadOnlyList<IChannelMention>> MentionedChannels { get; } = new List<IChannelMention>();

    public IReadOnlyList<IAttachment> Attachments { get; } = new List<IAttachment>();

    public IReadOnlyList<IEmbed> Embeds { get; set; } = new List<IEmbed>();

    public Optional<IReadOnlyList<IReaction>> Reactions { get; } = new List<IReaction>();

    public Optional<string> Nonce => throw new NotImplementedException();

    public bool IsPinned => throw new NotImplementedException();

    public Optional<Snowflake> WebhookID => throw new NotImplementedException();

    public MessageType Type { get; init; } = MessageType.Default;

    public Optional<IMessageActivity> Activity => throw new NotImplementedException();

    public Optional<IPartialApplication> Application => throw new NotImplementedException();

    public Optional<Snowflake> ApplicationID { get; } = new Snowflake(Guid.Empty.ToByteArray().Aggregate((x, y) => (byte)(x + y)));

    public Optional<IMessageReference> MessageReference => throw new NotImplementedException();

    public Optional<MessageFlags> Flags { get; init; }

    public Optional<IMessage?> ReferencedMessage => throw new NotImplementedException();

    public Optional<IMessageInteraction> Interaction => throw new NotImplementedException();

    public Optional<IChannel> Thread => throw new NotImplementedException();

    public Optional<IReadOnlyList<IMessageComponent>> Components => throw new NotImplementedException();

    public Optional<IReadOnlyList<IStickerItem>> StickerItems => throw new NotImplementedException();

    public Optional<int> Position => throw new NotImplementedException();

    public Optional<IApplicationCommandInteractionDataResolved> Resolved => throw new NotImplementedException();
}
