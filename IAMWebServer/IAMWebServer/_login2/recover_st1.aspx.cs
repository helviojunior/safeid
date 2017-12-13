using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using System.Data;
using System.IO;
using System.Web.Hosting;
using IAM.CodeManager;

namespace IAMWebServer._login2
{
    public partial class recover_st1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            String html = "";
            String error = "";

            html += "<form id=\"serviceLogin\" name=\"serviceLogin\" method=\"post\" action=\"" + Session["ApplicationVirtualPath"] + "login2/recover/step1/\"><div class=\"login_form\">";

            LoginData login = LoginUser.LogedUser(this);
            if (login != null)
            {
                if (Session["last_page"] != null)
                {
                    Response.Redirect(Session["last_page"].ToString());
                    Session["last_page"] = null;
                }
                else
                    Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/", false);
            }
            else if (Session["user_info"] == null || !(Session["user_info"] is Int64))
            {
                //Serviço não informado ou não encontrado
                html += "    <ul>";
                html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("invalid_session") + "</div>";
                html += "    </ul>";
            }
            else
            {

                Int64 entityId = (Int64)Session["user_info"];
                Int64 enterpriseID = ((EnterpriseData)Page.Session["enterprise_data"]).Id;

                String err = "";


                if (Request.HttpMethod == "POST")
                {
                    String sentTo = Request["sentTo"];
                    if ((sentTo == null) || (sentTo == ""))
                    {
                        error = MessageResource.GetMessage("select_option");
                    }
                    else
                    {
                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {
                            List<String> possibleData = new List<string>();
                            DataTable c = db.Select("select value from vw_entity_all_data where id = " + entityId);
                            if ((c != null) && (c.Rows.Count > 0))
                            {
                                foreach (DataRow dr in c.Rows)
                                    if (!possibleData.Contains(dr["value"].ToString().ToLower()))
                                        possibleData.Add(dr["value"].ToString().ToLower());

                                if (possibleData.Count > 0)
                                {
                                    DirectoryInfo pluginPath = new DirectoryInfo(Path.Combine(HostingEnvironment.MapPath("~"), "code_plugins"));
                                    if (!pluginPath.Exists)
                                        pluginPath.Create();

                                    List<CodeManagerPluginBase> plugins = CodePlugins.GetPlugins<CodeManagerPluginBase>(pluginPath.FullName);
                                    if (plugins.Count > 0)
                                    {

                                        CodeManagerPluginBase p = CodeManagerPluginBase.GetPluginByData(plugins, possibleData, sentTo);

                                        if (p != null)
                                        {

                                            try
                                            {
                                                

                                                DataTable tmp = db.Select(String.Format("select id, recovery_code from entity with(nolock) where deleted = 0 and id = {0}", entityId));
                                                if ((tmp == null) || (tmp.Rows.Count == 0))
                                                {
                                                    error = MessageResource.GetMessage("entity_not_found");
                                                }

                                                Dictionary<String, Object> config = new Dictionary<String, Object>();
                                                using (DataTable c1 = db.Select("select [key], [value] from code_plugin_par where enterprise_id = " + enterpriseID + " and uri = '" + p.GetPluginId().AbsoluteUri + "'"))
                                                {
                                                    if (c1 != null)
                                                        foreach (DataRow dr1 in c1.Rows)
                                                            CodeManagerPluginBase.FillConfig(p, ref config, dr1["key"].ToString(), dr1["value"]);

                                                    if (p.SendCode(config, possibleData, sentTo, tmp.Rows[0]["recovery_code"].ToString()))
                                                    {
                                                        Response.Redirect(Session["ApplicationVirtualPath"] + "login2/recover/step2/", false);
                                                        return;
                                                    }
                                                    else
                                                    {
                                                        error = "Erro enviando código de recuperação";
                                                    }
                                                }
                                                config.Clear();
                                                config = null;

                                            }
                                            catch (Exception ex)
                                            {
                                                error = ex.Message;
                                            }

                                        }
                                        else
                                        {
                                            error = MessageResource.GetMessage("option_not_found");
                                        }
                                    }
                                    else
                                    {
                                        error = MessageResource.GetMessage("option_not_found");
                                    }
                                }
                                else
                                {
                                    error = MessageResource.GetMessage("option_not_found");
                                }
                            }
                            else
                            {
                                error = MessageResource.GetMessage("option_not_found");
                            }

                            //Resgata todos os plugind possíveis


                            /*
                            DataTable c = db.Select("select * from vw_entity_mails where mail like '%@%' and entity_id = " + entityId);
                            if ((c != null) && (c.Rows.Count > 0))
                            {
                                DataRow drSentTo = null;
                                foreach (DataRow dr in c.Rows)
                                {
                                    String data = LoginUser.MaskData(dr["mail"].ToString(), true, false);
                                    if (sentTo.ToString().ToLower() == data)
                                    {
                                        drSentTo = dr;
                                        break;
                                    }
                                }

                                if (drSentTo == null)
                                    error = MessageResource.GetMessage("option_not_found");
                                else
                                {

                                    //if (LoginUser.SendCode(entityId, drSentTo["value"].ToString(), (Boolean)drSentTo["is_mail"], (Boolean)drSentTo["is_sms"], out err))
                                    if (LoginUser.SendCode(entityId, drSentTo["mail"].ToString(), true, false, out err))
                                    {
                                        Response.Redirect(Session["ApplicationVirtualPath"] + "login2/recover/step2/", false);
                                        return;
                                    }
                                    else
                                    {
                                        error = err;
                                    }

                                }
                            }
                            else
                            {
                                error = MessageResource.GetMessage("option_not_found");
                            }*/


                        }
                    }
                }

                LoginUser.NewCode(this, entityId, out err);
                if (err == "")
                {
                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {
                        

                        List<CodeData> dataList = new List<CodeData>();
                        List<String> possibleData = new List<string>();
                        DataTable c = db.Select("select value from vw_entity_all_data where id = " + entityId);
                        if ((c != null) && (c.Rows.Count > 0))
                        {
                            foreach (DataRow dr in c.Rows)
                                if (!possibleData.Contains(dr["value"].ToString().ToLower()))
                                    possibleData.Add(dr["value"].ToString().ToLower());

                            if (possibleData.Count > 0)
                            {
                                DirectoryInfo pluginPath = new DirectoryInfo(Path.Combine(HostingEnvironment.MapPath("~"), "code_plugins"));
                                if (!pluginPath.Exists)
                                    pluginPath.Create();

                                List<CodeManagerPluginBase> plugins = CodePlugins.GetPlugins<CodeManagerPluginBase>(pluginPath.FullName);
                                if (plugins.Count > 0)
                                {
                                    foreach (CodeManagerPluginBase p in plugins)
                                    {
                                        try
                                        {
                                            Dictionary<String, Object> config = new Dictionary<String, Object>();
                                            using (DataTable c1 = db.Select("select [key], [value] from code_plugin_par where enterprise_id = " + enterpriseID + " and uri = '" + p.GetPluginId().AbsoluteUri + "'"))
                                            {
                                                if (c1 != null)
                                                    foreach (DataRow dr1 in c1.Rows)
                                                        CodeManagerPluginBase.FillConfig(p, ref config, dr1["key"].ToString(), dr1["value"]);

                                                //Verifica se existe as configs deste plugin e se estão válidas
                                                if (p.ValidateConfigFields(config))
                                                    dataList.AddRange(p.ParseData(possibleData));
                                            }
                                            config.Clear();
                                            config = null;
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                    }
                                }
                            }
                        }

                        if (dataList.Count > 0)
                        {

                            html += "<ul>";
                            html += "    <li>";
                            html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("send_conf_to") + "</p>";
                            html += "    </li>";

                            foreach (CodeData data in dataList)
                                html += "    <li><p style=\"width:400px;padding:0 0 5px 10px;color:#000;\"><input name=\"sentTo\" type=\"radio\" value=\"" + data.DataId + "\">" + data.MaskedData + "</p></li>";

                            if (error != "")
                            {
                                html += "    <ul>";
                                html += "        <li><div class=\"error-box\">" + error + "</div>";
                                html += "    </ul>";
                            }

                            html += "    <li>";
                            html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                            html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("send_code") + "</button>";
                            html += "    </li>";
                            html += "</ul>     ";
                        }
                        else
                        {

                            html += "<ul>";
                            html += "    <li>";
                            html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">No method available</p>";
                            html += "    </li>";
                            html += "    <li>";
                            html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a></span>";
                            html += "    </li>";
                            html += "</ul>     ";
                        }

                        /*
                        //DataTable c = db.Select("select * from vw_entity_confirmations where enterprise_id = " + enterpriseID + " and  entity_id = " + entityId);
                        DataTable c = db.Select("select * from vw_entity_mails where mail like '%@%' and entity_id = " + entityId);
                        if ((c != null) && (c.Rows.Count > 0))
                        {

                            html += "<ul>";
                            html += "    <li>";
                            html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("send_conf_to") + "</p>";
                            html += "    </li>";

                            foreach (DataRow dr in c.Rows)
                            {
                                //String data = LoginUser.MaskData(dr["value"].ToString(), (Boolean)dr["is_mail"], (Boolean)dr["is_sms"]);
                                String data = LoginUser.MaskData(dr["mail"].ToString(), true, false);
                                if (data != "")
                                    html += "    <li><p style=\"width:400px;padding:0 0 5px 10px;color:#000;\"><input name=\"sentTo\" type=\"radio\" value=\"" + data + "\">" + data + "</p></li>";
                            }

                            if (error != "")
                            {
                                html += "    <ul>";
                                html += "        <li><div class=\"error-box\">" + error + "</div>";
                                html += "    </ul>";
                            }

                            html += "    <li>";
                            html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                            html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("send_code") + "</button>";
                            html += "    </li>";
                            html += "</ul>     ";
                        }
                        else
                        {

                            html += "<ul>";
                            html += "    <li>";
                            html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">No method available</p>";
                            html += "    </li>";
                            html += "    <li>";
                            html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a></span>";
                            html += "    </li>";
                            html += "</ul>     ";
                        }*/
                    }

                }
                else
                {
                    html += "    <ul>";
                    html += "        <li><div class=\"error-box\">" + err + "</div>";
                    html += "    </ul>";
                }

            }

            html += "</div></form>";

            holderContent.Controls.Add(new LiteralControl(html));
        }
    }
}