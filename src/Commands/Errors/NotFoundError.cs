using Remora.Results;

namespace WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

/// <summary>
/// An error that occurs when a user can't be found.
/// </summary>
public record UserNotFoundError() : ResultError("The user wasn't found and may not be registered");

/// <summary>
/// An error that occurs when a Project can't be found.
/// </summary>
public record ProjectNotFoundError() : ResultError("The Project wasn't found and may not be registered");

/// <summary>
/// An error that occurs when a Publisher can't be found.
/// </summary>
public record PublisherNotFoundError() : ResultError("The Publisher wasn't found and may not be registered");
