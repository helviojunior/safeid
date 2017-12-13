using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Web.Hosting;
using System.IO;

namespace IAMWebServer.css
{
    public partial class iamui : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Page.Response.ContentType = "text/css; charset=UTF-8";
            Page.Response.ContentEncoding = Encoding.UTF8;

            String text = "";
            if (String.IsNullOrWhiteSpace((String)Session["cssui"]))
            {

                String basePath = Path.Combine(Server.MapPath("~"), "css");

                List<FileInfo> files = new List<FileInfo>();
                files.Add(new FileInfo(Path.Combine(basePath, "iam.ui.fonts.css")));
                files.Add(new FileInfo(Path.Combine(basePath, "iam.ui.css")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery-ui-1.10.4.autocomplete.css")));
                files.Add(new FileInfo(Path.Combine(basePath, "iam.ui.login.css")));
                files.Add(new FileInfo(Path.Combine(basePath, "iam.ui.admin.css")));

                StringBuilder tmpText = new StringBuilder();
                tmpText.AppendLine("/*! SafeId v1.0.0 | (c) 2013 SafeTrend.com.br.");
                tmpText.AppendLine("//@ SafeID UI Style");
                tmpText.AppendLine("//@ Generated: " + DateTime.Now.ToString("yyyy-MM:dd HH:mm:ss"));
                tmpText.AppendLine("*/");
                tmpText.AppendLine("");

                foreach (FileInfo f in files)
                {
                    try
                    {
                        if (File.Exists(f.FullName))
                        {
                            using (TextReader tr = File.OpenText(f.FullName))
                                tmpText.Append(Minify.minifyCss(tr.ReadToEnd()));
                        }
                        else
                        {
#if DEBUG
                            tmpText.AppendLine("/*File not found " + f.Name + "*/");
#endif
                        }
                    }
                    catch { }
                }

                text = tmpText.ToString();
#if !DEBUG
                Session["cssui"] = text;
#endif
            }
            else
            {
                text = (String)Session["cssui"];
            }

            Byte[] bRet = Encoding.UTF8.GetBytes(text);
            Page.Response.Status = "200 OK";
            Page.Response.StatusCode = 200;
            Page.Response.OutputStream.Write(bRet, 0, bRet.Length);
            Page.Response.OutputStream.Flush();
        }
    }
}