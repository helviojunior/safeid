using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using IAM.WebAPI;
using System.Reflection;
using System.Web.Hosting;
using System.Net;
using SafeTrend.Json;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using IAM.Config;
using IAM.GlobalDefs.WebApi;
using IAM.GlobalDefs;
using IAM.PluginManager;
using IAM.PluginInterface;
using HttpMultipartParser;
using SafeTrend.Data;
using IAM.License;

namespace IAMWebServer._admin.action
{
    public partial class license : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 pluginId = 0;
            if ((action != "add_license") && (action != "upload_item_template") && (action != "upload") && (action != "add_new"))
            {
                try
                {
                    pluginId = Int64.Parse((String)RouteData.Values["id"]);

                    if (pluginId < 0)
                        pluginId = 0;
                }
                catch { }

                if (pluginId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("plugin_not_found"), 3000, true);
                    action = "";
                }
            }

            Int64 enterpriseId = 0;
            if ((Session["enterprise_data"]) != null && (Session["enterprise_data"] is EnterpriseData))
                enterpriseId = ((EnterpriseData)Session["enterprise_data"]).Id;

            String rData = "";
            //SqlConnection //conn = DB.GetConnection();
            String jData = "";

            try
            {

                switch (action)
                {
                    case "upload_item_template":

                        String id = Request.Form["id"];
                        String file = Request.Form["file"];
                        String tSize = Request.Form["size"];

                        if (String.IsNullOrEmpty(id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (String.IsNullOrEmpty(file))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (String.IsNullOrEmpty(tSize))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else
                        {
                            String userHtmlTemplate = "<div id=\"file{0}\" data-id=\"{0}\" data-name=\"{1}\" class=\"app-list-item file-item\">";
                            userHtmlTemplate += "<div class=\"form-content\"><input type=\"hidden\" name=\"file_name_{0}\" value=\"{1}\">";
                            userHtmlTemplate += "<input type=\"hidden\" name=\"{1}\" value=\"{0}\"></div>";
                            userHtmlTemplate += "<table>";
                            userHtmlTemplate += "   <tbody>";
                            userHtmlTemplate += "       <tr>";
                            userHtmlTemplate += "           <td class=\"colfull\">";
                            userHtmlTemplate += "               <div class=\"title\"><span class=\"name\" id=\"file_name_{0}\" data-id=\"{0}\">{1}</span><div class=\"clear-block\"></div></div>";
                            userHtmlTemplate += "               <div class=\"description\">{2}</div></div>";
                            userHtmlTemplate += "           </td>";
                            userHtmlTemplate += "       </tr>";
                            userHtmlTemplate += "   </tbody>";
                            userHtmlTemplate += "</table></div>";

                            String infoTemplate = "<div class=\"line\">";
                            infoTemplate += "<label>{1}</label>";
                            infoTemplate += "<span class=\"no-edit {0}\">{2}</span></div>";

                            String desc = "";

                            desc += String.Format(infoTemplate, "status",  "Status", "Enviando");

                            String tHtml = String.Format(userHtmlTemplate, id, file, desc);

                            contentRet = new WebJsonResponse("#" + id, tHtml);
                        }
                        
                        break;

                    case "upload":

                        MultipartFormDataParser mp = new MultipartFormDataParser(Request.InputStream);
                        List<String> fls = new List<String>();
                        
                        
                        String infoTemplate2 = "<div class=\"line\">";
                        infoTemplate2 += "<label>{1}</label>";
                        infoTemplate2 += "<span class=\"no-edit {0}\">{2}</span></div>";


                        // Loop through all the files
                        foreach (FilePart mpF in mp.Files)
                        {
                            try
                            {
                                String d = "";

                                try
                                {

                                    Byte[] rawAssembly = new Byte[mpF.Data.Length];
                                    mpF.Data.Read(rawAssembly, 0, rawAssembly.Length);


                                    IAMKeyData memKey = null;
                                    String fileData = Convert.ToBase64String(rawAssembly);
                                    try
                                    {
                                        memKey = IAMKey.ExtractFromCert(fileData);
                                    }
                                    catch { }

                                    if (memKey != null)
                                    {

                                        d += String.Format(infoTemplate2, "", "Nome", mpF.FileName);
                                        d += String.Format(infoTemplate2, "", "Tamanho", mpF.Data.Length + " bytes");

                                        Boolean useLicense = false;

                                        if (memKey.IsServerKey)
                                        {


                                            d += String.Format(infoTemplate2, "", "Definitiva?", (memKey.IsTemp ? MessageResource.GetMessage("no") : MessageResource.GetMessage("yes")));
                                            if (memKey.IsTemp)
                                                d += String.Format(infoTemplate2, "", "Expiração", (memKey.TempDate.HasValue ? MessageResource.FormatDate(memKey.TempDate.Value, true) : "não definido"));

                                            d += String.Format(infoTemplate2, "", "Entidades", (memKey.NumLic == 0 ? MessageResource.GetMessage("unlimited") : memKey.NumLic.ToString()));

                                            String installKey = "";

                                            using (IAM.Config.ServerKey2 sk = new IAM.Config.ServerKey2(IAMDatabase.GetWebConnection()))
                                                installKey = sk.ServerInstallationKey.AbsoluteUri;

                                            d += String.Format(infoTemplate2, "", "Chave de instalação", (memKey.InstallKey == installKey ? "Válida" : "Inválida"));

                                            if (memKey.InstallKey == installKey)
                                            {
                                                if (!memKey.IsTemp)
                                                    useLicense = true;
                                                else if ((memKey.IsTemp) && (memKey.TempDate.Value.CompareTo(DateTime.Now) > 0))
                                                    useLicense = true;
                                            }
                                        }
                                        else
                                        {
                                            d += String.Format(infoTemplate2, "", "Status", "Licença inválida");
                                        }

                                        if (useLicense)
                                            d += "<input type=\"hidden\" name=\"key_data\" value=\"" + fileData + "\">";

                                        /*
                                        if (p2.Count > 0)
                                            d += String.Format(infoTemplate2, "", "Plugins", String.Join(", ", p2));
                                        else
                                            d += String.Format(infoTemplate2, "", "Plugins", "Nenhum plugin encontrado no arquivo enviado");

                                        if (p2.Count > 0)
                                        {
                                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                            {
                                                DataTable dt = database.Select("select * from plugin where enterprise_id in (0," + enterpriseId + ") and (assembly in ('" + String.Join("','", p2) + "') or uri in ('" + String.Join("','", p2Uri) + "'))");

                                                if (dt.Rows.Count > 0)
                                                    throw new Exception("Plugin/uri ja cadastrado no sistema");
                                            }

                                            FileInfo newFile = new FileInfo(Path.Combine(pluginsDir.FullName, mpF.FileName));
                                            if (newFile.Exists)
                                                newFile.Delete();
                                            File.WriteAllBytes(newFile.FullName, rawAssembly);
                                        }*/
                                    }
                                    else
                                    {
                                        d += String.Format(infoTemplate2, "", "Status", "Arquivo válido");
                                    }

                                }
                                catch (Exception ex)
                                {
                                    d = String.Format(infoTemplate2, "", "Status", "Erro ao realizar o upload");
                                    d += String.Format(infoTemplate2, "", "Informação do erro", ex.Message);
                                }


                                fls.Add(JSON.Serialize2(new { name = mpF.FileName, html = d }));
                            }
                            catch
                            {
                                fls.Add(JSON.Serialize2(new { name = mpF.FileName, error = "Erro enviando o arquivo" }));
                            }

                        }
                        
                        Retorno.Controls.Add(new LiteralControl("{\"files\": [" + String.Join(",",fls) + "]}"));
                        contentRet = null;

                        break;

                    case "add_new":

                        String key_data = "";
                        key_data = Request.Form["key_data"];
                        if (!String.IsNullOrEmpty(key_data))
                        {
                            
                            IAMKeyData memKey = null;
                            try
                            {
                                memKey = IAMKey.ExtractFromCert(key_data);
                            }
                            catch { }

                            if (memKey != null)
                            {

                                
                                Boolean useLicense = false;

                                if (memKey.IsServerKey)
                                {
                                    String installKey = "";

                                    using (IAM.Config.ServerKey2 sk = new IAM.Config.ServerKey2(IAMDatabase.GetWebConnection()))
                                        installKey = sk.ServerInstallationKey.AbsoluteUri;

                                    if (memKey.InstallKey == installKey)
                                    {
                                        if (!memKey.IsTemp)
                                            useLicense = true;
                                        else if ((memKey.IsTemp) && (memKey.TempDate.Value.CompareTo(DateTime.Now) > 0))
                                            useLicense = true;
                                    }

                                    if (useLicense)
                                    {
                                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                        {
                                            db.openDB();
                                            Object trans = db.BeginTransaction();
                                            try
                                            {
                                                db.ExecuteNonQuery("delete from license where enterprise_id = " + enterpriseId , CommandType.Text, null, trans);

                                                using (DbParameterCollection par = new DbParameterCollection())
                                                {
                                                    par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                                                    par.Add("@license", typeof(String)).Value = key_data;

                                                    db.ExecuteNonQuery("insert into license (enterprise_id,license_data) VALUES(@enterprise_id,@license)", CommandType.Text, par, trans);

                                                }

                                                db.Commit();

                                                contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/license/");
                                            }
                                            catch (Exception ex)
                                            {
                                                db.Rollback();
                                                contentRet = new WebJsonResponse("", "Falha ao aplicar a licença", 5000, true);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        contentRet = new WebJsonResponse("", "Licença inválida", 5000, true);
                                    }
                                }
                                else
                                {
                                    contentRet = new WebJsonResponse("", "Licença inválida", 5000, true);
                                }
                            }

                        }
                        else
                        {
                            contentRet = new WebJsonResponse("", "Nenhuma licença válida encontrada para aplicar", 5000, true);
                        }

                        break;

                }

            }
            catch (Exception ex)
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
            }
            finally
            {
            }

            if (contentRet != null)
            {
                if (!String.IsNullOrWhiteSpace((String)Request["cid"]))
                    contentRet.callId = (String)Request["cid"];

            
                Retorno.Controls.Add(new LiteralControl(contentRet.ToJSON()));
            }

        }
    }
}
