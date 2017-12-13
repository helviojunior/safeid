using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

namespace APITest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Autentica com usuário que tenha permissão de administrador
            //Não é o usuário que se deseja trocar a senha
            //String authKey = GetAuthKey("intranet.integracao", "TGwmVdpI*g01VFmDkBf5");
            String authKey = GetAuthKey("intranet.integracao", "TGwmVdpI*g01VFmDkBf5");
            
            //Troca a senha
            if (!String.IsNullOrEmpty(authKey))
                ChangePassword(authKey, "edson.tanimura", "Fael*123", true);

            Console.WriteLine("Pressione ENTER para finalizar");
            Console.ReadLine();
        }

        public static Boolean ChangePassword(String auth, String username, String password, Boolean mustChange)
        {
            Boolean ok = false;

            //Efetua o login
            var loginRequest = new
            {
                apiver = "1.0",
                method = "user.changepassword",
                parameters = new
                {
                    user = username,//Usuário que se deseja trocar a senha
                    password = password,//Nova senha do usuário
                    must_change = mustChange //Define se o usuário é obrigado a trocar a senha no proximo logon no IM
                },
                auth = auth,
                id = 1
            };


            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            Dictionary<String, Object> ret = JsonWebRequest(new Uri("http://10.60.134.81/api/json.aspx"), jData, "application/json", null, "POST");
            if ((ret.ContainsKey("error")) && (ret["error"] != null))
            {
                Dictionary<String, Object> err = (Dictionary<String, Object>)ret["error"];
                Console.WriteLine(err["data"].ToString());
                
                ok = false;
            }
            else if ((ret.ContainsKey("result")) && (ret["result"] != null))
            {
                Dictionary<String, Object> result = (Dictionary<String, Object>)ret["result"];

                Console.WriteLine("Senha alterada com sucesso");
                ok = true;
            }

            return ok;
        }

        public static String GetAuthKey(String username, String password)
        {
            String authKey = "";

            //Efetua o login
            var loginRequest = new
            {
                apiver = "1.0",
                method = "user.login",
                parameters = new
                {
                    user = username,
                    password = password,
                    userData = true //Define se deseja ou não retornar os principais dados do usuário
                },
                id = 1
            };

            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            Dictionary<String, Object> ret = JsonWebRequest(new Uri("http://10.60.134.81/api/json.aspx"), jData, "application/json", null, "POST");
            if ((ret.ContainsKey("error")) && (ret["error"] != null))
            {
                Dictionary<String, Object> err = (Dictionary<String, Object>)ret["error"];
                Console.WriteLine(err["data"].ToString());
            }
            else if ((ret.ContainsKey("result")) && (ret["result"] != null))
            {
                Dictionary<String, Object> result = (Dictionary<String, Object>)ret["result"];
                authKey = result["sessionid"].ToString();
            }

            return authKey;
        }

        public static Dictionary<String, Object> JsonWebRequest(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "Mozilla/5.0 (compatible)";
            
            if (headers != null)
                foreach (String k in headers.Keys)
                    switch (k.ToLower())
                    {

                        default:
                            request.Headers.Add(k, headers[k]);
                            break;
                    }


            if (!String.IsNullOrEmpty(method))
            {
                switch (method.ToUpper())
                {
                    case "GET":
                    case "POST":
                    case "PUT":
                    case "DELETE":
                        request.Method = method.ToUpper();
                        break;

                    default:
                        request.Method = "GET";
                        break;
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(postData))
                    request.Method = "POST";
                else
                    request.Method = "GET";
            }

            try
            {
                if (!String.IsNullOrEmpty(postData))
                {
                    request.ContentType = ContentType.Split(";".ToCharArray(), 2)[0].Trim() + "; charset=UTF-8";

                    // Create POST data and convert it to a byte array.
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                    request.ContentLength = byteArray.Length;
                    using (Stream dataStream = request.GetRequestStream())
                    {
                        dataStream.Write(byteArray, 0, byteArray.Length);
                    }
                }

                //request.Headers.Add("Content-Type", "application/json; charset=UTF-8");
            }
            catch { }

            String jData = "";
            try
            {
                // Get the response.
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Encoding enc = Encoding.UTF8;
                    try
                    {
                        enc = Encoding.GetEncoding(response.ContentEncoding);
                    }
                    catch { }

                    Stream dataStream = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(dataStream, enc))
                        jData = reader.ReadToEnd();
                }

            }
            catch (Exception ex)
            {
                try
                {
                    if (ex is WebException)
                        using (WebResponse response = ((WebException)ex).Response)
                        {
                            HttpWebResponse httpResponse = (HttpWebResponse)response;
                            using (Stream data = response.GetResponseStream())
                            using (var reader = new StreamReader(data))
                            {
                                jData = reader.ReadToEnd();
                            }
                        }
                }
                catch { }
            }

            JavaScriptSerializer _ser = new JavaScriptSerializer();

            return _ser.Deserialize<Dictionary<String, Object>>(jData);
        }
    }
}
