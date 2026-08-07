using System.Collections.Generic;
using XsiBookkeeping.Web.Models;

namespace XsiBookkeeping.Web.Services
{
    public static class PermissionMatrix
    {
        private static readonly Dictionary<AppRole, HashSet<Permission>> RolePermissions = new Dictionary<AppRole, HashSet<Permission>>
        {
            {
                AppRole.User, new HashSet<Permission>
                {
                    Permission.ViewApp,
                    Permission.Reconcile,
                    Permission.Comment
                }
            },
            {
                AppRole.Admin, new HashSet<Permission>
                {
                    Permission.ViewApp,
                    Permission.Reconcile,
                    Permission.Comment,
                    Permission.DeleteAnyComment,
                    Permission.ManageCompanies
                }
            },
            {
                AppRole.Sysadmin, new HashSet<Permission>
                {
                    Permission.ViewApp,
                    Permission.Reconcile,
                    Permission.Comment,
                    Permission.DeleteAnyComment,
                    Permission.ManageCompanies,
                    Permission.ManageUsers,
                    Permission.ViewAudit
                }
            }
        };

        public static bool Can(AppRole role, Permission permission)
        {
            return RolePermissions.TryGetValue(role, out var perms) && perms.Contains(permission);
        }

        public static AppRole ParseRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return AppRole.User;
            switch (role.Trim())
            {
                case "Sysadmin": return AppRole.Sysadmin;
                case "Admin": return AppRole.Admin;
                default: return AppRole.User;
            }
        }

        public static string RoleName(AppRole role)
        {
            return role.ToString();
        }
    }
}
