using GzsBilling.Domain.Enums;

namespace GzsBilling.Api.Authorization;

public static class RolePermissions
{
    public static readonly Dictionary<SystemRole, Permission> Map = new()
    {
        [SystemRole.SuperAdmin] = Permission.SuperAdminAll,
        [SystemRole.Admin] = Permission.AdminAll,
        [SystemRole.Manager] = Permission.ManagerPermissions,
        [SystemRole.Operator] = Permission.OperatorPermissions,
        [SystemRole.Shareholder] = Permission.ShareholderPermissions,
        [SystemRole.ReadOnly] = Permission.ReadOnlyPermissions,
    };

    public static Permission GetPermissions(SystemRole role)
    {
        return Map.GetValueOrDefault(role, Permission.None);
    }

    public static bool HasPermission(SystemRole role, Permission permission)
    {
        var rolePermissions = GetPermissions(role);
        return rolePermissions.HasFlag(permission);
    }
}
