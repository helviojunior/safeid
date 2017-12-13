<%@ Page Title="" Language="C#" MasterPageFile="~/login/Login.Master" AutoEventWireup="true" CodeBehind="changepwd.aspx.cs" Inherits="IAMWebServer.login.changepwd" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<form id="serviceRecover" name="serviceLogin" method="post" action="/consoleapi/changepassword2/">
    <asp:PlaceHolder ID="holderContent" runat="server"></asp:PlaceHolder>
</form>
</asp:Content>
