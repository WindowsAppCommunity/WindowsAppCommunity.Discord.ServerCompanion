using Remora.Results;

namespace WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

/// <summary>
/// An error that occurs when a user profile can't be found.
/// </summary>
public record UserAlreadyRegistered() : ResultError("The user is already registered");