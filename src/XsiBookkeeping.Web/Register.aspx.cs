using System;
using System.Web.UI;

namespace XsiBookkeeping.Web
{
    public partial class RegisterPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("~/Login.aspx?mode=register", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
