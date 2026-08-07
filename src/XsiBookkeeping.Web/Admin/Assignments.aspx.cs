using System.Collections.Generic;
using System.Linq;
using System.Text;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web.Admin
{
    public partial class AssignmentsPage : LedgerPageBase
    {
        private readonly UserRepository _userRepo = new UserRepository();

        protected void Page_Load(object sender, System.EventArgs e)
        {
            if (!Can(Permission.ManageCompanies))
            {
                Response.Redirect("~/AccessDenied.aspx?reason=forbidden");
                return;
            }

            LoadLedgerData();
            ContentLiteral.Text = BuildHtml();
        }

        private string BuildHtml()
        {
            var assignableUsers = _userRepo.GetAll()
                .Where(u => u.IsActive && u.Role == AppRole.User)
                .OrderBy(u => u.WindowsLogin)
                .ToList();

            var allTasks = Data.Accounts.ToList();
            var assignedTaskCount = allTasks.Count(a => Repo.GetAccountAssignmentUserIds(a.AccountId).Count > 0);
            var unassignedCount = allTasks.Count - assignedTaskCount;

            var sb = new StringBuilder();
            sb.Append("<div class=\"ledger-container-wide assignments-page fade-in\">");

            sb.Append("<header class=\"assign-head\">");
            sb.Append("<div class=\"assign-head-text\">");
            sb.Append("<div class=\"ledger-kicker\">Administration</div>");
            sb.Append("<h1 class=\"ledger-title\">Task Assignments</h1>");
            sb.Append("<p class=\"assign-subtitle\">Click a team member to assign or unassign. Changes save automatically.</p>");
            sb.Append("</div>");
            sb.Append("<div class=\"assign-head-actions\">");
            sb.Append("<a href=\"../Tasks.aspx\" class=\"ledger-btn ledger-btn-accent\">Tasks</a>");
            sb.Append("<a href=\"Users.aspx\" class=\"ledger-btn\">Users</a>");
            sb.Append("</div></header>");

            sb.Append("<div class=\"ledger-pills assign-pills\">");
            sb.Append(Pill(allTasks.Count.ToString(), "Total tasks", "ledger-pill-neutral"));
            sb.Append(Pill(assignedTaskCount.ToString(), "Assigned", "ledger-pill-green"));
            sb.Append(Pill(unassignedCount.ToString(), "Unassigned", unassignedCount > 0 ? "ledger-pill-red" : "ledger-pill-neutral"));
            sb.Append(Pill(assignableUsers.Count.ToString(), "Team members", "ledger-pill-neutral"));
            sb.Append("</div>");

            if (Data.Companies.Count == 0)
            {
                sb.Append("<div class=\"assign-empty-state\">");
                sb.Append("<div class=\"assign-empty-icon\">📋</div>");
                sb.Append("<h2>No companies yet</h2>");
                sb.Append("<p>Add companies and tasks first, then assign them here.</p>");
                sb.Append("<a href=\"../Tasks.aspx\" class=\"ledger-btn ledger-btn-primary\">Go to Tasks</a>");
                sb.Append("</div></div>");
                return sb.ToString();
            }

            if (assignableUsers.Count == 0)
            {
                sb.Append("<div class=\"assign-alert\">");
                sb.Append("<strong>No User-role accounts.</strong> Add team members on <a href=\"Users.aspx\">Users</a> before assigning tasks.");
                sb.Append("</div>");
            }

            sb.Append("<div class=\"assign-sticky-bar\">");
            sb.Append("<div class=\"assign-search-wrap\">");
            sb.Append("<svg class=\"assign-search-svg\" width=\"18\" height=\"18\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"8\"/><path d=\"M21 21l-4.35-4.35\"/></svg>");
            sb.Append("<input type=\"search\" id=\"assign-search\" class=\"assign-search-input\" placeholder=\"Search company or task…\" autocomplete=\"off\" />");
            sb.Append("</div>");
            sb.Append("<div class=\"assign-filter-group\" role=\"group\" aria-label=\"Filter\">");
            sb.Append("<button type=\"button\" class=\"assign-filter-btn active\" data-assign-filter=\"all\">All</button>");
            sb.Append("<button type=\"button\" class=\"assign-filter-btn\" data-assign-filter=\"unassigned\">Unassigned</button>");
            sb.Append("<button type=\"button\" class=\"assign-filter-btn\" data-assign-filter=\"assigned\">Assigned</button>");
            sb.Append("</div></div>");

            sb.Append("<div id=\"assign-company-list\" class=\"assign-co-list\">");
            foreach (var company in Data.Companies)
                sb.Append(RenderCompanyPanel(company, assignableUsers));
            sb.Append("</div>");

            sb.Append("<div id=\"assign-no-results\" class=\"assign-no-results hidden\">No tasks match your search or filter.</div>");
            sb.Append("<div id=\"assign-toast\" class=\"assign-toast hidden\" role=\"status\" aria-live=\"polite\"></div>");
            sb.Append("</div>");
            return sb.ToString();
        }

        private string RenderCompanyPanel(Company company, List<AppUser> assignableUsers)
        {
            var accounts = Data.Accounts
                .Where(a => a.CompanyId == company.CompanyId)
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Name)
                .ToList();

            var assignedInCompany = accounts.Count(a => Repo.GetAccountAssignmentUserIds(a.AccountId).Count > 0);
            var pct = accounts.Count > 0 ? (int)System.Math.Round(assignedInCompany * 100.0 / accounts.Count) : 0;
            var searchTerms = (company.Name + " " + string.Join(" ", accounts.Select(a => a.Name))).ToLowerInvariant();
            var complete = assignedInCompany == accounts.Count && accounts.Count > 0;

            var sb = new StringBuilder();
            sb.Append($"<section class=\"assign-co\" data-search=\"{H(searchTerms)}\" data-company-id=\"{company.CompanyId}\">");
            sb.Append("<div class=\"assign-co-header\">");
            sb.Append($"<button type=\"button\" class=\"assign-co-toggle open\" data-action=\"toggle-assign-co\" aria-expanded=\"true\">");
            sb.Append("<span class=\"assign-co-chevron\" aria-hidden=\"true\">▶</span>");
            sb.Append("</button>");
            sb.Append("<div class=\"assign-co-info\">");
            sb.Append($"<span class=\"assign-co-name\">{H(company.Name)}</span>");
            sb.Append($"<span class=\"assign-co-country\">{CountryBadge(company.Country)}</span>");
            sb.Append("</div>");
            sb.Append("<div class=\"assign-co-progress\">");
            sb.Append($"<span class=\"assign-co-stats\">{assignedInCompany}/{accounts.Count}</span>");
            sb.Append("<div class=\"progress-bar-wrap\"><div class=\"progress-bar-fill\" style=\"width:" + pct + "%;background:" + (complete ? "#15803d" : pct > 0 ? "#f59e0b" : "#e8e4dc") + "\"></div></div>");
            sb.Append("</div></div>");

            sb.Append("<div class=\"assign-co-body open\">");
            if (accounts.Count == 0)
            {
                sb.Append("<div class=\"assign-co-empty\">No tasks yet. <a href=\"../Tasks.aspx\">Add on Tasks page</a></div>");
            }
            else
            {
                sb.Append("<div class=\"assign-table-head\">");
                sb.Append("<span>Task</span><span>Team</span><span></span>");
                sb.Append("</div>");
                foreach (var account in accounts)
                    sb.Append(RenderTaskRow(account, assignableUsers));
            }
            sb.Append("</div></section>");
            return sb.ToString();
        }

        private string RenderTaskRow(Account account, List<AppUser> assignableUsers)
        {
            var assignedIds = Repo.GetAccountAssignmentUserIds(account.AccountId);
            var isAssigned = assignedIds.Count > 0;

            var sb = new StringBuilder();
            sb.Append($"<div class=\"assign-row\" data-account-id=\"{account.AccountId}\" data-assigned=\"{(isAssigned ? "true" : "false")}\">");

            sb.Append("<div class=\"assign-row-task\">");
            sb.Append($"<span class=\"assign-row-dot{(isAssigned ? " on" : "")}\" aria-hidden=\"true\"></span>");
            sb.Append($"<span class=\"assign-row-name\">{H(account.Name)}</span>");
            sb.Append("</div>");

            sb.Append($"<div class=\"assignment-users assign-people\" data-account-id=\"{account.AccountId}\">");
            if (assignableUsers.Count == 0)
            {
                sb.Append("<span class=\"assign-people-empty\">No users</span>");
            }
            else
            {
                foreach (var user in assignableUsers)
                {
                    var label = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.WindowsLogin;
                    var initial = label.Substring(0, 1).ToUpperInvariant();
                    var color = PeriodHelper.AuthorColor(user.WindowsLogin);
                    var isSelected = assignedIds.Contains(user.AppUserId);
                    var selected = isSelected ? " selected" : "";
                    var checkedAttr = isSelected ? " checked=\"checked\"" : "";

                    sb.Append($"<label class=\"assign-person{selected}\" style=\"--person-color:{color}\" title=\"{H(label)}\">");
                    sb.Append($"<input type=\"checkbox\" class=\"assignment-check\" data-user-id=\"{user.AppUserId}\"{checkedAttr} />");
                    sb.Append($"<span class=\"assign-person-avatar\">{H(initial)}</span>");
                    sb.Append($"<span class=\"assign-person-label\">{H(label)}</span>");
                    sb.Append("</label>");
                }
            }
            sb.Append("</div>");

            sb.Append("<div class=\"assign-row-status\" aria-live=\"polite\"></div>");
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string Pill(string value, string label, string cssClass)
        {
            return $"<div class=\"ledger-pill {cssClass}\"><div class=\"ledger-pill-value\">{value}</div><div class=\"ledger-pill-label\">{label}</div></div>";
        }

        private static string CountryBadge(string country)
        {
            switch (country)
            {
                case "CA": return "🍁 Canada";
                case "US": return "⭐ US";
                default: return "🌐 —";
            }
        }
    }
}
