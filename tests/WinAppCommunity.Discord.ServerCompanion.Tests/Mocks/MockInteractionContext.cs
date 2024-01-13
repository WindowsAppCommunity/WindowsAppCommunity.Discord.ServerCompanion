using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;

namespace WinAppCommunity.Discord.ServerCompanion.Tests.Mocks;

public class MockInteractionContext : IInteractionContext
{
    public bool HasRespondedToInteraction { get; set; }
    public bool IsOriginalEphemeral { get; set; }

    public required IInteraction Interaction  { get; init; }
}
