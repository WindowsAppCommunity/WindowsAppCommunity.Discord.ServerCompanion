using Remora.Results;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;

public record UnhandledExceptionError(Exception ex) : ResultError($"{ex}");
