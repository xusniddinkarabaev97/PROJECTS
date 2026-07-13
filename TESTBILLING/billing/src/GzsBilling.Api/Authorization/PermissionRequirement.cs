using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using GzsBilling.Domain.Enums;

namespace GzsBilling.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public Permission RequiredPermission { get; }

    public PermissionRequirement(Permission permission)
    {
        RequiredPermission = permission;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    public Permission Permission { get; }

    public RequirePermissionAttribute(Permission permission)
    {
        Permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        var permissionsClaim = user.FindFirst("permissions")?.Value;

        if (string.IsNullOrEmpty(permissionsClaim))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!long.TryParse(permissionsClaim, out var permissionsValue))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userPermissions = (Permission)permissionsValue;

        if (!userPermissions.HasFlag(Permission) && !userPermissions.HasFlag(Permission.SuperAdminAll))
        {
            context.Result = new ForbidResult();
        }
    }
}
