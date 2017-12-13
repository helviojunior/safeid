using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using IAM.WebAPI;
using System.Data.SqlClient;

namespace IAMWebServer._autoservice
{
    public partial class user : System.Web.UI.Page
    {
        public LMenu menu1, menu2, menu3;
        public LoginData login;
        public String subtitle;
        protected void Page_Load(object sender, EventArgs e)
        {
            MAutoservice mClass = ((MAutoservice)this.Master);

            menu1 = menu2 = menu3 = null;

            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();

            menu1 = new LMenu("Home", ApplicationVirtualPath + "autoservice/");
            menu3 = new LMenu("Usuário", ApplicationVirtualPath + "autoservice/user/");

            login = LoginUser.LogedUser(this.Page);

            if (login == null)
            {
                Session["last_page"] = Request.ServerVariables["PATH_INFO"];
                Response.Redirect("/login/");
            }

            String action = "";
            if (RouteData.Values["action"] != null)
                action = RouteData.Values["action"].ToString().ToLower();

            
            String html = "";
            switch (action)
            {
                case "changepassword":
                    subtitle = "Troca de senha";

                    html += "<section><form id=\"pwdForm\" name=\"pwdForm\" method=\"post\" action=\"/consoleapi/changepassword/\" onsubmit=\"return iamadmin.GenericSubmit('#pwdForm');\">";
                    html += "    <div class=\"no-tabs pb10\">";
                    html += "        <div class=\"form-group\">";
                    html += "            <label>" + MessageResource.GetMessage("current_password") + "</label>";
                    html += "            <input id=\"current_password\" name=\"current_password\" placeholder=\"" + MessageResource.GetMessage("current_password") + "\" type=\"password\" maxlength=\"128\" maxlength=\"128\" onfocus=\"$('#current_password').addClass('focus');\" onblur=\"$('#current_password').removeClass('focus');\">";
                    html += "        </div>";
                    html += "        <div class=\"form-group\">";
                    html += "            <label>" + MessageResource.GetMessage("new_password") + "</label>";
                    html += "            <input id=\"password\" name=\"password\" placeholder=\"" + MessageResource.GetMessage("new_password") + "\" type=\"password\" maxlength=\"128\" maxlength=\"128\" onkeyup=\"iamadmin.passwordStrength('#password');\" onfocus=\"$('#password').addClass('focus');\" onblur=\"$('#password').removeClass('focus');\">";
                    html += "        </div>";
                    html += "        <div class=\"form-group\">";
                    html += "            <label>" + MessageResource.GetMessage("new_password_confirm") + "</label>";
                    html += "            <input id=\"password2\" name=\"password2\" placeholder=\"" + MessageResource.GetMessage("new_password_confirm") + "\" type=\"password\" maxlength=\"128\" onfocus=\"$('#password2').addClass('focus');\" onblur=\"$('#password2').removeClass('focus');\">";
                    html += "        </div>";
                    html += "        <div id=\"passwordStrength\" class=\"form-group\">";
                    html += "            <label>" + MessageResource.GetMessage("password_strength") + "</label>";
                    html += "            <div class=\"form-group-content\"><span>" + MessageResource.GetMessage("unknow") + "</span><div class=\"bar\"></div></div>";
                    html += "        </div>";
                    html += "        <div class=\"clear-block\"></div>";
                    html += "    </div>";
                    html += "    <button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">" + MessageResource.GetMessage("change_password") + "</button>";
                    html += "    <a href=\"" + ApplicationVirtualPath + "autoservice/user/\" class=\"button link floatleft\">" + MessageResource.GetMessage("cancel") + "</a>";
                    html += "</form></section>";
                    break;

                default:

                    subtitle = "Informações gerais";
                    html += "<section><form>";
                    html += "    <div class=\"no-tabs pb10\">";
                    html += "        <div class=\"form-group\">";
                    html += "            <label>Nome</label>";
                    html += "            <span class=\"no-edit\">"+ login.FullName +"</span>";
                    html += "        </div>";
                    html += "        <div class=\"form-group\">";
                    html += "            <label>Login</label>";
                    html += "            <span class=\"no-edit\">"+ login.Login +"</span>";
                    html += "        </div>";
                    html += "        <div class=\"clear-block\"></div>";
                    html += "    </div>";
                    html += "</form></section>";
                    
                    break;
            }
            contentHolder.Controls.Add(new LiteralControl(html));

            String sideHTML = "";
            sideHTML += "<ul class=\"user-profile\">";
            sideHTML += "    <li id=\"user-profile-general\" " + (action == "" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "autoservice/user/\">Informações gerais</a></span></li>";
            sideHTML += "    <li id=\"user-profile-password\" " + (action == "changepassword" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "autoservice/user/changepassword/\">Troca de senha</a></span></li>";
            //sideHTML += "    <i id=\"scans-expand-filters\" class=\"icon-right\"></i>";
            //sideHTML += "    <li id=\"user-profile-tags\" " + (action == "" ? "class=\"bold\"" : "") + " href=\"#/users/edit/64764a0f77d1af87fbf808c3c043348c/folders\"><span>Folders</span></li>";
            //sideHTML += "    <li id=\"user-profile-plugin-rules\" " + (action == "" ? "class=\"bold\"" : "") + " href=\"#/users/edit/64764a0f77d1af87fbf808c3c043348c/rules\" class=\"mHide\"><span>Plugin Rules</span></li>";
            sideHTML += "</ul>";

            sideHolder.Controls.Add(new LiteralControl(sideHTML));

            String titleBarHTML = "";

            titleBarHTML += "<ul class=\"mobile-button-bar w50 \">";
            titleBarHTML += "    <li id=\"user-profile-general-mobile\" "+ (action == "" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "autoservice/user/\">Informações gerais</a></li>";
            titleBarHTML += "    <li id=\"user-profile-password-mobile\" " + (action == "changepassword" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "autoservice/user/changepassword/\">Troca de senha</a></li>";
            titleBarHTML += "</ul>";

            titleBarContent.Controls.Add(new LiteralControl(titleBarHTML));

        }
    }
}