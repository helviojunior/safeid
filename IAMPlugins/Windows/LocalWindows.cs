using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Globalization;
using System.Collections.Specialized;
using System.Management;
using System.Text.RegularExpressions;

namespace Windows
{

    enum UserFlags
    {
        SCRIPT = 1,        // 0x1
        ACCOUNTDISABLE = 2,        // 0x2
        HOMEDIR_REQUIRED = 8,        // 0x8
        LOCKOUT = 16,       // 0x10
        PASSWD_NOTREQD = 32,       // 0x20
        PASSWD_CANT_CHANGE = 64,       // 0x40
        ENCRYPTED_TEXT_PASSWORD_ALLOWED = 128,      // 0x80
        TEMP_DUPLICATE_ACCOUNT = 256,      // 0x100
        NORMAL_ACCOUNT = 512,      // 0x200
        INTERDOMAIN_TRUST_ACCOUNT = 2048,     // 0x800
        WORKSTATION_TRUST_ACCOUNT = 4096,     // 0x1000
        SERVER_TRUST_ACCOUNT = 8192,     // 0x2000
        DONT_EXPIRE_PASSWD = 65536,    // 0x10000
        MNS_LOGON_ACCOUNT = 131072,   // 0x20000
        SMARTCARD_REQUIRED = 262144,   // 0x40000
        TRUSTED_FOR_DELEGATION = 524288,   // 0x80000
        NOT_DELEGATED = 1048576,  // 0x100000
        USE_DES_KEY_ONLY = 2097152,  // 0x200000
        DONT_REQUIRE_PREAUTH = 4194304,  // 0x400000
        PASSWORD_EXPIRED = 8388608,  // 0x800000
        TRUSTED_TO_AUTHENTICATE_FOR_DELEGATION = 16777216 // 0x1000000
    };

    class LocalWindows
    {
        private String _serverIP;
        private String _ldapBase;
        private String _username;
        private String _passwd;
        //private String _objectClassConta;
        //private String _userAttribute;
        //private String _passwdAtribute;
        private String _groupAtribute;
        //private Boolean _openned;
        DirectoryEntry _deRoot;

        public String GroupAttribute { get { return _groupAtribute; } set { _groupAtribute = value; } }
        public DirectoryEntry DirectoryEntryRoot { get { return _deRoot; } }

        public LocalWindows(String ServerIP)
        {
            _serverIP = ServerIP;
        }

        public LocalWindows(String ServerIP, String Username, String Password)
        {
            _serverIP = ServerIP;
            _username = Username;
            _passwd = Password;
        }

        public void Bind()
        {
            //_ldapBase = "WinNT://" + _serverIP;
            String step = "";
            try
            {
                _ldapBase = string.Format("WinNT://{0}/{1},user", _serverIP, _username);
                try
                {
                    step = "Auth secure";
                    _deRoot = new DirectoryEntry(_ldapBase, _username, _passwd, AuthenticationTypes.Secure);

                    object nativeObject = _deRoot.NativeObject;
                    step = "Set parent on auth secure";
                    _deRoot = _deRoot.Parent;
                    nativeObject = _deRoot.NativeObject;

                }
                catch(Exception ex)
                {
                    if ((ex is System.Runtime.InteropServices.COMException) && (((System.Runtime.InteropServices.COMException)ex).ErrorCode == -2147024891))
                    {
                        throw new Exception("Access denied on create DOM object (-2147024891/0x80070005)", ex);
                    }
                    else
                    {
                        step = "Auth";
                        _deRoot = new DirectoryEntry(_ldapBase, _username, _passwd);

                        object nativeObject = _deRoot.NativeObject;
                        step = "Set parent";
                        _deRoot = _deRoot.Parent;
                        nativeObject = _deRoot.NativeObject;
                    }
                }
            }
            catch (Exception ex3)
            {
#if DEBUG
                step += " " + _passwd;
#endif

                if ((ex3 is System.Runtime.InteropServices.COMException) && (((System.Runtime.InteropServices.COMException)ex3).ErrorCode == -2147024891))
                {
                    throw new Exception("Access denied on create DOM object (-2147024891/0x80070005)", ex3);
                }
                else
                {

                    String code = "";
                    if (ex3 is System.Runtime.InteropServices.COMException)
                    {
                        Byte[] tmp = BitConverter.GetBytes(((System.Runtime.InteropServices.COMException)ex3).ErrorCode);
                        Array.Reverse(tmp);
                        code = "0x" + BitConverter.ToString(tmp).Replace("-", "");
                    }


                    throw new Exception("Erro " + (code != "" ? "(" + code + ") " : "") + " on bind with path '" + _ldapBase + "' (" + step + ")", ex3);
                }
            }
        }


        public DirectoryEntry FindUser(String login)
        {
            return _Find(_deRoot, "User", login);
        }

        public DirectoryEntry FindGroup(String group)
        {
            return _Find(_deRoot, "Group", group);
        }


        public Boolean AddUser(String login, String password)
        {
            return AddUser(_deRoot, login, password);
        }

        public Boolean AddUser(DirectoryEntry container, String login, String password)
        {

            if (ObjectExists(login, "user", false))
                throw new Exception("Login already exists");


            DirectoryEntry _user = container.Children.Add(login, "user");

            //A primeira senha é gerada randomicamente para cumprir com todos os sequisitos de complexidade
            //Para evitar erro ao inserir o usuário
            String tPwd = IAM.Password.RandomPassword.Generate(16);
            _user.Invoke("SetPassword", (Object)tPwd);
            _user.CommitChanges();

            UserFlags flags = UserFlags.NORMAL_ACCOUNT | UserFlags.DONT_EXPIRE_PASSWD | UserFlags.PASSWD_CANT_CHANGE;

            _user.Invoke("Put", new object[] { "UserFlags", (Int32)flags });
            
            //Define a senha correta do usuário
            _user.Invoke("SetPassword", (Object)password);
            _user.CommitChanges();

            _user.Close();

            return true;
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

        public String FindOrCreateGroup(String groupName)
        {
            return FindOrCreateGroup(_deRoot, groupName);
        }

        public String FindOrCreateGroup(DirectoryEntry container, String groupName)
        {
            DirectoryEntry _group = null;
            try
            {
                _group = container.Children.Find(groupName, "group");
            }
            catch { }

            if (_group == null)
            {
                DirectoryEntry newchild = container.Children.Add(groupName, "group");
                //newchild.Properties["name"].Value = groupName;
                newchild.CommitChanges();

                _group = newchild;

            }

            return (_group != null ? _group.Name : "");
        }

        public Boolean AddUserToGroup(String UserName, String GroupName)
        {
            return AddUserToGroup(_deRoot, UserName, GroupName);
        }

        public Boolean AddUserToGroup(DirectoryEntry deRoot, String UserName, String GroupName)
        {
            try
            {
                DirectoryEntry _root = _Find(deRoot, "group", GroupName);
                DirectoryEntry _user = _Find(deRoot, "user", UserName);

                if (_root == null)
                    throw new Exception("Group '" + GroupName + "' not found");

                if (_root == null)
                    throw new Exception("User '" + UserName + "' not found");

                String userPath = String.Format(CultureInfo.CurrentUICulture, "WinNT://{0},user", _user.Name);
                _root.Invoke("Add", new object[] { userPath });

                _root.CommitChanges();
                _root.Close();
                return true;
            }
            catch { return false; }
        }

        public Boolean RemoveUserFromGroup(String UserName, String GroupName)
        {
            return RemoveUserFromGroup(_deRoot, UserName, GroupName);
        }

        public Boolean RemoveUserFromGroup(DirectoryEntry deRoot, String UserName, String GroupName)
        {
            try
            {
                DirectoryEntry _root = _Find(deRoot, "group", GroupName);
                DirectoryEntry _user = _Find(deRoot, "user", UserName);

                if (_root == null)
                    throw new Exception("Group '" + GroupName + "' not found");

                if (_root == null)
                    throw new Exception("User '" + UserName + "' not found");


                String userPath = String.Format(CultureInfo.CurrentUICulture, "WinNT://{0},user", _user.Name);
                _root.Invoke("Remove", new object[] { userPath });

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

            DirectoryEntry obj = _Find(baseDe, schemaClassName, name);
            if (obj != null)
                return true;
            else
                return false;
        }

        public List<DirectoryEntry> ListAllUsers()
        {
            List<DirectoryEntry> ret = new List<DirectoryEntry>();


            foreach (DirectoryEntry childEntry in _deRoot.Children)
                if (childEntry.SchemaClassName == "User")
                {
                    /*DirectoryEntry user = _Find(_deRoot, "User", childEntry.Name); //entry.Children.Find(usr, "User");

                    if (user == null)
                        continue;*/

                    ret.Add(childEntry);
                }


            return ret;
        }

        /*
        public List<DirectoryEntry> ListAllUsers()
        {
            List<DirectoryEntry> ret = new List<DirectoryEntry>();


            ConnectionOptions connOpt = new ConnectionOptions();
            connOpt.Username = _username;
            connOpt.Password = _passwd;

            ManagementScope scope = null;
            try
            {
                scope = new ManagementScope("\\\\" + _serverIP + "\\root\\cimv2");
                scope.Connect();
            }
            catch
            {
                scope = new ManagementScope("\\\\" + _serverIP + "\\root\\cimv2", connOpt);
                scope.Connect();
            }

            SelectQuery sQuery = new SelectQuery("Win32_UserAccount", "LocalAccount=True");
            ManagementObjectSearcher mSearcher = new ManagementObjectSearcher(scope, sQuery);
            List<object> AllUserlist = new List<object>();
            foreach (ManagementObject mgmtObj in mSearcher.Get())
            {
                AllUserlist.Add(mgmtObj["Name"]);
            }

            //Remove os usuários de sistema, deixando somente os outros
            sQuery = new SelectQuery("Win32_SystemUsers");
            mSearcher = new ManagementObjectSearcher(scope, sQuery);
            List<object> SysUserlist = new List<object>();
            Regex re = new Regex(".*Win32_UserAccount.Name=\"(.*)\".D.*", RegexOptions.None);

            foreach (ManagementObject mgmtObj in mSearcher.Get())
            {
                string SysUserName = re.Split(mgmtObj["PartComponent"].ToString())[1];
                SysUserlist.Add(SysUserName);
                AllUserlist.Remove(SysUserName);
            }



            foreach (String usr in AllUserlist)
            {

                DirectoryEntry user = _Find(_deRoot, "User", usr); //entry.Children.Find(usr, "User");

                if (user == null)
                    continue;

                ret.Add(user);
            }

            return ret;
        }*/

        private DirectoryEntry _Find(DirectoryEntry deRoot, String schemaClassName, String objectName)
        {
            DirectoryEntry _entry = null;
            try
            {
                /*
                foreach (DirectoryEntry de in deRoot.Children)
                {
                    if ((de.SchemaClassName.ToLower() == schemaClassName.ToLower()) && (de.Name.ToLower() == objectName.ToLower()))
                    {
                        _entry = de;
                        break;
                    }
                }*/

                _entry = deRoot.Children.Find(objectName, schemaClassName);
            }
            catch(Exception ex) {
                //throw new Exception("Error finding object '" + objectName + "' with schema class " + schemaClassName, ex);

                //Tenta de outro modo

                foreach (DirectoryEntry de in deRoot.Children)
                {
                    if ((de.SchemaClassName.ToLower() == schemaClassName.ToLower()) && (de.Name.ToLower() == objectName.ToLower()))
                    {
                        _entry = de;
                        break;
                    }
                }

            }

            return _entry;
        }


        
    }
}
