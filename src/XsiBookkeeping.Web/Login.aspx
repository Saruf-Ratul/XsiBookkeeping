<%@ Page Title="Login" Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="XsiBookkeeping.Web.LoginPage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title><%= PageTitle %></title>
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link href="https://fonts.googleapis.com/css2?family=Nunito:wght@400;600;700&display=swap" rel="stylesheet" />
    <link href="<%= ResolveUrl("~/Assets/css/ledger.css") %>" rel="stylesheet" />
</head>
<body class="ledger-app login-page">
    <div class="login-shell fade-in">
        <div class="login-card">
            <div class="login-brand">
                <img src="<%= LogoUrl %>" alt="Xceleran Ledger" class="login-logo" />
            </div>

            <form id="LoginForm" runat="server">
            <asp:HiddenField ID="AuthModeField" runat="server" Value="signin" />
            <asp:Panel ID="SignInPanel" runat="server">
                <h1 class="login-title">Sign in</h1>
                <p class="login-subtitle">Monthly reconciliation tracker</p>
                <asp:Label ID="SuccessLabel" runat="server" CssClass="login-success" Visible="false" />
                <asp:Label ID="ErrorLabel" runat="server" CssClass="login-error" Visible="false" />
                <div class="login-field">
                    <label for="<%= UsernameInput.ClientID %>">Username</label>
                    <asp:TextBox ID="UsernameInput" runat="server" CssClass="login-input" placeholder="Username" />
                </div>
                <div class="login-field">
                    <label for="<%= PasswordInput.ClientID %>">Password</label>
                    <asp:TextBox ID="PasswordInput" runat="server" TextMode="Password" CssClass="login-input" placeholder="Password" />
                </div>
                <div class="login-remember">
                    <asp:CheckBox ID="RememberCheck" runat="server" Text="Keep me signed in" />
                </div>
                <asp:Button ID="LoginButton" runat="server" Text="Sign In" CssClass="login-submit" OnClick="LoginButton_Click" />
                <p class="login-footer">Don&apos;t have an account? <asp:HyperLink ID="CreateAccountLink" runat="server" CssClass="login-footer-link">Create one</asp:HyperLink></p>
            </asp:Panel>

            <asp:Panel ID="RegisterPanel" runat="server" Visible="false">
                <h1 class="login-title">Create account</h1>
                <p class="login-subtitle">Sign up for Xceleran Ledger</p>
                <asp:Label ID="RegisterErrorLabel" runat="server" CssClass="login-error" Visible="false" />
                <div class="login-field">
                    <label for="<%= RegisterUsernameInput.ClientID %>">Username</label>
                    <asp:TextBox ID="RegisterUsernameInput" runat="server" CssClass="login-input" placeholder="Username" />
                </div>
                <div class="login-field">
                    <label for="<%= DisplayNameInput.ClientID %>">Display name <span class="login-optional">(optional)</span></label>
                    <asp:TextBox ID="DisplayNameInput" runat="server" CssClass="login-input" placeholder="Your name" />
                </div>
                <div class="login-field">
                    <label for="<%= RegisterPasswordInput.ClientID %>">Password</label>
                    <asp:TextBox ID="RegisterPasswordInput" runat="server" TextMode="Password" CssClass="login-input" placeholder="At least 8 characters" />
                </div>
                <div class="login-field">
                    <label for="<%= ConfirmPasswordInput.ClientID %>">Confirm password</label>
                    <asp:TextBox ID="ConfirmPasswordInput" runat="server" TextMode="Password" CssClass="login-input" placeholder="Re-enter password" />
                </div>
                <div class="login-field">
                    <label for="<%= AdminCodeInput.ClientID %>">Admin code</label>
                    <asp:TextBox ID="AdminCodeInput" runat="server" TextMode="Password" CssClass="login-input" placeholder="Required to create an account" />
                </div>
                <asp:Button ID="RegisterButton" runat="server" Text="Create Account" CssClass="login-submit" OnClick="RegisterButton_Click" />
                <p class="login-footer">Already have an account? <asp:HyperLink ID="SignInLink" runat="server" CssClass="login-footer-link">Sign in</asp:HyperLink></p>
            </asp:Panel>
            </form>
        </div>
    </div>
</body>
</html>
