using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;

namespace CAS.PluginInterface
{
    [Serializable()]
    public class CASUserInfo: CASJsonBase, ICloneable
    {
        public Boolean Success;
        public Uri Service;
        public String UserName;
        public String RecoveryCode;
        public String ErrorText;
        public List<String> Emails;

        [OptionalField()]
        public Dictionary<String, String> Attributes;

        public CASUserInfo()
        {
            this.Success = false; //Definir como falso por padrão pois é usado em outras áreas do sistema
            this.Attributes = new Dictionary<string, string>();
            this.Emails = new List<string>();
        }

        public CASUserInfo(CASTicketResult ticket)
            :base()
        {
            this.Success = ticket.Success;
            this.Service = ticket.Service;
            this.UserName = ticket.UserName;
            this.ErrorText = ticket.ErrorText;
            this.Attributes = ticket.Attributes;
        }


        public CASUserInfo(String errorText)
            : base()
        {
            this.ErrorText = errorText;
        }


        public Object Clone()
        {
            CASUserInfo newItem = new CASUserInfo();
            newItem.Success = this.Success;
            newItem.Service = this.Service;
            newItem.UserName = this.UserName;
            newItem.ErrorText = this.ErrorText;
            newItem.Emails = this.Emails;
            newItem.Attributes = this.Attributes;

            return newItem;
        }


        public void SaveToFile(String basePath)
        {
            if ((!Success) || (String.IsNullOrEmpty(UserName)))
                return;

            String jData = Serialize<CASUserInfo>(this);

            //Salva 2 arquivos, um com nome do tiket, outro com nome do usuário

            FileInfo rcodeFile = new FileInfo(Path.Combine(basePath, Service.Host + (Service.Port != 80 ? "-" + Service.Port : "") + "\\" + this.UserName + ".rcode"));

            if (!rcodeFile.Directory.Exists)
                rcodeFile.Directory.Create();

            File.WriteAllText(rcodeFile.FullName, jData, Encoding.UTF8);

            rcodeFile = null;
        }

        static public CASUserInfo GetUserInfo(String basePath, Uri service, String username)
        {

            if (service != null)
            {
                String tokenFile = Path.Combine(basePath, service.Host + (service.Port != 80 ? "-" + service.Port : "") + "\\{0}.rcode");
                if (!String.IsNullOrEmpty(username) && File.Exists(String.Format(tokenFile, username)))
                {
                    String txt = File.ReadAllText(String.Format(tokenFile, username), Encoding.UTF8);
                    return Deserialize<CASUserInfo>(txt);
                }
            }

            CASUserInfo ret = null;
            DirectoryInfo path = new DirectoryInfo(basePath);

            try
            {

                if (!String.IsNullOrEmpty(username))
                {
                    foreach (FileInfo f in path.GetFiles(username + ".rcode", SearchOption.AllDirectories))
                        if (ret == null)
                            try
                            {
                                String txt = File.ReadAllText(f.FullName, Encoding.UTF8);
                                ret = Deserialize<CASUserInfo>(txt);
                            }
                            catch { }
                        else
                            break;

                }
            }
            finally
            {
                path = null;
            }

            if (ret == null)
                ret = new CASUserInfo();

            if (ret.Success)
            {
                //Define o serviço atual
                if (service != null)
                    ret.Service = service;

                //Salva o token copiado
                ret.SaveToFile(basePath);
            }

            return ret;

        }

        public void Destroy(String basePath)
        {

            DirectoryInfo path = new DirectoryInfo(basePath);

            try
            {

                if (!String.IsNullOrEmpty(UserName))
                {
                    foreach (FileInfo f in path.GetFiles(UserName + ".rcode", SearchOption.AllDirectories))
                        try
                        {
                            f.Delete();
                        }
                        catch { }
                }
            }
            catch { }
            finally
            {
                path = null;
            }
        }

        public void NewCode()
        {
            Int32 Length = 6;

            String tmp = "";
            //65 a 90
            //97 a 122

            for (Int32 i = 65; i <= 90; i++)
            {
                tmp += Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) });
            }

            /*
            for (Int32 i = 97; i <= 122; i++)
            {
                tmp += Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) });
            }

            for (Int32 i = 0; i <= 9; i++)
            {
                tmp += i.ToString();
            }*/

            String passwd = "";
            Random rnd = new Random();

            while (passwd.Length < Length)
            {
                Int32 next = rnd.Next(0, tmp.Length - 1);
                passwd += tmp[next];
                Thread.Sleep(50);
            }

            this.RecoveryCode = passwd;


        }

    }
}
