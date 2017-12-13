<%@ Page Title="SafeID - Gestão de identidades e acessos" Language="C#" MasterPageFile="~/_autoservice/Autoservice.Master" AutoEventWireup="true" CodeBehind="access_request.aspx.cs" Inherits="IAMWebServer._autoservice.access_request" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <asp:PlaceHolder ID="headContent" runat="server"></asp:PlaceHolder>
</asp:Content>

<asp:Content ID="Content4" ContentPlaceHolderID="title" runat="server">
    <%=login.Login%>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="titleContent" runat="server">
        
    <div class="mobile-button-bar-wrapper" style="display: block;">
        <asp:PlaceHolder ID="titleBarContent" runat="server"></asp:PlaceHolder>
    <div class="clear"></div></div>
    
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="content" runat="server">
    
        <section id="main" class="wrapper">
            <aside>
                <% if (menu1 != null) { %>
                <div class="sep">
                    <div class="section-nav-header">
                        <div class="crumbs">
        
                            <div class="subject subject-color">
                                <a href="<%=menu1.HRef%>"><%=menu1.Name%></a>
                            </div>
        
                            <% if (menu2 != null) { %>
                            <div class="topic topic-color">
                                <a href="<%=menu2.HRef%>"><%=menu2.Name%></a>
                            </div>
                            <% } %>
                        </div>
                        <% if (menu3 != null) { %>
                        <div class="crumbs tutorial-title">
                            <h2 class="title tutorial-color">
                                <%=menu3.Name%>
                            </h2>
                        </div>
                        <% } %>
                    </div>
                </div>
                <% } %>
                <asp:PlaceHolder ID="sideHolder" runat="server"></asp:PlaceHolder>
            </aside>
            <div class="content">
                <h3><%=login.Login%> / <span class="subtitle"><%=subtitle%></span></h3>
                <asp:PlaceHolder ID="contentHolder" runat="server"></asp:PlaceHolder>
            </div>
            <div class="clear-block"></div>
        </section>
    
</asp:Content>
