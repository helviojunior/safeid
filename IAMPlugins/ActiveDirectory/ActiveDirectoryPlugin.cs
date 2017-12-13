using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.DirectoryServices;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using IAM.PluginInterface;

namespace ActiveDirectory
{
    public class ActiveDirectoryPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "IAM for Microsoft Active Directory Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com base de dados LDAP do Microsoft Active Directory"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://IAM/plugins/activedirectory");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Servidor AD", "ldap_server", "IP ou nome do servidor para conexão", PluginConfigTypes.String, true, @""));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário para conexão", PluginConfigTypes.String, true, @""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha para conexão", PluginConfigTypes.Password, true, @""));
            conf.Add(new PluginConfigFields("OU base", "ou_base", "Unidade Organizacional (OU) base para adição e busca dos usuários (ex. \\IAMUsers\\Colaboradores)", PluginConfigTypes.String, false, @"IAMUsers"));

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
            

            String ldapServer = config["ldap_server"].ToString();
            String username = config["username"].ToString();
            String password = config["password"].ToString();

            //Create a dictionary with the most knolege properties
            Dictionary<String, String> mostKnolege = GetCommonItems();

            foreach (String k in mostKnolege.Keys)
            {
                if (!ret.fields.ContainsKey(k))
                    ret.fields.Add(k, new List<string>());

                ret.fields[k].Add(mostKnolege[k]);
            }

            try
            {
                DirectoryEntry entry = new DirectoryEntry("LDAP://" + ldapServer, username, password, AuthenticationTypes.Secure);

                DirectorySearcher search = new DirectorySearcher(entry);
                search.SearchScope = SearchScope.Subtree;
                //search.Filter = "(&(objectClass=user)(sAMAccountName=helvio.junior))";
                search.Filter = "(samAccountType=805306368)";
                search.PropertiesToLoad.Add("distinguishedName");
                search.PropertiesToLoad.Add("company");
                search.PropertiesToLoad.Add("department");
                
                SearchResultCollection result = search.FindAll();

                if (result != null)
                {
                    Int32 count = 0;
                    foreach (SearchResult sr in result)
                    {
                        if (count >= 20)
                            break;

                        try
                        {

                            DirectoryEntry entry1 = new DirectoryEntry("LDAP://" + ldapServer + "/" + sr.Properties["distinguishedName"][0].ToString(), username, password);
                            entry1.AuthenticationType = AuthenticationTypes.Secure;
                            foreach (PropertyValueCollection property in entry1.Properties)
                            {
                                if (!ret.fields.ContainsKey(property.PropertyName))
                                    ret.fields.Add(property.PropertyName, new List<string>());

                                //Separa os itens que mecessita algum tratamento
                                switch (property.PropertyName.ToLower())
                                {
                                    case "lastlogon":
                                    case "whencreated":
                                    case "lockouttime":
                                        try
                                        {
                                            Int64 tmp = Int64.Parse(property[0].ToString());
                                            DateTime tmp2 = DateTime.FromFileTime(tmp);

                                            if (tmp2.Year > 1970)//Se a data for inferior nem envia
                                                ret.fields[property.PropertyName].Add(tmp2.ToString("yyyy-MM-dd HH:mm:ss"));
                                        }
                                        catch (Exception ex)
                                        { }
                                        break;

                                    case "useraccountcontrol":
                                        foreach (Object p1 in property)
                                        {
                                            UserAccountControl ctrl = (UserAccountControl)p1;

                                            foreach (UserAccountControl c in Enum.GetValues(typeof(UserAccountControl)))
                                            {
                                                //Verifica se está utilizando
                                                if ((ctrl & c) == c)
                                                    ret.fields[property.PropertyName].Add(c.ToString());
                                            }
                                        }

                                        break;

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
                            iLog(this, PluginLogType.Error, "Erro ao importar o registro (" + sr.Path + "): " + ex.Message);
                        }

                    }

                }

                ret.success = true;
                search.Dispose();
            }
            catch (Exception ex)
            {
                iLog(this, PluginLogType.Error, ex.Message);
            }

            return ret;
        }

        private Dictionary<String, String> GetCommonItems()
        {
            Dictionary<String, String> mostKnolege = new Dictionary<string, string>();
            mostKnolege.Add("displayName", "Display Name");
            mostKnolege.Add("sAMAccountName", "Login");
            mostKnolege.Add("company", "Enterprise Name");
            mostKnolege.Add("department", "Department Name");
            mostKnolege.Add("description", "Description");
            mostKnolege.Add("facsimileTelephoneNumber", "FAX phone number");
            mostKnolege.Add("homePhone", "Home phone number");
            mostKnolege.Add("mobile", "Mobile phone number");
            mostKnolege.Add("ipPhone", "IP phone number");
            mostKnolege.Add("info", "Notes");
            mostKnolege.Add("l", "City");
            mostKnolege.Add("mail", "E-mail Address");
            mostKnolege.Add("postalCode", "ZIP Code");
            mostKnolege.Add("postOfficeBox", "Post Office Box");
            mostKnolege.Add("st", "State/Province");
            mostKnolege.Add("streetAddress", "Street Address");
            mostKnolege.Add("telephoneNumber", "Main telephone Number");
            mostKnolege.Add("title", "Job title");
            mostKnolege.Add("wWWHomePage", "Web Page");
            mostKnolege.Add("physicalDeliveryOfficeName", "Office");

            return mostKnolege;

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

            String ldapServer = config["ldap_server"].ToString();
            String username = config["username"].ToString();
            String password = config["password"].ToString();
            String ou_base = (config.ContainsKey("ou_base") ? config["ou_base"].ToString() : "");
            String _dnBase = "";

            LDAP ldap = new LDAP(ldapServer, username, password, _dnBase);

            LDAP.DebugLog reg = new LDAP.DebugLog(delegate(String text)
            {
#if DEBUG
                    //Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "LDAP log: " + text, "");
#endif
            });

            ldap.Log += reg;

            try
            {
                ldap.Bind();
            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, "Error on connect to ActiveDirectory: " + ex.Message);
                Log2(this, PluginLogType.Error, 0, 0, "Error on connect to ActiveDirectory: " + ex.Message, "");
                ldap = null;
                return;
            }

            DirectoryEntry entry = null;
            try
            {

                //Caso haja o ou_base, buscar/criar a OU para listar os usuários
                if (!String.IsNullOrWhiteSpace(ou_base))
                    entry = ldap.AddContainerTree(ou_base);

            }
            catch { }

            //Realiza a busca de todas as OUs e grupos
            if (ImportPackageStruct != null)
            {
                PluginConnectorBaseImportPackageStruct structPackage = new PluginConnectorBaseImportPackageStruct(importId);

                try
                {
                    if (entry == null)
                        entry = ldap.DirectoryEntryRoot;

                    DirectorySearcher search = new DirectorySearcher(entry);
                    search.SearchScope = SearchScope.Subtree;
                    search.Filter = "(objectCategory=group)";
                    search.PropertiesToLoad.Add("distinguishedName");
                    search.PropertiesToLoad.Add("name");

                    SearchResultCollection result = search.FindAll();

                    if (result != null)
                    {

                        foreach (SearchResult sr in result)
                        {
                            try
                            {
                                structPackage.AddGroup(sr.Properties["name"][0].ToString());
                            }
                            catch (Exception ex)
                            {
                                Log(this, PluginLogType.Error, "Erro ao listar o grupo (" + sr.Path + "): " + ex.Message);
                            }
                            finally
                            {
                            }

                        }

                    }

                    search.Dispose();
                }
                catch (Exception ex)
                {
                    Log(this, PluginLogType.Error, ex.Message);
                }


                try
                {
                    if (entry == null)
                        entry = ldap.DirectoryEntryRoot;

                    DirectorySearcher search = new DirectorySearcher(entry);
                    search.SearchScope = SearchScope.Subtree;
                    search.Filter = "(objectClass=organizationalUnit)";
                    search.PropertiesToLoad.Add("distinguishedName");
                    search.PropertiesToLoad.Add("name");

                    SearchResultCollection result = search.FindAll();

                    if (result != null)
                    {

                        foreach (SearchResult sr in result)
                        {
                            try
                            {
                                /*
                                String dn = sr.Properties["distinguishedName"][0].ToString();
                                //String name = sr.Properties["name"][0].ToString();
                                String[] ou = dn.Replace(entry.Properties["distinguishedName"][0].ToString(), "").Replace(",", "").Replace("OU=", "\\").Trim(" ,".ToCharArray()).Split("\\".ToCharArray());

                                Array.Reverse(ou);

                                String path = "\\" + String.Join("\\", ou);*/

                                structPackage.AddContainer(DNToPath(sr.Properties["distinguishedName"][0].ToString(), entry));

                            }
                            catch (Exception ex)
                            {
                                Log(this, PluginLogType.Error, "Erro ao listar a OU (" + sr.Path + "): " + ex.Message);
                            }
                            finally
                            {
                            }

                        }

                    }

                    search.Dispose();
                }
                catch (Exception ex)
                {
                    Log(this, PluginLogType.Error, ex.Message);
                }

                //Envia o pacote da estrutura 
                ImportPackageStruct(structPackage);
            }

            //Realiza a busca dos usuários
            try
            {
                //DirectoryEntry entry = new DirectoryEntry("LDAP://" + ldapServer, username, password, AuthenticationTypes.Secure);
                if (entry == null)
                    entry = ldap.DirectoryEntryRoot;

                DirectorySearcher search = new DirectorySearcher(entry);
                search.SearchScope = SearchScope.Subtree;
                //search.Filter = "(&(objectClass=user)(sAMAccountName=helvio.junior))";
                search.Filter = "(samAccountType=805306368)";
                search.PropertiesToLoad.Add("useraccountcontrol");
                search.PropertiesToLoad.Add("distinguishedName");
                search.PropertiesToLoad.Add("company");
                search.PropertiesToLoad.Add("department");
                search.PropertiesToLoad.Add("memberOf");

                foreach (PluginConnectorBaseDeployPackageMapping m in fieldMapping)
                    if (!search.PropertiesToLoad.Contains(m.dataName))
                        search.PropertiesToLoad.Add(m.dataName);

                /*
                search.PropertiesToLoad.Add("displayName");
                search.PropertiesToLoad.Add("mail");
                search.PropertiesToLoad.Add("sAMAccountName");
                search.PropertiesToLoad.Add("objectClass");
                search.PropertiesToLoad.Add("distinguishedName");
                search.PropertiesToLoad.Add("lastLogonTimestamp");
                search.PropertiesToLoad.Add("whenCreated");
                
                search.PropertiesToLoad.Add("lockoutTime");
                search.PropertiesToLoad.Add("proxyAddresses");
                search.PropertiesToLoad.Add("mailNickname");
                search.PropertiesToLoad.Add("telephoneNumber");
                search.PropertiesToLoad.Add("userPrincipalName");
                search.PropertiesToLoad.Add("memberOf");*/

                SearchResultCollection result = search.FindAll();

                if (result != null)
                {

                    foreach (SearchResult sr in result)
                    {

                        PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);

                        try
                        {

                            using (DirectoryEntry entry1 = new DirectoryEntry("LDAP://" + ldapServer + "/" + sr.Properties["distinguishedName"][0].ToString(), username, password))
                            {
                                entry1.AuthenticationType = AuthenticationTypes.Secure;
                                String ou = entry1.Parent.Path;
                                ou = ou.Replace("LDAP://" + ldapServer + "/", "");

                                package.container = DNToPath(ou, entry);

                                if (fieldMapping.Exists(f => (f.dataName == "organizationslUnit")) || fieldMapping.Exists(f => (f.dataName == "organizationslunit")))
                                    package.AddProperty("organizationslUnit", ou, "string");

                            }
                            

                            foreach (String p in sr.Properties.PropertyNames)
                            {
                                //Separa os itens que mecessita algum tratamento
                                switch (p.ToLower())
                                {
                                    case "lastlogon":
                                    case "whencreated":
                                    case "lockouttime":
                                        try
                                        {
                                            Int64 tmp = Int64.Parse(sr.Properties[p][0].ToString());
                                            DateTime tmp2 = DateTime.FromFileTime(tmp);

                                            if (tmp2.Year > 1970)//Se a data for inferior nem envia
                                                package.AddProperty( p, tmp2.ToString("o"), (fieldMapping.Exists(f => (f.dataName == p)) ? fieldMapping.Find(f => (f.dataName == p)).dataType : "datetime"));
                                        }
                                        catch (Exception ex)
                                        { }
                                        break;

                                    case "useraccountcontrol":
                                        foreach (Object p1 in sr.Properties[p])
                                        {
                                            UserAccountControl ctrl = (UserAccountControl)p1;

                                            foreach (UserAccountControl c in Enum.GetValues(typeof(UserAccountControl)))
                                            {
                                                //Verifica se está utilizando
                                                if ((ctrl & c) == c)
                                                    package.AddProperty( p, c.ToString(), (fieldMapping.Exists(f => (f.dataName == p)) ? fieldMapping.Find(f => (f.dataName == p)).dataType : "string"));
                                            }
                                        }

                                        break;

                                    case "memberof":
                                        foreach (Object p1 in sr.Properties[p])
                                        {
                                            //Trata o grupo
                                            try
                                            {

                                                using (DirectoryEntry entry1 = new DirectoryEntry("LDAP://" + ldapServer + "/" + p1.ToString(), username, password))
                                                {
                                                    entry1.AuthenticationType = AuthenticationTypes.Secure;
                                                    package.AddGroup(entry1.Properties["name"][0].ToString());
                                                }

                                            }
                                            catch { }
                                            

                                            if (fieldMapping.Exists(m => (m.dataName == "memberOf")))
                                                package.AddProperty(p, p1.ToString(), (fieldMapping.Exists(f => (f.dataName == p)) ? fieldMapping.Find(f => (f.dataName == p)).dataType : "string"));

                                        }

                                        break;

                                    default:
                                        foreach (Object p1 in sr.Properties[p])
                                            package.AddProperty( p, p1.ToString(), (fieldMapping.Exists(f => (f.dataName == p)) ? fieldMapping.Find(f => (f.dataName == p)).dataType : "string"));
                                        break;
                                }
                            }

                            ImportPackageUser(package);
                        }
                        catch (Exception ex)
                        {
                            Log(this, PluginLogType.Error, "Erro ao importar o registro (" + sr.Path + "): " + ex.Message);
                        }
                        finally
                        {
                            package.Dispose();
                            package = null;
                        }

                    }

                }
                
                search.Dispose();
            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, ex.Message);
            }

        }

        private String DNToPath(String dn, DirectoryEntry entry)
        {
            String path = "\\";

            String[] ou = dn.Replace(entry.Properties["distinguishedName"][0].ToString(), "").Replace(",", "").Replace("cn=", "\\").Replace("ou=", "\\").Replace("CN=", "\\").Replace("OU=", "\\").Trim(" ,".ToCharArray()).Split("\\".ToCharArray());

            Array.Reverse(ou);

            path += String.Join("\\", ou);

            return path.TrimEnd("\\".ToCharArray());
        }


        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            //Não implementado
        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            if (!CheckInputConfig(config, true, Log))
                return;

            String deployLogShort = "";
            String deployLogLong = "";

            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {

                List<String> prop = new List<String>();

                LDAP ldap = new LDAP(config["ldap_server"].ToString(), config["username"].ToString(), config["password"].ToString(), "");

                LDAP.DebugLog reg = new LDAP.DebugLog(delegate(String text)
                {
#if DEBUG
                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "LDAP log: " + text, "");
#endif
                });

                ldap.Log += reg;

                try
                {
                    ldap.Bind();
                }
                catch (Exception ex)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Error on connect to ActiveDirectory: " + ex.Message);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on connect to ActiveDirectory: " + ex.Message, "");
                    ldap = null;
                    return;
                }

                String login = package.login;
                
                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (dt.dataName.ToLower() == "samaccountname")
                        login = dt.dataValue;
                    /*else if (dt.dataName.ToLower() == "displayname")
                        login = dt.dataValue;*/

                if (login == "")
                    login = package.login;

                if (login == "")
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("IAM Login not found in properties list");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Login not found in properties list", "");
                    return;
                }

                String container = "";// package.container;
                String ou_base = (config.ContainsKey("ou_base") ? config["ou_base"].ToString() : "");
                if (!String.IsNullOrWhiteSpace(ou_base))
                    container += ou_base.TrimEnd("\\ ".ToCharArray());

                if (container == "")
                    container = "IAMUsers";

                container = container.Trim("\\ ".ToCharArray());

                DirectoryEntry baseCN = ldap.DirectoryEntryRoot;

                if ((container != null) && (container != ""))
                    baseCN = ldap.AddContainerTree(container);


                if (!String.IsNullOrWhiteSpace(package.container))
                    container += "\\" + package.container.Trim("\\ ".ToCharArray());

                container = container.Trim("\\ ".ToCharArray());

                DirectoryEntry user = null;
                SearchResultCollection res = ldap.Find(login);
                DirectoryEntry ct = ldap.DirectoryEntryRoot;

                if ((container != null) && (container != ""))
                    ct = ldap.AddContainerTree(container);


#if DEBUG
                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "Container = " + ct.Path, "");
                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "Find user? " + (res.Count > 0), "");
#endif

                if (res.Count == 0)
                {

                    if (package.password == "")
                    {
                        package.password = IAM.Password.RandomPassword.Generate(16);
                        processLog.AppendLine("User not found in AD and IAM Password not found in properties list, creating a random password (" + package.password + ")");
                    }
                    
                    ldap.AddUser(ct, package.fullName.fullName, login, package.password);
                    res = ldap.Find(login);

                    processLog.AppendLine("User added");
                }
                
                user = res[0].GetDirectoryEntry();

                try
                {
                    if (container != "IAMUsers")
                        ldap.ChangeObjectContainer(user, ct);
                }
                catch(Exception ex) {
                    processLog.AppendLine("Error on change user container: " + ex.Message);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on change user container: " + ex.Message, "");
                }

#if DEBUG
                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "User = " + user.Path, "");
#endif

                UserAccountControl ctrl = (UserAccountControl)user.Properties["useraccountcontrol"][0];

                //Limpa as flags que serão verificadas por este sistema
                if ((ctrl & UserAccountControl.ACCOUNTDISABLE) == UserAccountControl.ACCOUNTDISABLE)
                    ctrl -= UserAccountControl.ACCOUNTDISABLE;

                if ((package.locked) || (package.temp_locked))
                    ctrl = (UserAccountControl)((Int32)ctrl + UserAccountControl.ACCOUNTDISABLE);

                processLog.AppendLine("User locked? " + (package.locked || package.temp_locked ? "true" : "false"));

                String[] propNames = new String[user.Properties.PropertyNames.Count];
                user.Properties.PropertyNames.CopyTo(propNames, 0);

                

                user.Properties["displayname"].Value = package.fullName.fullName;

                user.Properties["givenName"].Value = package.fullName.givenName;
                user.Properties["sn"].Value = package.fullName.familyName;
                
                user.Properties["userAccountControl"].Value = ctrl;

                try
                {
                    try
                    {
                        user.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on commit user data: " + ex.Message);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on commit user data: " + ex.Message, "");
                        return;
                    }

                    try
                    {
                        if (!String.IsNullOrWhiteSpace(package.password))
                            user.Invoke("SetPassword", (Object)package.password);

                        user.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on set user password, check the password complexity rules");
                        processLog.AppendLine(ex.Message);
                        if (ex.InnerException != null)
                            processLog.AppendLine(ex.InnerException.Message);

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

                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on set user password, check the password complexity rules", ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : "") +Environment.NewLine + sPs);
                        return;
                    }

                    //Atribui as outras variáveis
                    processLog.AppendLine("Property update");
                    try
                    {

                        processLog.AppendLine("\tCompany: " + package.enterprise);

                        processLog.AppendLine("\tCompany exists: " + user.Properties.Contains("company"));

                        if (!String.IsNullOrEmpty(package.enterprise))
                            if (user.Properties.Contains("company"))
                            {
                                user.Properties["company"].Value = package.enterprise;
                            }
                            else
                            {
                                user.Properties["company"].Add(package.enterprise);
                            }


                        user.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        processLog.AppendLine("\tError on set user company: " + ex.Message);
                    }

                    //Monta todos os campos que serão inseridos/atualizados
                    Dictionary<String, String> data = new Dictionary<String, String>();

                    Dictionary<String, String> mostKnolege = GetCommonItems();

                    foreach (String k in mostKnolege.Keys)
                        if (!data.ContainsKey(k))
                                data.Add(k, null);

                    foreach (PropertyValueCollection property in user.Properties)
                        if (!data.ContainsKey(property.PropertyName.ToLower()))
                            data.Add(property.PropertyName.ToLower(), null);


                    foreach (PluginConnectorBasePackageData dt in package.importsPluginData)
                        if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                        {
                            data[dt.dataName.ToLower()] = dt.dataValue;
                            //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "1. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("1. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                        }

                    foreach (PluginConnectorBasePackageData dt in package.pluginData)
                        if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                        {
                            data[dt.dataName.ToLower()] = dt.dataValue;
                            //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "2. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("2. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                        }

                    foreach (PluginConnectorBasePackageData dt in package.properties)
                        if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                        {
                            data[dt.dataName.ToLower()] = dt.dataValue;
                            //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "3. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("3. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                        }

                    //Remove os ítens protegidos pelo AD, onde a forma de atualização deve ser outra
                    data.Remove("whencreated");
                    data.Remove("lastlogon");
                    data.Remove("name");
                    data.Remove("lockouttime");
                    data.Remove("useraccountcontrol");
                    data.Remove("memberof");
                    data.Remove("distinguishedname");
                    data.Remove("samaccountname");
                    data.Remove("displayname");
                    data.Remove("givenname");
                    data.Remove("sn");
                    data.Remove("cn");
                   
                    foreach (String k in data.Keys)
                        if (data[k] != null)
                            try
                            {

                                //
                                SearchResultCollection res2 = ldap.Find(login);
                                user = res2[0].GetDirectoryEntry();

                                processLog.AppendLine("\t" + k + " exists: " + user.Properties.Contains(k));

                                if (!String.IsNullOrEmpty(package.enterprise))
                                    if (user.Properties.Contains(k))
                                    {
                                        user.Properties[k].Value = data[k];
                                    }
                                    else
                                    {
                                        user.Properties[k].Add(data[k]);
                                    }

                                user.CommitChanges();
                            }
                            catch (Exception ex)
                            {
                                processLog.AppendLine("\tError setting data '" + k + "': " + ex.Message);
                            }


                    processLog.AppendLine("RBAC");

                    //Busca o usuário novamente
                    //Para não aplicas as informações incorretas
                    //Devido a definição das propriedades anteriores
                    res = ldap.Find(login);
                    user = res[0].GetDirectoryEntry();

                    //Executa as ações do RBAC
                    if ((package.pluginAction != null) && (package.pluginAction.Count > 0))
                    {
                        foreach (PluginConnectorBaseDeployPackageAction act in package.pluginAction)
                            try
                            {
                                processLog.AppendLine("\tRole: " + act.roleName + " (" + act.actionType.ToString() + ") " + act.ToString());

                                switch (act.actionKey.ToLower())
                                {
                                    case "group":
                                        if (act.actionType == PluginActionType.Add)
                                        {
                                            String grpCN = ldap.FindOrCreateGroup(baseCN, act.actionValue);

                                            if (ldap.addUserToGroup(user.Name, grpCN))
                                                processLog.AppendLine("\tUser added in group " + act.actionValue + " by role " + act.roleName);
                                        }
                                        else if (act.actionType == PluginActionType.Remove)
                                        {
                                            String grpCN = ldap.FindOrCreateGroup(baseCN, act.actionValue);
                                            if (ldap.removeUserFromGroup(user.Name, grpCN))
                                                processLog.AppendLine("\tUser removed from group " + act.actionValue + " by role " + act.roleName);
                                        }
                                        break;

                                    default:
                                        processLog.AppendLine("\tAction not recognized: " + act.actionKey);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                processLog.AppendLine("\tError on execute action (" + act.actionKey + "): " + ex.Message);
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
                    processLog.AppendLine("User updated with password");
                else
                    processLog.AppendLine("User updated without password");
            }
            catch(Exception ex) {
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

                String _dnBase = "";
                List<String> prop = new List<String>();

                LDAP ldap = new LDAP(config["ldap_server"].ToString(), config["username"].ToString(), config["password"].ToString(), _dnBase);


                LDAP.DebugLog reg = new LDAP.DebugLog(delegate(String text)
                {
                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "LDAP log: " + text, "");
                });

                ldap.Log += reg;

                try
                {
                    ldap.Bind();
                }
                catch (Exception ex)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on connect to ActiveDirectory: " + ex.Message, "");
                    ldap = null;
                    return;
                }

                String login = package.login;
                String container = package.container;

                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (dt.dataName.ToLower() == "samaccountname")
                        login = dt.dataValue;
                /*else if (dt.dataName.ToLower() == "displayname")
                    login = dt.dataValue;*/

                if (login == "")
                    login = package.login;

                if (login == "")
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Login not found in properties list", "");
                    return;
                }

                if (container == "")
                    container = "IAMUsers";

                DirectoryEntry user = null;
                SearchResultCollection res = ldap.Find(login);
                DirectoryEntry ct = ldap.DirectoryEntryRoot;

                if ((container != null) && (container != ""))
                    ct = ldap.AddContainerTree(container);

                if (res.Count == 0) //User not found
                {
                    Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found in AD", "");
                    return;
                }

                ldap.DeleteObject(res[0].GetDirectoryEntry());

                NotityDeletedUser(this, package.entityId, package.identityId);

                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User deleted", "");
            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process delete: " + ex.Message, "");
            }
        }

        private String GetCnName(String cn)
        {
            return cn.Split(",".ToCharArray())[0].Replace("cn=", "").Replace("CN=", "");
        }

        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }
}
