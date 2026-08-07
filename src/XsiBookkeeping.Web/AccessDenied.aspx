<%@ Page Title="Access Denied" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AccessDenied.aspx.cs" Inherits="XsiBookkeeping.Web.AccessDeniedPage" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Access Denied</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="ledger-container fade-in">
        <div class="access-denied-card">
            <div class="access-denied-icon">!</div>
            <h1 class="ledger-title" style="margin-bottom:8px">Access Denied</h1>
            <p class="access-denied-text"><asp:Literal ID="MessageLiteral" runat="server" /></p>
            <p class="access-denied-login">Signed in as <strong><asp:LoginName runat="server" /></strong></p>
            <p class="access-denied-hint">Contact a Sysadmin to request access, or sign in with a different account.</p>
            <p style="margin-top:20px"><a href="Login.aspx?signedOut=1" class="ledger-btn ledger-btn-primary" style="text-decoration:none;display:inline-block">Back to sign in</a></p>
        </div>
    </div>
</asp:Content>
