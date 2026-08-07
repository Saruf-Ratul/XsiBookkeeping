using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web.Handlers
{
    public class ApiHandler : IHttpHandler, IRequiresSessionState
    {
        private readonly LedgerRepository _repo = new LedgerRepository();
        private readonly UserRepository _userRepo = new UserRepository();
        private readonly AuditService _audit = new AuditService();

        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Cache-Control", "no-cache");

            if (context.Request.HttpMethod != "POST")
            {
                WriteJson(context, 405, new { success = false, error = "POST required" });
                return;
            }

            var appUser = UserContextService.GetCurrent(context);
            if (appUser == null || !appUser.IsActive)
            {
                WriteJson(context, 403, new { success = false, error = "Forbidden" });
                return;
            }

            var action = context.Request.QueryString["action"];
            var body = ReadBody(context);
            var user = context.User?.Identity?.Name ?? "Unknown";

            try
            {
                switch (action)
                {
                    case "toggleCompletion":
                        Require(context, appUser, Permission.Reconcile);
                        HandleToggle(context, body, user, appUser);
                        break;
                    case "saveReason":
                        Require(context, appUser, Permission.Reconcile);
                        HandleSaveReason(context, body, user, appUser);
                        break;
                    case "addComment":
                        Require(context, appUser, Permission.Comment);
                        HandleAddComment(context, body, user, appUser);
                        break;
                    case "deleteComment":
                        HandleDeleteComment(context, body, user, appUser);
                        break;
                    case "upsertCompany":
                        Require(context, appUser, Permission.ManageCompanies);
                        HandleUpsertCompany(context, body, user);
                        break;
                    case "deleteCompany":
                        Require(context, appUser, Permission.ManageCompanies);
                        HandleDeleteCompany(context, body, user);
                        break;
                    case "upsertAccount":
                        Require(context, appUser, Permission.ManageCompanies);
                        HandleUpsertAccount(context, body, user);
                        break;
                    case "deleteAccount":
                        Require(context, appUser, Permission.ManageCompanies);
                        HandleDeleteAccount(context, body, user);
                        break;
                    case "upsertUser":
                        Require(context, appUser, Permission.ManageUsers);
                        HandleUpsertUser(context, body, user, appUser);
                        break;
                    case "deactivateUser":
                        Require(context, appUser, Permission.ManageUsers);
                        HandleDeactivateUser(context, body, user, appUser);
                        break;
                    case "activateUser":
                        Require(context, appUser, Permission.ManageUsers);
                        HandleActivateUser(context, body, user, appUser);
                        break;
                    case "setAssignments":
                        Require(context, appUser, Permission.ManageCompanies);
                        HandleSetAssignments(context, body, user);
                        break;
                    default:
                        WriteJson(context, 400, new { success = false, error = "Unknown action" });
                        break;
                }
            }
            catch (AuthorizationException)
            {
                _audit.Log(user, "AccessDenied", "Api", action, "Permission denied");
                WriteJson(context, 403, new { success = false, error = "Forbidden" });
            }
            catch (System.Exception ex)
            {
                WriteJson(context, 500, new { success = false, error = ex.Message });
            }
        }

        private static void Require(HttpContext context, AppUser appUser, Permission permission)
        {
            if (!PermissionMatrix.Can(appUser.Role, permission))
                throw new AuthorizationException();
        }

        private static JObject ReadBody(HttpContext context)
        {
            using (var reader = new System.IO.StreamReader(context.Request.InputStream))
            {
                var text = reader.ReadToEnd();
                return string.IsNullOrWhiteSpace(text) ? new JObject() : JObject.Parse(text);
            }
        }

        private static void RequireCompanyAccess(AppUser appUser, long companyId, LedgerRepository repo)
        {
            if (!repo.CanAccessCompany(appUser, companyId))
                throw new AuthorizationException();
        }

        private static void RequireAccountAccess(AppUser appUser, long accountId, LedgerRepository repo)
        {
            if (!repo.CanAccessAccount(appUser, accountId))
                throw new AuthorizationException();
        }

        private void HandleToggle(HttpContext context, JObject body, string user, AppUser appUser)
        {
            var companyId = body.Value<long>("companyId");
            var accountId = body.Value<long>("accountId");
            RequireAccountAccess(appUser, accountId, _repo);
            var monthKey = body.Value<string>("monthKey");
            var status = _repo.ToggleCompletion(companyId, accountId, monthKey, user);
            WriteJson(context, 200, new { success = true, data = new { status } });
        }

        private void HandleSaveReason(HttpContext context, JObject body, string user, AppUser appUser)
        {
            var companyId = body.Value<long>("companyId");
            RequireCompanyAccess(appUser, companyId, _repo);
            var period = body.Value<string>("period");
            var reason = body.Value<string>("reason") ?? "";
            _repo.SaveReason(companyId, period, reason, user);
            WriteJson(context, 200, new { success = true });
        }

        private void HandleAddComment(HttpContext context, JObject body, string user, AppUser appUser)
        {
            var companyId = body.Value<long>("companyId");
            RequireCompanyAccess(appUser, companyId, _repo);
            var content = (body.Value<string>("content") ?? "").Trim();
            if (string.IsNullOrEmpty(content))
            {
                WriteJson(context, 400, new { success = false, error = "Content required" });
                return;
            }
            var comment = _repo.AddComment(companyId, user, content);
            WriteJson(context, 200, new
            {
                success = true,
                data = new
                {
                    commentId = comment.CommentId,
                    companyId = comment.CompanyId,
                    author = comment.Author,
                    content = comment.Content,
                    formattedTime = PeriodHelper.FormatTime(comment.CreatedAtUtc)
                }
            });
        }

        private void HandleDeleteComment(HttpContext context, JObject body, string user, AppUser appUser)
        {
            var commentId = body.Value<long>("commentId");
            var author = _userRepo.GetCommentAuthor(commentId);
            var canDeleteAny = PermissionMatrix.Can(appUser.Role, Permission.DeleteAnyComment);
            var isOwn = string.Equals(author, user, System.StringComparison.OrdinalIgnoreCase);

            if (!canDeleteAny && !isOwn)
                throw new AuthorizationException();

            if (!PermissionMatrix.Can(appUser.Role, Permission.Comment))
                throw new AuthorizationException();

            var companyId = _repo.GetCommentCompanyId(commentId);
            if (!companyId.HasValue)
            {
                WriteJson(context, 404, new { success = false, error = "Comment not found" });
                return;
            }
            RequireCompanyAccess(appUser, companyId.Value, _repo);

            var ok = _repo.DeleteComment(commentId, user, force: canDeleteAny && !isOwn);
            if (ok && canDeleteAny && !isOwn)
                _audit.Log(user, "DeleteComment", "Comment", commentId.ToString(), $"Deleted comment by {author}");
            WriteJson(context, 200, new { success = ok, error = ok ? null : "Not allowed" });
        }

        private void HandleUpsertCompany(HttpContext context, JObject body, string user)
        {
            var companyId = body["companyId"]?.Type == JTokenType.Null ? (long?)null : body.Value<long?>("companyId");
            var name = (body.Value<string>("name") ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                WriteJson(context, 400, new { success = false, error = "Name required" });
                return;
            }
            var country = body.Value<string>("country");
            var company = _repo.UpsertCompany(companyId, name, country);
            _audit.Log(user, companyId.HasValue ? "UpdateCompany" : "CreateCompany", "Company", company.CompanyId.ToString(), name);
            WriteJson(context, 200, new { success = true, data = company });
        }

        private void HandleDeleteCompany(HttpContext context, JObject body, string user)
        {
            var companyId = body.Value<long>("companyId");
            _repo.DeleteCompany(companyId);
            _audit.Log(user, "DeleteCompany", "Company", companyId.ToString());
            WriteJson(context, 200, new { success = true });
        }

        private void HandleUpsertAccount(HttpContext context, JObject body, string user)
        {
            var accountId = body["accountId"]?.Type == JTokenType.Null ? (long?)null : body.Value<long?>("accountId");
            var companyId = body.Value<long>("companyId");
            var name = (body.Value<string>("name") ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                WriteJson(context, 400, new { success = false, error = "Name required" });
                return;
            }
            var account = _repo.UpsertAccount(accountId, companyId, name);
            _audit.Log(user, accountId.HasValue ? "UpdateAccount" : "CreateAccount", "Account", account.AccountId.ToString(), name);
            WriteJson(context, 200, new { success = true, data = account });
        }

        private void HandleDeleteAccount(HttpContext context, JObject body, string user)
        {
            var accountId = body.Value<long>("accountId");
            _repo.DeleteAccount(accountId);
            _audit.Log(user, "DeleteAccount", "Account", accountId.ToString());
            WriteJson(context, 200, new { success = true });
        }

        private void HandleUpsertUser(HttpContext context, JObject body, string actor, AppUser actorUser)
        {
            var appUserId = body["appUserId"]?.Type == JTokenType.Null ? (long?)null : body.Value<long?>("appUserId");
            var windowsLogin = (body.Value<string>("windowsLogin") ?? "").Trim();
            var displayName = (body.Value<string>("displayName") ?? "").Trim();
            var role = PermissionMatrix.ParseRole(body.Value<string>("role"));
            var isActive = body["isActive"]?.Type == JTokenType.Boolean ? body.Value<bool>("isActive") : true;
            var password = body.Value<string>("password");

            if (string.IsNullOrEmpty(windowsLogin))
            {
                WriteJson(context, 400, new { success = false, error = "Username required" });
                return;
            }

            if (!appUserId.HasValue && string.IsNullOrWhiteSpace(password))
            {
                WriteJson(context, 400, new { success = false, error = "Password required for new users" });
                return;
            }

            if (appUserId.HasValue)
            {
                var existing = _userRepo.GetById(appUserId.Value);
                if (existing != null && existing.Role == AppRole.Sysadmin && role != AppRole.Sysadmin)
                {
                    if (_userRepo.CountActiveSysadmins(appUserId.Value) == 0)
                    {
                        WriteJson(context, 400, new { success = false, error = "Cannot demote the last Sysadmin" });
                        return;
                    }
                }
                if (existing != null && existing.Role == AppRole.Sysadmin && !isActive)
                {
                    if (_userRepo.CountActiveSysadmins(appUserId.Value) == 0)
                    {
                        WriteJson(context, 400, new { success = false, error = "Cannot deactivate the last Sysadmin" });
                        return;
                    }
                }
            }

            var user = _userRepo.Upsert(appUserId, windowsLogin, displayName, role, isActive, password);
            _audit.Log(actor, appUserId.HasValue ? "UpdateUser" : "CreateUser", "AppUser", user.AppUserId.ToString(),
                $"{user.WindowsLogin} role={PermissionMatrix.RoleName(role)} active={isActive}");
            WriteJson(context, 200, new { success = true, data = user });
        }

        private void HandleDeactivateUser(HttpContext context, JObject body, string actor, AppUser actorUser)
        {
            var appUserId = body.Value<long>("appUserId");
            var existing = _userRepo.GetById(appUserId);
            if (existing == null)
            {
                WriteJson(context, 404, new { success = false, error = "User not found" });
                return;
            }

            if (existing.Role == AppRole.Sysadmin && _userRepo.CountActiveSysadmins(appUserId) == 0)
            {
                WriteJson(context, 400, new { success = false, error = "Cannot deactivate the last Sysadmin" });
                return;
            }

            _userRepo.SetActive(appUserId, false);
            _audit.Log(actor, "DeactivateUser", "AppUser", appUserId.ToString(), existing.WindowsLogin);
            WriteJson(context, 200, new { success = true });
        }

        private void HandleActivateUser(HttpContext context, JObject body, string actor, AppUser actorUser)
        {
            var appUserId = body.Value<long>("appUserId");
            var existing = _userRepo.GetById(appUserId);
            if (existing == null)
            {
                WriteJson(context, 404, new { success = false, error = "User not found" });
                return;
            }

            _userRepo.SetActive(appUserId, true);
            _audit.Log(actor, "ActivateUser", "AppUser", appUserId.ToString(), existing.WindowsLogin);
            WriteJson(context, 200, new { success = true });
        }

        private void HandleSetAssignments(HttpContext context, JObject body, string actor)
        {
            var accountId = body.Value<long>("accountId");
            var appUserIds = body["appUserIds"] != null
                ? body["appUserIds"].ToObject<long[]>()
                : new long[0];

            if (accountId <= 0)
            {
                WriteJson(context, 400, new { success = false, error = "Invalid task" });
                return;
            }

            _repo.SetAccountAssignments(accountId, appUserIds, actor);
            var names = string.Join(", ", appUserIds);
            _audit.Log(actor, "SetAssignments", "Account", accountId.ToString(), $"Users: {names}");
            WriteJson(context, 200, new { success = true });
        }

        private static void WriteJson(HttpContext context, int statusCode, object obj)
        {
            context.Response.StatusCode = statusCode;
            context.Response.Write(JsonConvert.SerializeObject(obj));
        }

        private class AuthorizationException : System.Exception { }
    }
}
