using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace WinAppCommunity.Discord.ServerCompanion.Tests.Mocks;

public class MockMessage : IMessage
{
    public Snowflake ID { get; } = Snowflake.CreateTimestampSnowflake();

    public Snowflake ChannelID => throw new NotImplementedException();

    public IUser Author => throw new NotImplementedException();

    public string Content => throw new NotImplementedException();

    public DateTimeOffset Timestamp => throw new NotImplementedException();

    public DateTimeOffset? EditedTimestamp => throw new NotImplementedException();

    public bool IsTTS => throw new NotImplementedException();

    public bool MentionsEveryone => throw new NotImplementedException();

    public IReadOnlyList<IUser> Mentions => throw new NotImplementedException();

    public IReadOnlyList<Snowflake> MentionedRoles => throw new NotImplementedException();

    public Optional<IReadOnlyList<IChannelMention>> MentionedChannels => throw new NotImplementedException();

    public IReadOnlyList<IAttachment> Attachments => throw new NotImplementedException();

    public IReadOnlyList<IEmbed> Embeds => throw new NotImplementedException();

    public Optional<IReadOnlyList<IReaction>> Reactions => throw new NotImplementedException();

    public Optional<string> Nonce => throw new NotImplementedException();

    public bool IsPinned => throw new NotImplementedException();

    public Optional<Snowflake> WebhookID => throw new NotImplementedException();

    public MessageType Type => throw new NotImplementedException();

    public Optional<IMessageActivity> Activity => throw new NotImplementedException();

    public Optional<IPartialApplication> Application => throw new NotImplementedException();

    public Optional<Snowflake> ApplicationID => throw new NotImplementedException();

    public Optional<IMessageReference> MessageReference => throw new NotImplementedException();

    public Optional<MessageFlags> Flags => throw new NotImplementedException();

    public Optional<IMessage?> ReferencedMessage => throw new NotImplementedException();

    public Optional<IMessageInteraction> Interaction => throw new NotImplementedException();

    public Optional<IChannel> Thread => throw new NotImplementedException();

    public Optional<IReadOnlyList<IMessageComponent>> Components => throw new NotImplementedException();

    public Optional<IReadOnlyList<IStickerItem>> StickerItems => throw new NotImplementedException();

    public Optional<int> Position => throw new NotImplementedException();

    public Optional<IApplicationCommandInteractionDataResolved> Resolved => throw new NotImplementedException();
}
