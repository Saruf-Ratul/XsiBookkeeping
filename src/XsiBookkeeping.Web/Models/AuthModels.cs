using System;

namespace XsiBookkeeping.Web.Models
{
    public enum AppRole
    {
        User,
        Admin,
        Sysadmin
    }

    public enum Permission
    {
        ViewApp,
        Reconcile,
        Comment,
        DeleteAnyComment,
        ManageCompanies,
        ManageUsers,
        ViewAudit
    }

    public class AppUser
    {
        public long AppUserId { get; set; }
        public string WindowsLogin { get; set; }
        public string DisplayName { get; set; }
        public AppRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }

    public class AuditLogEntry
    {
        public long AuditLogId { get; set; }
        public string ActorLogin { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
