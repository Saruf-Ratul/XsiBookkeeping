using System;
using System.Web.UI;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web
{
    public partial class LogoutPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            AuthHelper.SignOut(Context);
            Response.Redirect("~/Login.aspx?signedOut=1", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
