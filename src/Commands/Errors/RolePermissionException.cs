namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;

/// <summary>
/// An exception that occurs when a user does not have the required roles to execute a command.
/// </summary>
public class RolePermissionException : Exception
{
    /// <summary>
    /// Gets the role names that were required.
    /// </summary>
    public string[] RequiredRoles { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RolePermissionException"/> class.
    /// </summary>
    /// <param name="requiredRoles">The role names that were required.</param>
    public RolePermissionException(params string[] requiredRoles)
        : base($"You do not have permission to use this command. Required role(s): {string.Join(", ", requiredRoles)}")
    {
        RequiredRoles = requiredRoles;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RolePermissionException"/> class with a custom message.
    /// </summary>
    /// <param name="message">A custom error message.</param>
    /// <param name="requiredRoles">The role names that were required.</param>
    public RolePermissionException(string message, params string[] requiredRoles)
        : base($"{message} Required role(s): {string.Join(", ", requiredRoles)}")
    {
        RequiredRoles = requiredRoles;
    }
}
