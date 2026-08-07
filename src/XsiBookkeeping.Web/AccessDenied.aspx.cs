using System;
using System.Web.UI;

namespace XsiBookkeeping.Web
{
    public partial class AccessDeniedPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var reason = Request.QueryString["reason"];
            if (reason == "admin")
                MessageLiteral.Text = "You do not have permission to access administration pages.";
            else if (reason == "forbidden")
                MessageLiteral.Text = "You do not have permission to perform that action.";
            else
                MessageLiteral.Text = "Your account is not provisioned in Xceleran Ledger.";
        }
    }
}
