using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.DirectoryServices;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Management;
using System.Text.RegularExpressions;
using IAM.PluginInterface;
using System.Security.Principal;
using SafeTrend.Json;

namespace Windows
{


    public class WindowsPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "IAM for Microsoft Windows Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com base de dados de usuários do Microsoft Windows"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/windows");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Host", "server", "IP ou nome do windows para conexão", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário para conexão", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha para conexão", PluginConfigTypes.Password, true, ","));

            return conf.ToArray();
        }


        public override PluginConnectorConfigActions[] GetConfigActions()
        {

            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();
            conf.Add(new PluginConnectorConfigActions("Adição/remoção em grupo", "group", "Adicionar/remover o usuário em um grupo", "Nome do grupo", "group_name", "Nome do grupo que o usuário será adicionado/removido"));

            return conf.ToArray();
        }

        //

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
                WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                iLog(this, PluginLogType.Information, "Current user: " + System.Security.Principal.WindowsIdentity.GetCurrent().Name);
                iLog(this, PluginLogType.Information, "Has administrative right: " + pricipal.IsInRole(WindowsBuiltInRole.Administrator));
            }
            catch { }

            try
            {

                LocalWindows lWin = new LocalWindows(config["server"].ToString(), config["username"].ToString(), config["password"].ToString());

                try
                {
                    lWin.Bind();
                }
                catch (Exception ex)
                {
                    iLog(this, PluginLogType.Error, "Error on connect to Windows '" + config["server"].ToString() + "': " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
                    lWin = null;
                    ret.success = false;
                    return ret;
                }

                Log(this, PluginLogType.Information, "Successfully connected on " + config["server"].ToString());

                Log(this, PluginLogType.Information, "Trying to list the users...");

                Int32 count = 0;
                try
                {
                    foreach (DirectoryEntry user in lWin.ListAllUsers())
                    {
                        if (count >= 20)
                            break;

                        try
                        {

                            foreach (PropertyValueCollection property in user.Properties)
                            {
                                if (!ret.fields.ContainsKey(property.PropertyName))
                                    ret.fields.Add(property.PropertyName, new List<string>());

                                //Separa os itens que mecessita algum tratamento
                                switch (property.PropertyName.ToLower())
                                {

                                    default:
                                        foreach (Object p1 in property)
                                            ret.fields[property.PropertyName].Add(p1.ToString());
                                        break;
                                }
                            }


                            count++;

                        }
                        catch (Exception ex)
                        {
                            Log(this, PluginLogType.Error, "Erro ao importar o registro (" + user.Path + "): " + ex.Message);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Log(this, PluginLogType.Error, "Erro listar os usuários");
                    throw ex;
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

            List<String> prop = new List<String>();

            try
            {

                LocalWindows lWin = new LocalWindows(config["server"].ToString(), config["username"].ToString(), config["password"].ToString());

                try
                {
                    lWin.Bind();
                }
                catch (Exception ex)
                {
                    Log2(this, PluginLogType.Error, 0, 0, "Error on connect to Windows '" + config["server"].ToString() + "': " + ex.Message + (ex.InnerException != null ? ex.InnerException.Message : ""), "");
                    lWin = null;
                    return;
                }

                Log(this, PluginLogType.Information, "Successfully connected on " + config["server"].ToString());

                Log(this, PluginLogType.Information, "Trying to list the users...");
                foreach (DirectoryEntry user in lWin.ListAllUsers())
                {
                    PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                    try
                    {

                        object obGroups = user.Invoke("Groups");
                        foreach (object ob in (IEnumerable)obGroups)
                        {
                            // Create object for each group. 
                            DirectoryEntry obGpEntry = new DirectoryEntry(ob);
                            package.AddProperty("memberOf", obGpEntry.Name, (fieldMapping.Exists(f => (f.dataName == "memberOf")) ? fieldMapping.Find(f => (f.dataName == "memberOf")).dataType : "string"));

                            //Registry(importId, regId, "memberOf", obGpEntry.Name, (fieldMapping.Exists(f => (f.dataName == "memberOf")) ? fieldMapping.Find(f => (f.dataName == "memberOf")).dataType : "string"));
                        }

                        foreach (String p in user.Properties.PropertyNames)
                        {
                            //Separa os itens que mecessita algum tratamento
                            switch (p.ToLower())
                            {
                                case "lastlogin":
                                    try
                                    {
                                        foreach (Object p1 in user.Properties[p])
                                        {
                                            DateTime tmp2 = DateTime.Parse(p1.ToString());

                                            if (tmp2.Year > 1970)//Se a data for inferior nem envia
                                                package.AddProperty(p, tmp2.ToString("yyyy-MM-dd HH:mm:ss"), (fieldMapping.Exists(f => (f.dataName == p)) ? fieldMapping.Find(f => (f.dataName == p)).dataType : "string"));
                                        }
                                    }
                                    catch (Exception ex)
                                    { }
                                    break;

                                case "loginhours":
                                    break;

                                case "objectsid":
                                    try
                                    {
                                        Byte[] tmp2 = (Byte[])user.Properties[p][0];
                                        package.AddProperty(p, BitConverter.ToString(tmp2).Replace("-", ""), (fieldMapping.Exists(f => (f.dataName == p)) ? fieldMapping.Find(f => (f.dataName == p)).dataType : "string"));
                                    }
                                    catch (Exception ex)
                                    { }
                                    break;

                                default:
                                    foreach (Object p1 in user.Properties[p])
                                        package.AddProperty(p, p1.ToString(), (fieldMapping.Exists(f => (f.dataName == p)) ? fieldMapping.Find(f => (f.dataName == p)).dataType : "string"));
                                    break;
                            }
                        }

                        ImportPackageUser(package);
                    }
                    catch (Exception ex)
                    {
                        Log(this, PluginLogType.Error, "Erro ao importar o registro (" + user.Path + "): " + ex.Message);
                    }
                    finally
                    {
                        package.Dispose();
                        package = null;
                    }

                }

            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, ex.Message);
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

            StringBuilder logText = new StringBuilder();
            try
            {
                WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                logText.AppendLine("Current user: " + System.Security.Principal.WindowsIdentity.GetCurrent().Name);
                logText.AppendLine("Has administrative right: " + pricipal.IsInRole(WindowsBuiltInRole.Administrator));
            }
            catch { }
            
            try
            {

                if (package.login.Length > 20)
                    throw new Exception("Maximum size of login name reached, this method support up to 20 characters.");

                LocalWindows lWin = new LocalWindows(config["server"].ToString(), config["username"].ToString(), config["password"].ToString());

                try
                {
                    lWin.Bind();
                }
                catch (Exception ex)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on connect to Windows '" + config["server"].ToString() + "': " + ex.Message + (ex.InnerException != null ? ex.InnerException.Message : ""), "");
                    lWin = null;
                    return;
                }


                logText.AppendLine("Successfully connected on " + config["server"].ToString());

                
                String login = package.login;

                if (login == "")
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Login not found in properties list", "");
                    return;
                }

                logText.AppendLine("Trying to find user '" + package.login + "'...");
                DirectoryEntry user = lWin.FindUser(package.login);

                if (user == null)
                {
                    logText.AppendLine("User not found, creating...");

                    //Usuário não encontrado, cria
                    if (package.password == "")
                    {
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found in AD and IAM Password not found in properties list, creating a random password (" + package.password + ")", "");
                    }

                    //Primeira senha define uma randômica de 20 caracteres para passar o sistema de complexidade e não apresentar erro
                    //nos próximos passos será tentato trocar a senha
                    lWin.AddUser(package.login, IAM.Password.RandomPassword.Generate(20));
                    user = lWin.FindUser(package.login);

                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User added", "");
                }
                else
                {
                    logText.AppendLine("User founded");
                }
                
                logText.AppendLine("User path: " + user.Path);

                try
                {
                    UserFlags ctrl = (UserFlags)user.InvokeGet("userFlags");

                    //Limpa as flags que serão verificadas por este sistema
                    if ((ctrl & UserFlags.ACCOUNTDISABLE) == UserFlags.ACCOUNTDISABLE)
                        ctrl -= UserFlags.ACCOUNTDISABLE;

                    if ((package.locked) || (package.temp_locked))
                        ctrl = (UserFlags)((Int32)ctrl + UserFlags.ACCOUNTDISABLE);

                    logText.AppendLine("Setting user flags...");
                    user.Invoke("Put", new object[] { "UserFlags", (Int32)ctrl });
                    user.CommitChanges();

                }
                catch (Exception ex)
                {
                    logText.AppendLine("Error applying user flags: " + ex.Message);
                    user = lWin.FindUser(package.login);
                }

                try
                {
                    logText.AppendLine("Setting user password...");
                    if (!String.IsNullOrWhiteSpace(package.password))
                        user.Invoke("SetPassword", new Object[] { package.password });

                    user.CommitChanges();
                }
                catch (Exception ex)
                {
                    String sPs = "";
                    try
                    {
                        PasswordStrength ps = CheckPasswordStrength(package.password, package.fullName.fullName);

                        sPs += "Length = " + package.password.Length + Environment.NewLine;
                        sPs += "Contains Uppercase? " + ps.HasUpperCase + Environment.NewLine;
                        sPs += "Contains Lowercase? " + ps.HasLowerCase + Environment.NewLine;
                        sPs += "Contains Symbol? " + ps.HasSymbol + Environment.NewLine;
                        sPs += "Contains Number? " + ps.HasDigit + Environment.NewLine;
                        sPs += "Contains part of the name/username? " + ps.HasNamePart + Environment.NewLine;

                    }
                    catch { }

                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on set user password, check the password complexity rules", ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : "") + Environment.NewLine + sPs);
                    return;
                }



                try
                {
                    logText.AppendLine("Setting user access...");
                    //Executa as ações do RBAC
                    if ((package.pluginAction != null) && (package.pluginAction.Count > 0))
                    {
                        foreach (PluginConnectorBaseDeployPackageAction act in package.pluginAction)
                            try
                            {
                                switch (act.actionKey.ToLower())
                                {
                                    case "group":
                                        if (act.actionType == PluginActionType.Add)
                                        {
                                            String grpCN = lWin.FindOrCreateGroup(act.actionValue);
                                            if (lWin.AddUserToGroup(user.Name, grpCN))
                                                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User added in group " + act.actionValue + " by role " + act.roleName, "");
                                        }
                                        else if (act.actionType == PluginActionType.Remove)
                                        {
                                            String grpCN = lWin.FindOrCreateGroup(act.actionValue);
                                            if (lWin.RemoveUserFromGroup(user.Name, grpCN))
                                                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User removed from group " + act.actionValue + " by role " + act.roleName, "");
                                        }
                                        break;

                                    default:
                                        Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "Action not recognized: " + act.actionKey, "");
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on execute action (" + act.actionKey + "): " + ex.Message, "");
                            }
                    }
                }
                finally
                {
                    user.Close();
                }


                NotityChangeUser(this, package.entityId);

                if (package.password != "")
                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User updated with password", "");
                else
                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User updated without password", "");


            }
            catch (Exception ex)
            {

                logText.AppendLine("Error: " + ex.Message);
                if (ex.InnerException != null)
                    logText.AppendLine(ex.InnerException.Message);
                logText.AppendLine("");
                logText.AppendLine("");
                logText.AppendLine("");

                logText.AppendLine("### Package details");
                String debugInfo = JSON.Serialize2(new { package = package, fieldMapping = fieldMapping });
                if (package.password != "")
                    debugInfo = debugInfo.Replace(package.password, "Replaced for user security");

                logText.AppendLine(debugInfo);

                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message, logText.ToString());
                logText = null;
            }
        }


        public override void ProcessDelete(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            if (!CheckInputConfig(config, true, Log))
                return;

            try
            {

                LocalWindows lWin = new LocalWindows(config["server"].ToString(), config["username"].ToString(), config["password"].ToString());

                try
                {
                    lWin.Bind();
                }
                catch (Exception ex)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on connect to Windows '" + config["server"].ToString() + "': " + ex.Message, "");
                    lWin = null;
                    return;
                }


                String login = package.login;
                String container = package.container;

                if (login == "")
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Login not found in properties list", "");
                    return;
                }

                if (container == "")
                    container = "IAMUsers";

                DirectoryEntry user = lWin.FindUser(package.login);

                if (user == null)
                {
                    Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found", "");
                    return;
                }

                user.Parent.Children.Remove(user);

                NotityDeletedUser(this, package.entityId, package.identityId);

                if (package.password != "")
                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User updated with password", "");
                else
                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User updated without password", "");


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
