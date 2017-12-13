<%@ Page Title="SafeID - Gestão de identidades e acessos" Language="C#" MasterPageFile="~/_autoservice/Autoservice.Master" AutoEventWireup="true" CodeBehind="autoservice.aspx.cs" Inherits="IAMWebServer._autoservice.autoservice" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server"></asp:Content>

<asp:Content ID="Content4" ContentPlaceHolderID="title" runat="server">
    Opções
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="content" runat="server">
    
        <section id="main" class="wrapper full-width">
            <div class="content">
                <asp:PlaceHolder ID="contentHolder" runat="server"></asp:PlaceHolder>
            </div>
            <div class="clear-block"></div>
        </section>
    
</asp:Content>

<asp:Content ID="Content7" ContentPlaceHolderID="POSTContent" runat="server"><asp:PlaceHolder ID="POSTData" runat="server"></asp:PlaceHolder></asp:Content>
