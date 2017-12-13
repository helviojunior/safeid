using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SafeTrend.Data;
using IAM.GlobalDefs;
using System.Drawing;
using System.IO;
using System.Data;

namespace IAMWebServer._mail
{
    public partial class mail : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            
            if (RouteData.Values["type"] == null)
                return;

            switch (RouteData.Values["type"].ToString().ToLower())
            {
                case "v":
                    if (RouteData.Values["id"] == null)
                        return;

                    if (String.IsNullOrEmpty((String)RouteData.Values["id"]))
                        return;

                    try
                    {
                        using(DbParameterCollection par = new DbParameterCollection())
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {
                            par.Add("@key", typeof(String)).Value = (String)RouteData.Values["id"];
                            par.Add("@ip", typeof(String)).Value = (Request.Params["REMOTE_ADDR"] != null ? (String)Request.Params["REMOTE_ADDR"] : "");
                            par.Add("@user_agent", typeof(String)).Value = (Request.Params["HTTP_USER_AGENT"] != null ? (String)Request.Params["HTTP_USER_AGENT"] : "");
                            
                            database.ExecuteNonQuery("insert into st_messages_views (message_id,date,ip_addr,user_agent) select id, GETDATE(), @ip, @user_agent from st_messages m with(nolock) where m.[key] = @key", par);
                        }

                    }
                    catch { }

                    //Cria a imagem de 1 por 1 pixel com este pixel transparente
                    Bitmap empty = new Bitmap(1, 1);
                    empty.SetPixel(0, 0, Color.Transparent);

                    //Grava a imagem na variavel bRet
                    Byte[] bRet = new Byte[0];
                    using (MemoryStream s = new MemoryStream())
                    {
                        empty.Save(s, System.Drawing.Imaging.ImageFormat.Png);
                        bRet = s.ToArray();
                    }

                    empty.Dispose();

                    //Retorna a imagem para o navegador
                    Response.Clear();
                    Response.ContentType = "image/png";
                    Response.AddHeader("Content-Length", bRet.Length.ToString());

                    Response.Status = "200 OK";
                    Response.StatusCode = 200;
                    Response.OutputStream.Write(bRet, 0, bRet.Length);
                    Response.OutputStream.Flush();

                    break;

                case "l":
                    if (RouteData.Values["id"] == null)
                        return;

                    if (String.IsNullOrEmpty((String)RouteData.Values["id"]))
                        return;

                    try
                    {
                        
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {
                            DataTable dtLink = null;
                            using (DbParameterCollection par = new DbParameterCollection())
                            {
                                par.Add("@key", typeof(String)).Value = (String)RouteData.Values["id"];
                                
                                dtLink = database.ExecuteDataTable("select * from st_messages_links m with(nolock) where m.[key] = @key", par);
                            }

                            if ((dtLink != null) && (dtLink.Rows.Count > 0))
                            {
                                using (DbParameterCollection par = new DbParameterCollection())
                                {
                                    par.Add("@link_id", typeof(Int64)).Value = dtLink.Rows[0]["id"];
                                    par.Add("@ip", typeof(String)).Value = (Request.Params["REMOTE_ADDR"] != null ? (String)Request.Params["REMOTE_ADDR"] : "");
                                    par.Add("@user_agent", typeof(String)).Value = (Request.Params["HTTP_USER_AGENT"] != null ? (String)Request.Params["HTTP_USER_AGENT"] : "");

                                    database.ExecuteNonQuery("insert into st_messages_links_click (messages_links_id,date,ip_addr,user_agent) VALUES(@link_id, GETDATE(), @ip, @user_agent)", par);
                                }

                                String url = "";
                                if (Request.Params["HTTPS"].ToLower() == "on")
                                    url += "https://";
                                else
                                    url += "http://";

                                url += Request.Params["HTTP_HOST"];
                                url += Request.RawUrl;

                                Byte[] bRet1 = Encoding.UTF8.GetBytes("<html><body>Reload the page to get source for: " + url + " to: " + dtLink.Rows[0]["link"].ToString() + "</body></html>");

                                //Retorna o redirect ao navegador
                                Response.Clear();
                                Response.ContentType = "text/html; charset=UTF-8";
                                Response.AddHeader("Content-Length", bRet1.Length.ToString());
                                Response.AddHeader("Location", dtLink.Rows[0]["link"].ToString());
                                Response.AddHeader("Date", DateTime.Now.ToString("R"));//Date:Wed, 16 Jul 2014 08:39:04 GMT
                                Response.ExpiresAbsolute = new DateTime(1990,1,1);
                                

                                Response.Status = "301 Moved Permanently";
                                Response.StatusCode = 301;
                                Response.OutputStream.Write(bRet1, 0, bRet1.Length);
                                Response.OutputStream.Flush();
                            }

                            
                        }

                    }
                    catch { }
                    break;

                default:
                    Tools.Tool.notifyException(new Exception("Method not defined"), this);
                    break;
            }

        }
    }
}