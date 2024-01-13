using Remora.Results;

namespace WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

/// <summary>
/// An error that occurs when a user profile can't be found.
/// </summary>
public record UserProfileNotFoundError() : ResultError("The user wasn't found and may not be registered");