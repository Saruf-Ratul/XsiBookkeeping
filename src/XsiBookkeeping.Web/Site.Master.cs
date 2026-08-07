using System;
using System.Text;
using System.Web;
using System.Web.UI;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web
{
    public partial class SiteMaster : MasterPage
    {
        protected string PermissionsJson = "{}";
        protected string RoleBadge = "";
        protected string ApiUrl = "";
        protected string LogoUrl = "";
        protected string CurrentUserJs = "";
        protected string RoleJs = "";
        protected bool ShowUsersNav;
        protected bool ShowAuditNav;
        protected bool ShowAssignmentsNav;

        protected void Page_Load(object sender, EventArgs e)
        {
            ApiUrl = ResolveUrl("~/Handlers/Api.ashx");
            LogoUrl = ResolveUrl("~/Assets/images/xceleran-ledger-logo.png");
            var identityName = Context.User != null && Context.User.Identity != null
                ? Context.User.Identity.Name
                : "";
            CurrentUserJs = HttpUtility.JavaScriptStringEncode(identityName);
            RoleJs = HttpUtility.JavaScriptStringEncode(RoleBadge);
            UserDisplayLiteral.Text = HttpUtility.HtmlEncode(identityName);

            var path = Request.AppRelativeCurrentExecutionFilePath ?? "";
            NavOverview.CssClass = "ledger-nav-link" + (path.IndexOf("Overview", StringComparison.OrdinalIgnoreCase) >= 0 ? " active" : "");
            NavTasks.CssClass = "ledger-nav-link" + (path.IndexOf("Tasks", StringComparison.OrdinalIgnoreCase) >= 0 ? " active" : "");
            NavReport.CssClass = "ledger-nav-link" + (path.IndexOf("Report", StringComparison.OrdinalIgnoreCase) >= 0 ? " active" : "");

            var user = UserContextService.GetCurrent(Context);
            if (user != null)
            {
                RoleBadge = PermissionMatrix.RoleName(user.Role);
                RoleJs = HttpUtility.JavaScriptStringEncode(RoleBadge);
                var display = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.WindowsLogin;
                UserDisplayLiteral.Text = HttpUtility.HtmlEncode(display);
                CurrentUserJs = HttpUtility.JavaScriptStringEncode(display);
                ShowUsersNav = PermissionMatrix.Can(user.Role, Permission.ManageUsers);
                ShowAuditNav = PermissionMatrix.Can(user.Role, Permission.ViewAudit);
                ShowAssignmentsNav = PermissionMatrix.Can(user.Role, Permission.ManageCompanies);
                PermissionsJson = BuildPermissionsJson(user.Role);
                NavUsers.Visible = ShowUsersNav;
                NavAudit.Visible = ShowAuditNav;
                NavAssignments.Visible = ShowAssignmentsNav;
                RoleBadgeLiteral.Text = $"<span class=\"ledger-role-badge\">{HttpUtility.HtmlEncode(RoleBadge)}</span>";
                RoleBadgePanel.Visible = true;
            }
            else
            {
                RoleBadgePanel.Visible = false;
                NavUsers.Visible = false;
                NavAudit.Visible = false;
                NavAssignments.Visible = false;
            }

            if (ShowAssignmentsNav)
                NavAssignments.CssClass = "ledger-nav-link" + (path.IndexOf("Assignments", StringComparison.OrdinalIgnoreCase) >= 0 ? " active" : "");

            if (ShowUsersNav)
                NavUsers.CssClass = "ledger-nav-link" + (path.IndexOf("Users", StringComparison.OrdinalIgnoreCase) >= 0 ? " active" : "");

            if (ShowAuditNav)
                NavAudit.CssClass = "ledger-nav-link" + (path.IndexOf("AuditLog", StringComparison.OrdinalIgnoreCase) >= 0 ? " active" : "");
        }

        private static string BuildPermissionsJson(AppRole role)
        {
            var sb = new StringBuilder("{");
            sb.Append($"\"reconcile\":{(PermissionMatrix.Can(role, Permission.Reconcile) ? "true" : "false")},");
            sb.Append($"\"comment\":{(PermissionMatrix.Can(role, Permission.Comment) ? "true" : "false")},");
            sb.Append($"\"deleteAnyComment\":{(PermissionMatrix.Can(role, Permission.DeleteAnyComment) ? "true" : "false")},");
            sb.Append($"\"manageCompanies\":{(PermissionMatrix.Can(role, Permission.ManageCompanies) ? "true" : "false")},");
            sb.Append($"\"manageUsers\":{(PermissionMatrix.Can(role, Permission.ManageUsers) ? "true" : "false")}");
            sb.Append("}");
            return sb.ToString();
        }
    }
}
