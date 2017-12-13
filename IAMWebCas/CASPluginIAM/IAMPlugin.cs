using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
using CAS.PluginInterface;
using SafeTrend.Json;
using SafeTrend.Data;

namespace CASPluginIAM
{
    public class IAMPlugin : CASConnectorBase
    {
        public override event LogEvent Log;
        public override String GetPluginName() { return "CAS Plugin for SafeID Identity Manager"; }
        public override Uri GetPluginId() { return new Uri("connector://CAS/plugins/IAMPlugin");}

        private Uri urlAPI;

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();

            conf.Add(new PluginConfigFields("URL of SafeID API", "api", "", PluginConfigTypes.Uri, true, ","));
            conf.Add(new PluginConfigFields("Username of API Account", "username", "", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Password of API Account", "password", "", PluginConfigTypes.String, true, ","));

            /*
            conf.Add(new PluginConfigFields("OU base de busca", "dn_base", "", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Atributo de login", "username_attr", "", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Atributo de senha", "password_attr", "", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Atributo de grupo", "group_attr", "", PluginConfigTypes.String, false, ","));*/

            return conf.ToArray();
        }

        public IAMPlugin()
            : base() { }

        public IAMPlugin(DbBase database, Uri service, Dictionary<String, Object> config, Object state)
            : base(database, service, config, state)
        {
            
        }

        protected override CASTicketResult iGrant(CASTicketResult oldToken, String username, String password)
        {

            CASTicketResult ret = new CASTicketResult();
            ret.BuildTokenCodes();
            ret.CreateByCredentials = true;
            ret.Service = this.Service;
            ret.UserName = username;
            ret.Success = false;

            String lastStep = "Starting";

            try
            {
                this.urlAPI = new Uri(Config["api"].ToString());

                lastStep = "Get token";
                //APIAccessToken accessToken = GetToken(username, password);
                APIAccessToken accessToken = GetToken(base.Config);

                lastStep = "Token check";
                if ((accessToken != null) && (accessToken.IsValid))
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

                    lastStep = "Serialize";
                    JavaScriptSerializer _ser = new JavaScriptSerializer();
                    String jData = _ser.Serialize(loginRequest);

                    lastStep = "Auth";
                    APIUserAuthResult jRet = JSON.JsonWebRequest<APIUserAuthResult>(urlAPI, jData, "application/json", null, "POST");

                    lastStep = "Trata auth";
                    if (jRet == null)
                    {
                        ret.ErrorText = "Please enter a valid username and password";
                    }
                    else if (jRet.error != null)
                    {
                        if (jRet.error.data.ToLower().IndexOf("not found") != -1)
                            ret.ErrorText = "Please enter a valid username and password";
                        else if (jRet.error.data.ToLower().IndexOf("locked") != -1)
                            ret.ErrorText = "Please enter a valid username and password";
                        else if (jRet.error.data.ToLower().IndexOf("incorrect") != -1)
                            ret.ErrorText = "Please enter a valid username and password";

                    }
                    else if (jRet.result == null)
                    {
                        //Nda
                        ret.ErrorText = "Please enter a valid username and password";
                    }
                    else if (jRet.result.userid != 0)
                    {

                        lastStep = "Trata OK";

                        ret.UserName = jRet.result.login;

                        ret.ChangePasswordNextLogon = jRet.result.must_change;

                        //New
                        if (ret.Attributes == null)
                            ret.Attributes = new Dictionary<string, string>();

                        //Copia os atributos to token antigo
                        if ((oldToken != null) && (oldToken.Attributes != null))
                            foreach (String key in oldToken.Attributes.Keys)
                                if (ret.Attributes.ContainsKey(key))
                                    ret.Attributes[key] = oldToken.Attributes[key];
                                else
                                    ret.Attributes.Add(key, oldToken.Attributes[key]);

                        lastStep = "Trata OK attr";


                        //Define os novos atributos ou substitui os antigos
                        if (ret.Attributes.ContainsKey("userid"))
                            ret.Attributes["userid"] = jRet.result.userid.ToString();
                        else
                            ret.Attributes.Add("userid", jRet.result.userid.ToString());


                        try
                        {
                            ret.UserId = ret.Attributes["userid"];
                        }
                        catch
                        {
                            ret.UserId = ret.UserName;
                        }

                        /*
                        List<String> grp = new List<String>();
                        if (jRet.result.roles != null)
                            foreach (APIRoleData r in jRet.result.roles)
                                if (!grp.Contains(r.name))
                                    grp.Add(r.name);*/

                        ret.Success = true;
                    }


                }
                else
                {
                    ret.ErrorText = "Invalid token - API integration error" + (((accessToken != null) && (!String.IsNullOrEmpty(accessToken.error))) ? ": " + accessToken.error : "");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Execution error. Last step = " + lastStep, ex);
            }
            return ret;
        }
        /*
        protected CASTicketResult iGrant_old(CASTicketResult oldToken, String username, String password)
        {

            CASTicketResult ret = new CASTicketResult();
            ret.BuildTokenCodes();
            ret.CreateByCredentials = true;
            ret.Service = this.Service;
            ret.UserName = username;
            ret.Success = false;

            String lastStep = "Starting";

            try
            {
                this.urlAPI = new Uri(Config["api"].ToString());

                lastStep = "Get token";
                APIAccessToken accessToken = GetToken(base.Config);

                lastStep = "Token check";
                if ((accessToken != null) && (accessToken.IsValid))
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

                    lastStep = "Serialize";
                    JavaScriptSerializer _ser = new JavaScriptSerializer();
                    String jData = _ser.Serialize(loginRequest);

                    lastStep = "Auth";
                    APIUserAuthResult jRet = JSON.JsonWebRequest<APIUserAuthResult>(urlAPI, jData, "application/json", null, "POST");

                    lastStep = "Trata auth";
                    if (jRet == null)
                    {
                        ret.ErrorText = "Please enter a valid username and password";
                    }
                    else if (jRet.error != null)
                    {
                        if (jRet.error.data.ToLower().IndexOf("not found") != -1)
                            ret.ErrorText = "Please enter a valid username and password";
                        else if (jRet.error.data.ToLower().IndexOf("locked") != -1)
                            ret.ErrorText = "Please enter a valid username and password";
                        else if (jRet.error.data.ToLower().IndexOf("incorrect") != -1)
                            ret.ErrorText = "Please enter a valid username and password";

                    }
                    else if (jRet.result == null)
                    {
                        //Nda
                        ret.ErrorText = "Please enter a valid username and password";
                    }
                    else if (jRet.result.userid != 0)
                    {

                        lastStep = "Trata OK";

                        ret.UserName = jRet.result.login;

                        //New
                        if (ret.Attributes == null)
                            ret.Attributes = new Dictionary<string, string>();

                        //Copia os atributos to token antigo
                        if ((oldToken != null) && (oldToken.Attributes != null))
                            foreach (String key in oldToken.Attributes.Keys)
                                if (ret.Attributes.ContainsKey(key))
                                    ret.Attributes[key] = oldToken.Attributes[key];
                                else
                                    ret.Attributes.Add(key, oldToken.Attributes[key]);

                        lastStep = "Trata OK attr";


                        //Define os novos atributos ou substitui os antigos
                        if (ret.Attributes.ContainsKey("userid"))
                            ret.Attributes["userid"] = jRet.result.userid.ToString();
                        else
                            ret.Attributes.Add("userid", jRet.result.userid.ToString());

                        /*
                        List<String> grp = new List<String>();
                        if (jRet.result.roles != null)
                            foreach (APIRoleData r in jRet.result.roles)
                                if (!grp.Contains(r.name))
                                    grp.Add(r.name);

                        ret.Success = true;
                    }


                }
                else
                {
                    ret.ErrorText = "Invalid token - API integration error" + (((accessToken != null) && (!String.IsNullOrEmpty(accessToken.error))) ? ": " + accessToken.error : "");
                }
            }
            catch(Exception ex) {
                throw new Exception("Execution error. Last step = " + lastStep, ex);
            }
            return ret;
        }*/


        public override CASChangePasswordResult ChangePassword(CASTicketResult ticket, String password)
        {
            return iChangePassword(ticket.UserId, password);
        }

        public override CASChangePasswordResult ChangePassword(CASUserInfo user, String password)
        {
            return iChangePassword(user.UserName, password);
        }

        public CASChangePasswordResult iChangePassword(String userName, String password)
        {
            CASChangePasswordResult ret = new CASChangePasswordResult(false, userName);

            String lastStep = "Starting";

            try
            {
                this.urlAPI = new Uri(Config["api"].ToString());

                lastStep = "Get token";
                APIAccessToken accessToken = new APIAccessToken();
                accessToken.error = "Unknow error";
                try
                {
                    accessToken = GetToken(Config);
                }
                catch(Exception ex) {
                    accessToken.error = "Erro on get Token: " + ex.Message;
                }

                lastStep = "Token check";
                if ((accessToken != null) && (accessToken.IsValid))
                {

                    lastStep = "Serialize";
                    JavaScriptSerializer _ser = new JavaScriptSerializer();
                    String jData = "";
                    try
                    {
                        jData = _ser.Serialize(new
                         {
                             jsonrpc = "1.0",
                             method = "user.changepassword",
                             parameters = new
                             {
                                 userid = Int64.Parse(userName),
                                 password = password,
                                 must_change = false
                             },
                             auth = accessToken.Authorization,
                             id = 1
                         });
                    }
                    catch
                    {
                        jData = _ser.Serialize(new
                         {
                             jsonrpc = "1.0",
                             method = "user.changepassword",
                             parameters = new
                             {
                                 user = userName,
                                 password = password,
                                 must_change = false
                             },
                             auth = accessToken.Authorization,
                             id = 1
                         });
                    }

                    lastStep = "Auth";
                    APIUserChangePasswordResult jRet = JSON.JsonWebRequest<APIUserChangePasswordResult>(urlAPI, jData, "application/json", null, "POST");

                    lastStep = "Trata auth";
                    if (jRet == null)
                    {
                        ret.ErrorText = "Please enter a valid password";
                    }
                    else if (jRet.error != null)
                    {

                        String add = "";
                        if (jRet.error.lowercase)
                            add += "Letra minúscula";

                        if (jRet.error.uppercase)
                        {
                            if (add != "") add += ", ";
                            add += "Letra maiúscula";
                        }

                        if (jRet.error.number_char)
                        {
                            if (add != "") add += ", ";
                            add += "Tamanho mínimo";
                        }

                        if (jRet.error.numbers)
                        {
                            if (add != "") add += ", ";
                            add += "Número";
                        }

                        if (jRet.error.symbols)
                        {
                            if (add != "") add += ", ";
                            add += "Simbolos";
                        }

                        if (jRet.error.name_part)
                        {
                            if (add != "") add += ", ";
                            add += "Não pode conter parte do nome";
                        }

                        ret.ErrorText = jRet.error.data + add;

                    }
                    else if (jRet.result == null)
                    {
                        //Nda
                        ret.ErrorText = "Please enter a valid password";
                    }
                    else if (jRet.result.success)
                    {
                        ret.Success = true;
                    }


                }
                else
                {
                    ret.ErrorText = "Invalid token - API integration error" + (((accessToken != null) && (!String.IsNullOrEmpty(accessToken.error))) ? ": " + accessToken.error : "");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Execution error. Last step = " + lastStep, ex);
            }

            return ret;
        }

        public override CASUserInfo FindUser(String username)
        {
            CASUserInfo uInfo = new CASUserInfo();
            //uInfo.ErrorText = "User not found";
            uInfo.Success = false;
            uInfo.UserName = username;
            //uInfo.Emails.Add("helvio_junior@hotmail.com");
            //uInfo.Emails.Add("junior.helvio@gmail.com");

            String lastStep = "Starting";
            
            try
            {
                this.urlAPI = new Uri(Config["api"].ToString());

                lastStep = "Get token";
                APIAccessToken accessToken = GetToken(base.Config);

                lastStep = "Token check";
                if ((accessToken != null) && (accessToken.IsValid))
                {

                    var loginRequest = new
                    {
                        jsonrpc = "1.0",
                        method = "user.search",
                        parameters = new
                        {
                            text = username,
                            additional_field = "e-mail,email,login"
                        },
                        auth = accessToken.Authorization,
                        id = 1
                    };

                    lastStep = "Serialize";
                    JavaScriptSerializer _ser = new JavaScriptSerializer();
                    String jData = _ser.Serialize(loginRequest);

                    lastStep = "User search";
                    APISearchResult jRet = JSON.JsonWebRequest<APISearchResult>(urlAPI, jData, "application/json", null, "POST");

                    lastStep = "Trata auth";
                    if (jRet == null)
                    {
                        uInfo.ErrorText = "User not found";
                    }
                    else if (jRet.error != null)
                    {
                        uInfo.ErrorText = jRet.error.data;
                    }
                    else if (jRet.result == null)
                    {
                        //Nda
                        uInfo.ErrorText = "User not found";
                    }
                    else if (jRet.result.Count == 0)
                    {
                        uInfo.ErrorText = "User not found";
                    }
                    else
                    {

                        lastStep = "Trata OK";

                        foreach (APIUserData uData in jRet.result)
                        {
                            if (uData.login == username)
                            {
                                //Resgata todas as informações deste usuário

                                var userRequest = new
                                {
                                    jsonrpc = "1.0",
                                    method = "user.get",
                                    parameters = new
                                    {
                                        userid = uData.userid
                                    },
                                    auth = accessToken.Authorization,
                                    id = 1
                                };

                                lastStep = "Serialize 2";
                                jData = _ser.Serialize(userRequest);

                                lastStep = "User request";
                                APIUserGetResult jRet2 = JSON.JsonWebRequest<APIUserGetResult>(urlAPI, jData, "application/json", null, "POST");

                                lastStep = "Trata User request";
                                if (jRet2 == null)
                                {
                                    uInfo.ErrorText = "User not found";
                                }
                                else if (jRet2.error != null)
                                {
                                    uInfo.ErrorText = jRet2.error.data;
                                }
                                else if ((jRet2.result == null)|| (jRet2.result.info == null))
                                {
                                    //Nda
                                    uInfo.ErrorText = "User not found";
                                }
                                else if (jRet2.result.info.userid == 0)
                                {
                                    uInfo.ErrorText = "User not found";
                                }
                                else if ((jRet2.result.properties == null) || (jRet2.result.properties.Count == 0))
                                {
                                    uInfo.ErrorText = "User properties not found";
                                }
                                else
                                {
                                    foreach (APIUserDataProperty p in jRet2.result.properties)
                                        if ((p.name.ToLower() == "email") || (p.name.ToLower() == "e-mail"))
                                            if (!uInfo.Emails.Contains(p.value))
                                                uInfo.Emails.Add(p.value);

                                    lastStep = "Trata OK 2";
                                    
                                    uInfo.Success = true;
                                }

                                break;
                            }
                        }

                    }


                }
                else
                {
                    uInfo.ErrorText = "Invalid token - API integration error" + (((accessToken != null) && (!String.IsNullOrEmpty(accessToken.error))) ? ": " + accessToken.error : "");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Execution error. Last step = " + lastStep, ex);
            }
            
            return uInfo;
        }


        private APIAccessToken GetToken(String username, String password)
        {

            APIAccessToken accessToken = new APIAccessToken();

            try
            {

                //Efetua o login
                var loginRequest = new
                {
                    jsonrpc = "1.0",
                    method = "user.login",
                    parameters = new
                    {
                        user = username,
                        password = password,
                        userData = true //Deve retornar os dados para poder pegar o userID
                    },
                    id = 1
                };

                JavaScriptSerializer _ser = new JavaScriptSerializer();
                String jData = _ser.Serialize(loginRequest);


                if (jData == null)
                    throw new Exception("Username is empty");


                APIAuthResult ret = JSON.JsonWebRequest<APIAuthResult>(urlAPI, jData, "application/json", null, "POST");
                if (ret == null)
                {
                    accessToken.error = "Empty return";
                    Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + accessToken.error);
                    return accessToken;
                }
                else if (ret.error != null)
                {
                    accessToken.error = ret.error.data;
                    Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + accessToken.error);
                    return accessToken;
                }
                else if (!String.IsNullOrWhiteSpace(ret.result.sessionid))
                {
                    accessToken.access_token = ret.result.sessionid;
                    accessToken.expires_in = ret.result.expires;
                    accessToken.create_time = ret.result.create_time;
                    accessToken.userid = ret.result.userid;
                }

            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + ex.Message);
                return null;
            }

            return accessToken;
        }


        private APIAccessToken GetToken(Dictionary<String, Object> config)
        {
            
            APIAccessToken accessToken = new APIAccessToken();
            //accessToken.LoadFromFile(sufix);

            //Verifica em cache se o token ainda e válido
            //if (!accessToken.IsValid){

            //accessToken = new APIAccessToken();

            try
            {

                if (config == null)
                    throw new Exception("Config is empty");

                if (config["username"] == null)
                    throw new Exception("Username is empty");

                if (config["password"] == null)
                    throw new Exception("Username is empty");

                if (urlAPI == null)
                    throw new Exception("URI is empty");

                if (Service == null)
                    throw new Exception("Service is empty");

                String sufix = "-" + Service.Host + (Service.Port != 80 ? "-" + Service.Port : "");


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


                if (jData == null)
                    throw new Exception("Username is empty");


                APIAuthResult ret = JSON.JsonWebRequest<APIAuthResult>(urlAPI, jData, "application/json", null, "POST");
                if (ret == null)
                {
                    accessToken.error = "Empty return";
                    if (Log != null)
                        Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + accessToken.error);
                    return accessToken;
                }
                else if (ret.error != null)
                {
                    accessToken.error = ret.error.data;
                    if (Log != null)
                        Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + accessToken.error);
                    return accessToken;
                }
                else if (!String.IsNullOrWhiteSpace(ret.result.sessionid))
                {
                    accessToken.access_token = ret.result.sessionid;
                    accessToken.expires_in = ret.result.expires;
                    accessToken.create_time = ret.result.create_time;
                    try
                    {
                        accessToken.SaveToFile(sufix);
                    }
                    catch { }
                }

            }
            catch (Exception ex)
            {
                if (Log != null)
                    Log(this, PluginLogType.Error, "Error on get API Auth 1.0 Token: " + ex.Message);
                accessToken.error = "Error on get API Auth 1.0 Token: " + ex.Message;
            }

            //}

            return accessToken;
        }


        private static String MD5Checksum(String data)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "");
        }


    }
}
