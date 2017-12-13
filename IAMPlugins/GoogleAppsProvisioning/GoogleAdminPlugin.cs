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

namespace GoogleAdmin
{
    public class GoogleAdminPlugin : PluginConnectorBase
    {
               
        public override String GetPluginName() { return "IAM for Google Apps Provisionin API Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com Google Apps"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://IAM/plugins/GoogleAdmin");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Email Address of Administrator Account", "admin_email_address", "", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Email Address of Service Account", "service_account_email_address", "", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("X509 Certificate of Service Account", "service_account_cert_data", "", PluginConfigTypes.Base64FileData, true, ","));
            conf.Add(new PluginConfigFields("Domain", "mail_domain", "", PluginConfigTypes.String, true, ","));

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
                iLog(this, PluginLogType.Information, "Trying to get a Google Auth Token");
                GoogleAccessToken accessToken = GetToken("", config, 0, 0);
                if (accessToken == null)
                    return ret;

                GoogleDirectoryResponse resp = null;
                try
                {
                    iLog(this, PluginLogType.Information, "Google Authorization = " + accessToken.Authorization);

                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("Authorization", accessToken.Authorization);

                    resp = JSON.JsonWebRequest<GoogleDirectoryResponse>(new Uri("https://www.googleapis.com/admin/directory/v1/users?domain=" + config["mail_domain"].ToString() + "&maxResults=20"), null, "application/json", headers);
                    if (resp.error != null)
                    {
                        iLog(this, PluginLogType.Error, "Error on get Google users list: (" + resp.error.code + ") " + resp.error.message);
                        return ret;
                    }

                    foreach (GoogleDirectoryUserInfo u in resp.users)
                    {
                        Dictionary<String, String> items = new Dictionary<string, string>();

                        items.Add("id", u.id);
                        items.Add("kind", u.kind);
                        items.Add("isAdmin", u.isAdmin.ToString());
                        items.Add("lastLoginTime", u.lastLoginTime);
                        items.Add("creationTime", u.creationTime);
                        items.Add("suspended", u.suspended.ToString());
                        items.Add("changePasswordAtNextLogin", u.changePasswordAtNextLogin.ToString());
                        items.Add("primaryEmail", u.primaryEmail);
                        items.Add("fullName", u.name.fullName);
                        items.Add("orgUnitPath", u.orgUnitPath);

                        if ((u.emails != null) && (u.emails.Count > 0))
                            items.Add("email", u.emails[0].address);

                        foreach (String key in items.Keys)
                        {

                            if (!ret.fields.ContainsKey(key))
                                ret.fields.Add(key, new List<string>());

                            ret.fields[key].Add(items[key]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    iLog(this, PluginLogType.Error, "Error on get Google users list: " + ex.Message);
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

            GoogleAccessToken accessToken = GetToken("", config, 0, 0);
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

                GoogleAccessToken accessToken = GetToken(cacheId, config, 0, 0);
                if (accessToken == null)
                    return;

                GoogleDirectoryResponse resp = null;
                do
                {
                    try
                    {
                        Dictionary<string, string> headers = new Dictionary<string, string>();
                        headers.Add("Authorization", accessToken.Authorization);

                        resp = JSON.JsonWebRequest<GoogleDirectoryResponse>(new Uri("https://www.googleapis.com/admin/directory/v1/users?domain=" + config["mail_domain"].ToString() + "&maxResults=50" + (resp != null && resp.nextPageToken != null ? "&pageToken=" + resp.nextPageToken : "")), null, "application/json", headers);
                        if (resp.error != null)
                        {
                            Log(this, PluginLogType.Error, "Error on get Google users list: (" + resp.error.code + ") " + resp.error.message);
                            return;
                        }

                        foreach (GoogleDirectoryUserInfo u in resp.users)
                        {
                            PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                            try
                            {
                                package.AddProperty("id", u.id, "string");
                                package.AddProperty("kind", u.kind, "string");
                                package.AddProperty("isAdmin", u.isAdmin.ToString(), "string");
                                package.AddProperty("lastLoginTime", u.lastLoginTime, "string");
                                package.AddProperty("creationTime", u.creationTime, "string");
                                package.AddProperty("suspended", u.suspended.ToString(), "string");
                                package.AddProperty("changePasswordAtNextLogin", u.changePasswordAtNextLogin.ToString(), "string");
                                package.AddProperty("primaryEmail", u.primaryEmail, "string");
                                package.AddProperty("fullName", u.name.fullName, "string");
                                package.AddProperty("orgUnitPath", u.orgUnitPath, "string");

                                if (u.emails != null)
                                    foreach (GoogleDirectoryUserEmail e in u.emails)
                                        if (!e.primary)
                                            package.AddProperty("email", e.address, "string");

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
                        Log(this, PluginLogType.Error, "Error on get Google users list: " + ex.Message);
                    }
                } while ((resp != null) && (resp.nextPageToken != null));



                /*
                 * Não realiza mais o revoke do token, pois salva o mesmo em arquivo para varias utilizações dentro do prazo de validade
                 */
                //Revoke this token
                /*
                try
                {
                    WebClient client = new WebClient();
                    client.DownloadData("https://accounts.google.com/o/oauth2/revoke?token=" + accessToken.access_token);
                }
                catch (Exception ex)
                {
                    Log(this, PluginLogType.Warning, "Non critical error on revoke token: " + ex.Message);
                }*/

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


                String email = "";
                String container = package.container;
                
                String mail_domain = config["mail_domain"].ToString().ToLower();

                //Busca o e-mail nas propriedades específicas desto usuário
                foreach (PluginConnectorBasePackageData dt in package.entiyData)
                    if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                        email = dt.dataValue;

                //Busca o e-mail nas propriedades específicas deste plugin
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.pluginData)
                        if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                            email = dt.dataValue;
                }

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

#if DEBUG
                try
                {
                    String jData = JSON.Serialize<PluginConnectorBaseDeployPackage>(package);
                    if (package.password != "")
                        jData = jData.Replace(package.password, "Replaced for user security");

                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "Package data", jData);
                }
                catch { }
#endif

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

                    logType = PluginLogType.Error;
                    processLog.AppendLine("IAM Email not found in properties list. " + jData);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Email not found in properties list", jData);
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

                    logType = PluginLogType.Error;
                    processLog.AppendLine("IAM Full Name not found in properties list. " + jData);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Full Name not found in properties list", jData);
                    return;
                }

                if (container == "")
                    container = "IAMUsers";

                
                if (package.fullName.fullName.Length > 60)//Regra do google
                    package.fullName = new FullName(package.fullName.fullName.Substring(0, 60));

                lastStep = "get Google Token";

                GoogleAccessToken accessToken = GetToken(cacheId, config, package.entityId, package.identityId);
                if (accessToken == null)
                    throw new Exception("accessToken is null");

                if (String.IsNullOrWhiteSpace(accessToken.Authorization))
                    throw new Exception("accessToken.Authorization is null");

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", accessToken.Authorization);

                lastStep = "get mail";

                GoogleDirectoryUserInfo resp = JSON.JsonWebRequest<GoogleDirectoryUserInfo>(new Uri("https://www.googleapis.com/admin/directory/v1/users/" + email), null, "application/json", headers);
                if (resp.error != null)
                {
                    //Verifica se o erro foi gerado por não haver o e-mail no google
                    Boolean notFound = false;
                    if (resp.error.errors != null)
                        foreach (GoogleDirectoryResponseError err in resp.error.errors)
                            if (err.reason.ToLower() == "notfound")
                            {
                                notFound = true;
                                resp = null;
                            }

                    if (!notFound)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on get Google users list: (" + resp.error.code + ") " + resp.error.message);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on get Google users list: (" + resp.error.code + ") " + resp.error.message, "");
                        return;
                    }
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


                if (resp == null)//Novo usuário
                {
                    lastStep = "deploy new user start";

                    if ((package.locked) || (package.temp_locked))
                    {
                        logType = PluginLogType.Warning;
                        processLog.AppendLine("User not found in Google and user is locked. Accound not created");
                        Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found in Google and user is locked. Accound not created", "");
                        return;
                    }

                    if (package.password == "")
                    {
                        lastStep = "password is empty, creating...";
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        processLog.AppendLine("User not found in Google and IAM Password not found in properties list, creating a random password (" + package.password + ")");
                    }

                    lastStep = "add container tree";
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
                    }

                    lastStep = "set user data variables";
                    var newUser = new
                    {
                        primaryEmail = email,
                        name = new { familyName = package.fullName.familyName, givenName = package.fullName.givenName },
                        suspended = package.locked,
                        password = package.password,
                        changePasswordAtNextLogin = false,
                        ipWhitelisted = false,
                        externalIds = new[] { new { value = sDate.ToString("yyyy-MM-dd HH:mm:ss"), type = "custom", customType = "IAMlastPasswordChanged" } },
                        orgUnitPath = "/" + container.Replace("\\", "/").Trim("/".ToCharArray()),
                        includeInGlobalAddressList = true

                    };


                    JavaScriptSerializer ser = new JavaScriptSerializer();

                    String jNewUser = ser.Serialize(newUser);

                    JSON.DebugMessage dbg = new JSON.DebugMessage(delegate(String data, String debug)
                    {
#if DEBUG
                        Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "JSON Debug message: " + data, debug);
#endif
                    });

                    lastStep = "set user data on google";
                    GoogleDirectoryResponseBase resp2 = JSON.JsonWebRequest<GoogleDirectoryResponseBase>(new Uri("https://www.googleapis.com/admin/directory/v1/users"), jNewUser, "application/json", headers, "POST", null, dbg);
                    if (resp2 == null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on add new user: return is empty");
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add new user: return is empty", jNewUser);
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

                    lastStep = "notify changes";
                    NotityChangeUser(this, package.entityId);

                    processLog.AppendLine("User inserted");

                    lastStep = "deploy new user end";
                }
                else //Usuário existente, somente atualiza
                {
                    lastStep = "deploy old user start";

                    JavaScriptSerializer ser = new JavaScriptSerializer();

                    String jUpdateUser = "";

#if DEBUG
                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "DEBUG: UserData", JSON.Serialize<GoogleDirectoryUserInfo>(resp));
#endif

                    if ((package.locked) && (resp.lastLoginTime == "1970-01-01T00:00:00.000Z"))
                    {

                        lastStep = "delete locked and never logged user";

                        GoogleDirectoryResponseBase resp2 = JSON.JsonWebRequest<GoogleDirectoryResponseBase>(new Uri("https://www.googleapis.com/admin/directory/v1/users/" + email), jUpdateUser, "application/json", headers, "DELETE");
                        if (resp2 == null)
                        {
                            logType = PluginLogType.Information;
                            processLog.AppendLine("User locked and never logged on Google. Google account deleted");
                            Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User locked and never logged on Google. Google account deleted", "");
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
                            processLog.AppendLine("User locked and never logged on Google. Google account deleted");
                            Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User locked and never logged on Google. Google account deleted", "");
                            return;
                        }
                    }


                    if ((package.password != null) && (package.password != ""))
                    {
                        
                        DateTime gDate = new DateTime(1970, 01, 01);
                        String gDateText = "";
                        try
                        {


                            if ((resp.externalIds != null) && (resp.externalIds.Count > 0))
                                foreach (GoogleDirectoryExternalIds e in resp.externalIds)
                                    if (e.customType.ToLower() == "iamlastpasswordchanged")
                                        gDateText = e.value;

                            if (gDateText != "")
                                gDate = DateTime.Parse(gDateText);

#if DEBUG
                            if (gDate.Year == 1970)
                                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "Google data", JSON.Serialize<GoogleDirectoryUserInfo>(resp));
#endif
                        }
                        catch (Exception ex)
                        {
                            processLog.AppendLine("Google date parse error (" + gDateText + "): " + ex.Message);
#if DEBUG
                            Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "Google date parse error (" + gDateText + "): " + ex.Message, "");
                            Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "Google data: " + JSON.Serialize<GoogleDirectoryUserInfo>(resp), "");
#endif
                        }

                        //Se o usuário está bloqueado altera a data de última atualização de senha para cair no "if" específico de atualização de senha
                        if ((package.locked) || (package.temp_locked))
                        {
                            package.password = IAM.Password.RandomPassword.Generate(16);

                            //Retrocede a data de ultima atualização da senha nos dados do google
                            //para que quando o usuário saia do status de bloqueio a senha verdadeira seja atualizada
                            sDate = sDate.AddDays(-1); 
                            gDate = new DateTime(1970, 01, 01);
                        }

                        if (gDate.CompareTo(sDate) >= 0) //A data do google é maior ou igual a data da ultima atualização no sistema
                        {
                            package.password = "";
                            processLog.AppendLine("Password is the most updated. Update date: " + gDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        else
                        {
                            processLog.AppendLine("Password is outdated, will be updated. Last update date: " + gDate.ToString("yyyy-MM-dd HH:mm:ss"));

                            /* Como o Gmail ao bloquear o usuário o mesmo para de receber e-mails
                            Foi encontrado como solução a troca da senha para evitar que o usuário acesse o e-mail porém continue recebendo e-mails
                            Se certificar que o código abaixo foi adicionado antes deste bloco de if para que a execução chegue neste ponto
                            if ((package.locked) || (package.temp_locked))
                            {
                                package.password = IAM.Password.RandomPassword.Generate(16);
                                sDate = sDate.AddDays(-1); 
                                gDate = new DateTime(1970, 01, 01);
                            }
                            */
                            if ((package.locked) || (package.temp_locked))
                            {
                                package.password = IAM.Password.RandomPassword.Generate(16);
                                processLog.AppendLine("User locked, password temporarily changed to a random password " + package.password);
                                package.locked = false;
                            }

                        }
                    }

                    //Nunca permitir que o usuário seja bloqueado, para não parar de receber e-mail.
                    //O código acima troca a senha deste usuário caso o mesmo esteja bloqueado
                    // para evitar que o mesmo tenha acesso ao e-mail.
                    package.locked = false;

                    //Log(this, PluginLogType.Information, JSON.Serialize<GoogleDirectoryUserInfo>(resp));

                    if ((package.password != null) && (package.password != ""))
                    {
                        lastStep = "password is empty, creating...";
                        lastStep = "set user data variables with password";
                        var updateUser = new
                        {
                            name = new { familyName = package.fullName.familyName, givenName = package.fullName.givenName },
                            suspended = package.locked,
                            password = package.password,
                            changePasswordAtNextLogin = false,
                            ipWhitelisted = false,
                            externalIds = new[] { new { value = sDate.ToString("o"), type = "custom", customType = "IAMlastPasswordChanged" } },
                            orgUnitPath = "/",// + container.Replace("\\","/"),
                            includeInGlobalAddressList = true

                        };

                        jUpdateUser = ser.Serialize(updateUser);
                    }
                    else
                    {
                        lastStep = "set user data variables without password";
                        var updateUser = new
                        {
                            name = new { familyName = package.fullName.familyName, givenName = package.fullName.givenName },
                            suspended = package.locked,
                            changePasswordAtNextLogin = false,
                            ipWhitelisted = false,
                            orgUnitPath = "/",// + container.Replace("\\","/"),
                            includeInGlobalAddressList = true

                        };

                        jUpdateUser = ser.Serialize(updateUser);

                    }

                    lastStep = "set user data on google";
                    GoogleDirectoryResponseBase resp3 = JSON.JsonWebRequest<GoogleDirectoryResponseBase>(new Uri("https://www.googleapis.com/admin/directory/v1/users/" + email), jUpdateUser, "application/json", headers, "PUT");

                    if (resp3.error != null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on update user: (" + resp3.error.code + ") " + resp3.error.message);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on update user: (" + resp3.error.code + ") " + resp3.error.message, "");
                        return;
                    }


                    NotityChangeUser(this, package.entityId);
                    processLog.AppendLine("User updated");

                    lastStep = "deploy old user end";
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


                if (package.fullName.fullName.Length > 60)//Regra do google
                    package.fullName = new FullName(package.fullName.fullName.Substring(0, 60));

                lastStep = "get Google Token";

                GoogleAccessToken accessToken = GetToken(cacheId, config, package.entityId, package.identityId);
                if (accessToken == null)
                    throw new Exception("accessToken is null");

                if (String.IsNullOrWhiteSpace(accessToken.Authorization))
                    throw new Exception("accessToken.Authorization is null");

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", accessToken.Authorization);

                lastStep = "get mail";

                GoogleDirectoryUserInfo resp = JSON.JsonWebRequest<GoogleDirectoryUserInfo>(new Uri("https://www.googleapis.com/admin/directory/v1/users/" + email), null, "application/json", headers);
                if (resp.error != null)
                {
                    //Verifica se o erro foi gerado por não haver o e-mail no google
                    Boolean notFound = false;
                    if (resp.error.errors != null)
                        foreach (GoogleDirectoryResponseError err in resp.error.errors)
                            if (err.reason.ToLower() == "notfound")
                            {
                                notFound = true;
                                resp = null;
                            }

                    if (!notFound)
                    {
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on get Google users list: (" + resp.error.code + ") " + resp.error.message, "");
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
                    Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found in Google Apps", "");
                    return;
                }

                JavaScriptSerializer ser = new JavaScriptSerializer();

                String jUpdateUser = "";

#if DEBUG
                if ((package.locked) || (package.temp_locked))
                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "DEBUG: UserData", JSON.Serialize<GoogleDirectoryUserInfo>(resp));
#endif 

                if ((package.locked) && (resp.lastLoginTime == "1970-01-01T00:00:00.000Z"))
                {

                    lastStep = "delete locked and never logged user";

                    GoogleDirectoryResponseBase resp2 = JSON.JsonWebRequest<GoogleDirectoryResponseBase>(new Uri("https://www.googleapis.com/admin/directory/v1/users/" + email), jUpdateUser, "application/json", headers, "DELETE");
                    if (resp2 == null)
                    {
                        Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User locked and never logged on Google. Google account deleted", "");
                        return;
                    }
                    else if (resp2.error != null)
                    {
                        //caso haja erro na exclusão deixa o processamento normal acontecer para bloquear o usuário
                    }
                    else
                    {
                        Log2(this, PluginLogType.Information, package.entityId, package.identityId, "Google account deleted", "");
                        return;
                    }
                }

                lastStep = "lock user account";

                var updateUser = new
                {
                    name = new { familyName = package.fullName.familyName, givenName = package.fullName.givenName },
                    suspended = true,
                    changePasswordAtNextLogin = false,
                    ipWhitelisted = false,
                    orgUnitPath = "/",// + container.Replace("\\","/"),
                    includeInGlobalAddressList = true

                };

                jUpdateUser = ser.Serialize(updateUser);

                GoogleDirectoryResponseBase resp3 = JSON.JsonWebRequest<GoogleDirectoryResponseBase>(new Uri("https://www.googleapis.com/admin/directory/v1/users/" + email), jUpdateUser, "application/json", headers, "PUT");

                if (resp3.error != null)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error locking user: (" + resp3.error.code + ") " + resp3.error.message, "");
                    return;
                }


                /*
                GoogleDirectoryResponseBase resp2 = JSON.JsonWebRequest<GoogleDirectoryResponseBase>(new Uri("https://www.googleapis.com/admin/directory/v1/users/" + email), jUpdateUser, "application/json", headers, "DELETE");
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
                */

                NotityDeletedUser(this, package.entityId, package.identityId);

                lastStep = "deploy old user end";

            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Erro on delete (last step: " + lastStep + "): " + ex.Message, "");
                throw ex;
            }
        }

        public Boolean AddContainerTree(PluginConnectorBaseDeployPackage package, GoogleAccessToken accessToken, String container)
        {
            if (accessToken == null)
                throw new Exception("AccessToken is null");

            if (container == null)
                throw new Exception("Container is null");

            if (package == null)
                throw new Exception("Package is null");

            if (accessToken.Authorization == null)
                throw new Exception("AccessToken Authorization is null");

            String[] tree = container.Replace("\\","/").Trim("/ ".ToCharArray()).Split("/".ToCharArray());

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", accessToken.Authorization);


            String lastNode = "";

            foreach (String ouName in tree)
            {
                try
                {
                    GoogleDirectoryResponseBase resp = JSON.JsonWebRequest<GoogleDirectoryResponseBase>(new Uri("https://www.googleapis.com/admin/directory/v1/customer/" + accessToken.customer_id + "/orgunits" + lastNode + "/" + ouName), null, "application/json", headers);
                    if (resp == null)
                    {
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error get OrganizationalUnit info: Google response is empty", "");
                        return false;
                    }

                    if (resp.error != null)
                    {
                        //Verifica se o erro foi gerado por não haver o e-mail no google
                        Boolean notFound = false;
                        if (resp.error.errors != null)
                            foreach (GoogleDirectoryResponseError err in resp.error.errors)
                                if (err.reason.ToLower() == "notfound")
                                {
                                    notFound = true;
                                    resp = null;
                                }

                        if (!notFound)
                        {
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error get OrganizationalUnit info: (" + resp.error.code + ") " + resp.error.message, "");
                            return false;
                        }
                    }
                    else
                    {
                        //Existe pode continuar
                        continue;
                    }

                    //Não existe deve criar

                    var updateUser = new
                    {
                        name = ouName,
                        description = ouName,
                        parentOrgUnitPath = (lastNode == "" ? "/" : lastNode),
                        ipWhitelisted = false,
                        blockInheritance = false

                    };

                    JavaScriptSerializer ser = new JavaScriptSerializer();

                    String jNewOU = ser.Serialize(updateUser);

                    GoogleDirectoryResponseBase resp2 = JSON.JsonWebRequest<GoogleDirectoryResponseBase>(new Uri("https://www.googleapis.com/admin/directory/v1/customer/" + accessToken.customer_id + "/orgunits"), jNewOU, "application/json", headers);
                    if (resp2.error != null)
                    {
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error create OrganizationalUnit info: (" + resp2.error.code + ") " + resp2.error.message, "");
                        return false;
                    }

                }
                finally
                {
                    lastNode += "/" + ouName;
                }

            }

            return true;
        }
        
        private GoogleAccessToken GetToken(String cacheId, Dictionary<String, Object> config, Int64 entityId, Int64 identityId)
        {
            GoogleAccessToken accessToken = new GoogleAccessToken();
            accessToken.LoadFromFile(cacheId);

            //Verifica em cache se o token ainda e válido
            if (!accessToken.IsValid)
            {
                try
                {

                    JSON.DebugMessage dbg = new JSON.DebugMessage(delegate(String data, String debug)
                    {
#if DEBUG
                        Log2(this, PluginLogType.Debug, entityId, identityId, "JSON Debug message: " + data, debug);
#endif
                        if (Log != null) Log(this, PluginLogType.Debug, "JSON Debug message: " + data + Environment.NewLine + debug);
                    });


                    accessToken = GoogleJsonWebToken.GetAccessToken(config["service_account_cert_data"].ToString(), config["service_account_email_address"].ToString(), "https://www.googleapis.com/auth/admin.directory.user https://www.googleapis.com/auth/admin.directory.group https://www.googleapis.com/auth/admin.directory.orgunit", config["admin_email_address"].ToString(), dbg);

                    if (accessToken == null)
                        throw new Exception("Access Token is null");

                    if (accessToken.error != null)
                        throw new Exception(accessToken.error);

                    if (String.IsNullOrWhiteSpace(accessToken.token_type))
                        throw new Exception("Access Token (token_type) is empty");

                    if (String.IsNullOrWhiteSpace(accessToken.access_token))
                        throw new Exception("Access Token (access_token) is empty");

                }
                catch (Exception ex)
                {
                    if (Log != null) Log(this, PluginLogType.Debug, "Error on get Google OAuth 2.0 Token: " + ex.Message);
                    Log2(this, PluginLogType.Error, entityId, identityId, "Error on get Google OAuth 2.0 Token: " + ex.Message, "");
                    return null;
                }

                try
                {

                    //Recupera o customerId
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("Authorization", accessToken.Authorization);

                    GoogleDirectoryUserInfo resp = JSON.JsonWebRequest<GoogleDirectoryUserInfo>(new Uri("https://www.googleapis.com/admin/directory/v1/users/" + config["admin_email_address"].ToString()), null, "application/json", headers);
                    if (resp.error != null)
                    {
                        Log(this, PluginLogType.Error, "Error on get Google users info: (" + resp.error.code + ") " + resp.error.message);
                        return null;
                    }
                    else
                    {
                        accessToken.customer_id = resp.customerId;
                    }

                    if ((accessToken.error == null) && (!String.IsNullOrEmpty(cacheId)))
                        accessToken.SaveToFile(cacheId);

                }
                catch (Exception ex)
                {
                    if (Log != null) Log(this, PluginLogType.Debug, "Error on get Google OAuth 2.0 Customer ID: " + ex.Message);
                    Log2(this, PluginLogType.Error, entityId, identityId, "Error on get Google OAuth 2.0 Customer ID: " + ex.Message, "");
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
