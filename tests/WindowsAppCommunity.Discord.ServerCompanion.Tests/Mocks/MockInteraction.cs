using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace WindowsAppCommunity.Discord.ServerCompanion.Tests.Mocks;

public class MockInteraction : IInteraction
{
    public Snowflake ID { get; set; }

    public Snowflake ApplicationID { get; set; }

    public InteractionType Type { get; set; }

    public Optional<OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>> Data { get; set; }

    public Optional<Snowflake> GuildID { get; set; }

    public Optional<IPartialChannel> Channel { get; set; }

    public Optional<Snowflake> ChannelID { get; set; }

    public Optional<IGuildMember> Member { get; set; }

    public Optional<IUser> User { get; set; }

    public string Token { get; set; } = Guid.NewGuid().ToString();

    public int Version { get; set; }

    public Optional<IMessage> Message { get; set; }

    public Optional<IDiscordPermissionSet> AppPermissions { get; set; }

    public Optional<string> Locale { get; set; }

    public Optional<string> GuildLocale { get; set; }

    public IReadOnlyList<IEntitlement> Entitlements { get; set; } = new List<IEntitlement>();
}
