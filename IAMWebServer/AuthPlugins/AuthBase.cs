using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Reflection;
using IAM.GlobalDefs;
using SafeTrend.Data;
using System.Data;
using IAM.Config;
using IAM.CA;

namespace IAM.AuthPlugins
{
    
    public abstract class AuthBase
    {
        // Informacoes do plugin
        public abstract Uri GetPluginId(); //auth://IAM/plugins/authbase
        public abstract String GetPluginName();
        public abstract AuthConfigFields[] GetConfigFields();
        public abstract String GetPluginDescription();

        public abstract Boolean ValidateConfigFields(Dictionary<String, Object> config, AuthEvent log);

        public abstract LoginResult Auth(IAMDatabase database, System.Web.UI.Page page);

        public abstract void Logout(IAMDatabase database, System.Web.UI.Page page);

        public abstract event AuthEvent Log;

        public Boolean Equal(AuthBase p) { return p.GetPluginId().Equals(this.GetPluginId()); }
        public Boolean Equal(Uri id) { return id.Equals(this.GetPluginId()); }

        public void SetLoginSession(System.Web.UI.Page page, LoginData data)
        {
            //Cria sessão e cookie do usuário
            try
            {
                //Adiciona o ciookie do usuário
                HttpCookie cookie = new HttpCookie("uid");
                //Define o valor do cookie
                cookie.Value = data.Id.ToString();
                //Time para expiração (1 min)
                DateTime dtNow = DateTime.Now;
                TimeSpan tsMinute = new TimeSpan(365, 0, 0, 0);
                cookie.Expires = dtNow + tsMinute;
                //Adiciona o cookie
                page.Response.Cookies.Add(cookie);
            }
            catch { }
            
            page.Session["login"] = data;

        }

        public LoginResult LocalAuth(IAMDatabase database, System.Web.UI.Page page, String username, String password, Boolean byPassPasswordCheck)
        {
            try
            {
                if ((username == null) || (username.Trim() == "") || (username == password) || (username.Trim() == ""))
                    return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));

                Int64 enterpriseId = 0;
                if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                    enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

                DbParameterCollection par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@login", typeof(String), username.Length).Value = username;

                DataTable tmp = null;


                tmp = database.ExecuteDataTable("select distinct id, alias, full_name, login, enterprise_id, password, must_change_password from vw_entity_logins with(nolock) where deleted = 0 and enterprise_id = @enterprise_id and locked = 0 and (login = @login or value = @login)", CommandType.Text, par);

                if ((tmp != null) && (tmp.Rows.Count > 0))
                {
                    foreach (DataRow dr in tmp.Rows)
                    {

                        using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(database.Connection, enterpriseId))
                        using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(dr["password"].ToString())))
                            if (byPassPasswordCheck || Encoding.UTF8.GetString(cApi.clearData) == password)
                            {
                                Random rnd = new Random();

                                LoginData l = new LoginData();
                                l.Alias = tmp.Rows[0]["alias"].ToString();
                                l.FullName = tmp.Rows[0]["full_name"].ToString();
                                l.Login = tmp.Rows[0]["login"].ToString();
                                l.Id = (Int64)tmp.Rows[0]["id"];
                                l.EnterpriseId = (Int64)tmp.Rows[0]["enterprise_id"];
                                l.SecurityToken = (Byte)rnd.Next(1, 255);

                                SetLoginSession(page, l);

                                database.ExecuteNonQuery("update entity set last_login = getdate() where id = " + l.Id, CommandType.Text, null);

                                database.AddUserLog(LogKey.User_Logged, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, l.Id, 0, MessageResource.GetMessage("user_logged") + " " + GetIPAddress(page), "{ \"ipaddr\":\"" + GetIPAddress(page) + "\"} ");

                                return new LoginResult(true, "User OK", (Boolean)tmp.Rows[0]["must_change_password"]);
                                break;
                            }
                            else
                            {
                                database.AddUserLog(LogKey.User_WrongPassword, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, MessageResource.GetMessage("user_wrong_password") + " " + GetIPAddress(page), "{ \"ipaddr\":\"" + GetIPAddress(page) + "\"} ");
                            }
                    }

                    return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));
                }
                else
                {
                    database.AddUserLog(LogKey.User_WrongUserAndPassword, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, MessageResource.GetMessage("user_wrong_password") + " " + GetIPAddress(page), "{ \"username\":\"" + username.Replace("'", "").Replace("\"", "") + "\", \"ipaddr\":\"" + GetIPAddress(page) + "\"} ");
                    return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));
                }

            }
            catch (Exception ex)
            {
                //Tools.Tool.notifyException(ex, page);
                return new LoginResult(false, "Internal error", ex.Message);
            }
            finally
            {

            }
        }

        private String GetIPAddress(System.Web.UI.Page page)
        {
            List<String> lIPAddress = new List<String>();

            if (!string.IsNullOrEmpty(page.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]))
            {
                lIPAddress.Add(page.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].Split(',')[0]);
            }

            if (!string.IsNullOrEmpty(page.Request.ServerVariables["REMOTE_ADDR"]))
            {
                lIPAddress.Add(page.Request.ServerVariables["REMOTE_ADDR"].Split(',')[0]);
            }

            return String.Join(", ", lIPAddress);
        }

        protected Dictionary<String, Object> GetAuthConfig(IAMDatabase database, System.Web.UI.Page page)
        {
            Dictionary<String, Object> config = new Dictionary<string, object>();

            Int64 enterpriseId = 0;
            if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
            par.Add("@plugin", typeof(String)).Value = this.GetPluginId().AbsoluteUri;

            DataTable conf = database.ExecuteDataTable("select distinct [key], [value] from dbo.enterprise_auth_par where enterprise_id = @enterprise_id and plugin = @plugin", CommandType.Text, par);
            if ((conf != null) && (conf.Rows.Count > 0))
            {
                foreach (DataRow dr in conf.Rows)
                    FillConfig(this, ref config, dr["key"].ToString(), dr["value"].ToString());
            }

            return config;
        }

        public static void FillConfig(AuthBase plugin, ref Dictionary<String, Object> config, String key, Object value)
        {
            /*if (!connectorConf.ContainsKey(d1[kCol]))
                connectorConf.Add(d1[kCol], d1[vCol].ToString());*/

            AuthConfigTypes type = AuthConfigTypes.String;
            List<AuthConfigFields> cfg = new List<AuthConfigFields>();
            AuthConfigFields[] tmp = plugin.GetConfigFields();
            if (tmp != null)
                cfg.AddRange(tmp);
            tmp = null;

            AuthConfigFields fCfg = cfg.Find(c => (c.Key.ToLower() == key));
            if (fCfg != null)
                type = fCfg.Type;

            switch (type)
            {
                case AuthConfigTypes.Boolean:
                case AuthConfigTypes.Uri:
                case AuthConfigTypes.Int32:
                case AuthConfigTypes.Int64:
                case AuthConfigTypes.DateTime:
                case AuthConfigTypes.String:
                case AuthConfigTypes.Password:
                    if (!config.ContainsKey(key))
                        config.Add(key, value);
                    break;

            }

        }

        protected Boolean CheckInputConfig(Dictionary<String, Object> config, AuthEvent Log)
        {

            if (config == null)
            {
                Log(this, AuthEventType.Error, "Config is null");
                return false;
            }

            foreach (AuthConfigFields conf in this.GetConfigFields())
            {
                if (conf.Required)
                {
                    if (!config.ContainsKey(conf.Key) || (config[conf.Key] == null) || (config[conf.Key].ToString().Trim() == ""))
                    {
                        Log(this, AuthEventType.Error, conf.Name + " not set");
                        return false;
                    }
                    else
                    {
                        //Realiza os testes de try
                        switch (conf.Type)
                        {
                            case AuthConfigTypes.Boolean:
                                try
                                {
                                    Boolean tst = Boolean.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, AuthEventType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case AuthConfigTypes.Uri:
                                try
                                {
                                    Uri tst = new Uri(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, AuthEventType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case AuthConfigTypes.Int32:
                                try
                                {
                                    Int32 tst = Int32.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, AuthEventType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case AuthConfigTypes.Int64:
                                try
                                {
                                    Int64 tst = Int64.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, AuthEventType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case AuthConfigTypes.DateTime:
                                try
                                {
                                    DateTime tst = DateTime.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, AuthEventType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case AuthConfigTypes.String:
                            case AuthConfigTypes.Password:
                                //Nada
                                break;

                        }
                    }
                }
            }

            return true;
        }


        public static AuthBase GetPlugin(Uri id)
        {
            AuthBase ret = null;

            List<AuthBase> tList = GetPlugins<AuthBase>(Assembly.GetExecutingAssembly());

            for (Int32 i = 0; i < tList.Count; i++)
                if (tList[i].Equal(id))
                    ret = tList[i];
                else
                    tList[i] = null;

            return ret;
        }


        public static List<T> GetPlugins<T>()
        {
            return GetPlugins<T>(Assembly.GetExecutingAssembly());
        }

        public static List<T> GetPlugins<T>(Byte[] rawAssembly)
        {
            List<T> tList = new List<T>();

            try
            {
                Assembly assembly = Assembly.Load(rawAssembly);
                tList.AddRange(GetPlugins<T>(assembly));
            }
            catch (Exception ex)
            {
                //Console.WriteLine("PluginManager error: " + ex.Message);
            }

            return tList;
        }

        public static List<T> GetPlugins<T>(string folder)
        {
            string[] files = Directory.GetFiles(folder, "*.dll");
            List<T> tList = new List<T>();

            foreach (string file in files)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(file);

                    tList.AddRange(GetPlugins<T>(assembly));
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("PluginManager error: " + ex.Message);
                }
            }

            return tList;
        }


        public static List<T> GetPlugins<T>(Assembly assembly)
        {
            List<T> tList = new List<T>();

            try
            {

                List<Type> types = new List<Type>();

                //Seleciona todos os tipos de todos os assemblies carregados
                //Filtrado se é classe e pelo nome do método desejado
                Assembly asm = Assembly.GetExecutingAssembly();
                try
                {
                    types.AddRange(from t in assembly.GetTypes()
                                   where t.IsClass
                                   select t
                                    );
                }
                catch { }

                foreach (Type type in types)
                {
                    if (!type.IsClass || type.IsNotPublic) continue;

                    Type baseType = type.BaseType;
                    while (baseType != null)
                    {
                        if (baseType.Equals(typeof(T)))
                        {
                            object obj = Activator.CreateInstance(type);
                            T t = (T)obj;
                            tList.Add(t);

                            break;
                        }

                        baseType = baseType.BaseType;
                    }

                }

                /*
                Type[] classes = assembly.GetTypes();

                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsNotPublic) continue;


                    if (type.BaseType.Equals(typeof(T))) //Primeiro nível
                    {
                        object obj = Activator.CreateInstance(type);
                        T t = (T)obj;
                        tList.Add(t);
                    }
                    else if ((type.BaseType.BaseType != null) && type.BaseType.BaseType.Equals(typeof(T))) //Segundo nível
                    {
                        object obj = Activator.CreateInstance(type);
                        T t = (T)obj;
                        tList.Add(t);
                    }

                }*/
            }
            catch (Exception ex)
            {
                //Console.WriteLine("PluginManager error: " + ex.Message);
            }

            return tList;
        }

    }
}
