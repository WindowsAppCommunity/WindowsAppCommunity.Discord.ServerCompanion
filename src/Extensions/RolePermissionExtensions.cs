using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// Extension methods for role-based permission checks.
/// </summary>
public static class RolePermissionExtensions
{
    /// <summary>
    /// Checks if the user with the given ID has any of the required roles in the specified guild.
    /// Throws <see cref="RolePermissionException"/> if the user does not have any of the required roles.
    /// </summary>
    /// <param name="userId">The user's Discord ID.</param>
    /// <param name="guildId">The guild's Discord ID.</param>
    /// <param name="guildApi">The Discord REST API for guild operations.</param>
    /// <param name="requiredRoleNames">The names of roles that grant permission (user needs at least one).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="RolePermissionException">Thrown when the user does not have any of the required roles.</exception>
    public static async Task RequireRolesAsync(
        this Snowflake userId, 
        Snowflake guildId, 
        IDiscordRestGuildAPI guildApi, 
        string[] requiredRoleNames,
        CancellationToken cancellationToken = default)
    {
        // Get all roles in the guild
        var guildRolesResult = await guildApi.GetGuildRolesAsync(guildId, cancellationToken);
        if (!guildRolesResult.IsSuccess)
            throw new RolePermissionException(requiredRoleNames);

        var guildRoles = guildRolesResult.Entity;
        
        // Find the role IDs that match the required role names (case-insensitive)
        var requiredRoleIds = guildRoles
            .Where(r => requiredRoleNames.Any(name => 
                string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Select(r => r.ID)
            .ToHashSet();

        if (requiredRoleIds.Count == 0)
        {
            // None of the required roles exist in this guild
            throw new RolePermissionException(requiredRoleNames);
        }

        // Get the guild member to check their roles
        var memberResult = await guildApi.GetGuildMemberAsync(guildId, userId, cancellationToken);
        if (!memberResult.IsSuccess)
            throw new RolePermissionException(requiredRoleNames);

        var member = memberResult.Entity;
        
        // Check if the user has any of the required roles
        var userRoleIds = member.Roles.ToHashSet();
        var hasRequiredRole = userRoleIds.Overlaps(requiredRoleIds);

        if (!hasRequiredRole)
            throw new RolePermissionException(requiredRoleNames);
    }
}
