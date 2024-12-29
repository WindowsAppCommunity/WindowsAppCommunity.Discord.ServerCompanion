using Remora.Results;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;

public record GenericResultError(string message) : ResultError(message);