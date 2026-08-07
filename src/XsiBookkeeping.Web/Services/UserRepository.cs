using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using XsiBookkeeping.Web.Models;

namespace XsiBookkeeping.Web.Services
{
    public class UserRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["LedgerDb"].ConnectionString;

        private const string SelectColumns =
            "AppUserId, WindowsLogin, DisplayName, Role, IsActive, PasswordHash, CreatedAtUtc, ModifiedAtUtc";

        public AppUser ValidateLogin(string username, string password)
        {
            var normalized = UserContextService.NormalizeLogin(username);
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                var row = conn.QueryFirstOrDefault<AppUserRow>(
                    "SELECT " + SelectColumns + " FROM AppUsers WHERE WindowsLogin = @Login AND IsActive = 1",
                    new { Login = normalized });
                if (row == null || string.IsNullOrEmpty(row.PasswordHash))
                    return null;
                if (!PasswordHasher.VerifyPassword(password, row.PasswordHash))
                    return null;
                return Map(row);
            }
        }

        public bool LoginExists(string username)
        {
            var normalized = UserContextService.NormalizeLogin(username);
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM AppUsers WHERE WindowsLogin = @Login",
                    new { Login = normalized }) > 0;
            }
        }

        public AppUser Register(string username, string displayName, string password)
        {
            if (LoginExists(username))
                throw new InvalidOperationException("That username is already taken.");

            var name = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim();
            return Upsert(null, username, name, AppRole.User, true, password);
        }

        public AppUser GetByWindowsLogin(string windowsLogin)
        {
            var normalized = UserContextService.NormalizeLogin(windowsLogin);
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                var row = conn.QueryFirstOrDefault<AppUserRow>(
                    "SELECT " + SelectColumns + " FROM AppUsers WHERE WindowsLogin = @Login AND IsActive = 1",
                    new { Login = normalized });
                return Map(row);
            }
        }

        public AppUser GetById(long appUserId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                var row = conn.QueryFirstOrDefault<AppUserRow>(
                    "SELECT " + SelectColumns + " FROM AppUsers WHERE AppUserId = @AppUserId",
                    new { AppUserId = appUserId });
                return Map(row);
            }
        }

        public List<AppUser> GetAll()
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.Query<AppUserRow>(
                    "SELECT " + SelectColumns + " FROM AppUsers ORDER BY Role DESC, WindowsLogin"
                ).Select(Map).Where(u => u != null).ToList();
            }
        }

        public int CountActiveSysadmins(long excludeUserId = 0)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM AppUsers WHERE Role = @Role AND IsActive = 1 AND AppUserId <> @ExcludeUserId",
                    new { Role = PermissionMatrix.RoleName(AppRole.Sysadmin), ExcludeUserId = excludeUserId });
            }
        }

        public AppUser Upsert(long? appUserId, string windowsLogin, string displayName, AppRole role, bool isActive, string password = null)
        {
            var normalized = UserContextService.NormalizeLogin(windowsLogin);
            var passwordHash = string.IsNullOrEmpty(password) ? null : PasswordHasher.HashPassword(password);

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                if (appUserId.HasValue && appUserId.Value > 0)
                {
                    if (!string.IsNullOrEmpty(passwordHash))
                    {
                        conn.Execute(@"
                            UPDATE AppUsers SET WindowsLogin = @WindowsLogin, DisplayName = @DisplayName, Role = @Role,
                                IsActive = @IsActive, PasswordHash = @PasswordHash, ModifiedAtUtc = SYSUTCDATETIME()
                            WHERE AppUserId = @AppUserId",
                            new
                            {
                                AppUserId = appUserId.Value,
                                WindowsLogin = normalized,
                                DisplayName = displayName,
                                Role = PermissionMatrix.RoleName(role),
                                IsActive = isActive,
                                PasswordHash = passwordHash
                            });
                    }
                    else
                    {
                        conn.Execute(@"
                            UPDATE AppUsers SET WindowsLogin = @WindowsLogin, DisplayName = @DisplayName, Role = @Role,
                                IsActive = @IsActive, ModifiedAtUtc = SYSUTCDATETIME()
                            WHERE AppUserId = @AppUserId",
                            new
                            {
                                AppUserId = appUserId.Value,
                                WindowsLogin = normalized,
                                DisplayName = displayName,
                                Role = PermissionMatrix.RoleName(role),
                                IsActive = isActive
                            });
                    }
                    return GetById(appUserId.Value);
                }

                if (string.IsNullOrEmpty(passwordHash))
                    throw new InvalidOperationException("Password is required for new users.");

                var row = conn.QueryFirst<AppUserRow>(@"
                    INSERT INTO AppUsers (WindowsLogin, DisplayName, Role, IsActive, PasswordHash)
                    OUTPUT INSERTED.AppUserId, INSERTED.WindowsLogin, INSERTED.DisplayName, INSERTED.Role, INSERTED.IsActive, INSERTED.PasswordHash, INSERTED.CreatedAtUtc, INSERTED.ModifiedAtUtc
                    VALUES (@WindowsLogin, @DisplayName, @Role, @IsActive, @PasswordHash)",
                    new
                    {
                        WindowsLogin = normalized,
                        DisplayName = displayName,
                        Role = PermissionMatrix.RoleName(role),
                        IsActive = isActive,
                        PasswordHash = passwordHash
                    });
                return Map(row);
            }
        }

        public void SetActive(long appUserId, bool isActive)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                conn.Execute(
                    "UPDATE AppUsers SET IsActive = @IsActive, ModifiedAtUtc = SYSUTCDATETIME() WHERE AppUserId = @AppUserId",
                    new { AppUserId = appUserId, IsActive = isActive });
            }
        }

        public string GetCommentAuthor(long commentId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.ExecuteScalar<string>(
                    "SELECT Author FROM Comments WHERE CommentId = @CommentId",
                    new { CommentId = commentId });
            }
        }

        private static AppUser Map(AppUserRow row)
        {
            if (row == null) return null;
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

        private class AppUserRow
        {
            public long AppUserId { get; set; }
            public string WindowsLogin { get; set; }
            public string DisplayName { get; set; }
            public string Role { get; set; }
            public bool IsActive { get; set; }
            public string PasswordHash { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime? ModifiedAtUtc { get; set; }
        }
    }
}
