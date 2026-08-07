using System;
using System.Web;
using System.Web.Security;

namespace XsiBookkeeping.Web.Services
{
    public static class AuthHelper
    {
        public static void SignOut(HttpContext context)
        {
            if (context == null) return;

            FormsAuthentication.SignOut();

            ExpireCookie(context, FormsAuthentication.FormsCookieName, FormsAuthentication.FormsCookiePath);

            if (context.Session != null)
                context.Session.Abandon();

            ExpireCookie(context, "ASP.NET_SessionId", "/");
        }

        private static void ExpireCookie(HttpContext context, string name, string path)
        {
            var expired = new HttpCookie(name, "")
            {
                Expires = DateTime.Now.AddYears(-1),
                Path = string.IsNullOrEmpty(path) ? "/" : path
            };
            context.Response.Cookies.Add(expired);
        }
    }
}
