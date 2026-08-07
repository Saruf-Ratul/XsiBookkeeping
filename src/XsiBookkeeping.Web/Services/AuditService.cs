using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using Dapper;
using XsiBookkeeping.Web.Models;

namespace XsiBookkeeping.Web.Services
{
    public class AuditService
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["LedgerDb"].ConnectionString;

        public void Log(string actorLogin, string action, string entityType = null, string entityId = null, string details = null)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                conn.Execute(@"
                    INSERT INTO AuditLogs (ActorLogin, Action, EntityType, EntityId, Details)
                    VALUES (@ActorLogin, @Action, @EntityType, @EntityId, @Details)",
                    new
                    {
                        ActorLogin = UserContextService.NormalizeLogin(actorLogin),
                        Action = action,
                        EntityType = entityType,
                        EntityId = entityId,
                        Details = details
                    });
            }
        }

        public List<AuditLogEntry> GetRecent(int limit = 200)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.Query<AuditLogEntry>(@"
                    SELECT TOP (@Limit) AuditLogId, ActorLogin, Action, EntityType, EntityId, Details, CreatedAtUtc
                    FROM AuditLogs ORDER BY CreatedAtUtc DESC",
                    new { Limit = limit }).AsList();
            }
        }
    }
}
