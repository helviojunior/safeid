using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.Config;
using SafeTrend.Json;
using IAM.CA;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using IAM.GlobalDefs;

namespace IAMWebServer.proxy.methods
{
    public partial class transfer_receive_ok : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Request.InputStream.Position = 0;

            try
            {
                JSONRequest req = JSON.GetRequest(Request.InputStream);

                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                {
                    ProxyConfig config = new ProxyConfig();
                    config.GetDBConfig(database.Connection, ((EnterpriseData)Page.Session["enterprise_data"]).Id, req.host);

                    if (config.fqdn != null) //Encontrou o proxy
                    {

                        DirectoryInfo outDir = null;

                        using (ServerDBConfig c = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                            outDir = new DirectoryInfo(Path.Combine(c.GetItem("outboundFiles"), config.proxy_name));

                        if (!outDir.Exists)
                            outDir.Create();

                        if ((req.data != null) && (req.data != ""))
                        {
                            //Recebeu o nome do arquivo, envia o unico arquivo
                            FileInfo fName = null;
                            try
                            {
                                fName = new FileInfo(Path.Combine(outDir.FullName, req.data.Trim("..\\/".ToCharArray())));
                            }
                            catch
                            {
                                ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, "Filename is invalid", "")));
                                return;
                            }

                            if (fName.Exists)
                            {
                                try
                                {
                                    fName.Delete();

                                    ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(true, "", "Notify received")));


                                    //Verifica se pode remover o diretório
                                    try
                                    {
                                        if (outDir.GetFiles("*.iamdat", SearchOption.AllDirectories).Length == 0)
                                            outDir.Delete(true);
                                    }
                                    catch { }

                                }
                                catch (Exception ex)
                                {
                                    ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, "Error deleting file " + fName.Name + ", " + ex.Message, "")));
                                }
                            }
                            else
                            {
                                ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, "File not found '" + req.data + "'", "")));
                            }
                        }

                    }
                }
            }
            catch(Exception ex) {
                Tools.Tool.notifyException(ex);
                throw ex;
            }
        }
    }
}