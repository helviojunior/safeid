using System;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Drawing;
using System.Security.Principal;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Web.Routing;
using System.Text.RegularExpressions;
using IAM.Config;
using System.Web.Hosting;
using IAM.GlobalDefs;

namespace Tools
{

    /// <summary>
    /// Summary description for Util
    /// </summary>
    public class Tool
    {
        
        public static void UpdateUri(Page page)
        {
            if (page.Session["Uri"] == null)
            {
                Int64 enterpriseId = 0;

                if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                    enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

                if (enterpriseId == 0)
                    return;


                IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString());
                try
                {
                    Uri url = new Uri((page.Request.Params["HTTPS"].ToLower() == "on" ? "https://" : "http://") + page.Request.Params["HTTP_HOST"]);
                    
                    //Se for localhost ignora a requisição
                    if (url.Host.ToLower() == "localhost")
                        return;

                    try
                    {
                        System.Net.IPAddress ip = System.Net.IPAddress.Parse(url.Host);

                        //Se é IP (não ocorrer o exception), ignora a requisição
                        return;
                    }
                    catch { }

                    database.ExecuteNonQuery("update [enterprise] set last_uri = '" + url.AbsoluteUri + "' where id = " + enterpriseId);

                    page.Session["Uri"] = url;
                }
                catch
                {
                    page.Session["Uri"] = null;
                }
            }
        }

        public static List<String> AllPartsOfLength(string value, int length)
        {
            List<String> parts = new List<string>();

            for (int startPos = 0; startPos <= value.Length - length; startPos++)
                parts.Add(value.Substring(startPos, length));
            
            return parts;
        }


        static public String GetIPAddress()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;

            /*List<String> lIPAddress = new List<String>();

            if (!string.IsNullOrEmpty(context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]))
            {
                lIPAddress.Add(context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].Split(',')[0]);
            }

            if (!string.IsNullOrEmpty(context.Request.ServerVariables["REMOTE_ADDR"]))
            {
                lIPAddress.Add(context.Request.ServerVariables["REMOTE_ADDR"].Split(',')[0]);
            }

            return String.Join(", ", lIPAddress);*/


            String ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                ipAddress += ",";

                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }

            return context.Request.ServerVariables["REMOTE_ADDR"];

        }

        public static Boolean IsMobile(MasterPage page)
        {
            return IsMobile(page.Page);
        }

        public static Boolean IsMobile(Page page)
        {
            String u = page.Request.ServerVariables["HTTP_USER_AGENT"];
            Regex b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino", RegexOptions.IgnoreCase);
            Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase);
            if (b.IsMatch(u) || v.IsMatch(u.PadLeft(4))) { return true; } else { return false; }

            //Source http://detectmobilebrowsers.com/
        }

        /// <summary>
        /// Convert a .NET Color to a hex string.
        /// </summary>
        /// <returns>ex: "FFFFFF", "AB12E9"</returns>
        public static String ColorToHexString(Color color)
        {
            byte[] bytes = new byte[3];
            bytes[0] = color.R;
            bytes[1] = color.G;
            bytes[2] = color.B;

            return BitConverter.ToString(bytes).Replace("-", "");
        }


        static public String formatPrice(Int64 price)
        {
            Double dPrice = price / 100F;
            return String.Format("R$ {0:0.00}", dPrice);
        }

        static public String formatCEP(String cep)
        {
            if (cep.Length > 5)
            {
                return cep.Substring(0, 5) + "-" + cep.Substring(5);
            }
            else
            {
                return cep;
            }
        }

        static public void sendEmail(String Subject, String to, String body, Boolean isHTML)
        {
            sendEmail(Subject, to, null, body, isHTML);
        }

        static public void sendEmail(String Subject, String to, String replyTo, String body, Boolean isHTML)
        {

            using (ServerDBConfig conf = new ServerDBConfig(IAMDatabase.GetWebConnection()))
            {

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(conf.GetItem("mailFrom"));

                if (to.IndexOf(",") > 0)
                    foreach(String t in to.Split(",".ToCharArray()))
                        if (!String.IsNullOrEmpty(t))
                            mail.To.Add(new MailAddress(t));

                mail.Subject = Subject;

                mail.IsBodyHtml = isHTML;
                mail.Body = body;

                if (replyTo != null)
                {
                    try
                    {
                        mail.ReplyTo = new MailAddress(replyTo);
                    }
                    catch { }
                }

                SmtpClient client = new SmtpClient();
                client.Host = conf.GetItem("smtpServer");
                client.Port = 25;
                client.EnableSsl = false;

                try
                {
                    Int32 port = Int32.Parse(conf.GetItem("smtpPort"));
                    switch (port)
                    {
                        case 587:
                            client.EnableSsl = true;
                            break;

                        case 465:
                            client.EnableSsl = true;
                            break;
                    }
                }
                catch { }

                client.Credentials = new System.Net.NetworkCredential(conf.GetItem("username"), conf.GetItem("password"));


                System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; });

                client.Send(mail);

                client = null;
                mail = null;
            }
        }

        static public String GetData(Control cBase, String key)
        {
            try
            {
                Control cntl = cBase.FindControl(key);
                if (cntl is System.Web.UI.WebControls.TextBox)
                {
                    TextBox input = (TextBox)cntl;
                    if (input != null)
                    {
                        if (input.Text != "")
                        {
                            return TrataInjection(input.Text);
                        }

                    }
                }
                else if (cntl is System.Web.UI.WebControls.DropDownList)
                {
                    DropDownList input2 = (DropDownList)cntl;
                    if (input2.SelectedIndex >= 0)
                        return input2.SelectedItem.Value;
                }
                else if (cntl is System.Web.UI.WebControls.RadioButtonList)
                {
                    RadioButtonList input3 = (RadioButtonList)cntl;
                    if (input3.SelectedIndex >= 0)
                        return input3.SelectedItem.Value;
                }
            }
            catch { }
            return "";
        }

        static public String TrataInjection(String data)
        {
            if (data == null)
                return "";

            String tmp = data.Trim();
            //tmp = tmp.Replace("'", "");
            //tmp = tmp.Replace("\"", "");
            //tmp = tmp.Replace("<", "");
            //tmp = tmp.Replace(">", "");
            tmp = tmp.Replace("--", " ");
            tmp = tmp.Replace(";", " ");
            tmp = tmp.Replace("select", " ");
            tmp = tmp.Replace("update", " ");
            tmp = tmp.Replace("insert", " ");
            tmp = tmp.Replace("drop", " ");
            tmp = tmp.Replace("delete", " ");
            tmp = tmp.Replace("xp_", " ");

            return tmp;
        }

        static public String CryptPassword(String password)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            Byte[] crypher = md5.ComputeHash(Encoding.UTF8.GetBytes(password));

            return Convert.ToBase64String(crypher);
        }

        static public Boolean ComparePassword(String clearPasswd, String CyphedPasswd)
        {
            return (CyphedPasswd == CryptPassword(clearPasswd));
        }

        static public string GenerateRandomCode()
        {
            List<String> codes = new List<String>();

            for (Int32 i = 65; i <= 90; i++)
            {
                codes.Add(Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) }));
            }

            for (Int32 i = 0; i <= 9; i++)
            {
                codes.Add(i.ToString());
            }

            for (Int32 i = 97; i <= 122; i++)
            {
                codes.Add(Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) }));
            }

            Random rnd = new Random();
            string s = "";
            for (int i = 0; i < 6; i++)
                s = String.Concat(s, codes[rnd.Next(0, codes.Count - 1)]);
            return s;
        }


        static public void notifyException(Exception ex)
        {

            try
            {
                String texto = "";
                texto += "----------------------------------" + Environment.NewLine;
                texto += DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + Environment.NewLine + Environment.NewLine;

                texto += "----------------------------------" + Environment.NewLine;
                texto += ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + Environment.NewLine;

                if (ex is HttpException)
                {
                    HttpException httpEx = (HttpException)ex;
                    texto += "HttpException: " + httpEx.GetHttpCode() + Environment.NewLine + Environment.NewLine;
                }

                if (ex.InnerException != null)
                {
                    texto += "InnerException: " + ex.InnerException.Message + Environment.NewLine + ex.InnerException.StackTrace + Environment.NewLine + Environment.NewLine;

                    if (ex.InnerException.InnerException != null)
                        texto += "InnerException: " + ex.InnerException.InnerException.Message + Environment.NewLine + ex.InnerException.InnerException.StackTrace + Environment.NewLine + Environment.NewLine;
                }


                try
                {
                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "HostingEnvironment Properties" + Environment.NewLine;
                    texto += "Application ID: " + HostingEnvironment.ApplicationID;
                    texto += "Application Physical Path: " + HostingEnvironment.ApplicationPhysicalPath;
                    texto += "Application Virtual Path:	" + HostingEnvironment.ApplicationVirtualPath;
                    texto += "Site Name: " + HostingEnvironment.SiteName;
                    texto += Environment.NewLine;
                }
                catch { }

                texto += Environment.NewLine;
                texto += "----------------------------------" + Environment.NewLine;
                try
                {
                    texto += "Windows User: " + WindowsIdentity.GetCurrent().Name + Environment.NewLine;

                }
                catch { }
                try
                {

                    //System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(page);
                    //DirectoryInfo pluginsDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "plugins"));

                    //texto += "Physical Directory: " + WindowsIdentity.GetCurrent().Name + Environment.NewLine;
                }
                catch { }

                try
                {
                    texto += "Environment Directory: " + Environment.CurrentDirectory + Environment.NewLine;
                }
                catch { }

                using (ServerDBConfig conf = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                    sendEmail("Erro em IAM", conf.GetItem("to"), texto, false);
                texto = null;
            }
            catch { }
        }

        //HttpContext context
        static public void notifyException(Exception ex, HttpContext context)
        {

            try
            {
                String texto = "";
                texto += "----------------------------------" + Environment.NewLine;
                texto += DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + Environment.NewLine + Environment.NewLine;

                texto += "----------------------------------" + Environment.NewLine;
                texto += ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + Environment.NewLine;

                if (ex is HttpException)
                {
                    HttpException httpEx = (HttpException)ex;
                    texto += "HttpException: " + httpEx.GetHttpCode() + Environment.NewLine + Environment.NewLine;
                }

                if (ex.InnerException != null)
                {
                    texto += "InnerException: " + ex.InnerException.Message + Environment.NewLine + ex.InnerException.StackTrace + Environment.NewLine + Environment.NewLine;

                    if (ex.InnerException.InnerException != null)
                        texto += "InnerException: " + ex.InnerException.InnerException.Message + Environment.NewLine + ex.InnerException.InnerException.StackTrace + Environment.NewLine + Environment.NewLine;
                }

                try
                {
                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "HostingEnvironment Properties" + Environment.NewLine;
                    texto += "Application ID: " + HostingEnvironment.ApplicationID;
                    texto += "Application Physical Path: " + HostingEnvironment.ApplicationPhysicalPath;
                    texto += "Application Virtual Path:	" + HostingEnvironment.ApplicationVirtualPath;
                    texto += "Site Name: " + HostingEnvironment.SiteName;
                    texto += Environment.NewLine;
                }
                catch { }


                try
                {
                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Windows User: " + WindowsIdentity.GetCurrent().Name + Environment.NewLine;

                }
                catch { }

                if (context != null)
                {
                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.Headers" + Environment.NewLine;
                    try
                    {
                        foreach (String key in context.Request.Headers.Keys)
                        {
                            texto += key + " = " + context.Request.Headers[key] + Environment.NewLine;
                        }
                    }
                    catch { }
                    
                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.Querystring" + Environment.NewLine;
                    try
                    {
                        foreach (String key in context.Request.QueryString.Keys)
                        {
                            texto += key + " = " + context.Request.QueryString[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.Form" + Environment.NewLine;
                    try
                    {
                        foreach (String key in context.Request.Form.Keys)
                        {
                            texto += key + " = " + context.Request.Form[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Session" + Environment.NewLine;
                    try
                    {
                        foreach (String key in context.Session.Keys)
                        {
                            texto += key + " = " + context.Session[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.Params" + Environment.NewLine;
                    try
                    {
                        foreach (String key in context.Request.Params.Keys)
                        {
                            texto += key + " = " + context.Request.Params[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.ServerVariables" + Environment.NewLine;
                    try
                    {
                        foreach (String key in context.Request.ServerVariables.Keys)
                        {
                            texto += key + " = " + context.Request.ServerVariables[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                }

                using (ServerDBConfig conf = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                    sendEmail("Erro em IAM", conf.GetItem("to"), texto, false);
                texto = null;
            }
            catch { }
        }

        static public void notifyException(Exception ex, Page page)
        {

            try
            {
                String texto = "";
                texto += "----------------------------------" + Environment.NewLine;
                texto += "Exception: " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + Environment.NewLine;

                if (ex is HttpException)
                {
                    HttpException httpEx = (HttpException)ex;
                    texto += "HttpException: " + httpEx.GetHttpCode() + Environment.NewLine + Environment.NewLine;
                }

                if (ex.InnerException != null)
                {
                    texto += "InnerException: " + ex.InnerException.Message + Environment.NewLine + ex.InnerException.StackTrace + Environment.NewLine + Environment.NewLine;

                    if (ex.InnerException.InnerException != null)
                        texto += "InnerException: " + ex.InnerException.InnerException.Message + Environment.NewLine + ex.InnerException.InnerException.StackTrace + Environment.NewLine + Environment.NewLine;
                }

                try
                {
                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Windows User: " + WindowsIdentity.GetCurrent().Name + Environment.NewLine;

                }
                catch { }

                if (page != null)
                {
                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.Headers" + Environment.NewLine;
                    try
                    {
                        foreach (String key in page.Request.Headers.Keys)
                        {
                            texto += key + " = " + page.Request.Headers[key] + Environment.NewLine;
                        }
                    }
                    catch { }


                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "RouteData.Values" + Environment.NewLine;
                    try
                    {
                        foreach (String key in page.RouteData.Values.Keys)
                        {
                            texto += key + " = " + page.RouteData.Values[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.Querystring" + Environment.NewLine;
                    try
                    {
                        foreach (String key in page.Request.QueryString.Keys)
                        {
                            texto += key + " = " + page.Request.QueryString[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.Form" + Environment.NewLine;
                    try
                    {
                        foreach (String key in page.Request.Form.Keys)
                        {
                            texto += key + " = " + page.Request.Form[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Session" + Environment.NewLine;
                    try
                    {
                        foreach (String key in page.Session.Keys)
                        {
                            texto += key + " = " + page.Session[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.Params" + Environment.NewLine;
                    try
                    {
                        foreach (String key in page.Request.Params.Keys)
                        {
                            texto += key + " = " + page.Request.Params[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                    texto += Environment.NewLine;
                    texto += "----------------------------------" + Environment.NewLine;
                    texto += "Request.ServerVariables" + Environment.NewLine;
                    try
                    {
                        foreach (String key in page.Request.ServerVariables.Keys)
                        {
                            texto += key + " = " + page.Request.ServerVariables[key] + Environment.NewLine;
                        }
                    }
                    catch { }

                }

                using (ServerDBConfig conf = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                    sendEmail("Erro em IAM", conf.GetItem("to"), texto, false);
                texto = null;
            }
            catch { }
        }
    }

}