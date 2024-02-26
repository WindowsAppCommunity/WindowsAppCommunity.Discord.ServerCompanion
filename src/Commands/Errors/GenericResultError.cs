using Remora.Results;

namespace WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

public record GenericResultError(string message) : ResultError(message);