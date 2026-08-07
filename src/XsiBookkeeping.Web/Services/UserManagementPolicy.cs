using System.Collections.Generic;
using System.Linq;
using XsiBookkeeping.Web.Models;

namespace XsiBookkeeping.Web.Services
{
    public static class UserManagementPolicy
    {
        private static readonly AppRole[] SysadminAssignable = { AppRole.User, AppRole.Admin, AppRole.Sysadmin };
        private static readonly AppRole[] AdminAssignable = { AppRole.User, AppRole.Admin };

        public static IReadOnlyList<AppRole> AssignableRoles(AppRole actor)
        {
            return actor == AppRole.Sysadmin
                ? SysadminAssignable
                : AdminAssignable;
        }

        public static bool CanViewUser(AppRole actor, AppRole targetRole)
        {
            if (actor == AppRole.Sysadmin) return true;
            return targetRole != AppRole.Sysadmin;
        }

        public static bool CanAssignRole(AppRole actor, AppRole role)
        {
            return AssignableRoles(actor).Contains(role);
        }

        public static bool CanModifyUser(AppRole actor, AppRole targetRole, AppRole newRole)
        {
            if (!CanViewUser(actor, targetRole)) return false;
            if (!CanAssignRole(actor, newRole)) return false;
            return true;
        }
    }
}
