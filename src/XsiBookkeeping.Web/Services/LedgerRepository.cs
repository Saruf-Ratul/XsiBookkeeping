using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using XsiBookkeeping.Web.Models;

namespace XsiBookkeeping.Web.Services
{
    public class LedgerRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["LedgerDb"].ConnectionString;

        public LedgerData LoadAll(string periodKey, string prevPeriodKey, AppUser scopeUser = null, bool restrictToAssignments = false)
        {
            var data = new LedgerData();

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                data.Companies = conn.Query<Company>(
                    "SELECT CompanyId, Name, Country, SortOrder FROM Companies ORDER BY SortOrder, CompanyId"
                ).ToList();

                var allAccounts = conn.Query<Account>(
                    "SELECT AccountId, CompanyId, Name, SortOrder FROM Accounts ORDER BY SortOrder, AccountId"
                ).ToList();

                if (restrictToAssignments && scopeUser != null && scopeUser.Role == AppRole.User)
                {
                    var assignedAccountIds = GetAssignedAccountIds(conn, scopeUser.AppUserId);

                    data.Accounts = allAccounts
                        .Where(a => assignedAccountIds.Contains(a.AccountId))
                        .ToList();

                    var visibleCompanyIdsFromAccounts = new HashSet<long>(data.Accounts.Select(a => a.CompanyId));
                    data.Companies = data.Companies
                        .Where(c => visibleCompanyIdsFromAccounts.Contains(c.CompanyId))
                        .ToList();
                }
                else
                {
                    data.Accounts = allAccounts;
                }

                var visibleCompanyIds = new HashSet<long>(data.Companies.Select(c => c.CompanyId));
                data.Companies = data.Companies.Where(c => visibleCompanyIds.Contains(c.CompanyId)).ToList();

                var completions = conn.Query<Completion>(
                    "SELECT CompanyId, AccountId, MonthKey, Status FROM Completions"
                ).Where(c => visibleCompanyIds.Contains(c.CompanyId)).ToList();

                foreach (var c in completions)
                {
                    var key = PeriodHelper.CompletionKey(c.CompanyId, c.AccountId, c.MonthKey);
                    data.Completions[key] = c.Status;
                }

                var comments = conn.Query<Comment>(
                    "SELECT CommentId, CompanyId, Author, Content, CreatedAtUtc FROM Comments ORDER BY CreatedAtUtc"
                ).Where(cm => visibleCompanyIds.Contains(cm.CompanyId)).ToList();

                foreach (var cm in comments)
                {
                    if (!data.Comments.ContainsKey(cm.CompanyId))
                        data.Comments[cm.CompanyId] = new List<Comment>();
                    data.Comments[cm.CompanyId].Add(cm);
                }

                var reasons = conn.Query<OverdueReason>(
                    "SELECT CompanyId, Period, Reason FROM OverdueReasons WHERE Period IN (@PeriodKey, @PrevPeriodKey)",
                    new { PeriodKey = periodKey, PrevPeriodKey = prevPeriodKey }
                ).Where(r => visibleCompanyIds.Contains(r.CompanyId)).ToList();

                var thisMonth = reasons.Where(r => r.Period == periodKey).ToDictionary(r => r.CompanyId, r => r.Reason);
                var prevMonth = reasons.Where(r => r.Period == prevPeriodKey).ToDictionary(r => r.CompanyId, r => r.Reason);

                foreach (var company in data.Companies)
                {
                    if (thisMonth.ContainsKey(company.CompanyId))
                    {
                        data.OverdueReasons[company.CompanyId] = thisMonth[company.CompanyId];
                    }
                    else
                    {
                        var compAccs = data.Accounts.Where(a => a.CompanyId == company.CompanyId).ToList();
                        var prevDone = compAccs.Count > 0 && compAccs.All(a =>
                            GetStatus(data.Completions, company.CompanyId, a.AccountId, prevPeriodKey) == "done");
                        data.OverdueReasons[company.CompanyId] = prevDone
                            ? ""
                            : (prevMonth.ContainsKey(company.CompanyId) ? prevMonth[company.CompanyId] : "");
                    }
                }

                data.AssigneesByCompany = LoadAssigneesByCompany(conn, visibleCompanyIds);
                data.AssigneesByAccount = LoadAssigneesByAccount(conn, new HashSet<long>(data.Accounts.Select(a => a.AccountId)));
            }

            return data;
        }

        public bool CanAccessCompany(AppUser user, long companyId)
        {
            if (user == null || !user.IsActive) return false;
            if (user.Role == AppRole.Admin || user.Role == AppRole.Sysadmin) return true;

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                if (IsUserAssignedToCompany(conn, user.AppUserId, companyId))
                    return true;

                return conn.ExecuteScalar<int>(@"
                    SELECT COUNT(*)
                    FROM AccountAssignments aa
                    INNER JOIN Accounts a ON a.AccountId = aa.AccountId
                    WHERE aa.AppUserId = @AppUserId AND a.CompanyId = @CompanyId",
                    new { AppUserId = user.AppUserId, CompanyId = companyId }) > 0;
            }
        }

        public bool CanAccessAccount(AppUser user, long accountId)
        {
            if (user == null || !user.IsActive) return false;
            if (user.Role == AppRole.Admin || user.Role == AppRole.Sysadmin) return true;

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return IsUserAssignedToAccount(conn, user.AppUserId, accountId);
            }
        }

        public HashSet<long> GetAssignedCompanyIds(long appUserId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return GetAssignedCompanyIds(conn, appUserId);
            }
        }

        public List<long> GetAccountAssignmentUserIds(long accountId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.Query<long>(
                    "SELECT AppUserId FROM AccountAssignments WHERE AccountId = @AccountId ORDER BY AppUserId",
                    new { AccountId = accountId }).ToList();
            }
        }

        public void SetAccountAssignments(long accountId, IEnumerable<long> appUserIds, string assignedByLogin)
        {
            var ids = (appUserIds ?? Enumerable.Empty<long>()).Distinct().ToList();
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    conn.Execute(
                        "DELETE FROM AccountAssignments WHERE AccountId = @AccountId",
                        new { AccountId = accountId }, tx);

                    foreach (var appUserId in ids)
                    {
                        conn.Execute(@"
                            INSERT INTO AccountAssignments (AccountId, AppUserId, AssignedByLogin)
                            VALUES (@AccountId, @AppUserId, @AssignedByLogin)",
                            new { AccountId = accountId, AppUserId = appUserId, AssignedByLogin = assignedByLogin }, tx);
                    }

                    tx.Commit();
                }
            }
        }

        public List<long> GetAssignmentUserIds(long companyId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.Query<long>(
                    "SELECT AppUserId FROM CompanyAssignments WHERE CompanyId = @CompanyId ORDER BY AppUserId",
                    new { CompanyId = companyId }).ToList();
            }
        }

        public void SetCompanyAssignments(long companyId, IEnumerable<long> appUserIds, string assignedByLogin)
        {
            var ids = (appUserIds ?? Enumerable.Empty<long>()).Distinct().ToList();
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    conn.Execute(
                        "DELETE FROM CompanyAssignments WHERE CompanyId = @CompanyId",
                        new { CompanyId = companyId }, tx);

                    foreach (var appUserId in ids)
                    {
                        conn.Execute(@"
                            INSERT INTO CompanyAssignments (CompanyId, AppUserId, AssignedByLogin)
                            VALUES (@CompanyId, @AppUserId, @AssignedByLogin)",
                            new { CompanyId = companyId, AppUserId = appUserId, AssignedByLogin = assignedByLogin }, tx);
                    }

                    tx.Commit();
                }
            }
        }

        public long? GetCommentCompanyId(long commentId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.ExecuteScalar<long?>(
                    "SELECT CompanyId FROM Comments WHERE CommentId = @CommentId",
                    new { CommentId = commentId });
            }
        }

        public long? GetAccountCompanyId(long accountId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.ExecuteScalar<long?>(
                    "SELECT CompanyId FROM Accounts WHERE AccountId = @AccountId",
                    new { AccountId = accountId });
            }
        }

        private static HashSet<long> GetAssignedCompanyIds(SqlConnection conn, long appUserId)
        {
            return new HashSet<long>(conn.Query<long>(
                "SELECT CompanyId FROM CompanyAssignments WHERE AppUserId = @AppUserId",
                new { AppUserId = appUserId }));
        }

        private static HashSet<long> GetAssignedAccountIds(SqlConnection conn, long appUserId)
        {
            return new HashSet<long>(conn.Query<long>(
                "SELECT AccountId FROM AccountAssignments WHERE AppUserId = @AppUserId",
                new { AppUserId = appUserId }));
        }

        private static bool IsUserAssignedToCompany(SqlConnection conn, long appUserId, long companyId)
        {
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM CompanyAssignments WHERE AppUserId = @AppUserId AND CompanyId = @CompanyId",
                new { AppUserId = appUserId, CompanyId = companyId }) > 0;
        }

        private static bool IsUserAssignedToAccount(SqlConnection conn, long appUserId, long accountId)
        {
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM AccountAssignments WHERE AppUserId = @AppUserId AND AccountId = @AccountId",
                new { AppUserId = appUserId, AccountId = accountId }) > 0;
        }

        private static Dictionary<long, List<AppUser>> LoadAssigneesByAccount(SqlConnection conn, HashSet<long> accountIds)
        {
            var result = new Dictionary<long, List<AppUser>>();
            if (accountIds.Count == 0) return result;

            var rows = conn.Query<AccountAssignmentRow>(@"
                SELECT aa.AccountId, u.AppUserId, u.WindowsLogin, u.DisplayName, u.Role, u.IsActive, u.CreatedAtUtc, u.ModifiedAtUtc
                FROM AccountAssignments aa
                INNER JOIN AppUsers u ON u.AppUserId = aa.AppUserId
                WHERE aa.AccountId IN @AccountIds AND u.IsActive = 1
                ORDER BY aa.AccountId, u.WindowsLogin",
                new { AccountIds = accountIds.ToList() }).ToList();

            foreach (var row in rows)
            {
                if (!result.ContainsKey(row.AccountId))
                    result[row.AccountId] = new List<AppUser>();
                result[row.AccountId].Add(MapAssignmentUser(row));
            }

            return result;
        }

        private static AppUser MapAssignmentUser(AccountAssignmentRow row)
        {
            return new AppUser
            {
                AppUserId = row.AppUserId,
                WindowsLogin = row.WindowsLogin,
                DisplayName = row.DisplayName,
                Role = PermissionMatrix.ParseRole(row.Role),
                IsActive = row.IsActive,
                CreatedAtUtc = row.CreatedAtUtc,
                ModifiedAtUtc = row.ModifiedAtUtc
            };
        }

        private static AppUser MapAssignmentUser(AssignmentRow row)
        {
            return new AppUser
            {
                AppUserId = row.AppUserId,
                WindowsLogin = row.WindowsLogin,
                DisplayName = row.DisplayName,
                Role = PermissionMatrix.ParseRole(row.Role),
                IsActive = row.IsActive,
                CreatedAtUtc = row.CreatedAtUtc,
                ModifiedAtUtc = row.ModifiedAtUtc
            };
        }

        private static bool IsUserAssigned(SqlConnection conn, long appUserId, long companyId)
        {
            return IsUserAssignedToCompany(conn, appUserId, companyId);
        }

        private static Dictionary<long, List<AppUser>> LoadAssigneesByCompany(SqlConnection conn, HashSet<long> companyIds)
        {
            var result = new Dictionary<long, List<AppUser>>();
            if (companyIds.Count == 0) return result;

            var rows = conn.Query<AssignmentRow>(@"
                SELECT ca.CompanyId, u.AppUserId, u.WindowsLogin, u.DisplayName, u.Role, u.IsActive, u.CreatedAtUtc, u.ModifiedAtUtc
                FROM CompanyAssignments ca
                INNER JOIN AppUsers u ON u.AppUserId = ca.AppUserId
                WHERE ca.CompanyId IN @CompanyIds AND u.IsActive = 1
                ORDER BY ca.CompanyId, u.WindowsLogin",
                new { CompanyIds = companyIds.ToList() }).ToList();

            foreach (var row in rows)
            {
                if (!result.ContainsKey(row.CompanyId))
                    result[row.CompanyId] = new List<AppUser>();
                result[row.CompanyId].Add(MapAssignmentUser(row));
            }

            return result;
        }

        private class AccountAssignmentRow
        {
            public long AccountId { get; set; }
            public long AppUserId { get; set; }
            public string WindowsLogin { get; set; }
            public string DisplayName { get; set; }
            public string Role { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime? ModifiedAtUtc { get; set; }
        }

        private class AssignmentRow
        {
            public long CompanyId { get; set; }
            public long AppUserId { get; set; }
            public string WindowsLogin { get; set; }
            public string DisplayName { get; set; }
            public string Role { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime? ModifiedAtUtc { get; set; }
        }

        public LedgerData LoadAll(string periodKey, string prevPeriodKey)
        {
            return LoadAll(periodKey, prevPeriodKey, null, false);
        }

        public static string GetStatus(Dictionary<string, string> completions, long companyId, long accountId, string monthKey)
        {
            var key = PeriodHelper.CompletionKey(companyId, accountId, monthKey);
            return completions.TryGetValue(key, out var status) ? status : "none";
        }

        public CompanyStats GetCompanyStats(LedgerData data, Company company, int year, int monthZeroBased)
        {
            var mk = PeriodHelper.ToMonthKey(year, monthZeroBased);
            var accs = data.Accounts.Where(a => a.CompanyId == company.CompanyId).ToList();
            double score = 0;
            var done = 0;
            foreach (var a in accs)
            {
                var v = GetStatus(data.Completions, company.CompanyId, a.AccountId, mk);
                if (v == "done") { score += 1; done++; }
                else if (v == "in-progress") score += 0.5;
            }
            return new CompanyStats
            {
                Done = done,
                Total = accs.Count,
                Pct = accs.Count > 0 ? (int)Math.Round(score / accs.Count * 100) : 0
            };
        }

        public bool IsPeriodDone(LedgerData data, Company company, string periodKey)
        {
            var accs = data.Accounts.Where(a => a.CompanyId == company.CompanyId).ToList();
            if (accs.Count == 0) return false;
            return accs.All(a => GetStatus(data.Completions, company.CompanyId, a.AccountId, periodKey) == "done");
        }

        public ReconciledThrough GetReconciledThrough(LedgerData data, Company company)
        {
            var accs = data.Accounts.Where(a => a.CompanyId == company.CompanyId).ToList();
            if (accs.Count == 0) return null;

            var today = PeriodHelper.GetToday();
            for (var i = 0; i < 24; i++)
            {
                var mo = today.Month - i;
                var yr = today.Year;
                while (mo < 0) { mo += 12; yr--; }
                var mk = PeriodHelper.ToMonthKey(yr, mo);
                if (accs.All(a => GetStatus(data.Completions, company.CompanyId, a.AccountId, mk) == "done"))
                    return new ReconciledThrough { Year = yr, Month = mo };
            }
            return null;
        }

        public string ToggleCompletion(long companyId, long accountId, string monthKey, string userName)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                var current = conn.QueryFirstOrDefault<string>(
                    "SELECT Status FROM Completions WHERE CompanyId = @CompanyId AND AccountId = @AccountId AND MonthKey = @MonthKey",
                    new { CompanyId = companyId, AccountId = accountId, MonthKey = monthKey });

                var next = PeriodHelper.NextStatus(current);
                conn.Execute(@"
                    MERGE Completions AS target
                    USING (SELECT @CompanyId AS CompanyId, @AccountId AS AccountId, @MonthKey AS MonthKey) AS source
                    ON target.CompanyId = source.CompanyId AND target.AccountId = source.AccountId AND target.MonthKey = source.MonthKey
                    WHEN MATCHED THEN UPDATE SET Status = @Status, UpdatedAtUtc = SYSUTCDATETIME(), UpdatedByUser = @User
                    WHEN NOT MATCHED THEN INSERT (CompanyId, AccountId, MonthKey, Status, UpdatedByUser) VALUES (@CompanyId, @AccountId, @MonthKey, @Status, @User);",
                    new { CompanyId = companyId, AccountId = accountId, MonthKey = monthKey, Status = next, User = userName });
                return next;
            }
        }

        public void SaveReason(long companyId, string period, string reason, string userName)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                conn.Execute(@"
                    MERGE OverdueReasons AS target
                    USING (SELECT @CompanyId AS CompanyId, @Period AS Period) AS source
                    ON target.CompanyId = source.CompanyId AND target.Period = source.Period
                    WHEN MATCHED THEN UPDATE SET Reason = @Reason, UpdatedAtUtc = SYSUTCDATETIME(), UpdatedByUser = @User
                    WHEN NOT MATCHED THEN INSERT (CompanyId, Period, Reason, UpdatedByUser) VALUES (@CompanyId, @Period, @Reason, @User);",
                    new { CompanyId = companyId, Period = period, Reason = reason ?? "", User = userName });
            }
        }

        public Comment AddComment(long companyId, string author, string content)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.QueryFirst<Comment>(@"
                    INSERT INTO Comments (CompanyId, Author, Content)
                    OUTPUT INSERTED.CommentId, INSERTED.CompanyId, INSERTED.Author, INSERTED.Content, INSERTED.CreatedAtUtc
                    VALUES (@CompanyId, @Author, @Content);",
                    new { CompanyId = companyId, Author = author, Content = content });
            }
        }

        public bool DeleteComment(long commentId, string author, bool force = false)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                if (force)
                {
                    return conn.Execute(
                        "DELETE FROM Comments WHERE CommentId = @CommentId",
                        new { CommentId = commentId }) > 0;
                }
                return conn.Execute(
                    "DELETE FROM Comments WHERE CommentId = @CommentId AND Author = @Author",
                    new { CommentId = commentId, Author = author }) > 0;
            }
        }

        public Company UpsertCompany(long? companyId, string name, string country)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                if (companyId.HasValue && companyId.Value > 0)
                {
                    conn.Execute(
                        "UPDATE Companies SET Name = @Name, Country = @Country WHERE CompanyId = @CompanyId",
                        new { CompanyId = companyId.Value, Name = name, Country = string.IsNullOrWhiteSpace(country) ? null : country });
                    return conn.QueryFirst<Company>(
                        "SELECT CompanyId, Name, Country, SortOrder FROM Companies WHERE CompanyId = @CompanyId",
                        new { CompanyId = companyId.Value });
                }

                var sortOrder = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(SortOrder), -1) + 1 FROM Companies");
                return conn.QueryFirst<Company>(@"
                    INSERT INTO Companies (Name, Country, SortOrder)
                    OUTPUT INSERTED.CompanyId, INSERTED.Name, INSERTED.Country, INSERTED.SortOrder
                    VALUES (@Name, @Country, @SortOrder);",
                    new { Name = name, Country = string.IsNullOrWhiteSpace(country) ? null : country, SortOrder = sortOrder });
            }
        }

        public void DeleteCompany(long companyId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                conn.Execute("DELETE FROM Companies WHERE CompanyId = @CompanyId", new { CompanyId = companyId });
            }
        }

        public Account UpsertAccount(long? accountId, long companyId, string name)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                if (accountId.HasValue && accountId.Value > 0)
                {
                    conn.Execute(
                        "UPDATE Accounts SET Name = @Name WHERE AccountId = @AccountId",
                        new { AccountId = accountId.Value, Name = name });
                    return conn.QueryFirst<Account>(
                        "SELECT AccountId, CompanyId, Name, SortOrder FROM Accounts WHERE AccountId = @AccountId",
                        new { AccountId = accountId.Value });
                }

                var sortOrder = conn.ExecuteScalar<int>(
                    "SELECT ISNULL(MAX(SortOrder), -1) + 1 FROM Accounts WHERE CompanyId = @CompanyId",
                    new { CompanyId = companyId });
                return conn.QueryFirst<Account>(@"
                    INSERT INTO Accounts (CompanyId, Name, SortOrder)
                    OUTPUT INSERTED.AccountId, INSERTED.CompanyId, INSERTED.Name, INSERTED.SortOrder
                    VALUES (@CompanyId, @Name, @SortOrder);",
                    new { CompanyId = companyId, Name = name, SortOrder = sortOrder });
            }
        }

        public void DeleteAccount(long accountId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                conn.Execute("DELETE FROM Completions WHERE AccountId = @AccountId", new { AccountId = accountId });
                conn.Execute("DELETE FROM Accounts WHERE AccountId = @AccountId", new { AccountId = accountId });
            }
        }
    }
}
