using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IAM.PluginInterface;
using System.Net;
using System.Web;
using SafeTrend.Json;

namespace CPanelV2
{

    public class CPanelPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "IAM cPanel V2 Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir cPanel V2 jSON API"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/cpanelv2");
        }

        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("URL do servidor", "server_uri", "URL do servidor", PluginConfigTypes.Uri, true, @"http://localhost:2082/"));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha", PluginConfigTypes.Password, true, ""));

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

                iLog(this, PluginLogType.Error, "Operação não implementada neste plugin");

                //ret.success = true;
            }
            catch (Exception ex)
            {
                iLog(this, PluginLogType.Error, ex.Message);
            }

            return ret;
        }

        public override Boolean TestPlugin(Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
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


            String lastStep = "";
            try
            {
                lastStep = "0";
                if (!CheckInputConfig(config, true, Log))
                    return;

                lastStep = "1";
                Uri serverUri = new Uri(config["server_uri"].ToString());

                lastStep = "2";
                try
                {
                    String mail_domain = "";

                    try
                    {
                        if (config["iam_mail_domain"] != null)
                        {
                            if (!String.IsNullOrWhiteSpace(config["iam_mail_domain"].ToString()))
                                mail_domain = config["iam_mail_domain"].ToString().ToLower();
                        }
                    }
                    catch { }

                    Log(this, PluginLogType.Debug, "mail_domain: " + mail_domain);

                    lastStep = "3";
                    cPanelLogin login = JSON.JsonWebRequest<cPanelLogin>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/login/?login_only=1"), "user=" + config["username"].ToString() + "&pass=" + config["password"].ToString(), "application/x-www-form-urlencoded", null, "POST", null);
                    if (login == null)
                    {
                        Log2(this, PluginLogType.Error, 0, 0, "Unexpected error on cPannel authentication", "");
                        return;
                    }

                    lastStep = "4";

                    if (login.status != 1)
                        throw new Exception("error on login: " + (login.message != null ? login.message : ""));

                    lastStep = "5";
                    string authInfo = config["username"].ToString() + ":" + config["password"].ToString();
                    authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("Authorization", "Basic " + authInfo);

                    lastStep = "6";
                    cPanelResultBase accounts = JSON.JsonWebRequest<cPanelResultBase>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + login.security_token + "/json-api/cpanel?cpanel_jsonapi_version=2&cpanel_jsonapi_func=listpopswithdisk&cpanel_jsonapi_module=Email&api2_paginate=1&api2_paginate_size=100000&api2_paginate_start=1&api2_sort=1&api2_sort_column=user&api2_sort_method=alphabet&api2_sort_reverse=0"), "", "application/x-www-form-urlencoded", headers, "GET");
                    if (accounts.cpanelresult == null)
                    {
                        Log2(this, PluginLogType.Error, 0, 0, "Unexpected error on get cPannel user list", "");
                        return;
                    }

                    lastStep = "7";
                    if (accounts.cpanelresult.error != null)
                    {
                        Log2(this, PluginLogType.Error, 0, 0, "Error on get cPannel users list: " + accounts.cpanelresult.error, "");
                        return;
                    }

                    lastStep = "8";
                    foreach (cPanelResultData u in accounts.cpanelresult.data)
                    {
                        lastStep = "8.1";
                        if ((!String.IsNullOrWhiteSpace(mail_domain)) && (u.domain.ToLower().Trim() != mail_domain.Trim()))
                            continue;
                        
                        PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                        try
                        {

                            package.AddProperty("user", u.user, "string");
                            package.AddProperty("email", u.email, "string");
                            package.AddProperty("domain", u.domain, "string");

                            ImportPackageUser(package);
                        }catch{}
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
                    Log2(this, PluginLogType.Error, 0, 0, "Error on get cPannel users list: " + ex.Message, "");
                }

            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, "Erro on import (" + lastStep + "): " + ex.Message);
                Log2(this, PluginLogType.Error, 0, 0, "Erro on import (" + lastStep + "): " + ex.Message, "");
                throw ex;
            }

        }

        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            //Não implementado
        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {
                Uri serverUri = new Uri(config["server_uri"].ToString());

                CookieContainer cookie = new CookieContainer();
                cPanelLogin cPlogin = JSON.JsonWebRequest<cPanelLogin>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/login/?login_only=1"), "user=" + config["username"].ToString() + "&pass=" + config["password"].ToString(), "application/x-www-form-urlencoded", null, "POST", cookie);

                if (cPlogin.status != 1)
                    throw new Exception("error on login: " + cPlogin.message);


                string authInfo = config["username"].ToString() + ":" + config["password"].ToString();
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Basic " + authInfo);


                //Lista as zonas DNS para verificar se os e-mails a serem importados fazem parte das zonas disponíveis
                //Object accounts = JSON.JsonWebRequest<Object>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + login.security_token + "/json-api/cpanel?cpanel_jsonapi_version=2&cpanel_jsonapi_func=listzones&cpanel_jsonapi_module=Dns"), "", "application/x-www-form-urlencoded", headers, "GET");
                /*if (accounts.cpanelresult == null)
                {
                    Log(this, PluginLogType.Error, "Unexpected erro on get cPannel user list");
                }

                if (accounts.cpanelresult.error != null)
                {
                    Log(this, PluginLogType.Error, "Error on get cPannel users list: " + accounts.cpanelresult.error);
                    return;
                }


                foreach (cPannelResultUserData u in accounts.cpanelresult.data)
                {

                }
                */


                String login = package.login;
                String email = package.login;
                String container = package.container;

                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (dt.dataName.ToLower() == "login")
                        login = dt.dataValue;
                    else if (dt.dataName.ToLower() == "email")
                        email = dt.dataValue;

                if (login == "")
                    login = package.login;

                if (login == "")
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("IAM Login not found in properties list");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Login not found in properties list", "");
                    return;
                }

                if (email == "")
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("IAM E-mail not found in properties list");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM E-mail not found in properties list", "");
                    return;
                }

                if (container == "")
                    container = "IAMUsers";

                cPanelResultBase accounts = JSON.JsonWebRequest<cPanelResultBase>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + cPlogin.security_token + "/json-api/cpanel?cpanel_jsonapi_version=2&cpanel_jsonapi_func=listpopswithdisk&cpanel_jsonapi_module=Email&api2_paginate=1&api2_paginate_size=100000&api2_paginate_start=1&api2_sort=1&api2_sort_column=user&api2_sort_method=alphabet&api2_sort_reverse=0&api2_filter=1&api2_filter_type=contains&api2_filter_column=email&api2_filter_term=" + HttpUtility.UrlEncode(email)), "", "application/x-www-form-urlencoded", headers, "GET");
                if (accounts.cpanelresult == null)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Unexpected error on get cPannel user list");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unexpected error on get cPannel user list", "");
                    return;
                }

                if (accounts.cpanelresult.error != null)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Error on get cPannel users list: " + accounts.cpanelresult.error);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on get cPannel users list: " + accounts.cpanelresult.error, "");
                    return;
                }

                if (accounts.cpanelresult.data.Count == 0)
                {

                    if (package.password == "")
                    {
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        processLog.AppendLine("User not found in AD and IAM Password not found in properties list, creating a random password (" + package.password + ")");
                    }


                    if ((package.locked) || (package.temp_locked))
                    {
                        //O cPannel não permite o bloqueio da conta, a forma encontrada de bloquea-la é trocando a senha
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        processLog.AppendLine("User locked, password temporarily changed to a random password " + package.password);
                    }


                    String[] maisParts = email.Split("@".ToCharArray(), 2);

                    cPanelResultBase retNewUser = JSON.JsonWebRequest<cPanelResultBase>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + cPlogin.security_token + "/json-api/cpanel?cpanel_jsonapi_version=2&cpanel_jsonapi_module=Email&cpanel_jsonapi_func=addpop&email=" + maisParts[0] + "&password=" + HttpUtility.UrlEncode(package.password) + "&quota=250&domain=" + maisParts[1]), "", "application/x-www-form-urlencoded", headers, "GET");
                    if (retNewUser.cpanelresult == null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Unexpected error on add user on cPannel");
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unexpected error on add user on cPannel", "");
                        return;
                    }

                    if (retNewUser.cpanelresult.error != null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on add user on cPannel: " + retNewUser.cpanelresult.error);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add user on cPannel: " + retNewUser.cpanelresult.error, "");
                        return;
                    }

                    if (retNewUser.cpanelresult.data.Count == 0)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Unexpected error on add user on cPannel");
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unexpected error on add user on cPannel", "");
                        return;
                    }

                    if (retNewUser.cpanelresult.data[0].result != "1")
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on add user on cPannel: " + retNewUser.cpanelresult.data[0].reason);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add user on cPannel: " + retNewUser.cpanelresult.data[0].reason, "");
                        return;
                    }

                    processLog.AppendLine("User added");
                }
                else
                {
                    //Usuário antigo, somente atualiza
                    //cPannelResultData userData = accounts.cpanelresult.data[0];


                    if ((package.locked) || (package.temp_locked))
                    {
                        //O cPannel não permite o bloqueio da conta, a forma encontrada de bloquea-la é trocando a senha
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        processLog.AppendLine("User locked, password temporarily changed to a random password " + package.password);
                    }


                    if (!String.IsNullOrWhiteSpace(package.password))
                    {

                        String[] maisParts = email.Split("@".ToCharArray(), 2);

                        cPanelResultBase changePwd = JSON.JsonWebRequest<cPanelResultBase>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + cPlogin.security_token + "/json-api/cpanel?cpanel_jsonapi_version=2&cpanel_jsonapi_module=Email&cpanel_jsonapi_func=passwdpop&email=" + maisParts[0] + "&domain=" + maisParts[1] + "&password=" + HttpUtility.UrlEncode(package.password)), "", "application/x-www-form-urlencoded", headers, "GET");
                        if (changePwd.cpanelresult == null)
                        {
                            logType = PluginLogType.Error;
                            processLog.AppendLine("Unexpected error on add user on cPannel");
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unexpected error on add user on cPannel", "");
                            return;
                        }

                        if (changePwd.cpanelresult.error != null)
                        {
                            logType = PluginLogType.Error;
                            processLog.AppendLine("Error on add user on cPannel: " + changePwd.cpanelresult.error);
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add user on cPannel: " + changePwd.cpanelresult.error, "");
                            return;
                        }

                        if (changePwd.cpanelresult.data.Count == 0)
                        {
                            logType = PluginLogType.Error;
                            processLog.AppendLine("Unexpected error on add user on cPannel");
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unexpected error on add user on cPannel", "");
                            return;
                        }

                        if (changePwd.cpanelresult.data[0].result != "1")
                        {
                            logType = PluginLogType.Error;
                            processLog.AppendLine("Error on add user on cPannel: " + changePwd.cpanelresult.data[0].reason);
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add user on cPannel: " + changePwd.cpanelresult.data[0].reason, "");
                            return;
                        }


                    }


                    NotityChangeUser(this, package.entityId);

                    if (!String.IsNullOrWhiteSpace(package.password))
                        processLog.AppendLine("User updated with password");
                    else
                        processLog.AppendLine("User updated without password");

                }

                processLog.AppendLine("User locked? " + (package.locked ? "true" : "false"));

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process deploy: " + ex.Message);
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message, "");
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

            if (!CheckInputConfig(config, true, Log))
                return;


            try
            {
                Uri serverUri = new Uri(config["server_uri"].ToString());

                CookieContainer cookie = new CookieContainer();
                cPanelLogin cPlogin = JSON.JsonWebRequest<cPanelLogin>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/login/?login_only=1"), "user=" + config["username"].ToString() + "&pass=" + config["password"].ToString(), "application/x-www-form-urlencoded", null, "POST", cookie);

                if (cPlogin.status != 1)
                    throw new Exception("error on login: " + cPlogin.message);


                string authInfo = config["username"].ToString() + ":" + config["password"].ToString();
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Basic " + authInfo);

                String login = package.login;
                String email = package.login;
                String container = package.container;

                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (dt.dataName.ToLower() == "login")
                        login = dt.dataValue;
                    else if (dt.dataName.ToLower() == "email")
                        email = dt.dataValue;

                if (login == "")
                    login = package.login;

                if (login == "")
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Login not found in properties list", "");
                    return;
                }

                if (email == "")
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM E-mail not found in properties list", "");
                    return;
                }

                if (container == "")
                    container = "IAMUsers";

                cPanelResultBase accounts = JSON.JsonWebRequest<cPanelResultBase>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + cPlogin.security_token + "/json-api/cpanel?cpanel_jsonapi_version=2&cpanel_jsonapi_func=listpopswithdisk&cpanel_jsonapi_module=Email&api2_paginate=1&api2_paginate_size=100000&api2_paginate_start=1&api2_sort=1&api2_sort_column=user&api2_sort_method=alphabet&api2_sort_reverse=0&api2_filter=1&api2_filter_type=contains&api2_filter_column=email&api2_filter_term=" + HttpUtility.UrlEncode(email)), "", "application/x-www-form-urlencoded", headers, "GET");
                if (accounts.cpanelresult == null)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unexpected error on get cPannel user list", "");
                    return;
                }

                if (accounts.cpanelresult.error != null)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on get cPannel users list: " + accounts.cpanelresult.error, "");
                    return;
                }

                if (accounts.cpanelresult.data.Count == 0)
                {
                    Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found", "");
                    return;
                }

                //Usuário antigo, somente atualiza
                //cPannelResultData userData = accounts.cpanelresult.data[0];

                throw new NotImplementedException();

                NotityDeletedUser(this, package.entityId, package.identityId);

                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User deleted", "");

            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message, "");
            }

        }



        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }
}
