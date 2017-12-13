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

namespace IAMWebServer._admin.action
{
    public partial class plugin : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 pluginId = 0;
            if ((action != "add_plugin") && (action != "upload_item_template") && (action != "upload") && (action != "add_new"))
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
                            userHtmlTemplate += "               <div class=\"links small\">";
                            userHtmlTemplate += "                   <div class=\"last\"><div class=\"ico icon-close\" onclick=\"$('#file{0}').remove();\">Excluir plugin</div></a><div class=\"clear-block\"></div></div>";
                            userHtmlTemplate += "               </div>";
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

                                DirectoryInfo pluginsDir = null;

                                try
                                {
                                    using (ServerDBConfig c = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                                        pluginsDir = new DirectoryInfo(Path.Combine(c.GetItem("pluginFolder"), "temp\\" + ((EnterpriseData)Page.Session["enterprise_data"]).Id));

                                    if (!pluginsDir.Exists)
                                        pluginsDir.Create();
                                }
                                catch {
                                    pluginsDir = null;
                                }

                                if (pluginsDir == null)
                                {
                                    d += String.Format(infoTemplate2, "", "Status", "Diretório de plugins não encontrado");
                                }
                                else
                                {

                                    try
                                    {
                                        if (!pluginsDir.Exists)
                                            pluginsDir.Create();

                                        Byte[] rawAssembly = new Byte[mpF.Data.Length];
                                        mpF.Data.Read(rawAssembly, 0, rawAssembly.Length);

                                        List<String> p2 = new List<String>();
                                        List<String> p2Uri = new List<String>();
                                        try
                                        {
                                            //Realiza teste de compatibilidade com os plugins
                                            List<PluginBase> p1 = Plugins.GetPlugins<PluginBase>(rawAssembly);
                                            if (p1.Count > 0)
                                                d += String.Format(infoTemplate2, "", "Status", "Arquivo válido");
                                            else
                                                d += String.Format(infoTemplate2, "", "Status", "Arquivo de plugin inválido");

                                            foreach (PluginBase p in p1)
                                            {
                                                p2.Add(p.GetPluginName());
                                                p2Uri.Add(p.GetPluginId().AbsoluteUri);
                                            }
                                        }
                                        catch
                                        {
                                            d += String.Format(infoTemplate2, "", "Status", "Arquivo de plugin inválido");
                                        }

                                        d += String.Format(infoTemplate2, "", "Nome", mpF.FileName);
                                        d += String.Format(infoTemplate2, "", "Tamanho", mpF.Data.Length + " bytes");
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

                                            FileInfo newFile = new FileInfo(Path.Combine(pluginsDir.FullName,  mpF.FileName));
                                            if (newFile.Exists)
                                                newFile.Delete();
                                            File.WriteAllBytes(newFile.FullName, rawAssembly);
                                        }
                                    }
                                    catch(Exception ex) {
                                        d = String.Format(infoTemplate2, "", "Status", "Erro ao realizar o upload");
                                        d += String.Format(infoTemplate2, "", "Informação do erro", ex.Message);
                                    }
                                }

                                fls.Add(JSON.Serialize2(new { name = mpF.FileName, html = d }));
                            }
                            catch {
                                fls.Add(JSON.Serialize2(new { name = mpF.FileName, error = "Erro enviando o arquivo" }));
                            }
                            
                        }
                        
                        Retorno.Controls.Add(new LiteralControl("{\"files\": [" + String.Join(",",fls) + "]}"));
                        contentRet = null;

                        break;

                    case "add_new":
                        Dictionary<String, String> files = new Dictionary<string, string>();
                        foreach (String key in Request.Form.Keys)
                            if ((key != null) && (key.ToLower().IndexOf("file_name") == 0))
                                if (!files.ContainsKey(Request.Form[key].ToLower()))
                                    files.Add(Request.Form[key].ToLower(), Request.Form[Request.Form[key]]);

                        if (files.Count == 0)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("plugin_not_found"), 3000, true);
                            break;
                        }

                        DirectoryInfo pluginsBase = null;
                        DirectoryInfo pluginsTemp = null;
                        try
                        {

                            using (ServerDBConfig c = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                                pluginsBase = new DirectoryInfo(c.GetItem("pluginFolder"));

                            pluginsTemp = new DirectoryInfo(Path.Combine(pluginsBase.FullName, "temp\\" + ((EnterpriseData)Page.Session["enterprise_data"]).Id));

                            if (!pluginsTemp.Exists)
                                pluginsTemp.Create();
                        }
                        catch
                        {
                            pluginsTemp = null;
                        }

                        if (pluginsTemp == null)
                        {
                            contentRet = new WebJsonResponse("", "Diretório de plugins não encontrado", 3000, true);
                            break;
                        }

                        List<WebJsonResponse> multRet = new List<WebJsonResponse>();

                        String infoTemplate3 = "<div class=\"line {0}\">";
                        infoTemplate3 += "<label>{1}</label>";
                        infoTemplate3 += "<span class=\"no-edit\">{2}</span></div>";

                        Boolean hasError = false;
                        foreach (String f in files.Keys)
                        {
                            try
                            {
                                FileInfo assemblyFile = new FileInfo(Path.Combine(pluginsTemp.FullName, f));

                                if (!assemblyFile.Exists)
                                    throw new Exception("Arquivo temporário não encontrado, refaça o upload");

                                Byte[] rawAssembly = File.ReadAllBytes(assemblyFile.FullName);
                                List<PluginBase> p1 = Plugins.GetPlugins<PluginBase>(rawAssembly);
                                if (p1.Count == 0)
                                    throw new Exception("Arquivo de plugin inválido");

                                foreach (PluginBase p in p1)
                                {

                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                    {
                                        DataTable dt = database.Select("select * from plugin where enterprise_id in (0," + enterpriseId + ") and (assembly = '" + p.GetPluginName() + "' or uri = '" + p.GetPluginId().AbsoluteUri + "')", null);

                                        if (dt.Rows.Count > 0)
                                            throw new Exception("Plugin/uri ja cadastrado no sistema");
                                    }

                                    FileInfo newF = new FileInfo(Path.Combine(pluginsBase.FullName, enterpriseId + "-" + assemblyFile.Name));
                                    try
                                    {
                                        
                                        assemblyFile.CopyTo(newF.FullName);

                                        DbParameterCollection par = new DbParameterCollection();
                                        par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                                        par.Add("@name", typeof(String)).Value = p.GetPluginName();
                                        par.Add("@scheme", typeof(String)).Value = p.GetPluginId().Scheme;
                                        par.Add("@uri", typeof(String)).Value = p.GetPluginId().AbsoluteUri;
                                        par.Add("@assembly", typeof(String)).Value = newF.Name;

                                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                            database.ExecuteNonQuery("INSERT INTO plugin ([enterprise_id],[name],[scheme],[uri],[assembly],[create_date]) VALUES(@enterprise_id, @name, @scheme, @uri, @assembly, getdate())", CommandType.Text, par);

                                        try
                                        {
                                            assemblyFile.Delete();
                                        }
                                        catch { }
                                    }
                                    catch(Exception ex) {

                                        try
                                        {
                                            newF.Delete();
                                        }
                                        catch { }



                                        throw ex;
                                    }

                                }

                                multRet.Add(new WebJsonResponse(".file-item[id=file" + files[f] + "] .description", String.Format(infoTemplate3, "", "Status", "Plugin inserido com sucesso")));
                                multRet.Add(new WebJsonResponse(".file-item[id=file" + files[f] + "] .form-content", "<input type=\"hidden\" />"));

                            }
                            catch (Exception ex)
                            {
                                hasError = true;
                                multRet.Add(new WebJsonResponse(".file-item[id=file" + files[f] + "] .description", String.Format(infoTemplate3, "error", "Error", ex.Message)));
                            }
                        }

                        if (!hasError)
                        {
                            multRet.Clear();
                            multRet.Add(new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/plugin/"));
                        }

                        Retorno.Controls.Add(new LiteralControl(JSON.Serialize<List<WebJsonResponse>>(multRet)));
                        contentRet = null;

                        break;

                    case "delete":
                        
                        var reqDel = new
                        {
                            jsonrpc = "1.0",
                            method = "plugin.delete",
                            parameters = new
                            {
                                pluginid = pluginId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDel);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleDeleteResult retDel = JSON.Deserialize<RoleDeleteResult>(jData);
                        if (retDel == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("plugin_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("plugin_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
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
