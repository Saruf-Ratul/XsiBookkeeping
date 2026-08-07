using System;
using System.Web.UI;
using XsiBookkeeping.Web.Models;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web
{
    public class AdminPageBase : LedgerPageBase
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (CurrentAppUser == null || !PermissionMatrix.Can(CurrentAppUser.Role, Permission.ManageUsers))
            {
                Response.Redirect("~/AccessDenied.aspx?reason=admin");
            }
        }
    }
}
