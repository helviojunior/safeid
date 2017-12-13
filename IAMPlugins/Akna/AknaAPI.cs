using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SafeTrend.Xml;
using System.Net;
using System.Security.Cryptography;

namespace akna
{
    internal class AknaAPI
    {
        private String username;
        private String password;
        private Uri serverUri = new Uri("https://api.akna.com.br/emkt/int/integracao.php");

        public AknaAPI(String username, String password)
        {
            this.username = username;
            this.password = password;
        }

        public T GetData<T>(String data, CookieContainer cookie)
        {
            return GetData<T>(data, cookie, null);
        }


        public T GetData<T>(String data, CookieContainer cookie, XML.DebugMessage debugCallback)
        {
            try
            {
                return XML.XmlWebRequest<T>(serverUri, getPostData(data), "application/x-www-form-urlencoded", null, "POST", cookie, debugCallback);
            }
            catch (DeserializeException ex)
            {
                throw ex.Exception;
            }
        }


        private String getPostData(String xml)
        {

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(this.password));

                StringBuilder post = new StringBuilder();
                post.Append("User=" + this.username);
                post.Append("&Pass=" + BitConverter.ToString(data).Replace("-", "").ToLower());
                post.Append("&XML=" + xml);

                return post.ToString();

            }
        }

    }
}
