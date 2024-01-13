using Remora.Results;

namespace WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

public record UnhandledExceptionError(Exception ex) : ResultError($"{ex}");
