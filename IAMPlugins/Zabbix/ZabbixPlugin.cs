using System;
using System.Collections.Generic;
using System.Text;
using IAM.PluginInterface;
using System.IO;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using SafeTrend.Json;
using System.Web;
using System.Security.Cryptography;

namespace Zabbix
{
    public class ZabbixPlugin : PluginConnectorBase
    {
               
        public override String GetPluginName() { return "Zabbix API Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com API do Zabbix"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://IAM/plugins/zabbix");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("URL Zabbix", "zabbix_uri", "URL de acesso ao servidor", PluginConfigTypes.Uri, true, @""));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário para conexão", PluginConfigTypes.String, true, @""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha para conexão", PluginConfigTypes.Password, true, @""));

            /*
            conf.Add(new PluginConfigFields("OU base de busca", "dn_base", "", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Atributo de login", "username_attr", "", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Atributo de senha", "password_attr", "", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Atributo de grupo", "group_attr", "", PluginConfigTypes.String, false, ","));*/

            return conf.ToArray();
        }


        public override PluginConnectorConfigActions[] GetConfigActions()
        {

            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();
            conf.Add(new PluginConnectorConfigActions("Adição/remoção em grupo", "group", "Adicionar/remover o usuário em um grupo", "Nome do grupo", "group_name", "Nome do grupo que o usuário será adicionado/removido"));

            return conf.ToArray();
        }

        public override PluginConnectorBaseFetchResult FetchFields(Dictionary<String, Object> config)
        {
            PluginConnectorBaseFetchResult ret = new PluginConnectorBaseFetchResult();

            LogEvent iLog = new LogEvent(delegate(Object sender, PluginLogType type, string text)
            {
                if (Log != null)
                    Log(sender, type, text);
            });


            if (!CheckInputConfig(config, true, iLog, true, true))
            {
                ret.success = false;
                return ret;
            }

            List<PluginConfigFields> cfg = new List<PluginConfigFields>();
            PluginConfigFields[] tmpF = this.GetConfigFields();
            foreach (PluginConfigFields cf in tmpF)
            {
                try
                {
                    iLog(this, PluginLogType.Information, "Field " + cf.Name + " (" + cf.Key + "): " + (config.ContainsKey(cf.Key) ? config[cf.Key].ToString() : "empty"));
                }
                catch (Exception ex)
                {
                    iLog(this, PluginLogType.Information, "Field " + cf.Name + " (" + cf.Key + "): error on get data -> " + ex.Message);
                }
            }



            try
            {
                iLog(this, PluginLogType.Information, "Trying to get a Zabbix Auth Token");
                Uri apiUri = GetZabbixUriFromConfig(config);
                String sData = "";

                ZabbixAccessToken accessToken = GetToken(apiUri, "", config, 0, 0);
                if (accessToken == null)
                    return ret;

                UserListResult resp = null;
                try
                {

                    sData = JSON.Serialize2(new
                    {
                        jsonrpc = "2.0",
                        method = "user.get",
                        _params = new
                        {
                            output = "extend"
                        },
                        auth = accessToken.access_token,
                        id = 1
                    });
                    sData = sData.Replace("_params", "params");

                    resp = JSON.JsonWebRequest<UserListResult>(apiUri, sData, "application/json", null, "POST");
                    if (resp.error != null)
                    {
                        Log(this, PluginLogType.Error, "Error on get Zabbix users list: (" + resp.error.code + ") " + resp.error.message);
                        return ret;
                    }

                    foreach (UserData u in resp.result)
                    {

                        Dictionary<String, String> items = new Dictionary<string, string>();

                        items.Add("userid", u.userid);
                        items.Add("alias", u.alias);
                        items.Add("name", u.name);
                        items.Add("surname", u.surname);
                        items.Add("url", u.url);
                        items.Add("type", u.type);
                        items.Add("lang", u.lang);

                        foreach (String key in items.Keys)
                        {

                            if (!ret.fields.ContainsKey(key))
                                ret.fields.Add(key, new List<string>());

                            ret.fields[key].Add(items[key]);
                        }

                        //   Console.WriteLine(u.name.fullName);
                    }
                }
                catch (Exception ex)
                {
                    iLog(this, PluginLogType.Error, "Error on get Zabbix users list: " + ex.Message);
                }

                ret.success = true;
            }
            catch (Exception ex)
            {
                iLog(this, PluginLogType.Error, ex.Message);
            }

            return ret;
        }

        public override Boolean TestPlugin(Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            if (!CheckInputConfig(config, true, Log))
                return false;

            Uri apiUri = GetZabbixUriFromConfig(config);

            ZabbixAccessToken accessToken = GetToken(apiUri,"", config, 0, 0);
            if (accessToken == null)
                return false;


            return true;
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


        public override void ProcessImport(String cacheId, String importId, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            if (!CheckInputConfig(config, true, Log))
                return;

            try
            {
                Uri apiUri = GetZabbixUriFromConfig(config);
                String sData = "";
                
                ZabbixAccessToken accessToken = GetToken(apiUri, cacheId, config, 0, 0);
                if (accessToken == null)
                    return;

                UserListResult resp = null;
                try
                {

                    sData = JSON.Serialize2(new
                    {
                        jsonrpc = "2.0",
                        method = "user.get",
                        _params = new
                        {
                            output = "extend"
                        },
                        auth = accessToken.access_token,
                        id = 1
                    });
                    sData = sData.Replace("_params", "params");

                    resp = JSON.JsonWebRequest<UserListResult>(apiUri, sData, "application/json", null, "POST");
                    if (resp.error != null)
                    {
                        Log(this, PluginLogType.Error, "Error on get Zabbix users list: (" + resp.error.code + ") " + resp.error.message);
                        return;
                    }

                    foreach (UserData u in resp.result)
                    {
                        PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                        try
                        {

                            package.AddProperty("userid", u.userid, "string");
                            package.AddProperty("alias", u.alias, "string");
                            package.AddProperty("name", u.name, "string");
                            package.AddProperty("surname", u.surname, "string");
                            package.AddProperty("url", u.url, "string");
                            package.AddProperty("type", u.type, "string");
                            package.AddProperty("lang", u.lang, "string");

                            ImportPackageUser(package);
                        }
                        finally
                        {
                            package.Dispose();
                            package = null;
                        }
                        //   Console.WriteLine(u.name.fullName);
                    }
                }
                catch (Exception ex)
                {
                    Log(this, PluginLogType.Error, "Error on get Zabbix users list: " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, 0, 0, "Erro on import: " + ex.Message, "");
                throw ex;
            }
        }

        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            //Não implementado
        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            String lastStep = "";
            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {
                lastStep = "starting";

                if (!CheckInputConfig(config, true, Log))
                    return;

                String container = package.container;
                


                lastStep = "check full name";

                if ((package.fullName == null) || (package.fullName.fullName.Trim() == ""))
                {
                    String jData = "";

                    try
                    {
                        jData = JSON.Serialize<PluginConnectorBaseDeployPackage>(package);
                        if (package.password != "")
                            jData = jData.Replace(package.password, "Replaced for user security");
                    }
                    catch { }

                    logType = PluginLogType.Error;
                    processLog.AppendLine("IAM Full Name not found in properties list. " + jData);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Full Name not found in properties list", jData);
                    return;
                }

                if (container == "")
                    container = "IAMUsers";

                if (package.fullName.fullName.Length > 60)//Regra do Zabbix
                    package.fullName = new FullName(package.fullName.fullName.Substring(0, 60));


                String email = "";

                //Busca o e-mail nas propriedades específicas deste plugin
                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (dt.dataValue.ToLower().IndexOf("@") > 1)
                        email = dt.dataValue;

                //Se não encontrou o e-mail testa nas propriedades maracas como ID
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.ids)
                        if (dt.dataValue.ToLower().IndexOf("@") > 1)
                            email = dt.dataValue;
                }

                //Se não encontrou o e-mail testa nas propriedades gerais
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.properties)
                        if (dt.dataValue.ToLower().IndexOf("@") > 1)
                            email = dt.dataValue;
                }


                lastStep = "get Zabbix Token";

                Uri apiUri = GetZabbixUriFromConfig(config);

                ZabbixAccessToken accessToken = GetToken(apiUri, cacheId, config, package.entityId, package.identityId);
                if (accessToken == null)
                    throw new Exception("accessToken is null");

                if (String.IsNullOrWhiteSpace(accessToken.Authorization))
                    throw new Exception("accessToken.Authorization is null");

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", accessToken.Authorization);

                lastStep = "get user";

                String sData = JSON.Serialize2(new
                {
                    jsonrpc = "2.0",
                    method = "user.get",
                    _params = new
                    {
                        output = "extend",
                        selectMedias = "extend",
                        filter = new { alias = package.login }
                    },
                    auth = accessToken.access_token,
                    id = 1
                });
                sData = sData.Replace("_params", "params");

                UserListResult resp = JSON.JsonWebRequest<UserListResult>(apiUri, sData, "application/json", null, "POST");
                if (resp.error != null)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Error on get Zabbix users list: (" + resp.error.code + ") " + resp.error.message);
                    Log(this, PluginLogType.Error, "Error on get Zabbix users list: (" + resp.error.code + ") " + resp.error.message);
                    return;
                }

                processLog.AppendLine("User locked? " + (package.locked || package.temp_locked ? "true" : "false"));

                DateTime sDate = DateTime.Now;
                try
                {
                    if ((package.lastChangePassword != null) && (package.lastChangePassword != ""))
                        sDate = DateTime.Parse(package.lastChangePassword);
                }
                catch (Exception ex)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("System date parse error (" + package.lastChangePassword + "): " + ex.Message);
                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "System date parse error (" + package.lastChangePassword + "): " + ex.Message, "");
                }


                Int64 userid = 0;

                if ((resp.result == null) || (resp.result.Count == 0))//Novo usuário
                {
                    lastStep = "deploy new user start";

                    if ((package.locked) || (package.temp_locked))
                    {
                        logType = PluginLogType.Warning;
                        processLog.AppendLine("User not found in Zabbix and user is locked. Accound not created");
                        Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found in Zabbix and user is locked. Accound not created", "");
                        return;
                    }

                    if (package.password == "")
                    {
                        lastStep = "password is empty, creating...";
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        processLog.AppendLine("User not found in Zabbix and IAM Password not found in properties list, creating a random password (" + package.password + ")");
                    }

                    lastStep = "add container tree";
                    /*
                    try
                    {
                        Boolean contOK = AddContainerTree(package, accessToken, container);

                        if (!contOK)
                        {
                            logType = PluginLogType.Error;
                            processLog.AppendLine("Error creating container");
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error creating container", "");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        container = "";
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error creating container", ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
                    }*/

                    //Cria/verifica o grupo padrão dos usuáios
                    sData = JSON.Serialize2(new
                    {
                        jsonrpc = "2.0",
                        method = "usergroup.get",
                        _params = new
                        {
                            filter = new { name = "SafeID Users" }
                        },
                        auth = accessToken.access_token,
                        id = 1
                    });
                    sData = sData.Replace("_params", "params");
                    
                    lastStep = "Create default groups";
                    String defaultGroupId = "0";
                    GroupListResult grpInfo = JSON.JsonWebRequest<GroupListResult>(apiUri, sData, "application/json", null, "POST");
                    if (grpInfo == null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on get group info: return is empty");
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: return is empty", sData);
                        return;
                    }
                    else if (grpInfo.error != null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on get group info: (" + grpInfo.error.code + ") " + grpInfo.error.message);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on get group info: (" + grpInfo.error.code + ") " + grpInfo.error.message, sData);
                        return;
                    }
                    else
                    {

                        if (grpInfo.result.Count > 0)
                        {
                            defaultGroupId = grpInfo.result[0].usrgrpid;
                        }
                        else
                        {
                            {
                                //Cria o grupo
                                sData = JSON.Serialize2(new
                                {
                                    jsonrpc = "2.0",
                                    method = "usergroup.create",
                                    _params = new
                                    {
                                        name = "SafeID Users",
                                    },
                                    auth = accessToken.access_token,
                                    id = 1
                                });
                                sData = sData.Replace("_params", "params");

                                GroupCreateResult newGroup = JSON.JsonWebRequest<GroupCreateResult>(apiUri, sData, "application/json", null, "POST");
                                if (newGroup == null)
                                {
                                    logType = PluginLogType.Error;
                                    processLog.AppendLine("Error on add new group: return is empty");
                                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: return is empty", sData);
                                    return;
                                }
                                else if (newGroup.error != null)
                                {
                                    logType = PluginLogType.Error;
                                    processLog.AppendLine("Error on add new group: (" + newGroup.error.code + ") " + newGroup.error.message);
                                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: (" + newGroup.error.code + ") " + newGroup.error.message, sData);
                                    return;
                                }
                                else if ((newGroup.result.usrgrpids == null) || (newGroup.result.usrgrpids.Count == 0))
                                {
                                    logType = PluginLogType.Error;
                                    processLog.AppendLine("Error on add new group: return is empty");
                                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: return is empty", sData);
                                    return;
                                }
                                else
                                {
                                    defaultGroupId = newGroup.result.usrgrpids[0];
                                }

                            }
                        }
                    }


                    lastStep = "set user data variables";
                                        
                    JSON.DebugMessage dbg = new JSON.DebugMessage(delegate(String data, String debug)
                    {
#if debug
                        Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "JSON Debug message: " + data, debug);
#endif
                    });

                    Object medias = new Object[] { };

                    if (!String.IsNullOrEmpty(email))
                    {
                        medias = new Object[] { new { mediatypeid = "1", sendto = email, active = 0, severity = 56, period = "1-7,00:00-24:00" } };
                    }

                    sData = JSON.Serialize2(new
                    {
                        jsonrpc = "2.0",
                        method = "user.create",
                        _params = new
                        {
                            alias = package.login,
                            name = package.fullName.givenName,
                            surname = package.fullName.familyName,
                            passwd = package.password,
                            usrgrps = new Object[] { new { usrgrpid = defaultGroupId } },
                            user_medias = medias
                            /*user_medias = new { 
                                mediatypeid= "1",
                                sendto="support@company.com",
                                active= 0,
                                severity= 63,
                                period= "1-7,00:00-24:00"
                            }*/
                        },
                        auth = accessToken.access_token,
                        id = 1
                    });
                    sData = sData.Replace("_params", "params");


                    lastStep = "set user data on Zabbix";
                    UserCreateResult resp2 = JSON.JsonWebRequest<UserCreateResult>(apiUri, sData, "application/json", null, "POST");
                    if (resp2 == null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on add new user: return is empty");
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new user: return is empty", sData);
                        //Log(this, PluginLogType.Error, "Error on add new user: (" + resp2.error.code + ") " + resp2.error.message);
                        return;
                    }
                    else if (resp2.error != null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on add new user: (" + resp2.error.code + ") " + resp2.error.message);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new user: (" + resp2.error.code + ") " + resp2.error.message, "");
                        //Log(this, PluginLogType.Error, "Error on add new user: (" + resp2.error.code + ") " + resp2.error.message);
                        return;
                    }
                    else if ((resp2.result.userids == null) || (resp2.result.userids.Count == 0))
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on add new user: return is empty");
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new user: return is empty", sData);
                        //Log(this, PluginLogType.Error, "Error on add new user: (" + resp2.error.code + ") " + resp2.error.message);
                        return;
                    }

                    try
                    {
                        userid = Int64.Parse(resp2.result.userids[0]);
                    }
                    catch
                    {
                        throw new Exception("Error converting userid '" + resp2.result.userids[0] + "' to Int64");
                    }

                    lastStep = "notify changes";
                    NotityChangeUser(this, package.entityId);

                    processLog.AppendLine("User inserted");

                    lastStep = "deploy new user end";
                }
                else //Usuário existente, somente atualiza
                {

                    try
                    {
                        userid = Int64.Parse(resp.result[0].userid);
                    }
                    catch
                    {
                        throw new Exception("Error converting userid '" + resp.result[0].userid + "' to Int64");
                    }


                    lastStep = "deploy old user start";

                    JavaScriptSerializer ser = new JavaScriptSerializer();

                    String jUpdateUser = "";

#if debug
                    if((package.locked) || (package.temp_locked))
                        Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "DEBUG: UserData", JSON.Serialize<ZabbixDirectoryUserInfo>(resp));
#endif
                    /*
                    if ((package.locked) && (resp.lastLoginTime == "1970-01-01T00:00:00.000Z"))
                    {

                        lastStep = "delete locked and never logged user";

                        ZabbixDirectoryResponseBase resp2 = JSON.JsonWebRequest<ZabbixDirectoryResponseBase>(new Uri("https://www.Zabbixapis.com/admin/directory/v1/users/" + email), jUpdateUser, "application/json", headers, "DELETE");
                        if (resp2 == null)
                        {
                            logType = PluginLogType.Information;
                            processLog.AppendLine("User locked and never logged on Zabbix. Zabbix account deleted");
                            Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User locked and never logged on Zabbix. Zabbix account deleted", "");
                            return;
                        }
                        else if (resp2.error != null)
                        {
                            processLog.AppendLine("Error on delete user: (" + resp2.error.code + ") " + resp2.error.message);
                            //caso haja erro na exclusão deixa o processamento normal acontecer para bloquear o usuário
                        }
                        else
                        {
                            logType = PluginLogType.Information;
                            processLog.AppendLine("User locked and never logged on Zabbix. Zabbix account deleted");
                            Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User locked and never logged on Zabbix. Zabbix account deleted", "");
                            return;
                        }
                    }
                    */

                    if ((package.password != null) && (package.password != ""))
                    {

                        if ((package.locked) || (package.temp_locked))
                        {
                            package.password = IAM.Password.RandomPassword.Generate(16);
                            processLog.AppendLine("User locked, password temporarily changed to a random password " + package.password);
                            package.locked = false;
                        }


                    }

                    if ((package.password != null) && (package.password != ""))
                    {
                        lastStep = "password is empty, creating...";
                        lastStep = "set user data variables with password";
                        jUpdateUser = JSON.Serialize2(new
                        {
                            jsonrpc = "2.0",
                            method = "user.update",
                            _params = new
                            {
                                userid = resp.result[0].userid,
                                alias = package.login,
                                name = package.fullName.givenName,
                                surname = package.fullName.familyName,
                                passwd = package.password
                                /*user_medias = new { 
                                    mediatypeid= "1",
                                    sendto="support@company.com",
                                    active= 0,
                                    severity= 63,
                                    period= "1-7,00:00-24:00"
                                }*/
                            },
                            auth = accessToken.access_token,
                            id = 1
                        });
                        jUpdateUser = jUpdateUser.Replace("_params", "params");

                    }
                    else
                    {
                        lastStep = "set user data variables without password";
                        jUpdateUser = JSON.Serialize2(new
                        {
                            jsonrpc = "2.0",
                            method = "user.update",
                            _params = new
                            {
                                userid = resp.result[0].userid,
                                alias = package.login,
                                name = package.fullName.givenName,
                                surname = package.fullName.familyName
                                /*user_medias = new { 
                                    mediatypeid= "1",
                                    sendto="support@company.com",
                                    active= 0,
                                    severity= 63,
                                    period= "1-7,00:00-24:00"
                                }*/
                            },
                            auth = accessToken.access_token,
                            id = 1
                        });
                        jUpdateUser = jUpdateUser.Replace("_params", "params");

                    }

                    lastStep = "set user data on Zabbix";
                    UserCreateResult resp3 = JSON.JsonWebRequest<UserCreateResult>(apiUri, jUpdateUser, "application/json", null, "POST");
                    if (resp3.error != null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on update user: (" + resp3.error.code + ") " + resp3.error.message);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on update user: (" + resp3.error.code + ") " + resp3.error.message, "");
                        return;
                    }

                    
                    //Update Medias
                    if (!String.IsNullOrEmpty(email))
                    {
                        Boolean addMail = true;
                        List<Object> medias = new List<object>();
                        if (resp.result[0].medias != null)
                            foreach (MediaData md in resp.result[0].medias)
                            {
                                medias.Add(new { mediatypeid = md.mediatypeid, sendto = md.sendto, active = md.active, severity = md.severity, period = md.period });
                                
                                if (md.sendto.ToLower() == email.ToLower())
                                    addMail = false;
                            }

                        if (addMail)
                        {
                            medias.Add(new { mediatypeid = "1", sendto = email, active = 0, severity = 56, period = "1-7,00:00-24:00" });

                            jUpdateUser = JSON.Serialize2(new
                            {
                                jsonrpc = "2.0",
                                method = "user.updatemedia",
                                _params = new
                                {
                                    users = new Object[] { new { userid = userid } },
                                    medias = medias.ToArray()
                                },
                                auth = accessToken.access_token,
                                id = 1
                            });
                            jUpdateUser = jUpdateUser.Replace("_params", "params");

                            UserCreateResult resp4 = JSON.JsonWebRequest<UserCreateResult>(apiUri, jUpdateUser, "application/json", null, "POST");
                            if (resp4.error != null)
                            {
                                logType = PluginLogType.Error;
                                processLog.AppendLine("Error on update user medias: (" + resp4.error.code + ") " + resp4.error.message);
                                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on update user medias: (" + resp4.error.code + ") " + resp4.error.message, "");
                                return;
                            }
                        }
                    }

                    NotityChangeUser(this, package.entityId);
                    processLog.AppendLine("User updated");

                    lastStep = "deploy old user end";
                }

                //Executa as ações do RBAC
                if ((package.pluginAction != null) && (package.pluginAction.Count > 0))
                {

                    lastStep = "get updated user groups";

                    sData = JSON.Serialize2(new
                    {
                        jsonrpc = "2.0",
                        method = "usergroup.get",
                        _params = new
                        {
                            userids = userid.ToString(),
                            output = "extend"
                        },
                        auth = accessToken.access_token,
                        id = 1
                    });
                    sData = sData.Replace("_params", "params");

                    GroupListResult userGroupsInfo = JSON.JsonWebRequest<GroupListResult>(apiUri, sData, "application/json", null, "POST");
                    if (userGroupsInfo.error != null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on get Zabbix users group: (" + userGroupsInfo.error.code + ") " + userGroupsInfo.error.message);
                        Log(this, PluginLogType.Error, "Error on get Zabbix users groups: (" + userGroupsInfo.error.code + ") " + userGroupsInfo.error.message);
                        return;
                    }
                    else if ((userGroupsInfo.result == null) || (userGroupsInfo.result.Count == 0))
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on get updated user groups: return is empty");
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on get updated user groups: return is empty", sData);
                        return;
                    }


                    foreach (PluginConnectorBaseDeployPackageAction act in package.pluginAction)
                        try
                        {
                            processLog.AppendLine("Role: " + act.roleName + " (" + act.actionType.ToString() + ") " + act.ToString());

                            switch (act.actionKey.ToLower())
                            {
                                case "group":
                                    //Find group by name

                                    //Cria/verifica o grupo padrão dos usuáios
                                    sData = JSON.Serialize2(new
                                    {
                                        jsonrpc = "2.0",
                                        method = "usergroup.get",
                                        _params = new
                                        {
                                            filter = new { name = act.actionValue }
                                        },
                                        auth = accessToken.access_token,
                                        id = 1
                                    });
                                    sData = sData.Replace("_params", "params");
                    
                                    lastStep = "Get RBAC User group";
                                    String groupID = "0";
                                    GroupListResult grpInfo = JSON.JsonWebRequest<GroupListResult>(apiUri, sData, "application/json", null, "POST");
                                    if (grpInfo == null)
                                    {
                                        logType = PluginLogType.Error;
                                        processLog.AppendLine("Error on get group info: return is empty");
                                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: return is empty", sData);
                                        return;
                                    }
                                    else if (grpInfo.error != null)
                                    {
                                        logType = PluginLogType.Error;
                                        processLog.AppendLine("Error on get group info: (" + grpInfo.error.code + ") " + grpInfo.error.message);
                                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on get group info: (" + grpInfo.error.code + ") " + grpInfo.error.message, sData);
                                        return;
                                    }
                                    else
                                    {

                                        if (grpInfo.result.Count > 0)
                                        {
                                            groupID = grpInfo.result[0].usrgrpid;
                                        }
                                        else if(act.actionType == PluginActionType.Add)
                                        {
                                            {
                                                //Cria o grupo
                                                sData = JSON.Serialize2(new
                                                {
                                                    jsonrpc = "2.0",
                                                    method = "usergroup.create",
                                                    _params = new
                                                    {
                                                        name = act.actionValue,
                                                    },
                                                    auth = accessToken.access_token,
                                                    id = 1
                                                });
                                                sData = sData.Replace("_params", "params");

                                                GroupCreateResult newGroup = JSON.JsonWebRequest<GroupCreateResult>(apiUri, sData, "application/json", null, "POST");
                                                if (newGroup == null)
                                                {
                                                    logType = PluginLogType.Error;
                                                    processLog.AppendLine("Error on add new group: return is empty");
                                                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: return is empty", sData);
                                                    return;
                                                }
                                                else if (newGroup.error != null)
                                                {
                                                    logType = PluginLogType.Error;
                                                    processLog.AppendLine("Error on add new group: (" + newGroup.error.code + ") " + newGroup.error.message);
                                                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: (" + newGroup.error.code + ") " + newGroup.error.message, sData);
                                                    return;
                                                }
                                                else if ((newGroup.result.usrgrpids == null) || (newGroup.result.usrgrpids.Count == 0))
                                                {
                                                    logType = PluginLogType.Error;
                                                    processLog.AppendLine("Error on add new group: return is empty");
                                                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: return is empty", sData);
                                                    return;
                                                }
                                                else
                                                {
                                                    groupID = newGroup.result.usrgrpids[0];
                                                }

                                            }
                                        }
                                    }

                                    if (act.actionType == PluginActionType.Add)
                                    {
                                        sData = JSON.Serialize2(new
                                        {
                                            jsonrpc = "2.0",
                                            method = "usergroup.massadd",
                                            _params = new
                                            {
                                                usrgrpids = groupID,
                                                userids = userid
                                            },
                                            auth = accessToken.access_token,
                                            id = 1
                                        });
                                        sData = sData.Replace("_params", "params");

                                        GroupCreateResult newGroup = JSON.JsonWebRequest<GroupCreateResult>(apiUri, sData, "application/json", null, "POST");
                                        if (newGroup == null)
                                        {
                                            logType = PluginLogType.Error;
                                            processLog.AppendLine("Error on add user at group: return is empty");
                                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: return is empty", sData);
                                            return;
                                        }
                                        else if (newGroup.error != null)
                                        {
                                            logType = PluginLogType.Error;
                                            processLog.AppendLine("Error on add user at group: (" + newGroup.error.code + ") " + newGroup.error.message);
                                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: (" + newGroup.error.code + ") " + newGroup.error.message, sData);
                                            return;
                                        }
                                        else if ((newGroup.result.usrgrpids == null) || (newGroup.result.usrgrpids.Count == 0))
                                        {
                                            logType = PluginLogType.Error;
                                            processLog.AppendLine("Error on add user at group: return is empty");
                                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new group: return is empty", sData);
                                            return;
                                        }

                                        processLog.AppendLine("User added in group " + act.actionValue + " by role " + act.roleName);
                                            
                                    }
                                    else if (act.actionType == PluginActionType.Remove)
                                    {
                                        List<String> newGrps = new List<string>();
                                        foreach (GroupData gd in userGroupsInfo.result)
                                            if (gd.usrgrpid != groupID.ToString())
                                                newGrps.Add(gd.usrgrpid);


                                        
                                        String jUpdateUser = JSON.Serialize2(new
                                        {
                                            jsonrpc = "2.0",
                                            method = "user.update",
                                            _params = new
                                            {
                                                userid = resp.result[0].userid,
                                                usrgrps = newGrps.ToArray()
                                            },
                                            auth = accessToken.access_token,
                                            id = 1
                                        });
                                        jUpdateUser = jUpdateUser.Replace("_params", "params");


                                        UserCreateResult resp3 = JSON.JsonWebRequest<UserCreateResult>(apiUri, jUpdateUser, "application/json", null, "POST");

                                        if (resp3.error != null)
                                        {
                                            logType = PluginLogType.Error;
                                            processLog.AppendLine("Error on update user: (" + resp3.error.code + ") " + resp3.error.message);
                                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on update user: (" + resp3.error.code + ") " + resp3.error.message, "");
                                            return;
                                        }

                                    }
                                    break;

                                default:
                                    processLog.AppendLine("Action not recognized: " + act.actionKey);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            processLog.AppendLine("Error on execute action (" + act.actionKey + "): " + ex.Message);
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on execute action (" + act.actionKey + "): " + ex.Message, "");
                        }
                }

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Erro on deploy (last step: " + lastStep + "): " + ex.Message);
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Erro on deploy (last step: " + lastStep + "): " + ex.Message, "");
            }
            finally
            {
                Log2(this, logType, package.entityId, package.identityId, "Deploy executed", processLog.ToString());
                processLog.Clear();
                processLog = null;
            }
        }


        public override void ProcessDelete(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            throw new NotImplementedException("");

            /*
            String lastStep = "";
            try
            {
                lastStep = "starting";

                if (!CheckInputConfig(config, true, Log))
                    return;


                String email = "";
                String container = package.container;

                String mail_domain = config["mail_domain"].ToString();

                //Busca o e-mail nas propriedades específicas deste plugin
                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                        email = dt.dataValue;

                //Se não encontrou o e-mail testa nas propriedades maracas como ID
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.ids)
                        if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                            email = dt.dataValue;
                }

                //Se não encontrou o e-mail testa nas propriedades gerais
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.properties)
                        if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                            email = dt.dataValue;
                }

                lastStep = "check mail";

                if (email == "")
                {
                    String jData = "";

                    try
                    {
                        jData = JSON.Serialize<PluginConnectorBaseDeployPackage>(package);
                        if (package.password != "")
                            jData = jData.Replace(package.password, "Replaced for user security");
                    }
                    catch { }

                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Email not found in properties list. " + jData, "");
                    return;
                }

                lastStep = "check full name";

                if ((package.fullName == null) || (package.fullName.fullName.Trim() == ""))
                {
                    String jData = "";

                    try
                    {
                        jData = JSON.Serialize<PluginConnectorBaseDeployPackage>(package);
                        if (package.password != "")
                            jData = jData.Replace(package.password, "Replaced for user security");
                    }
                    catch { }

                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Full Name not found in properties list. " + jData, "");
                    return;
                }

                if (container == "")
                    container = "IAMUsers";


                if (package.fullName.fullName.Length > 60)//Regra do Zabbix
                    package.fullName = new FullName(package.fullName.fullName.Substring(0, 60));

                lastStep = "get Zabbix Token";
                
                Uri apiUri = GetZabbixUriFromConfig(config);
                
                ZabbixAccessToken accessToken = GetToken(apiUri, cacheId, config, package.entityId, package.identityId);
                if (accessToken == null)
                    throw new Exception("accessToken is null");

                if (String.IsNullOrWhiteSpace(accessToken.Authorization))
                    throw new Exception("accessToken.Authorization is null");

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", accessToken.Authorization);

                lastStep = "get mail";

                ZabbixDirectoryUserInfo resp = JSON.JsonWebRequest<ZabbixDirectoryUserInfo>(new Uri("https://www.Zabbixapis.com/admin/directory/v1/users/" + email), null, "application/json", headers);
                if (resp.error != null)
                {
                    //Verifica se o erro foi gerado por não haver o e-mail no Zabbix
                    Boolean notFound = false;
                    if (resp.error.errors != null)
                        foreach (ZabbixDirectoryResponseError err in resp.error.errors)
                            if (err.reason.ToLower() == "notfound")
                            {
                                notFound = true;
                                resp = null;
                            }

                    if (!notFound)
                    {
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on get Zabbix users list: (" + resp.error.code + ") " + resp.error.message, "");
                        return;
                    }
                }

                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User locked? " + (package.locked || package.temp_locked ? "true" : "false"), "");


                DateTime sDate = DateTime.Now;
                try
                {
                    if ((package.lastChangePassword != null) && (package.lastChangePassword != ""))
                        sDate = DateTime.Parse(package.lastChangePassword);
                }
                catch (Exception ex)
                {
                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "System date parse error (" + package.lastChangePassword + "): " + ex.Message, "");
                }


                if (resp == null)//Novo usuário
                {
                    Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found in Zabbix Apps", "");
                    return;
                }

                JavaScriptSerializer ser = new JavaScriptSerializer();

                String jUpdateUser = "";

#if DEBUG
                if ((package.locked) || (package.temp_locked))
                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "DEBUG: UserData", JSON.Serialize<ZabbixDirectoryUserInfo>(resp));
#endif 

                lastStep = "delete locked and never logged user";

                ZabbixDirectoryResponseBase resp2 = JSON.JsonWebRequest<ZabbixDirectoryResponseBase>(new Uri("https://www.Zabbixapis.com/admin/directory/v1/users/" + email), jUpdateUser, "application/json", headers, "DELETE");
                if (resp2 == null)
                {
                    //OK, excluiu
                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User deleted", "");
                }
                else if (resp2.error != null)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on delete user: (" + resp2.error.code + ") " + resp2.error.message, "");

                    //caso haja erro na exclusão deixa o processamento normal acontecer para bloquear o usuário
                }
                else
                {
                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User deleted", "");
                    return;
                }


                NotityDeletedUser(this, package.entityId, package.identityId);

                lastStep = "deploy old user end";

            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Erro on delete (last step: " + lastStep + "): " + ex.Message, "");
                throw ex;
            }*/
        }

        private Uri GetZabbixUriFromConfig(Dictionary<String, Object> config)
        {
            try
            {
                Uri tmp1 = new Uri(config["zabbix_uri"].ToString());

                return new Uri(tmp1.Scheme + "://" + tmp1.Host + (!tmp1.IsDefaultPort ? ":" + tmp1.Port : "") + ("/" + tmp1.AbsolutePath.Trim("/".ToCharArray())).TrimEnd("/".ToCharArray()) + "/api_jsonrpc.php");
                
            }
            catch (Exception ex)
            {
                throw new Exception("Erro building Zabbix Uri", ex);
            }
        }

        private ZabbixAccessToken GetToken(Uri apiUri, String cacheId, Dictionary<String, Object> config, Int64 entityId, Int64 identityId)
        {
            ZabbixAccessToken accessToken = new ZabbixAccessToken();
            accessToken.LoadFromFile(cacheId);

            //Verifica em cache se o token ainda e válido
            if (!accessToken.IsValid)
            {
                try
                {

                    JSON.DebugMessage dbg = new JSON.DebugMessage(delegate(String data, String debug)
                    {
#if DEBUG
                        if (Log2 != null) Log2(this, PluginLogType.Debug, entityId, identityId, "JSON Debug message: " + data, debug);
#endif
                        if (Log != null) Log(this, PluginLogType.Debug, "JSON Debug message: " + data + Environment.NewLine + debug);
                    });


                    accessToken = ZabbixJsonWebToken.GetAccessToken(apiUri, config["username"].ToString(), config["password"].ToString(), dbg);

                    if (accessToken == null)
                        throw new Exception("Access Token is null");

                    if (accessToken.error != null)
                        throw new Exception(accessToken.error);

                    if (String.IsNullOrWhiteSpace(accessToken.access_token))
                        throw new Exception("Access Token (access_token) is empty");

                }
                catch (Exception ex)
                {
                    if (Log != null) Log(this, PluginLogType.Debug, "Error on get Zabbix Auth Token: " + ex.Message);
                    Log2(this, PluginLogType.Error, entityId, identityId, "Error on get Zabbix Auth Token: " + ex.Message, "");
                    return null;
                }


            }

            return accessToken;
        }


        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }

}
