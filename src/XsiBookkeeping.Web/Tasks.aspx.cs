using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web
{
    public partial class TasksPage : LedgerPageBase
    {
        private int _viewYear;
        private int _viewMonth;
        private string _monthKey;
        private string _countryFilter = "ALL";
        private string _statusFilter = "ALL";

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadLedgerData();

            _viewYear = Period.Year;
            _viewMonth = Period.Month;
            if (int.TryParse(Request.QueryString["year"], out var y)) _viewYear = y;
            if (int.TryParse(Request.QueryString["month"], out var m)) _viewMonth = m;
            _monthKey = PeriodHelper.ToMonthKey(_viewYear, _viewMonth);

            _countryFilter = Request.QueryString["country"] ?? "ALL";
            _statusFilter = Request.QueryString["status"] ?? "ALL";
            if (_countryFilter != "CA" && _countryFilter != "US") _countryFilter = "ALL";
            if (_statusFilter != "COMPLETE" && _statusFilter != "INCOMPLETE") _statusFilter = "ALL";

            ContentLiteral.Text = BuildHtml();
        }

        private string BuildHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"ledger-container-wide fade-in\">");

            sb.Append("<div class=\"tasks-header\">");
            sb.Append("<div>");
            sb.Append("<div class=\"ledger-kicker\">Monthly Reconciliation</div>");
            sb.Append($"<h1 class=\"ledger-title\">{PeriodHelper.MonthFull[_viewMonth]} <span class=\"ledger-title-muted\">{_viewYear}</span></h1>");
            sb.Append("</div>");
            sb.Append("<div class=\"tasks-nav\">");
            if (_viewYear != Period.Year || _viewMonth != Period.Month)
                sb.Append($"<a class=\"ledger-btn ledger-btn-accent\" href=\"{TasksUrl(Period.Year, Period.Month)}\">Current Period</a>");
            var prev = NavMonth(-1);
            var next = NavMonth(1);
            sb.Append($"<a class=\"ledger-btn ledger-btn-icon\" href=\"{TasksUrl(prev.Year, prev.Month)}\">‹</a>");
            sb.Append($"<a class=\"ledger-btn ledger-btn-icon\" href=\"{TasksUrl(next.Year, next.Month)}\">›</a>");
            sb.Append("</div></div>");

            sb.Append(BuildFilters());

            if (Data.Companies.Count == 0 && !SeesAllCompanies)
            {
                sb.Append("<div class=\"tasks-empty-state\">");
                sb.Append("<h2>No tasks assigned yet</h2>");
                sb.Append("<p>Your admin has not assigned any tasks to you yet. Contact an admin to get access.</p>");
                sb.Append("</div></div>");
                return sb.ToString();
            }

            var ca = FilterCompanies(Data.Companies.Where(c => c.Country == "CA").OrderBy(c => c.Name).ToList());
            var us = FilterCompanies(Data.Companies.Where(c => c.Country == "US").OrderBy(c => c.Name).ToList());
            var unassigned = FilterCompanies(Data.Companies.Where(c => c.Country != "CA" && c.Country != "US").OrderBy(c => c.Name).ToList());

            if (_countryFilter == "ALL" || _countryFilter == "CA")
                if (ca.Count > 0) { sb.Append("<div class=\"group-header\">" + CountryLabel("CA", true) + "</div>"); foreach (var c in ca) sb.Append(RenderCompany(c)); }
            if (_countryFilter == "ALL" || _countryFilter == "US")
                if (us.Count > 0) { sb.Append("<div class=\"group-header\">" + CountryLabel("US", true) + "</div>"); foreach (var c in us) sb.Append(RenderCompany(c)); }
            if (_countryFilter == "ALL")
                if (unassigned.Count > 0) { sb.Append("<div class=\"group-header\">📋 Unassigned</div>"); foreach (var c in unassigned) sb.Append(RenderCompany(c)); }

            sb.Append(BuildAddCompany());
            sb.Append("</div>");
            return sb.ToString();
        }

        private string BuildFilters()
        {
            var sb = new StringBuilder();
            var allCa = Data.Companies.Where(c => c.Country == "CA").ToList();
            var allUs = Data.Companies.Where(c => c.Country == "US").ToList();
            var countAll = ApplyStatusFilter(Data.Companies).Count;
            var countCa = ApplyStatusFilter(allCa).Count;
            var countUs = ApplyStatusFilter(allUs).Count;
            var countryScoped = ApplyCountryFilter(Data.Companies);
            var countComplete = countryScoped.Count(c => Repo.GetCompanyStats(Data, c, _viewYear, _viewMonth).Pct == 100);
            var countIncomplete = countryScoped.Count(c => Repo.GetCompanyStats(Data, c, _viewYear, _viewMonth).Pct < 100);
            var countStatusAll = countryScoped.Count;

            sb.Append("<div class=\"filter-bar\">");
            sb.Append("<div class=\"filter-group\">");
            sb.Append(FilterBtn("ALL", "All", countAll, _countryFilter == "ALL", ""));
            sb.Append(FilterBtn("CA", CountryLabel("CA", true), countCa, _countryFilter == "CA", ""));
            sb.Append(FilterBtn("US", CountryLabel("US", true), countUs, _countryFilter == "US", ""));
            sb.Append("</div><div class=\"filter-divider\"></div><div class=\"filter-group\">");
            sb.Append(StatusFilterBtn("ALL", "All", countStatusAll));
            sb.Append(StatusFilterBtn("INCOMPLETE", "Incomplete", countIncomplete));
            sb.Append(StatusFilterBtn("COMPLETE", "Complete ✓", countComplete));
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private string FilterBtn(string val, string label, int count, bool active, string extraClass)
        {
            var cls = "filter-btn" + (active ? " active" : "") + extraClass;
            return $"<a class=\"{cls}\" href=\"{FilterUrl(val, _statusFilter)}\">{label} <span style=\"font-size:11px;opacity:.8\">({count})</span></a>";
        }

        private string StatusFilterBtn(string val, string label, int count)
        {
            var cls = "filter-btn";
            if (_statusFilter == val)
                cls += val == "COMPLETE" ? " active-green" : val == "INCOMPLETE" ? " active-amber" : " active";
            return $"<a class=\"{cls}\" href=\"{FilterUrl(_countryFilter, val)}\">{label} <span style=\"font-size:11px;opacity:.8\">({count})</span></a>";
        }

        private List<Company> ApplyStatusFilter(IEnumerable<Company> list)
        {
            var items = list.ToList();
            if (_statusFilter == "COMPLETE")
                return items.Where(c => Repo.GetCompanyStats(Data, c, _viewYear, _viewMonth).Pct == 100).ToList();
            if (_statusFilter == "INCOMPLETE")
                return items.Where(c => Repo.GetCompanyStats(Data, c, _viewYear, _viewMonth).Pct < 100).ToList();
            return items;
        }

        private List<Company> ApplyCountryFilter(IEnumerable<Company> list)
        {
            var items = list.ToList();
            if (_countryFilter == "CA") return items.Where(c => c.Country == "CA").ToList();
            if (_countryFilter == "US") return items.Where(c => c.Country == "US").ToList();
            return items;
        }

        private List<Company> FilterCompanies(List<Company> list) => ApplyStatusFilter(list);

        private string RenderCompany(Company company)
        {
            var sb = new StringBuilder();
            var accs = Data.Accounts.Where(a => a.CompanyId == company.CompanyId).ToList();
            var stats = Repo.GetCompanyStats(Data, company, _viewYear, _viewMonth);
            var complete = stats.Pct == 100;
            var barColor = complete ? "#15803d" : stats.Pct > 0 ? "#f59e0b" : "#e8e4dc";

            sb.Append("<div class=\"company-row-wrap\">");
            sb.Append("<div class=\"checklist-panel\">");
            sb.Append($"<div class=\"checklist-header{(complete ? " complete" : "")}\">");
            sb.Append($"<button type=\"button\" class=\"expand-btn open\" data-action=\"expand-company\" data-company-id=\"{company.CompanyId}\">▶</button>");

            sb.Append($"<div id=\"company-view-{company.CompanyId}\" style=\"display:flex;align-items:center;gap:12px;flex:1\">");
            sb.Append("<div style=\"flex:1\">");
            sb.Append($"<span class=\"company-name\">{H(company.Name)}</span>");
            sb.Append($"<span class=\"company-stats\" style=\"color:{(complete ? "#15803d" : "#78716c")}\">{stats.Done}/{stats.Total}</span>");
            sb.Append("</div>");
            sb.Append($"<div class=\"progress-bar-wrap\"><div class=\"progress-bar-fill\" style=\"width:{stats.Pct}%;background:{barColor}\"></div></div>");
            sb.Append("<div style=\"display:flex;gap:4px\">");
            if (Can(Permission.ManageCompanies))
            {
                sb.Append($"<button type=\"button\" class=\"icon-btn\" data-action=\"edit-company\" data-company-id=\"{company.CompanyId}\">✎</button>");
                sb.Append($"<button type=\"button\" class=\"icon-btn\" data-action=\"delete-company\" data-company-id=\"{company.CompanyId}\">✕</button>");
            }
            sb.Append("</div></div>");

            if (Can(Permission.ManageCompanies))
            {
                sb.Append($"<div id=\"company-edit-{company.CompanyId}\" class=\"edit-inline hidden\" data-company-id=\"{company.CompanyId}\" data-country=\"{H(company.Country ?? "")}\" data-country-wrap=\"true\">");
                sb.Append($"<input class=\"ledger-input\" data-field=\"name\" value=\"{H(company.Name)}\" style=\"min-width:120px\" />");
                sb.Append($"<button type=\"button\" class=\"country-toggle{(company.Country == "CA" ? " active" : "")}\" data-action=\"toggle-country\" data-country=\"CA\">{CountryLabel("CA")}</button>");
                sb.Append($"<button type=\"button\" class=\"country-toggle{(company.Country == "US" ? " active" : "")}\" data-action=\"toggle-country\" data-country=\"US\">{CountryLabel("US")}</button>");
                sb.Append($"<button type=\"button\" class=\"ledger-btn ledger-btn-primary\" data-action=\"save-company-edit\">Save</button>");
                sb.Append($"<button type=\"button\" class=\"ledger-btn\" data-action=\"cancel-company-edit\" data-company-id=\"{company.CompanyId}\">Cancel</button>");
                sb.Append("</div>");
            }
            sb.Append("</div>");

            sb.Append($"<div id=\"company-body-{company.CompanyId}\" class=\"collapsed-body open\">");
            foreach (var account in accs)
                sb.Append(RenderAccount(company.CompanyId, account));

            if (Can(Permission.ManageCompanies))
            {
                sb.Append($"<div id=\"add-account-{company.CompanyId}\" class=\"add-row hidden\">");
                sb.Append("<input class=\"ledger-input\" placeholder=\"Item name...\" />");
                sb.Append($"<button type=\"button\" class=\"ledger-btn ledger-btn-primary\" data-action=\"add-account\" data-company-id=\"{company.CompanyId}\">Add</button>");
                sb.Append($"<button type=\"button\" class=\"ledger-btn\" data-action=\"cancel-add-account\" data-company-id=\"{company.CompanyId}\">Cancel</button>");
                sb.Append("</div>");
                sb.Append($"<button type=\"button\" class=\"add-item-btn\" data-action=\"show-add-account\" data-company-id=\"{company.CompanyId}\">+ Add item</button>");
            }
            sb.Append("</div></div>");

            sb.Append(RenderCommentsPanel(company.CompanyId));
            sb.Append("</div>");
            return sb.ToString();
        }

        private string RenderAccount(long companyId, Account account)
        {
            var st = LedgerRepository.GetStatus(Data.Completions, companyId, account.AccountId, _monthKey);
            var rowBg = st == "done" ? "#fafffe" : st == "in-progress" ? "#fffdf5" : "";
            var sb = new StringBuilder();
            sb.Append($"<div class=\"account-row\" style=\"background:{rowBg}\">");
            sb.Append(RenderCheckButton(companyId, account.AccountId, _monthKey));

            sb.Append($"<div id=\"account-view-{account.AccountId}\" style=\"display:flex;align-items:center;gap:12px;flex:1\">");
            sb.Append("<div style=\"flex:1\">");
            sb.Append($"<span class=\"account-name{(st == "done" ? " done" : "")}\">{H(account.Name)}</span>");
            sb.Append(RenderAccountAssigneeBadges(account.AccountId));
            sb.Append("</div>");
            if (st == "done") sb.Append("<span class=\"account-badge badge-done\">Reconciled</span>");
            else if (st == "in-progress") sb.Append("<span class=\"account-badge badge-progress\">In Progress</span>");
            else sb.Append("<span class=\"account-badge hidden\"></span>");
            sb.Append("<div style=\"display:flex;gap:3px\">");
            if (Can(Permission.ManageCompanies))
            {
                sb.Append($"<button type=\"button\" class=\"icon-btn\" data-action=\"edit-account\" data-account-id=\"{account.AccountId}\">✎</button>");
                sb.Append($"<button type=\"button\" class=\"icon-btn\" data-action=\"delete-account\" data-account-id=\"{account.AccountId}\">✕</button>");
            }
            sb.Append("</div></div>");

            if (Can(Permission.ManageCompanies))
            {
                sb.Append($"<div id=\"account-edit-{account.AccountId}\" class=\"edit-inline hidden\" data-account-id=\"{account.AccountId}\" data-company-id=\"{companyId}\">");
                sb.Append($"<input class=\"ledger-input\" data-field=\"name\" value=\"{H(account.Name)}\" />");
                sb.Append($"<button type=\"button\" class=\"ledger-btn ledger-btn-primary\" data-action=\"save-account-edit\">Save</button>");
                sb.Append($"<button type=\"button\" class=\"icon-btn\" data-action=\"cancel-account-edit\" data-account-id=\"{account.AccountId}\">✕</button>");
                sb.Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private string BuildAddCompany()
        {
            if (!Can(Permission.ManageCompanies)) return "";

            var sb = new StringBuilder();
            sb.Append("<div id=\"add-company-form\" class=\"add-company-box hidden\">");
            sb.Append("<input class=\"ledger-input\" data-field=\"company-name\" placeholder=\"Company name...\" style=\"min-width:160px;border-color:#e8e4dc\" />");
            sb.Append("<div data-country-wrap data-country=\"\">");
            sb.Append("<button type=\"button\" class=\"country-toggle\" data-action=\"toggle-country\" data-country=\"CA\">" + CountryLabel("CA") + "</button>");
            sb.Append("<button type=\"button\" class=\"country-toggle\" data-action=\"toggle-country\" data-country=\"US\">" + CountryLabel("US") + "</button>");
            sb.Append("</div>");
            sb.Append("<button type=\"button\" class=\"ledger-btn ledger-btn-primary\" data-action=\"add-company\">Add Company</button>");
            sb.Append("<button type=\"button\" class=\"ledger-btn\" data-action=\"cancel-add-company\">Cancel</button>");
            sb.Append("</div>");
            sb.Append("<button type=\"button\" id=\"add-company-btn\" class=\"add-company-dashed\" data-action=\"show-add-company\">+ Add Company</button>");
            return sb.ToString();
        }

        private YearMonth NavMonth(int dir)
        {
            var m = _viewMonth + dir;
            var y = _viewYear;
            if (m > 11) { m = 0; y++; }
            if (m < 0) { m = 11; y--; }
            return new YearMonth { Year = y, Month = m };
        }

        private string TasksUrl(int year, int month)
        {
            return $"Tasks.aspx?year={year}&month={month}&country={HttpUtility.UrlEncode(_countryFilter)}&status={HttpUtility.UrlEncode(_statusFilter)}";
        }

        private string FilterUrl(string country, string status)
        {
            return $"Tasks.aspx?year={_viewYear}&month={_viewMonth}&country={HttpUtility.UrlEncode(country)}&status={HttpUtility.UrlEncode(status)}";
        }

        private static string CountryLabel(string country, bool fullName = false)
        {
            switch (country)
            {
                case "CA":
                    return fullName ? "🍁 Canada" : "🍁 CA";
                case "US":
                    return fullName ? "⭐ United States" : "⭐ US";
                default:
                    return country;
            }
        }
    }
}
