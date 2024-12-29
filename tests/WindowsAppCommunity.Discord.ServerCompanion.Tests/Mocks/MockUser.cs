using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using System.Drawing;

namespace WindowsAppCommunity.Discord.ServerCompanion.Tests.Mocks;

public class MockGuildMember : IGuildMember
{
    public required Optional<IUser> User { get; set; }

    public Optional<string?> Nickname { get; set; }

    public Optional<IImageHash?> Avatar { get; set; }

    public IReadOnlyList<Snowflake> Roles { get; set; } = new List<Snowflake>();

    public DateTimeOffset JoinedAt { get; set; }

    public Optional<DateTimeOffset?> PremiumSince { get; set; }

    public bool IsDeafened { get; set; }

    public bool IsMuted { get; set; }

    public GuildMemberFlags Flags { get; set; }

    public Optional<bool?> IsPending { get; set; }

    public Optional<IDiscordPermissionSet> Permissions { get; set; }

    public Optional<DateTimeOffset?> CommunicationDisabledUntil { get; set; }
}

public class MockUser : IUser
{
    public required Snowflake ID { get; set; }

    public string Username { get; set; } = nameof(MockUser);

    public ushort Discriminator { get; set; }

    public IImageHash? Avatar { get; set; }

    public Optional<bool> IsBot { get; set; }

    public Optional<bool> IsSystem { get; set; }

    public Optional<bool> IsMFAEnabled { get; set; }

    public Optional<IImageHash?> Banner { get; set; }

    public Optional<Color?> AccentColour { get; set; }

    public Optional<string> Locale { get; set; }

    public Optional<bool> IsVerified { get; set; }

    public Optional<string?> Email { get; set; }

    public Optional<UserFlags> Flags { get; set; }

    public Optional<PremiumType> PremiumType { get; set; }

    public Optional<UserFlags> PublicFlags { get; set; }

    public Optional<IImageHash?> AvatarDecoration { get; set; }
}
