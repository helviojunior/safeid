using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.Reflection;
using IAM.PluginInterface;
using System.Web;
using SafeTrend.Json;

namespace JiraAPIv2
{
    public class JIRAPlugin : PluginConnectorBase
    {

        private String loginToken;
        private WebServiceInvoker invoker;
        private Uri jiraUri;
        private List<String> defaultGroups;

        public override String GetPluginName() { return "IAM JIRA v2 Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com aplicativo JIRA através da API v2"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/jiraapiv2");
        }

        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("URL do servidor", "server_uri", "URL do servidor", PluginConfigTypes.Uri, true, @"http://localhost/"));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha", PluginConfigTypes.Password, true, ""));
            conf.Add(new PluginConfigFields("Grupo padrão", "default_group", "Grupo padrão dos usuários", PluginConfigTypes.String, true, ""));

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

        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            //Não implementado
        }


        public override void ProcessImport(String cacheId, String importId, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            if (!CheckInputConfig(config, true, Log))
                return;

            String lastStep = "";
            try
            {
                GetLoginToken(config);

                setUserGrp(config);

                //List<string> methods = invoker.EnumerateServiceMethods("JiraSoapServiceService");

                //A versão atual do JIRA não permite busca dos usuários sem um grupo/role, desta forma vamos utilizar o grupo padrão para listar os usuários
                //Busca o grupo padrão
                foreach (String g in defaultGroups)
                {
                    WebServiceObjectInterface oGroup = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getGroup", new Object[] { this.loginToken, g });
                    if ((oGroup.BaseObject != null) && (oGroup.GetType().Name == "RemoteGroup"))
                    {
                        Object[] users = oGroup.GetPropertyValue<Object[]>("users");
                        foreach (Object c in users)
                        {

                            PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                            try
                            {
                                package.AddProperty("name", (String)GetPropertyValue(c, "name"), "string");
                                package.AddProperty("email", (String)GetPropertyValue(c, "email"), "string");
                                package.AddProperty("fullname", (String)GetPropertyValue(c, "fullname"), "string");

                                ImportPackageUser(package);
                            }
                            catch { }
                            finally
                            {
                                package.Dispose();
                                package = null;
                            }
                        }
                    }
                }
                
                //WebServiceObjectInterface oIssue = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getUser", new Object[] { this.loginToken, "helvio.junior@fael.edu.br" });

            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, "Erro on import (" + lastStep + "): " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
                Log2(this, PluginLogType.Error, 0, 0, "Erro on import (" + lastStep + "): " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""), "");
                throw ex;
            }

        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {
                GetLoginToken(config);

                setUserGrp(config);

                String login = package.login;
                
                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (dt.dataName.ToLower() == "login")
                        login = dt.dataValue;
                
                if (login == "")
                    login = package.login;

                if (login == "")
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("IAM Login not found in properties list");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Login not found in properties list", "");
                    return;
                }


                String email = "";
                String container = package.container;

                //Busca o e-mail nas propriedades específicas da entidade
                foreach (PluginConnectorBasePackageData dt in package.entiyData)
                    if ((dt.dataName.ToLower() == "email") && (dt.dataValue.ToLower().IndexOf("@") > 1))
                        email = dt.dataValue;

                //Busca o e-mail nas propriedades específicas deste plugin
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.pluginData)
                        if ((dt.dataName.ToLower() == "email") && (dt.dataValue.ToLower().IndexOf("@") > 1))
                            email = dt.dataValue;
                }

                //Se não encontrou o e-mail testa nas propriedades maracas como ID
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.ids)
                        if ((dt.dataName.ToLower() == "email") && (dt.dataValue.ToLower().IndexOf("@") > 1))
                            email = dt.dataValue;
                }

                //Se não encontrou o e-mail testa nas propriedades gerais
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.properties)
                        if ((dt.dataName.ToLower() == "email") && (dt.dataValue.ToLower().IndexOf("@") > 1))
                            email = dt.dataValue;
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

                WebServiceObjectInterface oUser = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getUser", new Object[] { this.loginToken, email });
                if ((oUser == null) || (oUser.BaseObject == null))
                {
                    //User not found, create then

                    if ((package.locked) || (package.temp_locked))
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("User not found in JIRA and user is locked. Accound not created");
                        Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found in JIRA and user is locked. Accound not created", "");
                        return;
                    }

                    if (package.password == "")
                    {
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        processLog.AppendLine("User not found in JIRA and IAM Password not found in properties list, creating a random password (" + package.password + ")");
                    }

                    if ((package.locked) || (package.temp_locked))
                    {
                        //O JIRA não permite o bloqueio da conta, a forma encontrada de bloquea-la é trocando a senha
                        package.password = IAM.Password.RandomPassword.Generate(16);

                        package.fullName.familyName += " (locked)";
                    }

                    oUser = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "createUser", new Object[] { this.loginToken, email, package.password, package.fullName.fullName, email });
                    if ((oUser == null) || (oUser.BaseObject == null))
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Unexpected error on add user on JIRA");
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unexpected error on add user on JIRA", "");
                        return;
                    }

                    //Mantem somente os grupos padrões
                    if (oUser.BaseObject != null)
                    {
                        List<String> groups = GetUserGroups(config, email);

                        foreach (String usrG in groups)
                        {
                            Boolean remove = false;
                            if ((package.locked) || (package.temp_locked))
                                remove = true;
                            else if (!defaultGroups.Exists(g => (g.ToLower() == usrG.ToLower())))
                                remove = true;

                            if (remove) //Remove o grupo do usuário
                            {
                                try
                                {
                                    WebServiceObjectInterface oGrp = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getGroup", new Object[] { this.loginToken, usrG });

                                    if (oGrp.BaseObject == null)
                                    {
                                        processLog.AppendLine("Error on remove user from group '" + usrG + "' group not found");
                                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on remove user from group '" + usrG + "' group not found", "");
                                    }
                                    else
                                    {
                                        WebServiceObjectInterface tst3 = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "removeUserFromGroup", new Object[] { this.loginToken, oGrp.BaseObject, oUser.BaseObject });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    processLog.AppendLine("Error on remove user from group '" + usrG + "' " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
                                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on remove user from group '" + usrG + "' " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""), "");
                                }
                            }
                        }

                    }
                    
                    processLog.AppendLine("User added");
                }
                else
                {
                    //User found, update

                    if ((package.locked) || (package.temp_locked))
                    {
                        //O JIRA não permite o bloqueio da conta, a forma encontrada de bloquea-la é trocando a senha
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        processLog.AppendLine("User locked, password temporarily changed to a random password " + package.password);
                    }

                    /*
                    oUser.SettPropertyValue("email", email);
                    oUser.SettPropertyValue("fullname", package.fullName.fullName + (package.locked ? " (locked)" : ""));
                    oUser.SettPropertyValue("name", email);*/

                    ChangeUser(config, email, package.fullName.fullName + (package.locked || package.temp_locked ? " (locked)" : ""), email);

                    if (!String.IsNullOrWhiteSpace(package.password) && (ChangePassword(config, email, package.password)))
                        processLog.AppendLine("User updated with password");
                    else
                        processLog.AppendLine("User updated without password");

                }

                //Verifica e redefine os grupos
                if (oUser.BaseObject != null)
                {

                    List<String> groups = GetUserGroups(config, email);

                    //Verifica os grupos padrões
                    foreach (String dfG in defaultGroups)
                    {
                        if ((package.locked) || (package.temp_locked))
                        {
                            foreach (String usrG in groups)
                            {
                                try
                                {
                                    WebServiceObjectInterface oGrp = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getGroup", new Object[] { this.loginToken, usrG });
                                    WebServiceObjectInterface tst3 = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "removeUserFromGroup", new Object[] { this.loginToken, oGrp.BaseObject, oUser.BaseObject });
                                }
                                catch (Exception ex)
                                {
                                    processLog.AppendLine("Error on remove user from group '" + usrG + "' " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
                                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on remove user from group '" + usrG + "' " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""), "");
                                }
                            }
                        }
                        else if (!groups.Exists(g => (g.ToLower() == dfG.ToLower())))
                        {
                            //Adiciona o grupo padrão
                            try
                            {
                                WebServiceObjectInterface oGrp = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getGroup", new Object[] { this.loginToken, dfG });
                                WebServiceObjectInterface tst3 = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "addUserToGroup", new Object[] { this.loginToken, oGrp.BaseObject, oUser.BaseObject });
                            }
                            catch (Exception ex)
                            {
                                processLog.AppendLine("Error on add user to group '" + dfG + "': " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
                                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on add user to group '" + dfG + "': " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""), "");
                            }
                        }
                    }

                    NotityChangeUser(this, package.entityId);

                    //Executa as ações do RBAC
                    if ((package.pluginAction != null) && (package.pluginAction.Count > 0))
                    {
                        foreach (PluginConnectorBaseDeployPackageAction act in package.pluginAction)
                            try
                            {

                                processLog.AppendLine("Role: " + act.roleName + " (" + act.actionType.ToString() + ") "+ act.ToString());

                                switch (act.actionKey.ToLower())
                                {
                                    case "group":
                                        if ((act.actionType == PluginActionType.Add) && (!groups.Exists(g => (g == act.actionValue))))
                                        {
                                            WebServiceObjectInterface oGrp = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getGroup", new Object[] { this.loginToken, act.actionValue });
                                            if (oGrp.BaseObject != null)
                                            {
                                                try
                                                {
                                                    WebServiceObjectInterface tst3 = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "addUserToGroup", new Object[] { this.loginToken, oGrp.BaseObject, oUser.BaseObject });

                                                    processLog.AppendLine("User added in group " + act.actionValue + " by role " + act.roleName);
                                                }
                                                catch { }
                                            }
                                            else
                                            {
                                                processLog.AppendLine("Erro adding in group " + act.actionValue + " by role " + act.roleName + ": Group nor found");
                                                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "Erro adding in group " + act.actionValue + " by role " + act.roleName + ": Group nor found", "");
                                            }
                                        }
                                        else if ((act.actionType == PluginActionType.Remove) && (groups.Exists(g => (g == act.actionValue))))
                                        {
                                            WebServiceObjectInterface oGrp = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getGroup", new Object[] { this.loginToken, act.actionValue });
                                            if (oGrp.BaseObject != null)
                                            {
                                                try
                                                {
                                                    WebServiceObjectInterface tst3 = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "addUserToGroup", new Object[] { this.loginToken, oGrp.BaseObject, oUser.BaseObject });

                                                    processLog.AppendLine("User removed in group " + act.actionValue + " by role " + act.roleName);
                                                }
                                                catch { }
                                            }
                                            else
                                            {
                                                processLog.AppendLine("Erro removing in group " + act.actionValue + " by role " + act.roleName + ": Group nor found");
                                                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "Erro removing in group " + act.actionValue + " by role " + act.roleName + ": Group nor found", "");
                                            }

                                        }
                                        break;

                                    default:
                                        processLog.AppendLine("Action not recognized: " + act.actionKey);
                                        Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "Action not recognized: " + act.actionKey, "");
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

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process deploy: " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""), "");
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

            /*
             * O JIRA permite a axclusão da conta, porém por questões de auditoria dos chamados a conta será somente desabilitada
             */

            try
            {
                GetLoginToken(config);

                setUserGrp(config);

                String login = package.login;
                String email = "";
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

                WebServiceObjectInterface oUser = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getUser", new Object[] { this.loginToken, email });
                if ((oUser == null) || (oUser.BaseObject == null))
                {
                    Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found", "");
                    return;
                }

                //Remove de todos os grupos
                if (oUser.BaseObject != null)
                {

                    List<String> groups = GetUserGroups(config, email);

                    foreach (String usrG in groups)
                    {
                        try
                        {
                            WebServiceObjectInterface oGrp = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "getGroup", new Object[] { this.loginToken, usrG });
                            WebServiceObjectInterface tst3 = new WebServiceObjectInterface(invoker, "JiraSoapServiceService", "removeUserFromGroup", new Object[] { this.loginToken, oGrp.BaseObject, oUser.BaseObject });
                        }
                        catch (Exception ex)
                        {
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on remove user from group '" + usrG + "' " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""), "");
                        }
                    }

                }

                //O JIRA não permite o bloqueio da conta, a forma encontrada de bloquea-la é trocando a senha
                package.password = IAM.Password.RandomPassword.Generate(16);

                ChangePassword(config, email, package.password);

                ChangeUser(config, email, package.fullName.fullName + (package.locked || package.temp_locked ? " (deleted)" : ""), email);

                NotityDeletedUser(this, package.entityId, package.identityId);

                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User deleted", "");


            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""), "");
            }

        }

        private Boolean ChangeUser(Dictionary<String, Object> config, String name, String fullName, String email)
        {
            CookieContainer cookie = new CookieContainer();
            String atl_token = AuthAndToken(config, ref cookie);

            if (atl_token == "")
                return false;

            Uri pwdUri = new Uri(this.jiraUri.Scheme + "://" + this.jiraUri.Host + ":" + this.jiraUri.Port + "/secure/admin/user/EditUser.jspa");

            HttpWebRequest requestPwd = (HttpWebRequest)WebRequest.Create(pwdUri);
            requestPwd.UserAgent = "Mozilla/5.0 (compatible; SafeID/1.0; +http://www.safeid.com.br)";
            requestPwd.CookieContainer = cookie;
            requestPwd.Method = "POST";
            requestPwd.ContentType = "application/x-www-form-urlencoded";
            requestPwd.AllowAutoRedirect = false;

            Byte[] byteArray = Encoding.UTF8.GetBytes("atl_token=" + atl_token + "&editName=" + HttpUtility.UrlEncode(name) + "&fullName=" + HttpUtility.UrlEncode(fullName) + "&email=" + HttpUtility.UrlEncode(email) + "&name=" + HttpUtility.UrlEncode(name) + "&Atualizar=Atualizar");
            requestPwd.ContentLength = byteArray.Length;
            using (Stream dataStream = requestPwd.GetRequestStream())
                dataStream.Write(byteArray, 0, byteArray.Length);

            using (HttpWebResponse responsePwd = (HttpWebResponse)requestPwd.GetResponse())
            {

                if (responsePwd.StatusCode == HttpStatusCode.Found)
                {
                    //http://xxx/secure/admin/user/ViewUser.jspa?name=yyy
                    String location = responsePwd.Headers["Location"];
                    if (location.ToLower().IndexOf("viewuser.jspa") != -1)
                        return true;
                }
            }


            return false;
        }

        private List<String> GetUserGroups(Dictionary<String, Object> config, String name)
        {
            /* Como a API não implementa um modo de troca de senha
             * Foi implementado como se os comandos estivessem sendo feitos através da console web
             * 1 - Efetua o login
             * 2 - Resgata o token
             * 3 - Troca a senha
             */

            List<String> groups = new List<string>();

            CookieContainer cookie = new CookieContainer();
            String atl_token = AuthAndToken(config, ref cookie);

            if (atl_token == "")
                return groups;

            Uri usersUri = new Uri(this.jiraUri.Scheme + "://" + this.jiraUri.Host + ":" + this.jiraUri.Port + "/secure/admin/user/UserBrowser.jspa");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(usersUri);
            request.UserAgent = "Mozilla/5.0 (compatible; SafeID/1.0; +http://www.safeid.com.br)";
            request.CookieContainer = cookie;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.AllowAutoRedirect = false;

            Byte[] byteArray = Encoding.UTF8.GetBytes("atl_token=" + atl_token + "&max=20&userNameFilter=" + HttpUtility.UrlEncode(name) + "&fullNameFilter=&emailFilter=&group=");
            request.ContentLength = byteArray.Length;
            using (Stream dataStream = request.GetRequestStream())
                dataStream.Write(byteArray, 0, byteArray.Length);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {

                    Encoding enc = Encoding.UTF8;
                    try
                    {
                        enc = Encoding.GetEncoding(response.ContentEncoding);
                    }
                    catch { }

                    String htmlData = "";

                    Stream dataStream = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(dataStream, enc))
                        htmlData = reader.ReadToEnd();

                    Regex r = new Regex(@"<tr[\s\S]*?</tr>", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                    foreach (Match m in r.Matches(htmlData))
                        if (m.Groups[0].Value.ToLower().IndexOf(name.ToLower()) != -1)
                        {
                            String tr = m.Groups[0].Value.Replace("\r", "").Replace("\n", "");

                            //<a href="ViewGroup.jspa?atl_token=PEjz7FsXY0&amp;name=jira-eadcon-users">jira-eadcon-users</a>

                            MatchCollection ms = Regex.Matches(tr, @"ViewGroup.jspa[\s\S]*?["">]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                            foreach (Match m1 in ms)
                            {
                                Match m2 = Regex.Match(m1.Value, @"name=(.*?)["">]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                                if ((m2.Success) && (!String.IsNullOrWhiteSpace(m2.Groups[1].Value)))
                                    groups.Add(m2.Groups[1].Value);
                            }
                        }
                }
            }

            return groups;
        }


        private Boolean ChangePassword(Dictionary<String, Object> config, String name, String password)
        {
            /* Como a API não implementa um modo de troca de senha
             * Foi implementado como se os comandos estivessem sendo feitos através da console web
             * 1 - Efetua o login
             * 2 - Resgata o token
             * 3 - Troca a senha
             */

            CookieContainer cookie = new CookieContainer();
            String atl_token = AuthAndToken(config, ref cookie);

            if (atl_token == "")
                return false;

            Uri pwdUri = new Uri(this.jiraUri.Scheme + "://" + this.jiraUri.Host + ":" + this.jiraUri.Port + "/secure/admin/user/SetPassword.jspa");

            HttpWebRequest requestPwd = (HttpWebRequest)WebRequest.Create(pwdUri);
            requestPwd.UserAgent = "Mozilla/5.0 (compatible; SafeID/1.0; +http://www.safeid.com.br)";
            requestPwd.CookieContainer = cookie;
            requestPwd.Method = "POST";
            requestPwd.ContentType = "application/x-www-form-urlencoded";
            requestPwd.AllowAutoRedirect = false;

            Byte[] byteArray = Encoding.UTF8.GetBytes("atl_token=" + atl_token + "&password=" + HttpUtility.UrlEncode(password) + "&confirm=" + HttpUtility.UrlEncode(password) + "&name=" + HttpUtility.UrlEncode(name) + "&Atualizar=Atualizar");
            requestPwd.ContentLength = byteArray.Length;
            using (Stream dataStream = requestPwd.GetRequestStream())
                dataStream.Write(byteArray, 0, byteArray.Length);

            using (HttpWebResponse responsePwd = (HttpWebResponse)requestPwd.GetResponse())
            {

                if (responsePwd.StatusCode == HttpStatusCode.Found)
                {
                    //http://xxx/secure/admin/user/SetPassword!success.jspa?name=yyy
                    String location = responsePwd.Headers["Location"];
                    if (location.ToLower().IndexOf("setpassword!success.jspa") != -1)
                        return true;
                }
            }


            return false;
        }


        private String AuthAndToken(Dictionary<String, Object> config, ref CookieContainer cookie)
        {
            String atl_token = "";

            Uri loginUri = new Uri(this.jiraUri.Scheme + "://" + this.jiraUri.Host + ":" + this.jiraUri.Port + "/rest/gadget/1.0/login");

            String postData = "os_username=" + HttpUtility.UrlEncode(config["username"].ToString()) + "&os_password=" + HttpUtility.UrlEncode(config["password"].ToString());

            LoginData login = JSON.JsonWebRequest<LoginData>(loginUri, postData, "application/x-www-form-urlencoded", null, "POST", cookie);
            if ((login != null) && (login.loginSucceeded))
            {

                //Resgata o token de autenticação
                Uri tokenUri = new Uri(this.jiraUri.Scheme + "://" + this.jiraUri.Host + ":" + this.jiraUri.Port + "/secure/admin/user/UserBrowser.jspa");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tokenUri);
                request.UserAgent = "Mozilla/5.0 (compatible; SafeID/1.0; +http://www.safeid.com.br)";
                request.CookieContainer = cookie;
                request.Method = "GET";
                request.AllowAutoRedirect = false;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {

                        Encoding enc = Encoding.UTF8;
                        try
                        {
                            enc = Encoding.GetEncoding(response.ContentEncoding);
                        }
                        catch { }

                        String htmlData = "";

                        Stream dataStream = response.GetResponseStream();
                        using (StreamReader reader = new StreamReader(dataStream, enc))
                            htmlData = reader.ReadToEnd();

                        Regex r = new Regex("atl_token=(.*?)&", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                        foreach (Match m in r.Matches(htmlData))
                            if (!String.IsNullOrWhiteSpace(m.Groups[1].Value))
                            {
                                atl_token = m.Groups[1].Value;
                                break;
                            }
                    }
                }

            }

            return atl_token;
        }

        private void GetLoginToken(Dictionary<String, Object> config)
        {
            if (invoker == null)
            {
                Uri serverUri = new Uri(config["server_uri"].ToString());
                //Calcula a URI do jira
                this.jiraUri = new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/rpc/soap/jirasoapservice-v2");

                invoker = new WebServiceInvoker(jiraUri);
            }

            this.loginToken = invoker.InvokeMethod<string>("JiraSoapServiceService", "login", new String[] { config["username"].ToString(), config["password"].ToString() });
        }



        private object GetPropertyValue(object srcobj, string propertyName)
        {
            if (srcobj == null)
                return null;

            object obj = srcobj;

            // Split property name to parts (propertyName could be hierarchical, like obj.subobj.subobj.property
            string[] propertyNameParts = propertyName.Split('.');

            foreach (string propertyNamePart in propertyNameParts)
            {
                if (obj == null) return null;

                // propertyNamePart could contain reference to specific 
                // element (by index) inside a collection
                if (!propertyNamePart.Contains("["))
                {
                    PropertyInfo pi = obj.GetType().GetProperty(propertyNamePart);
                    if (pi == null) return null;
                    obj = pi.GetValue(obj, null);
                }
                else
                {   // propertyNamePart is areference to specific element 
                    // (by index) inside a collection
                    // like AggregatedCollection[123]
                    //   get collection name and element index
                    int indexStart = propertyNamePart.IndexOf("[") + 1;
                    string collectionPropertyName = propertyNamePart.Substring(0, indexStart - 1);
                    int collectionElementIndex = Int32.Parse(propertyNamePart.Substring(indexStart, propertyNamePart.Length - indexStart - 1));
                    //   get collection object
                    PropertyInfo pi = obj.GetType().GetProperty(collectionPropertyName);
                    if (pi == null) return null;
                    object unknownCollection = pi.GetValue(obj, null);
                    //   try to process the collection as array
                    if (unknownCollection.GetType().IsArray)
                    {
                        object[] collectionAsArray = unknownCollection as Array[];
                        obj = collectionAsArray[collectionElementIndex];
                    }
                    else
                    {
                        //   try to process the collection as IList
                        System.Collections.IList collectionAsList = unknownCollection as System.Collections.IList;
                        if (collectionAsList != null)
                        {
                            obj = collectionAsList[collectionElementIndex];
                        }
                        else
                        {
                            // ??? Unsupported collection type
                        }
                    }
                }
            }

            return obj;
        }


        private void setUserGrp(Dictionary<String, Object> config)
        {

            defaultGroups = new List<string>();
            if (config["default_group"] is String)
            {
                String[] dfGp = config["default_group"].ToString().Split(",;".ToCharArray());
                foreach (String s in dfGp)
                    if ((!String.IsNullOrWhiteSpace(s)) && (!defaultGroups.Contains(s)))
                        defaultGroups.Add(s.Trim());
            }
            else if (config["default_group"] is String[])
            {
                foreach (String str in (String[])config["default_group"])
                {
                    String[] dfGp = str.Split(",;".ToCharArray());
                    foreach (String s in dfGp)
                        if ((!String.IsNullOrWhiteSpace(s)) && (!defaultGroups.Contains(s)))
                            defaultGroups.Add(s.Trim());
                }
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
