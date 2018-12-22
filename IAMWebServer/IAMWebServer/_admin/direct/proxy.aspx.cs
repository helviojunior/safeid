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
using IAM.GlobalDefs.WebApi;
using IAM.GlobalDefs;
using System.IO;
using System.Text;
using Zip;
using IAM.Config;

namespace IAMWebServer._admin.direct
{
    public partial class proxy : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            Int64 enterpriseId = 0;
            if ((Session["enterprise_data"]) != null && (Session["enterprise_data"] is EnterpriseData))
                enterpriseId = ((EnterpriseData)Session["enterprise_data"]).Id;


            String area = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["area"]))
                area = (String)RouteData.Values["area"];

            Int64 proxyId = 0;
            try
            {
                proxyId = Int64.Parse((String)RouteData.Values["id"]);

                if (proxyId < 0)
                    proxyId = 0;
            }
            catch { }

            if (proxyId == 0)
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("proxy_not_found"), 3000, true);
                area = "";
            }

            String rData = "";
            String jData = "";

            ProxyGetResult retProxy = null;

            try
            {

                rData = SafeTrend.Json.JSON.Serialize2(new
                {
                    jsonrpc = "1.0",
                    method = "proxy.get",
                    parameters = new
                    {
                        proxyid = proxyId
                    },
                    id = 1
                });

                jData = "";
                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    jData = WebPageAPI.ExecuteLocal(database, this, rData);


                if (String.IsNullOrWhiteSpace(jData))
                    throw new Exception("");

                retProxy = JSON.Deserialize<ProxyGetResult>(jData);
                if (retProxy == null)
                {
                    //error = MessageResource.GetMessage("proxy_not_found");
                    //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                }
                else if (retProxy.error != null)
                {
                    //error = retProxy.error.data;
                    retProxy = null;
                }
                else if (retProxy.result == null || retProxy.result.info == null)
                {
                    //error = MessageResource.GetMessage("proxy_not_found");
                    retProxy = null;
                }
                else
                {
                    //menu3.Name = retProxy.result.info.name;
                }

            }
            catch (Exception ex)
            {
                //error = MessageResource.GetMessage("api_error");
                Tools.Tool.notifyException(ex, this);
                retProxy = null;
                //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
            }

                

            try
            {

                switch (area)
                {
                    case "download":
                        if (retProxy != null)
                        {

                            DirectoryInfo tempPath = null;
                            DirectoryInfo proxyPath = null;
                            try
                            {
                                //Cria o diretório temporário
                                tempPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
                                proxyPath = new DirectoryInfo(Path.Combine(tempPath.FullName, "proxies\\" + enterpriseId + "_" + retProxy.result.info.name));
                                if (!proxyPath.Exists)
                                    proxyPath.Create();


                                //Realiza a leitura dos arquivos originais
                                Byte[] multProxy = File.ReadAllBytes(Path.Combine(Request.PhysicalApplicationPath, "_data\\multproxy.zip"));
                                Byte[] proxy = File.ReadAllBytes(Path.Combine(Request.PhysicalApplicationPath, "_data\\proxy.zip"));

                                //Descompacta os zips em uma estrutura temporária
                                ZIPUtil.DecompressData(multProxy, tempPath);
                                ZIPUtil.DecompressData(proxy, proxyPath);


                                //Cria o arquivo de configuração do proxy
                                String config = "";
                                config += "server=" + Request.Url.Host + (Request.Url.Port != 80 && Request.Url.Port != 443 ? ":" + Request.Url.Port : "") + Environment.NewLine;
                                config += "usehttps=" + (Tools.Tool.IsUsingHTTPS() ? "1" : "0") + Environment.NewLine;
                                config += "hostname=" + retProxy.result.info.name + Environment.NewLine;

                                //Resgata os dados de certificado
                                using (ProxyConfig cfg = new ProxyConfig())
                                {
                                    cfg.GetDBConfig(IAMDatabase.GetWebConnection(), enterpriseId, retProxy.result.info.name);

                                    config += "c1=" + cfg.server_cert + Environment.NewLine;
                                    config += "c2=" + cfg.client_cert + Environment.NewLine;
                                }

                                File.WriteAllText(Path.Combine(proxyPath.FullName, "proxy.conf"), config, Encoding.UTF8);

                                //Cria o arquivo zip com os dados e retorna
                                Byte[] bRet = ZIPUtil.Compress(tempPath);


                                Response.Clear();
                                Response.ContentType = "application/zip";
                                Response.AddHeader("Content-Disposition", "attachment; filename=IAMProxy.zip");
                                Response.AddHeader("Content-Length", bRet.Length.ToString());

                                Response.Status = "200 OK";
                                Response.StatusCode = 200;
                                Response.OutputStream.Write(bRet, 0, bRet.Length);
                                Response.OutputStream.Flush();
                            }
                            catch (Exception ex)
                            {
                                Response.Status = "500 Internal Error";
                                Response.StatusCode = 500;
                            }
                            finally
                            {
                                try
                                {
                                    if (tempPath != null)
                                        tempPath.Delete(true);
                                }
                                catch { }
                            }

                            contentRet = null;
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
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