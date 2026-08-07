using System;

namespace XsiBookkeeping.Web
{
    public partial class DefaultPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                Response.Redirect("~/Overview.aspx");
            else
                Response.Redirect("~/Login.aspx");
        }
    }
}
