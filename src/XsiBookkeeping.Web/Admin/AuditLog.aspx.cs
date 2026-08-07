using System;
using System.Text;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web.Admin
{
    public partial class AuditLogPage : LedgerPageBase
    {
        private readonly AuditService _audit = new AuditService();

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (CurrentAppUser == null || !PermissionMatrix.Can(CurrentAppUser.Role, Permission.ViewAudit))
            {
                Response.Redirect("~/AccessDenied.aspx?reason=admin");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ContentLiteral.Text = BuildHtml();
        }

        private string BuildHtml()
        {
            var logs = _audit.GetRecent(200);
            var sb = new StringBuilder();
            sb.Append("<div class=\"ledger-container-wide fade-in\">");
            sb.Append("<div style=\"margin-bottom:24px\">");
            sb.Append("<div class=\"ledger-kicker\">Administration</div>");
            sb.Append("<h1 class=\"ledger-title\">Audit Log</h1>");
            sb.Append("</div>");

            sb.Append("<div class=\"admin-card\">");
            sb.Append("<table class=\"admin-table\"><thead><tr>");
            sb.Append("<th>When</th><th>Actor</th><th>Action</th><th>Entity</th><th>Details</th>");
            sb.Append("</tr></thead><tbody>");

            if (logs.Count == 0)
                sb.Append("<tr><td colspan=\"5\" class=\"admin-empty\">No audit entries yet</td></tr>");

            foreach (var log in logs)
            {
                sb.Append("<tr>");
                sb.Append($"<td class=\"admin-mono\">{H(PeriodHelper.FormatTime(log.CreatedAtUtc))}</td>");
                sb.Append($"<td><code>{H(log.ActorLogin)}</code></td>");
                sb.Append($"<td>{H(log.Action)}</td>");
                sb.Append($"<td>{H(log.EntityType)} {H(log.EntityId)}</td>");
                sb.Append($"<td>{H(log.Details)}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div></div>");
            return sb.ToString();
        }
    }
}
