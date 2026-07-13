using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using GzsBilling.Domain.Enums;

namespace GzsBilling.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissionsClaim = context.User.FindFirst("permissions")?.Value;

        if (string.IsNullOrEmpty(permissionsClaim))
        {
            _logger.LogWarning("No permissions claim found for user {User}",
                context.User.FindFirst(ClaimTypes.Name)?.Value);
            return Task.CompletedTask;
        }

        if (!long.TryParse(permissionsClaim, out var permissionsValue))
        {
            _logger.LogWarning("Invalid permissions claim format: {Claim}", permissionsClaim);
            return Task.CompletedTask;
        }

        var userPermissions = (Permission)permissionsValue;

        // SuperAdmin has all permissions
        if (userPermissions.HasFlag(Permission.SuperAdminAll))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check specific permission
        if (userPermissions.HasFlag(requirement.RequiredPermission))
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {User} lacks permission {Permission}. Has: {UserPermissions}",
                context.User.FindFirst(ClaimTypes.Name)?.Value,
                requirement.RequiredPermission,
                userPermissions);
        }

        return Task.CompletedTask;
    }
}
