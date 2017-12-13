using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//JSON Web Request
using System.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net;


namespace IAM
{
    class IAMUserClass
    {
        private Uri urlAPI = new Uri("http://10.60.134.81/api/json.aspx");

        public Boolean ChangeUserPasswd(String authUser, String authPassword, String userLogin, String newPassword)
        {
            //Autentica para poder realizar a busca do usuário
            APIAccessToken accessToken = GetToken(authUser, authPassword);
            if ((accessToken != null) && (accessToken.IsValid))
            {

                    //Realiza a troca da senha
                    String jData = "{\"apiver\": \"1.0\",\"method\": \"user.changepassword\",\"parameters\": {\"user\": \"" + userLogin + "\", \"password\": \"" + newPassword + "\",\"must_change\": false},\"auth\": \"" + accessToken.Authorization + "\",\"id\": \"1\"}";

                    APIChangePasswdResult ret = JsonWebRequest<APIChangePasswdResult>(urlAPI, jData, "application/json", null, "POST");
                    if (ret == null)
                    {
                        throw new Exception("Erro on change password");
                    }
                    else if (ret.error != null)
                    {
                        String errMsg = "";

                        //Aqui pode ser tratado especificamente cada erro
                        //Obs: Melhorar as mensagens

                        if (ret.error.lowercase)
                            errMsg += "Letra minúscula não definida";
                        if (ret.error.uppercase)
                            errMsg += "Letra maiúscula não definida";
                        if (ret.error.number_char)
                            errMsg += "Tamanho mínimo da senha (mínimo 8 dígitos)";
                        if (ret.error.numbers)
                            errMsg += "Número não definido";
                        if (ret.error.symbols)
                            errMsg += "Caracteres especiais não definido";

                        throw new Exception("Erro on change password: " + ret.error.data + (ret.error.debug != null ? ret.error.debug : ""));
                    }
                    else if (ret.success == true)
                    {
                        //Senha trocada com sucesso
                        return true;
                    }
                
            }
            else
            {
                //Erro ao realizar a autenticação
                return false;
            }

            return false;
        }

        #region Internal Methods


        private APIAccessToken GetToken(String userName, String userPassword)
        {
            APIAccessToken accessToken = new APIAccessToken();
            accessToken.LoadFromFile();

            //Verifica em cache se o token ainda e válido
            if (!accessToken.IsValid)
            {
                accessToken = new APIAccessToken();

                try
                {

                    String jData = "{\"apiver\": \"1.0\",\"method\": \"user.login\",\"parameters\": {\"user\": \"" + userName + "\", \"password\": \"" + userPassword + "\", \"userData\": false},\"id\": \"1\"}";

                    APIAuthResult ret = JsonWebRequest<APIAuthResult>(urlAPI, jData, "application/json", null, "POST");
                    if (ret == null)
                    {
                        accessToken.error = "Empty return";
                        return accessToken;
                    }
                    else if (ret.error != null)
                    {
                        accessToken.error = ret.error.data + (ret.error.debug != null ? ret.error.debug : "");
                        return accessToken;
                    }
                    else if (!String.IsNullOrEmpty(ret.result.sessionid))
                    {
                        accessToken.access_token = ret.result.sessionid;
                        accessToken.expires_in = ret.result.expires;
                        accessToken.create_time = ret.result.create_time;
                        //accessToken.SaveToFile();
                    }

                }
                catch (Exception ex)
                {
                    accessToken.error = "Error on get API Auth 1.0 Token: " + ex.Message;
                    return accessToken;
                }

            }

            return accessToken;
        }

        public static T Deserialize<T>(String jsonText)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonText)))
                return (T)ser.ReadObject(ms);
        }

        public static String Serialize<T>(T obj)
        {
            String ret = "";

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));

            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, obj);
                ms.Flush();
                ret = Encoding.UTF8.GetString(ms.ToArray());
            }

            return ret;
        }


        public static T JsonWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "Mozilla/5.0 (compatible; SafeID/1.0; +http://www.safeid.com.br)";

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
            catch (Exception ex) { }

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

            if (jData == "")
                return (T)((Object)null);
            else
                return Deserialize<T>(jData);
        }
        #endregion Internal Methods

    }


    [Serializable()]
    public class APIAccessToken
    {
        public String access_token;
        public Int64 expires_in;
        public String error;
        public Int64 create_time;

        public APIAccessToken()
        {
            var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var issueTime = DateTime.Now;

            create_time = (int)issueTime.Subtract(utc0).TotalSeconds;
        }

        public String Authorization
        {
            get { return access_token; }
        }

        public Boolean IsValid
        {
            get
            {
                if ((access_token == null) || (access_token.Trim() == "") || (expires_in <= 0))
                    return false;

                DateTime utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                DateTime issueTime = DateTime.Now;

                if ((Int64)issueTime.Subtract(utc0).TotalSeconds < (create_time + expires_in - 600)) //Com 10 minutos a menos
                    return true;

                return false;

            }
        }

        public void SaveToFile()
        {
            if (create_time == 0)
            {
                var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var issueTime = DateTime.Now;

                create_time = (int)issueTime.Subtract(utc0).TotalSeconds;
            }

            String jData = IAMUserClass.Serialize<APIAccessToken>(this);
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            String tokenFile = Path.GetFullPath(asm.Location) + ".apiToken";
            File.WriteAllText(tokenFile, jData, Encoding.UTF8);
        }

        public void LoadFromFile()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            String tokenFile = Path.GetFullPath(asm.Location) + ".apiToken";

            if (!File.Exists(tokenFile))
                return;

            String jData = File.ReadAllText(tokenFile, Encoding.UTF8);
            APIAccessToken item = IAMUserClass.Deserialize<APIAccessToken>(jData);

            this.access_token = item.access_token;
            this.create_time = item.create_time;
            this.expires_in = item.expires_in;
        }

    }
    
    [Serializable()]
    public class APIAuthResult : APIResponseBase
    {
        [OptionalField()]
        public APIAuthKey result;
    }


    [Serializable()]
    public class APIAuthKey
    {
        [OptionalField()]
        public string sessionid;

        [OptionalField()]
        public Int64 expires;

        [OptionalField()]
        public Int64 create_time;

        [OptionalField()]
        public bool success;
    }


    [Serializable()]
    public class APIChangePasswdResult : APIResponseBase
    {
        [OptionalField()]
        public Boolean success;
    }


    [Serializable()]
    public class APIResponseBase
    {
        public string apiver;
        public string id;

        [OptionalField()]
        public APIResponseError error;

    }

    [Serializable()]
    public class APIResponseError
    {
        [OptionalField()]
        public Int32 code;

        [OptionalField()]
        public string data;

        [OptionalField()]
        public string message;

        [OptionalField()]
        public string debug;

        [OptionalField()]
        public string text;

        [OptionalField()]
        public Boolean lowercase;

        [OptionalField()]
        public Boolean name_part;

        [OptionalField()]
        public Boolean number_char;

        [OptionalField()]
        public Boolean numbers;

        [OptionalField()]
        public Boolean symbols;

        [OptionalField()]
        public Boolean uppercase;
    }

}
