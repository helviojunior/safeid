<%@ Page Title="SafeID - Gestão de identidades e acessos" Language="C#" MasterPageFile="~/_admin/Admin.Master" AutoEventWireup="true" CodeBehind="template.aspx.cs" Inherits="IAMWebServer._admin.template" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="mobileHeader" runat="server">
    <i id="mobile-menu-back" class="fontawesome back" data-href="" style="display: none;"></i>
    <ul id="mobile-menu-toggle-on" style="display: none;">
        <li></li>
        <li></li>
        <li></li>
    </ul>
    <i id="mobile-menu-new" data-href="" class="glyphicons add" style="display: none;"></i>
    <i id="mobile-menu-info" data-type="" class="glyphicons info" style="display: none;"></i>
    <h2 id="H2"><%=moduleName%></h2>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="navHolder" runat="server">
    <div class="menu-btn menu mHide">
        <span class="menu-item ico icon-home1">Menu</span>
        <ul class="pannel">
            <li>
                <h3>Dasboards</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/">Dashboard</a>
            </li>
            <li>
                <h3>Usuários</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/users/">Gerenciador de usuários</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/users/new/">Novo usuário</a>
            </li>
            <li>
                <h3>Sistema</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/logs/">Visualizador de logs do sistema</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/system_roles/">Perfis de sistema (roles)</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/service_status/">Status dos serviços</a>
            </li>
            <li>
                <h3>Perfis/roles & Pastas</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/roles/">Gerenciador de perfis</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/container/">Gerenciador de pastas</a>
            </li>
            <li>
                <h3>Filtros</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/filter/">Gerenciador de filtros</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/filter/new/">Novo filtro</a>
            </li>
            <li>
                <h3>Workflow & Requisições</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/workflow/">Gerenciador de workflows</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/access_request/">Requisições de acesso</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/workflow/new/">Novo workflow</a>
            </li>
            <li>
                <h3>Recurso & Plugin</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/resource/">Gerenciador de Recurso</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/plugin/">Gerenciador de Plugins</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/resource_plugin/">Recurso x Plugin</a>
            </li>
            <li>
                <h3>Campos</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/field/">Gerenciador de campos</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/field/new/">Novo campo</a>
            </li>
            <li>
                <h3>Proxy</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/proxy/">Gerenciador de proxies</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/proxy/new/">Novo proxy</a>
            </li>
            <li>
                <h3>Licenciamento</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/license/">Informações do licenciamento</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/license/new/">Carregar nova licença</a>
            </li>
            <li>
                <h3>Empresa & Contexto</h3>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/enterprise/">Gerenciador da empresa</a>
                <a href="<%=Session["ApplicationVirtualPath"]%>admin/context/">Gerenciador de contexto</a>
                <a href="http://www.safetrend.com.br" target="_blank">SafeTrend.com.br</a>
            </li>
            <!--li>
                <h3>SafeTrend</h3>
                <a href="http://www.safetrend.com.br" target="_blank">SafeTrend.com.br</a>
            </li-->
            
        </ul>
    </div>    
    <!--div class="menu-btn mHide">
    <span class="menu-item ico icon-start">Favoritos</span>
    </div-->
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="titleBar" runat="server">
    <div class="wrapper">
        <h2><%=moduleName%></h2>
        <% if (showSearchBox) {%>
        <div id="searchbox">
            <input type="text" placeholder='<%=searchText%>' class="search-scans" autocapitalize="off" autocomplete="off" autocorrect="off" spellcheck="false">
            <i class="icon-search"></i>
        </div>
        <% }%>
        <asp:PlaceHolder ID="btnBox" runat="server"></asp:PlaceHolder>
        <div class="clear-block"></div>
    </div>
    <div id="mobilebar"></div>
</asp:Content>
<asp:Content ID="Content5" ContentPlaceHolderID="content" runat="server">
    
        <section id="main" class="wrapper <%=(fullWidth ? "full-width" : "") %>">
            <aside>
                <asp:PlaceHolder ID="sideHolder" runat="server"></asp:PlaceHolder>
            </aside>
            <div class="content">
                <asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>
                <div id="content-wrapper"></div>
                <div id="content-loader"></div>
            </div>
            <div class="clear-block"></div>      
        </section>

</asp:Content>
