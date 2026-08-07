namespace XsiBookkeeping.Web.Models
{
    public class Company
    {
        public long CompanyId { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public int SortOrder { get; set; }
    }

    public class Account
    {
        public long AccountId { get; set; }
        public long CompanyId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
    }

    public class Completion
    {
        public long CompletionId { get; set; }
        public long CompanyId { get; set; }
        public long AccountId { get; set; }
        public string MonthKey { get; set; }
        public string Status { get; set; }
    }

    public class Comment
    {
        public long CommentId { get; set; }
        public long CompanyId { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
        public System.DateTime CreatedAtUtc { get; set; }
    }

    public class OverdueReason
    {
        public long OverdueReasonId { get; set; }
        public long CompanyId { get; set; }
        public string Period { get; set; }
        public string Reason { get; set; }
    }

    public class CompanyStats
    {
        public int Done { get; set; }
        public int Total { get; set; }
        public int Pct { get; set; }
    }

    public class CompanyAssignment
    {
        public long CompanyAssignmentId { get; set; }
        public long CompanyId { get; set; }
        public long AppUserId { get; set; }
        public System.DateTime AssignedAtUtc { get; set; }
        public string AssignedByLogin { get; set; }
    }

    public class AccountAssignment
    {
        public long AccountAssignmentId { get; set; }
        public long AccountId { get; set; }
        public long AppUserId { get; set; }
        public System.DateTime AssignedAtUtc { get; set; }
        public string AssignedByLogin { get; set; }
    }

    public class ReconciledThrough
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class LedgerData
    {
        public System.Collections.Generic.List<Company> Companies { get; set; } = new System.Collections.Generic.List<Company>();
        public System.Collections.Generic.List<Account> Accounts { get; set; } = new System.Collections.Generic.List<Account>();
        public System.Collections.Generic.Dictionary<string, string> Completions { get; set; } = new System.Collections.Generic.Dictionary<string, string>();
        public System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<Comment>> Comments { get; set; } = new System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<Comment>>();
        public System.Collections.Generic.Dictionary<long, string> OverdueReasons { get; set; } = new System.Collections.Generic.Dictionary<long, string>();
        public System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<AppUser>> AssigneesByCompany { get; set; } = new System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<AppUser>>();
        public System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<AppUser>> AssigneesByAccount { get; set; } = new System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<AppUser>>();
    }
}
