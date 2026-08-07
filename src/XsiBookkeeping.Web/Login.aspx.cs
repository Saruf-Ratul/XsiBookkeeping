using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using XsiBookkeeping.Web.Services;

namespace XsiBookkeeping.Web
{
    public partial class LoginPage : Page
    {
        private const string RequiredAdminCode = "XSI2026!";

        private readonly UserRepository _userRepo = new UserRepository();

        protected string PageTitle = "Sign In - Xceleran Ledger";
        protected string LogoUrl = "";

        private bool IsRegisterMode =>
            string.Equals(AuthModeField.Value, "register", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Request.QueryString["mode"], "register", StringComparison.OrdinalIgnoreCase);

        protected void Page_Init(object sender, EventArgs e)
        {
            if (ShouldShowRegisterPanel())
                ShowRegisterMode();
            else
                ShowSignInMode();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && string.Equals(Request.QueryString["mode"], "register", StringComparison.OrdinalIgnoreCase))
                AuthModeField.Value = "register";

            LogoUrl = ResolveUrl("~/Assets/images/xceleran-ledger-logo.png");
            CreateAccountLink.NavigateUrl = ResolveUrl("~/Login.aspx?mode=register");
            SignInLink.NavigateUrl = ResolveUrl("~/Login.aspx");

            if (!IsPostBack && string.Equals(Request.QueryString["signedOut"], "1", StringComparison.OrdinalIgnoreCase))
            {
                AuthHelper.SignOut(Context);
                if (!string.IsNullOrEmpty(Request.QueryString["ReturnUrl"]))
                {
                    Response.Redirect("~/Login.aspx?signedOut=1", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                ShowSignInMode();
                SuccessLabel.Text = "You have been signed out.";
                SuccessLabel.Visible = true;
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var appUser = _userRepo.GetByWindowsLogin(User.Identity.Name);
                if (appUser != null && appUser.IsActive)
                {
                    Response.Redirect("~/Overview.aspx");
                    return;
                }

                AuthHelper.SignOut(Context);
            }

            if (string.Equals(Request.QueryString["registered"], "1", StringComparison.OrdinalIgnoreCase)
                && !IsRegisterMode)
            {
                SuccessLabel.Text = "Account created. Sign in with your new username and password.";
                SuccessLabel.Visible = true;
            }
        }

        protected void LoginButton_Click(object sender, EventArgs e)
        {
            ShowSignInMode();

            var username = UsernameInput.Text.Trim();
            var password = PasswordInput.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowSignInError("Enter your username and password.");
                return;
            }

            var user = _userRepo.ValidateLogin(username, password);
            if (user == null)
            {
                ShowSignInError(GetLoginFailureMessage(username));
                return;
            }

            FormsAuthentication.SetAuthCookie(user.WindowsLogin, RememberCheck.Checked);
            var returnUrl = Request.QueryString["ReturnUrl"];
            if (!string.IsNullOrEmpty(returnUrl)
                && returnUrl.StartsWith("/")
                && !returnUrl.StartsWith("//")
                && !returnUrl.StartsWith("/Login.aspx", StringComparison.OrdinalIgnoreCase)
                && !returnUrl.StartsWith("/Register.aspx", StringComparison.OrdinalIgnoreCase))
                Response.Redirect(returnUrl);
            else
                Response.Redirect("~/Overview.aspx");
        }

        protected void RegisterButton_Click(object sender, EventArgs e)
        {
            ShowRegisterMode();

            var username = RegisterUsernameInput.Text.Trim();
            var displayName = DisplayNameInput.Text.Trim();
            var password = RegisterPasswordInput.Text;
            var confirmPassword = ConfirmPasswordInput.Text;
            var adminCode = AdminCodeInput.Text;

            if (string.IsNullOrEmpty(username))
            {
                ShowRegisterError("Enter a username.");
                return;
            }

            if (username.Length < 3)
            {
                ShowRegisterError("Username must be at least 3 characters.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowRegisterError("Enter a password.");
                return;
            }

            if (password.Length < 8)
            {
                ShowRegisterError("Password must be at least 8 characters.");
                return;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                ShowRegisterError("Passwords do not match.");
                return;
            }

            if (string.IsNullOrEmpty(adminCode))
            {
                ShowRegisterError("Enter the admin code.");
                return;
            }

            if (!string.Equals(adminCode, RequiredAdminCode, StringComparison.Ordinal))
            {
                ShowRegisterError("Invalid admin code. Account was not created.");
                return;
            }

            try
            {
                _userRepo.Register(username, displayName, password);
            }
            catch (InvalidOperationException ex)
            {
                ShowRegisterError(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                ShowRegisterError(GetRegisterFailureMessage(ex));
                return;
            }

            Response.Redirect("~/Login.aspx?registered=1", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ShowSignInMode()
        {
            PageTitle = "Sign In - Xceleran Ledger";
            AuthModeField.Value = "signin";
            SignInPanel.Visible = true;
            RegisterPanel.Visible = false;
        }

        private void ShowRegisterMode()
        {
            PageTitle = "Create Account - Xceleran Ledger";
            AuthModeField.Value = "register";
            SignInPanel.Visible = false;
            RegisterPanel.Visible = true;
        }

        private void ShowSignInError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.Visible = true;
        }

        private void ShowRegisterError(string message)
        {
            RegisterErrorLabel.Text = message;
            RegisterErrorLabel.Visible = true;
        }

        private bool ShouldShowRegisterPanel()
        {
            if (string.Equals(Request.QueryString["mode"], "register", StringComparison.OrdinalIgnoreCase))
                return true;

            if (IsPostBack)
            {
                var mode = Request.Form[AuthModeField.UniqueID];
                if (string.Equals(mode, "register", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private string GetLoginFailureMessage(string username)
        {
            try
            {
                if (!_userRepo.LoginExists(username))
                {
                    return "Unknown username. Run database/seed/bootstrap_sysadmin.sql to create the default admin account, or register with an admin code.";
                }
            }
            catch (Exception ex)
            {
                return GetRegisterFailureMessage(ex);
            }

            return "Invalid password, or the account is inactive. Ask a Sysadmin to activate your account or run database/seed/reset_admin_password.sql.";
        }

        private static string GetRegisterFailureMessage(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                var message = current.Message ?? "";
                if (message.IndexOf("Invalid object name 'AppUsers'", StringComparison.OrdinalIgnoreCase) >= 0
                    || message.IndexOf("Cannot open database", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "The user database is not set up yet. Run migrations V001-V003, then try again.";
                }

                if (message.IndexOf("Invalid column name 'PasswordHash'", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "The password column is missing. Run database/migrations/V003__UserPasswords.sql, then try again.";
                }
            }

            return "Could not create the account right now. Check the database connection and try again.";
        }
    }
}
