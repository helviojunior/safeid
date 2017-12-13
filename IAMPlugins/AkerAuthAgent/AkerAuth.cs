using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using IAM.PluginInterface;
using AkAuthAgent;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
using System.Security;
using SafeTrend.Json;

namespace AkerAuthAgent
{
    public class AkerAuth : PluginAgentBase
    {
        

        public override String GetPluginName() { return "IAM plugin for Aker Auth"; }
        public override String GetPluginDescription() { return "Plugin para agir como agente de autenticação da Aker com o IAM"; }

        public override Uri GetPluginId()
        {
            return new Uri("agent://iam/plugins/akerauth");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("API URL", "url_api", "", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Username", "username", "", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Password", "password", "", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Clientes autorizados", "client_list", "Lista de clientes autorizados. Cada item da lista deve conter o IP do cliente seguido da senha separado por virgula. Ex.: '127.0.0.2,123456'", PluginConfigTypes.StringList, true, ","));

            return conf.ToArray();
        }

        public override Boolean ValidateConfigFields(Dictionary<String, Object> config, Boolean checkDirectoryExists, LogEvent Log, Boolean checkImport, Boolean checkDeploy)
        {

            LogEvent iLog = new LogEvent(delegate(Object sender, PluginLogType type, string text)
            {
                if (Log != null)
                    Log(sender, type, text);
            });

            if (!CheckInputConfig(config, checkDirectoryExists, iLog, checkImport, checkDeploy))
                return false;

            //Verifica as informações próprias deste plugin
            return true;
        }

        public override void Start(Dictionary<String, Object> config)
        {
            if (!CheckInputConfig(config, true, Log))
                return;

            this.config = config;
            this.urlAPI = new Uri(config["url_api"].ToString());

            this.server = new AuthServer(1021);

            //Parse client List
            String[] cList = (String[])config["client_list"];
            foreach (String s in cList)
            {
                String[] dados = s.Split(",".ToCharArray(), 2);
                if (dados.Length == 2)
                {
                    try
                    {
                        server.ips.Add(IPAddress.Parse(dados[0]), dados[1]);
                    }
                    catch { }
                }
            }

            if (server.ips.Count == 0)
                throw new Exception("Client list is empty or not valid");

            foreach (IPAddress ip in server.ips.Keys)
                Log(this, PluginLogType.Information, "ACL enabled to client " + ip);

            //Realiza a primeira solicitação de token para criar o cache
            GetToken(config, 0, 0);

            server.OnConnectionStarted += new AuthServer.ConnectionStarted(server_OnConnectionStarted);
            server.OnListGroups += new AuthServer.ListGroups(server_OnListGroups);
            server.OnListUsers += new AuthServer.ListUsers(server_OnListUsers);
            server.OnUserValidate += new AuthServer.UserValidate(server_OnUserValidate);
            server.OnError += new AuthServer.Error(server_OnError);
            server.OnInfo += new AuthServer.Info(server_OnInfo);
            
            server.Listen();
        }

        public override void Stop()
        {
            server.Stop();
            server = null;
        }

        private void server_OnInfo(IPEndPoint client, string text)
        {
            //Console.WriteLine(text);
        }

        private void server_OnError(IPEndPoint client, string text)
        {
            //Console.WriteLine("Error: " + text);
            Log2(this, PluginLogType.Error, 0, 0, text, client.ToString());
        }

        private void server_OnUserValidate(IPEndPoint client, string username, string password, ref AuthUserResult result)
        {
            result.Username = username;
            result.Result = AuthResult.NoUser;

            Int64 entityId = 0;

            APIAccessToken accessToken = GetToken(config, 0, 0);
            if (accessToken != null)
            {
                var loginRequest = new
                {
                    jsonrpc = "1.0",
                    method = "user.auth",
                    parameters = new
                    {
                        user = username,
                        md5_password = MD5Checksum(password)
                    },
                    auth = accessToken.Authorization,
                    id = 1
                };

                JavaScriptSerializer _ser = new JavaScriptSerializer();
                String jData = _ser.Serialize(loginRequest);

                APIUserAuthResult ret = JSON.JsonWebRequest<APIUserAuthResult>(urlAPI, jData, "application/json", null, "POST");
                if (ret == null)
                {
                    //Nda
                }
                else if (ret.error != null)
                {
                    if (ret.error.data.ToLower().IndexOf("not found") != -1)
                        result.Result = AuthResult.NoUser;
                    else if (ret.error.data.ToLower().IndexOf("locked") != -1)
                        result.Result = AuthResult.NoUser;
                    else if (ret.error.data.ToLower().IndexOf("incorrect") != -1)
                        result.Result = AuthResult.BadPassword;
                    
                }
                else if (ret.result == null)
                {
                    //Nda
                }
                else if (ret.result.userid != 0)
                {
                    entityId = ret.result.userid;

                    result.Username = ret.result.login;
                    result.Result = AuthResult.OK;

                    List<String> grp = new List<String>();
                    if (ret.result.roles != null)
                        foreach (APIRoleData r in ret.result.roles)
                            if (!grp.Contains(r.name))
                                grp.Add(r.name);

                    result.Groups = grp.ToArray();
                }
            }

            Log2(this, PluginLogType.Information, entityId, 0, "Autenticação solicitada para " + username + " (" + result.Result.ToString() + ")", client.ToString());
        }

        private void server_OnListUsers(IPEndPoint client, ref List<string> users)
        {

            Log2(this, PluginLogType.Information, 0, 0, "Listagem de usuários solicitada", client.ToString());

            APIAccessToken accessToken = GetToken(config, 0, 0);
            if (accessToken == null)
                return;

            var loginRequest = new
            {
                jsonrpc = "1.0",
                method = "user.list",
                parameters = new
                {
                    page_size = Int32.MaxValue
                },
                auth = accessToken.Authorization,
                id = 1
            };

            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            APISearchResult ret = JSON.JsonWebRequest<APISearchResult>(urlAPI, jData, "application/json", null, "POST");
            if (ret == null)
            {
                return;
            }
            else if (ret.error != null)
            {
                return;
            }
            else if (ret.result == null || ret.result.Count == 0)
            {
                return;
            }

            foreach (APIUserData user in ret.result)
                users.Add(user.login);

        }

        private void server_OnListGroups(IPEndPoint client, ref List<string> groups)
        {
            Log2(this, PluginLogType.Information, 0, 0, "Listagem de grupos solicitada", client.ToString());


            APIAccessToken accessToken = GetToken(config, 0, 0);
            if (accessToken == null)
                return;

            var loginRequest = new
            {
                jsonrpc = "1.0",
                method = "role.list",
                parameters = new String[0],
                auth = accessToken.Authorization,
                id = 1
            };

            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            APIRoleListResult ret = JSON.JsonWebRequest<APIRoleListResult>(urlAPI, jData, "application/json", null, "POST");
            if (ret == null)
            {
                return;
            }
            else if (ret.error != null)
            {
                return;
            }
            else if (ret.result == null || ret.result.Count == 0)
            {
                return;
            }

            foreach (APIRoleData role in ret.result)
                if (!groups.Contains(role.name))
                    groups.Add(role.name);


            /*
            foreach (String grp in ldapAuth.ListaGrupos())
            {
                groups.Add(grp);
            }*/
        }

        private void server_OnConnectionStarted(IPEndPoint client, ref int usersCount)
        {
            usersCount = 0;// ldapAuth.Count;
        }


        private APIAccessToken GetToken(Dictionary<String, Object> config, Int64 entityId, Int64 identityId)
        {
            APIAccessToken accessToken = new APIAccessToken();
            accessToken.LoadFromFile();

            //Verifica em cache se o token ainda e válido
            if (!accessToken.IsValid)
            {
                accessToken = new APIAccessToken();

                try
                {
                    
                    //Efetua o login
                    var loginRequest = new
                    {
                        jsonrpc = "1.0",
                        method = "user.login",
                        parameters = new
                        {
                            user = config["username"].ToString(),
                            password = config["password"].ToString(),
                            userData = false //Define se deseja ou não retornar os principais dados do usuário
                        },
                        id = 1
                    };

                    JavaScriptSerializer _ser = new JavaScriptSerializer();
                    String jData = _ser.Serialize(loginRequest);

                    APIAuthResult ret = JSON.JsonWebRequest<APIAuthResult>(urlAPI, jData, "application/json", null, "POST");
                    if (ret == null)
                    {
                        accessToken.error = "Empty return";
                        Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + accessToken.error);
                        Log2(this, PluginLogType.Error, entityId, identityId, "Error on get API Auth 1.0 Token: " + accessToken.error, "");
                        return accessToken;
                    }
                    else if (ret.error != null)
                    {
                        accessToken.error = ret.error.data + (ret.error.debug != null ? ret.error.debug : "");
                        Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + accessToken.error);
                        Log2(this, PluginLogType.Error, entityId, identityId, "Error on get API Auth 1.0 Token: " + accessToken.error, "");
                        return accessToken;
                    }
                    else if (!String.IsNullOrWhiteSpace(ret.result.sessionid))
                    {
                        accessToken.access_token = ret.result.sessionid;
                        accessToken.expires_in = ret.result.expires;
                        accessToken.create_time = ret.result.create_time;
                        accessToken.SaveToFile();
                    }

                }
                catch (Exception ex)
                {
                    Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + ex.Message);
                    Log2(this, PluginLogType.Error, entityId, identityId, "Error on get API Auth 1.0 Token: " + ex.Message, "");
                    return null;
                }

            }

            return accessToken;
        }


        private static String MD5Checksum(String data)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "");
        }


        public override event LogEvent Log;
        public override event LogEvent2 Log2;
        private Dictionary<String, Object> config;
        private AuthServer server;
        private Uri urlAPI;
                    
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
    public class APIResponseBase
    {
        public string jsonrpc;
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

    }

    [Serializable()]
    public class APISearchResult : APIResponseBase
    {
        [OptionalField()]
        public List<APIUserData> result;
    }


    [Serializable()]
    public class APIUserAuthResult : APIResponseBase
    {
        [OptionalField()]
        public APIUserAuthData result;
    }


    [Serializable()]
    public class APIUserAuthData
    {
        [OptionalField()]
        public Int32 userid;

        [OptionalField()]
        public string full_name;

        [OptionalField()]
        public string login;

        [OptionalField()]
        public bool success;

        [OptionalField()]
        public List<APIRoleData> roles;
    }


    [Serializable()]
    public class APIRoleListResult : APIResponseBase
    {
        [OptionalField()]
        public List<APIRoleData> result;
    }

    [Serializable()]
    public class APIRoleData
    {
        [OptionalField()]
        public string resource_name;

        [OptionalField()]
        public string name;
    }

    [Serializable()]
    public class APIUserData
    {
        [OptionalField()]
        public Int32 userid;

        [OptionalField()]
        public string alias;

        [OptionalField()]
        public string full_name;

        [OptionalField()]
        public string login;

        [OptionalField()]
        public bool must_change_password;

        [OptionalField()]
        public Int32 change_password;

        [OptionalField()]
        public Int32 create_date;

        [OptionalField()]
        public bool locked;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public Int64 last_login;

    }

}
