using Remora.Results;
using WindowsAppCommunity.Sdk.Models;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;

/// <summary>
/// An error that occurs when a user can't be found.
/// </summary>
public record UserNotFoundError(string? secondaryMsg = null) : NotFoundError(nameof(User), secondaryMsg);

/// <summary>
/// An error that occurs when a Project can't be found.
/// </summary>
public record ProjectNotFoundError(string? secondaryMsg = null) : NotFoundError(nameof(Project), secondaryMsg);

/// <summary>
/// An error that occurs when a Publisher can't be found.
/// </summary>
public record PublisherNotFoundError(string? secondaryMsg = null) : NotFoundError(nameof(Publisher), secondaryMsg);
    
/// <summary>
/// An error that occurs when some data can't be found.
/// </summary>
/// <param name="notFoundName"></param>
public record NotFoundError(string notFoundName, string? secondaryMsg = null)  : ResultError($"The {notFoundName} wasn't found. {secondaryMsg}");