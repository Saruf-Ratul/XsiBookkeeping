using System.Linq;
using System.Text;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web
{
    public partial class ReportPage : LedgerPageBase
    {
        protected void Page_Load(object sender, System.EventArgs e)
        {
            LoadLedgerData();
            ContentLiteral.Text = BuildHtml();
        }

        private string BuildHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"ledger-container-report fade-in\">");
            sb.Append("<div style=\"margin-bottom:28px\">");
            sb.Append("<div class=\"ledger-kicker\">Status Overview</div>");
            sb.Append("<h1 class=\"ledger-title\">Report</h1>");
            sb.Append("</div>");

            double score = 0;
            var total = 0;
            var done = 0;
            foreach (var c in Data.Companies)
            {
                foreach (var a in Data.Accounts.Where(x => x.CompanyId == c.CompanyId))
                {
                    total++;
                    var v = LedgerRepository.GetStatus(Data.Completions, c.CompanyId, a.AccountId, PeriodKey);
                    if (v == "done") { score++; done++; }
                    else if (v == "in-progress") score += 0.5;
                }
            }
            var pct = total > 0 ? (int)System.Math.Round(score / total * 100) : 0;

            sb.Append("<div class=\"report-hero\">");
            sb.Append("<div class=\"report-hero-kicker\">Current Period</div>");
            sb.Append($"<div class=\"report-hero-title\">{PeriodHelper.MonthFull[Period.Month]} {Period.Year}</div>");
            sb.Append("<div class=\"report-hero-stats\">");
            sb.Append($"<div><div class=\"report-stat-value accent\">{pct}<span style=\"font-size:18px\">%</span></div><div class=\"report-stat-label\">Overall Complete</div></div>");
            sb.Append($"<div><div class=\"report-stat-value\">{done}<span style=\"font-size:18px;color:#a8a29e\">/{total}</span></div><div class=\"report-stat-label\">Accounts Reconciled</div></div>");
            sb.Append("<div class=\"report-progress-wrap\"><div class=\"report-progress-bar\"><div class=\"report-progress-fill\" style=\"width:" + pct + "%\"></div></div></div>");
            sb.Append("</div></div>");

            foreach (var company in Data.Companies.OrderBy(c => c.Name))
            {
                var reconThrough = Repo.GetReconciledThrough(Data, company);
                var curStats = Repo.GetCompanyStats(Data, company, Period.Year, Period.Month);
                var allDone = curStats.Pct == 100;
                var accs = Data.Accounts.Where(a => a.CompanyId == company.CompanyId).ToList();

                sb.Append($"<div class=\"report-company-card{(allDone ? " complete" : "")}\">");
                sb.Append("<div class=\"report-company-header\">");
                sb.Append($"<span class=\"report-company-name\">{H(company.Name)}</span>");
                if (allDone) sb.Append("<span class=\"complete-badge\">✓ Complete</span>");
                sb.Append("</div>");
                sb.Append("<div class=\"report-meta\">");
                sb.Append("<div><div class=\"report-meta-label\">Reconciled Through</div>");
                sb.Append($"<div class=\"report-meta-value{(reconThrough != null ? " green" : "")}\">");
                sb.Append(reconThrough != null ? $"{PeriodHelper.MonthFull[reconThrough.Month]} {reconThrough.Year}" : "—");
                sb.Append("</div></div>");
                sb.Append("<div><div class=\"report-meta-label\">Current Period</div>");
                sb.Append($"<div class=\"report-meta-value\"><span style=\"color:#c2410c\">{curStats.Done}</span><span style=\"color:#78716c\">/{curStats.Total}</span></div></div>");
                sb.Append("</div>");
                sb.Append("<div class=\"report-accounts\">");
                foreach (var a in accs)
                {
                    var st = PeriodHelper.DisplayStatus(LedgerRepository.GetStatus(Data.Completions, company.CompanyId, a.AccountId, PeriodKey));
                    var color = st == "done" ? "#15803d" : st == "progress" ? "#d97706" : "#78716c";
                    var dot = st == "done" ? "●" : st == "progress" ? "◑" : "○";
                    sb.Append($"<div class=\"report-account\" style=\"color:{color}\"><span style=\"font-size:10px\">{dot}</span><span>{H(a.Name)}</span></div>");
                }
                sb.Append("</div>");
                var barColor = allDone ? "#15803d" : curStats.Pct > 0 ? "#f59e0b" : "#e8e4dc";
                sb.Append($"<div class=\"progress-bar-wrap\" style=\"width:100%;height:6px\"><div class=\"progress-bar-fill\" style=\"width:{curStats.Pct}%;background:{barColor}\"></div></div>");
                sb.Append("</div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }
    }
}
