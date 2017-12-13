using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;
using System.Security.Principal;
using System.Security.AccessControl;

namespace ActiveDirectory
{

    enum UserAccountControl
    {
        SCRIPT = 0x0001,
        ACCOUNTDISABLE = 0x0002,
        HOMEDIR_REQUIRED = 0X0008,
        BLOQUEIO = 0x0010,
        PASSWD_NOTREQD = 0x0020,
        PASSWD_CANT_CHANGE =	0x0040,
        ENCRYPTED_TEXT_PWD_ALLOWED = 0x0080,
        TEMP_DUPLICATE_ACCOUNT = 0x0100,
        NORMAL_ACCOUNT = 0x0200,
        INTERDOMAIN_TRUST_ACCOUNT = 0x0800,
        WORKSTATION_TRUST_ACCOUNT = 0x1000,
        SERVER_TRUST_ACCOUNT = 0x2000,
        DONT_EXPIRE_PASSWORD = 0x10000,
        MNS_LOGON_ACCOUNT = 0x20000,
        SMARTCARD_REQUIRED = 0x40000,
        TRUSTED_FOR_DELEGATION = 0x80000,
        NOT_DELEGATED = 0x100000,
        USE_DES_KEY_ONLY = 0x200000,
        DONT_REQ_PREAUTH = 0x400000,
        PASSWORD_EXPIRED = 0x800000,
        TRUSTED_TO_AUTH_FOR_DELEGATION = 0x1000000,
        PARTIAL_SECRETS_ACCOUNT = 0x04000000
    };

    class LDAP
    {
        private String _serverIP;
        private String _ldapBase;
        private String _dnRoot;
        private String _dnPasswdRoot;
        private String _dnBase;
        private String _objectClassConta;
        private String _userAttribute;
        private String _passwdAtribute;
        private String _groupAtribute;
        private Boolean _openned;
        DirectoryEntry _deRoot;

        public String DNBase { get { return _dnBase; } }
        public String GroupAttribute { get { return _groupAtribute; } set { _groupAtribute = value; } }
        public DirectoryEntry DirectoryEntryRoot { get { return _deRoot; } }

        public delegate void DebugLog(String text);
        public event DebugLog Log;

        public LDAP(String ServerIP)
        {
            _serverIP = ServerIP;
        }

        public LDAP(String ServerIP, String DNRoot, String DBPasswordRoot, String DNBase)
        {
            _serverIP = ServerIP;
            _dnRoot = DNRoot;
            _dnPasswdRoot = DBPasswordRoot;
            _dnBase = DNBase;
        }

        public void Bind()
        {
            _ldapBase = "LDAP://" + _serverIP;
            _deRoot = new DirectoryEntry(
                    _ldapBase + (_dnBase != null && _dnBase.Trim() != "" ? "/" + _dnBase : ""),
                    _dnRoot, _dnPasswdRoot,
                    AuthenticationTypes.Secure);

            String teste = _deRoot.SchemaClassName;

        }

        public SearchResultCollection Find(String login)
        {
            return Find(_deRoot, login);
        }

        public SearchResultCollection Find()
        {
            return Find(_deRoot);
        }

        public SearchResultCollection Find(DirectoryEntry root)
        {
            return _Find(root, "*", "");
        }

        public SearchResultCollection Find(DirectoryEntry root, String login)
        {
            return _Find(root, "*", "(sAMAccountName=" + login + ")");
        }


        /*
        public SearchResultCollection Find(String DN)
        {
            try
            {
                DirectoryEntry _root = new DirectoryEntry(
                        _ldapBase + DN,
                        _dnRoot, _dnPasswdRoot,
                        AuthenticationTypes.Secure);
                return _Find(_root, "*", null);
            }
            catch { return null; }
        }*/

        public DirectoryEntry AddContainerTree(String container)
        {
            String[] tree = container.Trim("\\".ToCharArray()).Split("\\".ToCharArray());

            DirectoryEntry lastNode = _deRoot;

            foreach (String ouName in tree)
            {
                SearchResultCollection objetos = _Find(lastNode, "organizationalUnit", "(name=" + ouName + ")");
                if (objetos.Count == 0)
                {
                    DirectoryEntry newchild = lastNode.Children.Add("OU=" + ouName, "organizationalUnit");
                    newchild.CommitChanges();

                    lastNode = newchild;
                }
                else
                {
                    lastNode = objetos[0].GetDirectoryEntry();
                }

            }

            return lastNode;
        }

        public Boolean AddUser(String fullName, String login, String password)
        {
            return AddUser(_deRoot, fullName, login, password);
        }

        public Boolean AddUser(DirectoryEntry container, String fullName, String login, String password)
        {
            String step = "";
            try
            {
                step = "looking for user '" + login + "'";
                if (ObjectExists("sAMAccountName=" + login, "user", false))
                    throw new Exception("Login already exists");

                if (ObjectExists(container, "CN=" + fullName, "user", false))
                {
                    String newFullName = fullName + " (" + login + ")";

                    if (ObjectExists(container, "CN=" + newFullName, "user", false))
                    {
                        Int32 index = 1;
                        while (ObjectExists(container, "CN=" + fullName + " " + index, "user", false))
                            index++;

                        fullName = fullName + " " + index;
                    }
                    else
                    {
                        fullName = newFullName;
                    }

                }


                login = login.Replace("cn=", "").Replace("CN=", "");

                step = "adding container children";
                DirectoryEntry newchild = container.Children.Add("CN=" + login, "user");
                newchild.CommitChanges();

                System.Threading.Thread.Sleep(500);

                String userName = login.Replace("cn=", "").Replace("CN=", "");
                DirectoryEntry _user = newchild; //_deRoot.Children.Find(name, "user");

                //A primeira senha é gerada randomicamente para cumprir com todos os sequisitos de complexidade
                //Para evitar erro ao inserir o usuário
                step = "setting temp password";
                String tPwd = IAM.Password.RandomPassword.Generate(16);
                _user.Invoke("SetPassword", (Object)tPwd);

                String CNRoot = "";
                if (_user.Path.ToLower().IndexOf("dc=") != -1)
                {
                    String[] dc = _user.Path.ToLower().Substring(_user.Path.ToLower().IndexOf("dc=")).Replace("dc=", "").Split(",".ToCharArray());
                    foreach (String item in dc)
                    {
                        if (CNRoot != "") { CNRoot += "."; }
                        CNRoot += item;
                    }
                }

                _user.Properties["displayname"].Value = fullName;

                String[] names = fullName.Split(" ".ToCharArray(), 2);
                if (names.Length > 1)
                {
                    _user.Properties["givenName"].Value = names[0];
                    _user.Properties["sn"].Value = names[1];
                }

                if (CNRoot != "")
                {
                    _user.Properties["userPrincipalName"].Value = login + "@" + CNRoot;
                }

                _user.Properties["sAMAccountName"].Value = login;
                _user.Properties["pwdLastSet"].Value = -1;
                _user.Properties["userAccountControl"].Value = UserAccountControl.NORMAL_ACCOUNT | UserAccountControl.DONT_EXPIRE_PASSWORD;
                _user.CommitChanges();

                //Define a senha correta do usuário
                try
                {
                    _user.Invoke("SetPassword", (Object)password);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error setting user password, check the password complexity rules", ex);
                }

                //Retira a permissão do usuário trocar a senha
                step = "committing user data";
                UserChangePasswordRule(_user, false);
                _user.CommitChanges();

                _user.Close();

                newchild.Close();
                //_root.Close();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error " + step, ex);
                return false;
            }
        }

        public void DeleteObject(DirectoryEntry obj)
        {
            if (obj.Equals(_deRoot))
                throw new Exception("Not permited");

            obj.Parent.Children.Remove(obj);
        }

        public void UserChangePasswordRule(DirectoryEntry de, Boolean canChangePwd)
        {
            Guid changePasswordGuid = new Guid("{ab721a53-1e2f-11d0-9819-00aa0040529b}");

            IdentityReference selfSddl = new SecurityIdentifier(WellKnownSidType.SelfSid, null); // or  ("S-1-1-0");
            IdentityReference everyoneSddl = new SecurityIdentifier(WellKnownSidType.WorldSid, null); // or ("S-1-5-10");

            ActiveDirectoryAccessRule selfAccRule = new ActiveDirectoryAccessRule(selfSddl, ActiveDirectoryRights.ExtendedRight, canChangePwd ? AccessControlType.Allow : AccessControlType.Deny, changePasswordGuid);
            ActiveDirectoryAccessRule everyoneAccRule = new ActiveDirectoryAccessRule(everyoneSddl, ActiveDirectoryRights.ExtendedRight, canChangePwd ? AccessControlType.Allow : AccessControlType.Deny, changePasswordGuid);

            de.ObjectSecurity.AddAccessRule(selfAccRule);
            de.ObjectSecurity.AddAccessRule(everyoneAccRule);
        }

        public Boolean changeUserPassword(String CN, String password)
        {
            try
            {
                DirectoryEntry _root = _deRoot.Children.Find(CN, "user");
                _root.Invoke("SetPassword", (Object)password);
                _root.CommitChanges();
                _root.Close();
                return true;
            }
            catch { return false; }
        }

        public DirectoryEntry ChangeObjectContainer(DirectoryEntry obj, DirectoryEntry newContainer)
        {
            if (obj.Parent.Equals(newContainer))
                return obj;

            obj.MoveTo(newContainer);
            obj.CommitChanges();

            return obj;
        }

        public String FindOrCreateGroup(String groupName)
        {
            return FindOrCreateGroup(_deRoot, groupName);
        }

        public String FindOrCreateGroup(DirectoryEntry container, String groupName)
        {
            DirectoryEntry _group = null;

            WriteLog("FindOrCreateGroup(BaseCN) [" + groupName + "], CN = " + (container != null ? container.Path : ""));

            SearchResultCollection res = _Find(_deRoot, "group", "(CN=" + groupName + ")");
            if (res.Count > 0){
                _group = res[0].GetDirectoryEntry();
                WriteLog("FindOrCreateGroup(exists) [" + groupName + "], CN = " + (_group != null ? _group.Name : ""));
            }
            else
            {

                DirectoryEntry groupNode = null;

                SearchResultCollection objetos = _Find(container, "organizationalUnit", "(name=Groups)");
                if (objetos.Count == 0)
                {
                    DirectoryEntry newGroup = container.Children.Add("OU=Groups", "organizationalUnit");
                    newGroup.CommitChanges();

                    groupNode = newGroup;
                }
                else
                {
                    groupNode = objetos[0].GetDirectoryEntry();
                }

                DirectoryEntry newchild = groupNode.Children.Add("CN=" + groupName, "group");
                newchild.Properties["name"].Value = groupName;
                newchild.Properties["sAMAccountName"].Value = groupName;
                newchild.CommitChanges();

                _group = newchild;

                WriteLog("FindOrCreateGroup(add) [" + groupName + "], CN = " + (_group != null ? _group.Name : ""));
            }

            return (_group != null ? _group.Name : "");
        }

        public Boolean addUserToGroup(String CNUser, String CNGroup)
        {
            try
            {
                DirectoryEntry _root = null;
                DirectoryEntry _user = null;
                SearchResultCollection res = _Find(_deRoot, "group", "(" + CNGroup + ")");
                if (res.Count > 0)
                    _root = res[0].GetDirectoryEntry();

                res = _Find(_deRoot, "user", "(" + CNUser + ")");
                if (res.Count > 0)
                    _user = res[0].GetDirectoryEntry();

                _root.Invoke("Add", new object[] { _user.Path });
                _root.CommitChanges();
                _root.Close();

                WriteLog("addUserToGroup success, CNUser = " + CNUser + ", CNGroup = " + CNGroup);
                return true;
            }
            catch (Exception ex)
            {
                WriteLog("Erro on addUserToGroup: " + ex.Message);
                return false;
            }
        }



        public Boolean removeUserFromGroup(String CNUser, String CNGroup)
        {
            try
            {
                DirectoryEntry _root = null;
                DirectoryEntry _user = null;
                SearchResultCollection res = _Find(_deRoot, "group", "(" + CNGroup + ")");
                if (res.Count > 0)
                    _root = res[0].GetDirectoryEntry();

                res = _Find(_deRoot, "user", "(" + CNUser + ")");
                if (res.Count > 0)
                    _user = res[0].GetDirectoryEntry();

                _root.Properties["member"].Remove(_user.Path.Replace(_deRoot.Path, "").Trim("/".ToCharArray()));
                _root.CommitChanges();
                _root.Close();
                return true;
            }
            catch { return false; }
        }


        public Boolean removeObject(String CN)
        {
            try
            {
                DirectoryEntry _object = _deRoot.Children.Find(CN, "group");
                String Container = "";
                if (_object.Path.IndexOf(",") != -1)
                {
                    Container = _object.Path.Substring(_object.Path.IndexOf(",") + 1);
                }
                else
                {
                    return false;
                }

                _deRoot.Children.Remove(_object);
                _deRoot.CommitChanges();
                return true;
            }
            catch { return false; }
        }

        public Boolean ObjectExists(String name, String schemaClassName, Boolean CaseSentitive)
        {
            return ObjectExists(_deRoot, name, schemaClassName, CaseSentitive);
        }

        public Boolean ObjectExists(DirectoryEntry baseDe, String name, String schemaClassName, Boolean CaseSentitive)
        {

            SearchResultCollection objetos = _Find(baseDe, schemaClassName, "(" + name + ")");
            foreach (SearchResult result in objetos)
            {
                String searchpath = result.Path.Replace(_ldapBase, "").Trim("/".ToCharArray());
                String searchContainer = "";
                String CN = "";
                if (searchpath.IndexOf(",") != -1)
                {
                    searchContainer = searchpath.Substring(searchpath.IndexOf(",") + 1);
                    CN = searchpath.Substring(0, searchpath.IndexOf(",")).Trim("/\\ ".ToCharArray());
                }

                if (CaseSentitive)
                {
                    if (CN == name)
                    {
                        return true;
                    }
                }
                else
                {
                    if (CN.ToLower() == name.Trim().ToLower())
                    {
                        return true;
                    }
                }

            }
            return false;
        }

        public String getObjectPath(String name)
        {
            try
            {
                return _deRoot.Children.Find(name).Path.Replace(_ldapBase, "");
            }
            catch { return ""; }
        }


        public Boolean userMemberOf(String name, String groupName, Boolean CaseSentitive)
        {

            SearchResultCollection objetos = _Find(_deRoot, "person", "(" + _groupAtribute + "=" + groupName + ")");
            foreach (SearchResult result in objetos)
            {
                Boolean isResult = false;
                String searchpath = result.Path.Replace(_ldapBase, "");
                String searchContainer = "";
                String CN = "";
                if (searchpath.IndexOf(",") != -1)
                {
                    searchContainer = searchpath.Substring(searchpath.IndexOf(",") + 1);
                    CN = searchpath.Substring(0, searchpath.IndexOf(","));
                }

                if (CaseSentitive)
                {
                    if (CN == name)
                    {
                        isResult = true;
                    }
                }
                else
                {
                    if (CN.ToLower() == name.ToLower())
                    {
                        isResult = true;
                    }
                }
                if (isResult)
                {
                    ResultPropertyCollection rpc = result.Properties;
                    foreach (string property in rpc.PropertyNames)
                    {
                        if ((property.ToLower() == this._groupAtribute.ToLower()))
                        {

                            foreach (object value in rpc[property])
                            {
                                if (((CaseSentitive) && (value.ToString() == groupName)) || ((!CaseSentitive) && (value.ToString().ToLower() == groupName.ToLower())))
                                {
                                    return true;
                                }
                            }

                        }
                    }
                }

            }
            return false;
        }


        private SearchResultCollection _Find(DirectoryEntry _root, String objectClass, String filter)
        {
            try
            {

                WriteLog("_Find: _root = " + _root.Name + ", objectClass = " + objectClass + ", filter = " + filter);

                DirectorySearcher searcher = new DirectorySearcher(_root);
                if (filter != null)
                {
                    searcher.Filter = "(&(objectClass=" + objectClass + ")" + filter + ")";
                }
                else
                {
                    searcher.Filter = "(&(objectClass=" + objectClass + "))";
                }
                searcher.PropertiesToLoad.Add("*");

                WriteLog("_Find: searcher.Filter = " + searcher.Filter);

                //searcher.PropertiesToLoad.Add("objectClass");
                SearchResultCollection results = searcher.FindAll();

                WriteLog("_Find: results = " + (results == null ? "null" : results.Count.ToString()));

                if (results != null)
                {
                    for (Int32 i = 0; i < results.Count; i++)
                    {
                        WriteLog("_Find: results[0] = " + (results[0] == null ? "null" : results[0].Path));
                    }
                }

                /*
                foreach (SearchResult result in results)
                {
                
                    String searchpath = result.Path.Replace(_ldapBase, "");
                    String searchContainer = "";
                    if (searchpath.IndexOf(",") != -1){
                        searchContainer = searchpath.Substring(searchpath.IndexOf(",") + 1);
                    }

                    Console.WriteLine("Path: {0}", searchpath);
                    Console.WriteLine("Container: {0}", searchContainer);
                    ResultPropertyCollection rpc = result.Properties;
                    foreach (string property in rpc.PropertyNames)
                    {
                        foreach (object value in rpc[property])
                            Console.WriteLine("  property = {0}  value = {1}", property, value);
                    }
                    Console.WriteLine("\n");
                }*/
                return results;
            }
            catch
            {
                return null;
            }
        }

        private void WriteLog(String text)
        {
#if DEBUG
            if (Log != null)
                Log(text);
#endif
        }

    }
}
