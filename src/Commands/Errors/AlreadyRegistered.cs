using Remora.Results;

namespace WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

/// <summary>
/// An error that occurs when a user is already registered.
/// </summary>
public record UserAlreadyRegistered() : ResultError("The user is already registered");

/// <summary>
/// An error that occurs when a publisher is already registered.
/// </summary>
public record PublisherAlreadyRegistered() : ResultError("The publisher is already registered");

/// <summary>
/// An error that occurs when a Project is already registered.
/// </summary>
public record ProjectAlreadyRegistered() : ResultError("The Project is already registered");