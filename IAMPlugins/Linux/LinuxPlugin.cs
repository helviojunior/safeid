using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IAM.PluginInterface;
using System.Security.Cryptography;
using Renci.SshNet;

namespace Linux
{
    public class LinuxPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "Linux SSH Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com servidores linux através de SSH"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/linux");
        }

        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Servidor", "server", "IP ou nome do servidor para conexão", PluginConfigTypes.String, true, @""));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário para conexão", PluginConfigTypes.String, true, @""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha para conexão", PluginConfigTypes.Password, true, @""));
            conf.Add(new PluginConfigFields("Utilizar prefixo sudo", "use_prefix", "Habilita/desabilita a utilização do prefixo sudo para todos os comandos", PluginConfigTypes.Boolean, false, @""));
            conf.Add(new PluginConfigFields("Pré comando", "pre_cmd", "Comando a ser executado logo após a conexão, antes de todos os outros", PluginConfigTypes.String, false, @""));
            
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
                    iLog(this, PluginLogType.Error, "Field " + cf.Name + " (" + cf.Key + "): error on get data -> " + ex.Message);
                }
            }


            String server = config["server"].ToString();
            String username = config["username"].ToString();
            String password = config["password"].ToString();


            try
            {

                PasswordConnectionInfo connectionInfo = new PasswordConnectionInfo(server, username, password);
                connectionInfo.Timeout = new TimeSpan(0, 1, 0);

                using (SshClient client = new SshClient(connectionInfo))
                {
                    try
                    {
                        client.Connect();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erro on connect SSH", ex);
                    }



                    List<UserData> users = GetList(client, config);

                    ret.fields.Add("username", new List<string>());
                    ret.fields.Add("user_id", new List<string>());
                    ret.fields.Add("default_group", new List<string>());
                    ret.fields.Add("information", new List<string>());
                    ret.fields.Add("home_path", new List<string>());
                    ret.fields.Add("bash", new List<string>());


                    foreach (UserData u in users)
                    {

                        ret.fields["username"].Add(u.Username);

                        ret.fields["user_id"].Add(u.UserId);

                        ret.fields["default_group"].Add(u.DefaultGroup);

                        ret.fields["information"].Add(u.Information);

                        ret.fields["home_path"].Add(u.HomePath);

                        ret.fields["bash"].Add(u.Bash);

                    }
                    
                    client.Disconnect();
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

            LogEvent iLog = new LogEvent(delegate(Object sender, PluginLogType type, string text)
            {
                if (Log != null)
                    Log(sender, type, text);
            });

            if (!CheckInputConfig(config, true, Log))
                return;



            String server = config["server"].ToString();
            String username = config["username"].ToString();
            String password = config["password"].ToString();


            try
            {

                PasswordConnectionInfo connectionInfo = new PasswordConnectionInfo(server, username, password);
                connectionInfo.Timeout = new TimeSpan(0, 1, 0);

                using (SshClient client = new SshClient(connectionInfo))
                {
                    try
                    {
                        client.Connect();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erro on connect SSH", ex);
                    }


                    List<UserData> users = GetList(client, config);

                    foreach (UserData u in users)
                    {

                        PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);


                        String property = "";

                        property = "username";
                        package.AddProperty(property, u.Username, (fieldMapping.Exists(f => (f.dataName == property)) ? fieldMapping.Find(f => (f.dataName == property)).dataType : "string"));

                        property = "user_id";
                        package.AddProperty(property, u.UserId, (fieldMapping.Exists(f => (f.dataName == property)) ? fieldMapping.Find(f => (f.dataName == property)).dataType : "string"));

                        property = "default_group";
                        package.AddProperty(property, u.DefaultGroup, (fieldMapping.Exists(f => (f.dataName == property)) ? fieldMapping.Find(f => (f.dataName == property)).dataType : "string"));

                        property = "information";
                        package.AddProperty(property, u.Information, (fieldMapping.Exists(f => (f.dataName == property)) ? fieldMapping.Find(f => (f.dataName == property)).dataType : "string"));

                        property = "home_path";
                        package.AddProperty(property, u.HomePath, (fieldMapping.Exists(f => (f.dataName == property)) ? fieldMapping.Find(f => (f.dataName == property)).dataType : "string"));

                        property = "bash";
                        package.AddProperty(property, u.Bash, (fieldMapping.Exists(f => (f.dataName == property)) ? fieldMapping.Find(f => (f.dataName == property)).dataType : "string"));


                        ImportPackageUser(package);
                    }

                    client.Disconnect();
                }

            }
            catch (Exception ex)
            {
                iLog(this, PluginLogType.Error, ex.Message);
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



            String server = config["server"].ToString();
            String username = config["username"].ToString();
            String password = config["password"].ToString();

            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {

                PasswordConnectionInfo connectionInfo = new PasswordConnectionInfo(server, username, password);
                connectionInfo.Timeout = new TimeSpan(0, 1, 0);

                using (SshClient client = new SshClient(connectionInfo))
                {
                    try
                    {
                        client.Connect();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erro on connect SSH", ex);
                    }


                    String prefix = "echo '" + config["password"].ToString() + "' | sudo ";

                    if (config.ContainsKey("use_prefix"))
                    {
                        try
                        {
                            Boolean up = Boolean.Parse(config["use_prefix"].ToString());
                            if (!up)
                                prefix = "";
                        }
                        catch { }
                    }


                    List<UserData> users = GetList(client, config, package.login);

                    UserData selectedUser = null;
                    foreach (UserData u in users)
                        if (u.Username.ToLower() == package.login.ToLower())
                            selectedUser = u;

                    if (selectedUser != null)
                    {
                        //Usuário existente

                    }
                    else
                    {
                        //Não existe, cria

                        //useradd -G {group-name} username

                        //Cria grupo genérico para o IM

                        SshCommand grpAdd = client.RunCommand("groupadd IAMUsers ");
                        if (grpAdd.ExitStatus != 0)
                        {

                            if (grpAdd.Error.ToLower().IndexOf("already exists") == -1)
                            {
                                logType = PluginLogType.Error;
                                processLog.AppendLine("Error creating IAMUsers group: " + grpAdd.Error.Trim("\r\n".ToCharArray()));
                                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error creating IAMUsers group", grpAdd.Error.Trim("\r\n".ToCharArray()));
                                return;
                            }
                        }

                        SshCommand cmdAdd = client.RunCommand("useradd -G IAMUsers " + package.login);
                        if (cmdAdd.ExitStatus != 0)
                        {
                            logType = PluginLogType.Error;
                            processLog.AppendLine("Error creating users: " + cmdAdd.Error.Trim("\r\n".ToCharArray()));
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error creating users", cmdAdd.Error.Trim("\r\n".ToCharArray()));
                            return;
                        }

                        processLog.AppendLine("User added");
                    }

                    if (package.password != "")
                    {
                        String md5Pwd = "";
                        using (MD5 hAlg = MD5.Create())
                            md5Pwd = ComputeHash(hAlg, package.password);

                        SshCommand cmdChangePwd = client.RunCommand("echo '" + package.login + ":" + package.password + "' | chpasswd");
                        
                        if (cmdChangePwd.ExitStatus != 0)
                        {
                            logType = PluginLogType.Error;
                            processLog.AppendLine("Error on set user password, check the password complexity rules");
                            processLog.AppendLine(cmdChangePwd.Error.Trim("\r\n".ToCharArray()));

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

                                processLog.AppendLine(sPs);
                            }
                            catch { }

                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on set user password, check the password complexity rules", cmdChangePwd.Error.Trim("\r\n".ToCharArray()) + Environment.NewLine + sPs);
                            return;
                        }
                    }

                    //Lock and unlock account
                    //usermod -L 
                    //usermod -U

                    processLog.AppendLine("User locked? " + (package.locked || package.temp_locked ? "true" : "false"));

                    SshCommand userLock = client.RunCommand("usermod " + (package.locked || package.temp_locked ? "-L " : "-U ") + package.login);
                    if (userLock.ExitStatus != 0)
                    {

                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error " + (package.locked || package.temp_locked ? "locking" : "unlocking") + " user: " + userLock.Error.Trim("\r\n".ToCharArray()));
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error " + (package.locked || package.temp_locked ? "locking" : "unlocking") + " user", userLock.Error.Trim("\r\n".ToCharArray()));
                        return;
                    }

                    //Executa as ações do RBAC
                    if ((package.pluginAction != null) && (package.pluginAction.Count > 0))
                    {
                        List<GroupData> groups = GetUserGroups(client, config);

                        foreach (PluginConnectorBaseDeployPackageAction act in package.pluginAction)
                            try
                            {
                                processLog.AppendLine("Role: " + act.roleName + " (" + act.actionType.ToString() + ") " + act.ToString());

                                switch (act.actionKey.ToLower())
                                {
                                    case "group":
                                        GroupData findGroup = groups.Find(g => (g.Groupname == act.actionValue));
                                        GroupData findUserInGroup = groups.Find(g => (g.Groupname == act.actionValue && g.Users.Contains(package.login)));

                                        if ((act.actionType == PluginActionType.Add) && (findUserInGroup == null))
                                        {
                                            if (findGroup == null)
                                            {
                                                //Not found, add group

                                                SshCommand grpAdd = client.RunCommand("groupadd " + act.actionValue);
                                                if (grpAdd.ExitStatus != 0)
                                                {

                                                    if (grpAdd.Error.ToLower().IndexOf("already exists") == -1)
                                                    {
                                                        logType = PluginLogType.Error;
                                                        processLog.AppendLine("Error creating " + act.actionValue + " group: " + grpAdd.Error.Trim("\r\n".ToCharArray()));
                                                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error creating " + act.actionValue + " group", grpAdd.Error.Trim("\r\n".ToCharArray()));
                                                        continue;
                                                    }
                                                }

                                            }

                                            SshCommand userGrpAdd = client.RunCommand("usermod -a -G " + act.actionValue + " " + package.login);
                                            if (userGrpAdd.ExitStatus != 0)
                                            {

                                                logType = PluginLogType.Error;
                                                processLog.AppendLine("Error adding user on group " + act.actionValue + ": " + userGrpAdd.Error.Trim("\r\n".ToCharArray()));
                                                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error adding user on group " + act.actionValue, userGrpAdd.Error.Trim("\r\n".ToCharArray()));
                                                continue;
                                            }
                                            else
                                            {
                                                processLog.AppendLine("User added in group " + act.actionValue + " by role " + act.roleName);
                                            }
                                        }
                                        else if ((act.actionType == PluginActionType.Remove) && (findUserInGroup != null))
                                        {

                                            SshCommand userGrpDel = client.RunCommand("gpasswd -d " + package.login + " " + act.actionValue);
                                            if (userGrpDel.ExitStatus != 0)
                                            {

                                                logType = PluginLogType.Error;
                                                processLog.AppendLine("Error removing user on group " + act.actionValue + ": " + userGrpDel.Error.Trim("\r\n".ToCharArray()));
                                                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error removing user on group " + act.actionValue, userGrpDel.Error.Trim("\r\n".ToCharArray()));
                                                continue;
                                            }
                                            else
                                            {
                                                processLog.AppendLine("User removed from group " + act.actionValue + " by role " + act.roleName);
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

                    client.Disconnect();

                    NotityChangeUser(this, package.entityId);

                    if (package.password != "")
                        processLog.AppendLine("User updated with password");
                    else
                        processLog.AppendLine("User updated without password");
                }

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
            //Nda
        }

        private String ComputeHash(HashAlgorithm alg, String text)
        {


            // Convert the input string to a byte array and compute the hash.
            byte[] data = alg.ComputeHash(Encoding.UTF8.GetBytes(text));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();

        }

        private List<UserData> GetList(SshClient client, Dictionary<String, Object> config)
        {
            return GetList(client, config, null);
        }

        private List<UserData> GetList(SshClient client, Dictionary<String, Object> config, String filterUser)
        {
            List<UserData> ret = new List<UserData>();


            LogEvent iLog = new LogEvent(delegate(Object sender, PluginLogType type, string text)
            {
                if (Log != null)
                    Log(sender, type, text);
            });


            if (config.ContainsKey("pre_cmd") && (!String.IsNullOrEmpty(config["pre_cmd"].ToString())))
            {
                String preCmd = config["pre_cmd"].ToString();

                if (preCmd.ToLower().IndexOf("sudo") != -1)
                {

                    iLog(this, PluginLogType.Information, "Executing: " + config["pre_cmd"].ToString());
                    SshCommand command = client.RunCommand(config["pre_cmd"].ToString());

                    iLog(this, PluginLogType.Information, "ExitStatus: " + command.ExitStatus);

                    if (!String.IsNullOrEmpty(command.Error))
                        iLog(this, PluginLogType.Error, command.Error);

                    iLog(this, PluginLogType.Information, command.Result);
                }
                else
                {
                    iLog(this, PluginLogType.Information, "Pré command 'sudo' not permited");
                }
            }

            String prefix = "echo '" + config["password"].ToString() + "' | sudo ";

            if (config.ContainsKey("use_prefix"))
            {
                try
                {
                    Boolean up = Boolean.Parse(config["use_prefix"].ToString());
                    if (!up)
                        prefix = "";
                }
                catch { }
            }

            iLog(this, PluginLogType.Information, "Executing: cat /etc/passwd");
            SshCommand command2 = client.RunCommand("cat /etc/passwd " + (!String.IsNullOrEmpty(filterUser) ? " | grep " + filterUser : ""));

            iLog(this, PluginLogType.Information, "ExitStatus: " + command2.ExitStatus);

            if (!String.IsNullOrEmpty(command2.Error))
                iLog(this, PluginLogType.Error, command2.Error);

            Byte[] bData = Encoding.UTF8.GetBytes(command2.Result.Replace("\r", "").Replace("\n", "\r\n"));

            using (MemoryStream ms = new MemoryStream(bData))
            using (StreamReader reader = new StreamReader(ms, Encoding.UTF8))
            {

                while (!reader.EndOfStream)
                {
                    String line = reader.ReadLine();

                    String[] parts = line.Split(":".ToCharArray());

                    /**************
                    0 = username
                    1 = ignorar
                    2 = user id
                    3 = defaul user group
                    4 = User information
                    5 = user home path
                    6 = start bash
                    */

                    UserData newUser = new UserData();

                    for (Int32 i = 0; i < parts.Length; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                newUser.Username = parts[i];
                                break;

                            case 2:
                                newUser.UserId = parts[i];
                                break;

                            case 3:
                                newUser.DefaultGroup = parts[i];
                                break;

                            case 4:
                                newUser.Information = parts[i];
                                break;

                            case 5:
                                newUser.HomePath = parts[i];
                                break;

                            case 6:
                                newUser.Bash = parts[i];
                                break;

                        }

                    }

                    if (newUser.UserId != null)
                        ret.Add(newUser);
                }
            }

            return ret;
        }


        private List<GroupData> GetUserGroups(SshClient client, Dictionary<String, Object> config)
        {
            List<GroupData> ret = new List<GroupData>();


            LogEvent iLog = new LogEvent(delegate(Object sender, PluginLogType type, string text)
            {
                if (Log != null)
                    Log(sender, type, text);
            });


            if (config.ContainsKey("pre_cmd") && (!String.IsNullOrEmpty(config["pre_cmd"].ToString())))
            {
                String preCmd = config["pre_cmd"].ToString();

                if (preCmd.ToLower().IndexOf("sudo") != -1)
                {

                    iLog(this, PluginLogType.Information, "Executing: " + config["pre_cmd"].ToString());
                    SshCommand command = client.RunCommand(config["pre_cmd"].ToString());

                    iLog(this, PluginLogType.Information, "ExitStatus: " + command.ExitStatus);

                    if (!String.IsNullOrEmpty(command.Error))
                        iLog(this, PluginLogType.Error, command.Error);

                    iLog(this, PluginLogType.Information, command.Result);
                }
                else
                {
                    iLog(this, PluginLogType.Information, "Pré command 'sudo' not permited");
                }
            }

            String prefix = "echo '" + config["password"].ToString() + "' | sudo ";

            if (config.ContainsKey("use_prefix"))
            {
                try
                {
                    Boolean up = Boolean.Parse(config["use_prefix"].ToString());
                    if (!up)
                        prefix = "";
                }
                catch { }
            }

            iLog(this, PluginLogType.Information, "Executing: cat /etc/group");
            SshCommand command2 = client.RunCommand("cat /etc/group");

            iLog(this, PluginLogType.Information, "ExitStatus: " + command2.ExitStatus);

            if (!String.IsNullOrEmpty(command2.Error))
                iLog(this, PluginLogType.Error, command2.Error);

            Byte[] bData = Encoding.UTF8.GetBytes(command2.Result.Replace("\r", "").Replace("\n", "\r\n"));

            using (MemoryStream ms = new MemoryStream(bData))
            using (StreamReader reader = new StreamReader(ms, Encoding.UTF8))
            {

                while (!reader.EndOfStream)
                {
                    String line = reader.ReadLine();

                    String[] parts = line.Split(":".ToCharArray());

                    /**************
                    0 = group name
                    1 = x
                    2 = group id
                    3 = users
                    */

                    GroupData newGroup = new GroupData();

                    for (Int32 i = 0; i < parts.Length; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                newGroup.Groupname = parts[i];
                                break;

                            case 2:
                                newGroup.GroupId = parts[i];
                                break;

                            case 3:
                                foreach (String u in parts[i].Split(",;".ToCharArray()))
                                    if (!String.IsNullOrEmpty(u)) newGroup.Users.Add(u.Trim());
                                break;

                        }

                    }

                    if (newGroup.GroupId != null)
                        ret.Add(newGroup);
                }
            }

            return ret;
        }

        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }
}
