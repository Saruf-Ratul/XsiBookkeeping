using System.Linq;
using System.Text;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web
{
    public partial class OverviewPage : LedgerPageBase
    {
        protected void Page_Load(object sender, System.EventArgs e)
        {
            LoadLedgerData();
            ContentLiteral.Text = BuildHtml();
        }

        private string BuildHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"ledger-container fade-in\">");
            sb.Append("<div style=\"margin-bottom:32px\">");
            sb.Append("<div class=\"ledger-kicker\">Current Period</div>");
            sb.Append($"<h1 class=\"ledger-title\">{PeriodHelper.MonthFull[Period.Month]} <span class=\"ledger-title-muted\">{Period.Year}</span></h1>");
            sb.Append("</div>");

            var upToDate = Data.Companies.Where(c => Repo.IsPeriodDone(Data, c, PeriodKey)).OrderBy(c => c.Name).ToList();
            var overdue = Data.Companies.Where(c => !Repo.IsPeriodDone(Data, c, PeriodKey)).OrderBy(c => c.Name).ToList();

            sb.Append("<div class=\"ledger-pills\">");
            sb.Append($"<div class=\"ledger-pill ledger-pill-green\"><div class=\"ledger-pill-value\" style=\"color:#15803d\">{upToDate.Count}</div><div class=\"ledger-pill-label\" style=\"color:#15803d\">Up to date</div></div>");
            sb.Append($"<div class=\"ledger-pill ledger-pill-red\"><div class=\"ledger-pill-value\" style=\"color:#b91c1c\">{overdue.Count}</div><div class=\"ledger-pill-label\" style=\"color:#b91c1c\">Needs attention</div></div>");
            sb.Append($"<div class=\"ledger-pill ledger-pill-neutral\"><div class=\"ledger-pill-value\">{Data.Companies.Count}</div><div class=\"ledger-pill-label\" style=\"color:#78716c\">Total companies</div></div>");
            sb.Append("</div>");

            if (upToDate.Count > 0)
            {
                sb.Append("<div style=\"margin-bottom:28px\">");
                sb.Append($"<div class=\"ledger-section-title green\"><span>✓</span> Reconciled through {PeriodHelper.MonthFull[Period.Month]}</div>");
                sb.Append("<div class=\"done-grid\">");
                foreach (var company in upToDate)
                {
                    sb.Append("<div class=\"done-card\">");
                    sb.Append("<span class=\"done-card-check\">✓</span>");
                    sb.Append($"<span class=\"done-card-name\">{H(company.Name)}</span>");
                    sb.Append("</div>");
                }
                sb.Append("</div></div>");
            }

            if (overdue.Count > 0)
            {
                sb.Append("<div>");
                sb.Append($"<div class=\"ledger-section-title red\"><span>!</span> Not reconciled for {PeriodHelper.MonthFull[Period.Month]}</div>");
                sb.Append("<div class=\"overdue-list\">");
                foreach (var company in overdue)
                {
                    var stats = Repo.GetCompanyStats(Data, company, Period.Year, Period.Month);
                    var accs = Data.Accounts.Where(a => a.CompanyId == company.CompanyId).ToList();
                    var reason = Data.OverdueReasons.ContainsKey(company.CompanyId) ? Data.OverdueReasons[company.CompanyId] : "";

                    sb.Append("<div class=\"overdue-card\">");
                    sb.Append("<div class=\"overdue-row\">");
                    sb.Append("<div class=\"overdue-dot\"></div>");
                    sb.Append($"<span class=\"overdue-name\">{H(company.Name)}</span>");
                    sb.Append("<div class=\"status-dots\">");
                    foreach (var a in accs)
                        sb.Append(RenderStatusDot(company.CompanyId, a.AccountId, PeriodKey));
                    sb.Append("</div>");
                    sb.Append($"<span class=\"overdue-count\">{stats.Done}/{stats.Total}</span>");
                    sb.Append("</div>");
                    sb.Append("<div class=\"reason-row\">");
                    sb.Append("<span class=\"reason-label\">Reason</span>");
                    if (Can(Permission.Reconcile))
                    {
                        sb.Append($"<input type=\"text\" class=\"reason-input\" value=\"{H(reason)}\" placeholder=\"Why isn't this reconciled yet?\" data-action=\"save-reason\" data-company-id=\"{company.CompanyId}\" data-period=\"{H(PeriodKey)}\" />");
                        sb.Append("<span class=\"reason-status\">·</span>");
                    }
                    else
                    {
                        sb.Append($"<span class=\"reason-input\" style=\"color:#78716c\">{(string.IsNullOrWhiteSpace(reason) ? "—" : H(reason))}</span>");
                    }
                    sb.Append("</div></div>");
                }
                sb.Append("</div></div>");
            }

            if (Data.Companies.Count == 0)
            {
                var hint = Can(Permission.ManageCompanies)
                    ? "<a href=\"Tasks.aspx\" style=\"color:#c2410c;font-weight:600;text-decoration:none\">go to Tasks to add one</a>"
                    : "contact an Admin to add companies";
                sb.Append($"<div class=\"ledger-empty\">No companies yet — {hint}</div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }
    }
}
