using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Web.Hosting;
using System.IO;
using IAM.GlobalDefs;

namespace IAMWebServer.js
{
    public partial class iamui : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Page.Response.ContentType = "application/x-javascript; charset=UTF-8";
            Page.Response.ContentEncoding = Encoding.UTF8;

            String text = "";
            if (String.IsNullOrWhiteSpace((String)Session["js-ui"]))
            {

                String basePath = Path.Combine(Server.MapPath("~"), "js");

                List<FileInfo> files = new List<FileInfo>();
                files.Add(new FileInfo(Path.Combine(basePath, "jquery-1.10.2.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery-ui.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.mousewheel.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.blockUI.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.easing.1.3.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.tablesorter.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.appear.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "chosen.jquery.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "Chart.min.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "prism.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jQuery-flowchart-1.0.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.iframe-transport.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.fileupload.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery-fileDragDrop-1.0.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.maskedinput.min.js")));
                files.Add(new FileInfo(Path.Combine(basePath, "jquery.tree.js")));

                files.Add(new FileInfo(Path.Combine(basePath, "iam.ui.admin.js")));

                StringBuilder tmpText = new StringBuilder();
                tmpText.AppendLine("/*! SafeId v1.0.0 | (c) 2013 SafeTrend.com.br.");
                tmpText.AppendLine("//@ SafeID UI JavaScript");
                tmpText.AppendLine("//@ Generated: " + DateTime.Now.ToString("yyyy-MM:dd HH:mm:ss"));
                tmpText.AppendLine("*/");
                tmpText.AppendLine("");
                tmpText.AppendLine("var ApplicationVirtualPath = '" + HostingEnvironment.ApplicationVirtualPath + "';");
                tmpText.AppendLine("var LoadingText = '" + MessageResource.GetMessage("loading_mobile") + "';");


                foreach (FileInfo f in files)
                {
                    try
                    {
                        if (File.Exists(f.FullName))
                        {
                            using (TextReader tr = File.OpenText(f.FullName))
                            {
#if DEBUG
                                tmpText.AppendLine("/*File source: " + f.Name + "*/");
                                tmpText.AppendLine(tr.ReadToEnd());
                                
                                /*tmpText.AppendLine("var js = document.createElement(\"script\");");
                                tmpText.AppendLine("js.type = \"text/javascript\";");
                                tmpText.AppendLine("js.src = '/js/"+ f.Name +"\';");
                                tmpText.AppendLine("document.head.appendChild(js);");*/

                                tmpText.AppendLine("");
#else
                                tmpText.Append(tr.ReadToEnd());
#endif
                            }
                        }
                        else
                        {
                            tmpText.AppendLine("/*File not found " + f.Name + "*/");
                        }
                    }
                    catch { }
                }

                text = tmpText.ToString();
#if !DEBUG
                Session["js-ui"] = text;
#endif
            }
            else
            {
                text = (String)Session["js-ui"];
            }

            Byte[] bRet = Encoding.UTF8.GetBytes(text);
            Page.Response.Status = "200 OK";
            Page.Response.StatusCode = 200;
            Page.Response.OutputStream.Write(bRet, 0, bRet.Length);
            Page.Response.OutputStream.Flush();
        }
    }
}