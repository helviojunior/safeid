<%@ Page Title="" Language="C#" MasterPageFile="~/login/Login.Master" AutoEventWireup="true" CodeBehind="default_old.aspx.cs" Inherits="IAMWebServer.login.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<form id="serviceLogin" name="serviceLogin" method="post" action="/consoleapi/login/">
    <asp:PlaceHolder ID="holderContent" runat="server"></asp:PlaceHolder>
</form>
</asp:Content>
