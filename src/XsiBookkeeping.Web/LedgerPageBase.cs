using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web
{
    public class LedgerPageBase : Page
    {
        protected readonly LedgerRepository Repo = new LedgerRepository();
        protected LedgerData Data;
        protected YearMonth Period;
        protected YearMonth PrevPeriod;
        protected string PeriodKey;
        protected string PrevPeriodKey;
        protected AppUser CurrentAppUser;

        protected bool Can(Permission permission) =>
            CurrentAppUser != null && PermissionMatrix.Can(CurrentAppUser.Role, permission);

        protected bool SeesAllCompanies => Can(Permission.ManageCompanies);

        protected override void OnInit(EventArgs e)
        {
            CurrentAppUser = UserContextService.GetCurrent(Context);
            var path = Request.AppRelativeCurrentExecutionFilePath ?? "";
            var isAccessDenied = path.IndexOf("AccessDenied", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isAccessDenied && (CurrentAppUser == null || !CurrentAppUser.IsActive))
            {
                Response.Redirect("~/AccessDenied.aspx");
                return;
            }

            base.OnInit(e);
        }

        protected void LoadLedgerData()
        {
            Period = PeriodHelper.GetLastMonth();
            PrevPeriod = PeriodHelper.GetPreviousPeriod(Period.Year, Period.Month);
            PeriodKey = PeriodHelper.ToMonthKey(Period.Year, Period.Month);
            PrevPeriodKey = PeriodHelper.ToMonthKey(PrevPeriod.Year, PrevPeriod.Month);
            Data = Repo.LoadAll(PeriodKey, PrevPeriodKey, CurrentAppUser, restrictToAssignments: !SeesAllCompanies);
        }

        protected string RenderAssigneeBadges(long companyId)
        {
            if (!Data.AssigneesByCompany.ContainsKey(companyId) || Data.AssigneesByCompany[companyId].Count == 0)
                return "";

            var sb = new StringBuilder();
            sb.Append("<span class=\"assignee-badges\">");
            foreach (var user in Data.AssigneesByCompany[companyId])
            {
                var label = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.WindowsLogin;
                sb.Append($"<span class=\"assignee-badge\" title=\"{H(user.WindowsLogin)}\">{H(label)}</span>");
            }
            sb.Append("</span>");
            return sb.ToString();
        }

        protected string RenderAccountAssigneeBadges(long accountId)
        {
            if (!Data.AssigneesByAccount.ContainsKey(accountId) || Data.AssigneesByAccount[accountId].Count == 0)
                return SeesAllCompanies ? "<span class=\"assignee-empty\">Unassigned</span>" : "";

            var users = Data.AssigneesByAccount[accountId];
            if (!SeesAllCompanies && CurrentAppUser != null)
            {
                users = users.Where(u => u.AppUserId != CurrentAppUser.AppUserId).ToList();
                if (users.Count == 0) return "";
            }

            var sb = new StringBuilder();
            sb.Append("<span class=\"assignee-badges assignee-badges-inline\">");
            foreach (var user in users)
            {
                var label = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.WindowsLogin;
                sb.Append($"<span class=\"assignee-badge\" title=\"{H(user.WindowsLogin)}\">{H(label)}</span>");
            }
            sb.Append("</span>");
            return sb.ToString();
        }

        protected static string H(string s) => HttpUtility.HtmlEncode(s ?? "");

        protected string RenderStatusDot(long companyId, long accountId, string monthKey)
        {
            var st = LedgerRepository.GetStatus(Data.Completions, companyId, accountId, monthKey);
            var display = PeriodHelper.DisplayStatus(st);
            return $"<span class=\"status-dot {display}\" title=\"{H(st)}\"></span>";
        }

        protected string RenderCheckButton(long companyId, long accountId, string monthKey)
        {
            if (!Can(Permission.Reconcile))
                return RenderCheckButtonReadOnly(companyId, accountId, monthKey);

            var st = LedgerRepository.GetStatus(Data.Completions, companyId, accountId, monthKey);
            var cls = st == "done" ? "done" : st == "in-progress" ? "progress" : "";
            var sym = st == "done" ? "✓" : st == "in-progress" ? "–" : "";
            return $"<button type=\"button\" class=\"check-btn {cls}\" data-action=\"toggle-completion\" data-company-id=\"{companyId}\" data-account-id=\"{accountId}\" data-month-key=\"{H(monthKey)}\">{sym}</button>";
        }

        private string RenderCheckButtonReadOnly(long companyId, long accountId, string monthKey)
        {
            var st = LedgerRepository.GetStatus(Data.Completions, companyId, accountId, monthKey);
            var cls = st == "done" ? "done" : st == "in-progress" ? "progress" : "";
            var sym = st == "done" ? "✓" : st == "in-progress" ? "–" : "";
            return $"<span class=\"check-btn {cls}\" style=\"cursor:default;pointer-events:none\">{sym}</span>";
        }

        protected string RenderCommentsPanel(long companyId)
        {
            var sb = new StringBuilder();
            var comments = Data.Comments.ContainsKey(companyId) ? Data.Comments[companyId] : new System.Collections.Generic.List<Comment>();
            var canComment = Can(Permission.Comment);
            var canDeleteAny = Can(Permission.DeleteAnyComment);

            sb.Append($"<div class=\"comments-panel\" data-company-id=\"{companyId}\">");
            sb.Append("<div class=\"comments-header\"><span class=\"comments-title\">Comments</span>");
            sb.Append(comments.Count > 0 ? $"<span class=\"comments-count\">{comments.Count}</span>" : "<span class=\"comments-count\"></span>");
            sb.Append("</div><div class=\"comment-scroll\">");
            if (comments.Count == 0)
                sb.Append("<div class=\"comment-empty\">No comments yet</div>");
            foreach (var cm in comments)
            {
                var color = PeriodHelper.AuthorColor(cm.Author);
                var canDelete = canDeleteAny || string.Equals(cm.Author, User.Identity?.Name, StringComparison.OrdinalIgnoreCase);
                sb.Append("<div class=\"comment-row\">");
                sb.Append($"<div class=\"comment-avatar\" style=\"background:{color}\">{H(cm.Author.Substring(0, 1).ToUpper())}</div>");
                sb.Append("<div class=\"comment-body\">");
                sb.Append($"<div class=\"comment-meta\"><span class=\"comment-author\" style=\"color:{color}\">{H(cm.Author)}</span>");
                sb.Append($"<span class=\"comment-time\">{H(PeriodHelper.FormatTime(cm.CreatedAtUtc))}</span></div>");
                sb.Append($"<div>{H(cm.Content)}</div></div>");
                if (canDelete)
                    sb.Append($"<button type=\"button\" class=\"delete-comment\" data-action=\"delete-comment\" data-comment-id=\"{cm.CommentId}\" data-company-id=\"{companyId}\">✕</button>");
                sb.Append("</div>");
            }
            sb.Append("</div>");
            if (canComment)
            {
                sb.Append("<div class=\"comment-compose\">");
                sb.Append($"<textarea class=\"comment-textarea\" rows=\"1\" placeholder=\"Add a comment… (Enter to send)\" data-action=\"comment-input\" data-company-id=\"{companyId}\"></textarea>");
                sb.Append($"<button type=\"button\" class=\"ledger-btn ledger-btn-primary send-btn\" data-action=\"send-comment\">Send</button>");
                sb.Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }
    }
}
