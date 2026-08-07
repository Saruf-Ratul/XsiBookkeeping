using System;
using System.Web;
using XsiBookkeeping.Web.Models;

namespace XsiBookkeeping.Web.Services
{
    public static class UserContextService
    {
        private const string CacheKey = "Ledger.AppUser";

        public static string NormalizeLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return "";
            return login.Trim().ToUpperInvariant();
        }

        public static AppUser GetCurrent(HttpContext context)
        {
            if (context?.Items[CacheKey] is AppUser cached)
                return cached;

            var windowsLogin = context?.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(windowsLogin))
                return null;

            var repo = new UserRepository();
            var user = repo.GetByWindowsLogin(windowsLogin);
            if (context != null)
                context.Items[CacheKey] = user;
            return user;
        }

        public static void ClearCache(HttpContext context)
        {
            context?.Items.Remove(CacheKey);
        }
    }
}
