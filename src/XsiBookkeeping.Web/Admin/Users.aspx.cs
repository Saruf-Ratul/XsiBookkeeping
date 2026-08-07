using System.Text;
using XsiBookkeeping.Web;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web.Admin
{
    public partial class UsersPage : AdminPageBase
    {
        private readonly UserRepository _userRepo = new UserRepository();

        protected void Page_Load(object sender, System.EventArgs e)
        {
            ContentLiteral.Text = BuildHtml();
        }

        private string BuildHtml()
        {
            var users = _userRepo.GetAll();
            var sb = new StringBuilder();
            sb.Append("<div class=\"ledger-container-wide fade-in\">");
            sb.Append("<div style=\"margin-bottom:24px\">");
            sb.Append("<div class=\"ledger-kicker\">Administration</div>");
            sb.Append("<h1 class=\"ledger-title\">Users</h1>");
            sb.Append("</div>");

            sb.Append("<div class=\"admin-card add-company-box\" style=\"margin-bottom:20px\">");
            sb.Append("<div class=\"admin-form-title\">Add user</div>");
            sb.Append("<div class=\"admin-form-row\">");
            sb.Append("<input class=\"ledger-input\" id=\"new-windows-login\" placeholder=\"Username\" style=\"min-width:160px;border-color:#e8e4dc\" />");
            sb.Append("<input class=\"ledger-input\" id=\"new-display-name\" placeholder=\"Display name (optional)\" style=\"min-width:180px;border-color:#e8e4dc\" />");
            sb.Append("<input class=\"ledger-input\" id=\"new-password\" type=\"password\" placeholder=\"Password\" style=\"min-width:160px;border-color:#e8e4dc\" />");
            sb.Append("<select class=\"ledger-input\" id=\"new-role\" style=\"width:auto;border-color:#e8e4dc\">");
            sb.Append("<option value=\"User\">User</option>");
            sb.Append("<option value=\"Admin\">Admin</option>");
            sb.Append("<option value=\"Sysadmin\">Sysadmin</option>");
            sb.Append("</select>");
            sb.Append("<button type=\"button\" class=\"ledger-btn ledger-btn-primary\" data-action=\"admin-add-user\">Add User</button>");
            sb.Append("</div></div>");

            sb.Append("<div class=\"admin-card\">");
            sb.Append("<table class=\"admin-table\"><thead><tr>");
            sb.Append("<th>Username</th><th>Display name</th><th>Role</th><th>Status</th><th>New password</th><th>Actions</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var u in users)
            {
                var status = u.IsActive ? "<span class=\"admin-status active\">Active</span>" : "<span class=\"admin-status inactive\">Inactive</span>";
                sb.Append("<tr>");
                sb.Append($"<td><code>{H(u.WindowsLogin)}</code></td>");
                sb.Append($"<td><input class=\"ledger-input admin-inline-input\" data-field=\"displayName\" data-user-id=\"{u.AppUserId}\" value=\"{H(u.DisplayName)}\" /></td>");
                sb.Append("<td><select class=\"ledger-input admin-inline-input\" data-field=\"role\" data-user-id=\"" + u.AppUserId + "\">");
                foreach (AppRole role in new[] { AppRole.User, AppRole.Admin, AppRole.Sysadmin })
                {
                    var selected = u.Role == role ? " selected" : "";
                    sb.Append($"<option value=\"{PermissionMatrix.RoleName(role)}\"{selected}>{PermissionMatrix.RoleName(role)}</option>");
                }
                sb.Append("</select></td>");
                sb.Append($"<td>{status}</td>");
                sb.Append($"<td><input class=\"ledger-input admin-inline-input\" type=\"password\" data-field=\"password\" data-user-id=\"{u.AppUserId}\" placeholder=\"Leave blank to keep\" /></td>");
                sb.Append("<td class=\"admin-actions\">");
                sb.Append($"<button type=\"button\" class=\"ledger-btn ledger-btn-primary\" data-action=\"admin-save-user\" data-user-id=\"{u.AppUserId}\" data-windows-login=\"{H(u.WindowsLogin)}\" data-active=\"{u.IsActive.ToString().ToLower()}\">Save</button>");
                if (u.IsActive)
                    sb.Append($"<button type=\"button\" class=\"ledger-btn\" data-action=\"admin-deactivate-user\" data-user-id=\"{u.AppUserId}\">Deactivate</button>");
                else
                    sb.Append($"<button type=\"button\" class=\"ledger-btn ledger-btn-primary\" data-action=\"admin-activate-user\" data-user-id=\"{u.AppUserId}\">Activate</button>");
                sb.Append("</td></tr>");
            }

            sb.Append("</tbody></table></div></div>");
            return sb.ToString();
        }
    }
}
