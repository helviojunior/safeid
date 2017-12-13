using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;

namespace CAS.PluginInterface
{

    public abstract class CASConnectorBase
    {
        public Uri Service { get; internal set; }
        protected Dictionary<String, Object> Config { get; set; }
        protected DirectoryInfo BasePath { get; set; }
        public Object State { get; protected set; }

        // Informacoes do plugin
        public abstract Uri GetPluginId(); //interface://IAM/plugins/pluginbase
        public abstract String GetPluginName();
        public abstract PluginConfigFields[] GetConfigFields();

        //Valida através do plugin, ou seja consulta usuário e senha
        protected abstract CASTicketResult iGrant(CASTicketResult oldTicket, String username, String password);
        
        //Método para troca de senha
        public abstract CASChangePasswordResult ChangePassword(CASTicketResult ticket, String password);
        public abstract CASChangePasswordResult ChangePassword(CASUserInfo userInfo, String password);
        
        //Busca o usuário
        public abstract CASUserInfo FindUser(String username);

        public abstract event LogEvent Log;

        public CASConnectorBase()
        {

        }


        public CASConnectorBase(DirectoryInfo basePath)
        {
            SetStart(basePath, null, null, null);
        }


        public CASConnectorBase(DirectoryInfo basePath, Uri service, Dictionary<String, Object> config, Object state)
            :this()
        {
            SetStart(basePath, service, config, state);
        }

        public void SetStart(DirectoryInfo basePath, Uri service, Dictionary<String, Object> config, Object state)
        {
            this.Service = service;
            this.Config = config;
            this.BasePath = basePath;
            this.State = state;

        }

        //Valida através dos tokens existentes
        public CASTicketResult Grant(HttpCookie cookie, Boolean renew, Boolean warm)
        {

            if ((cookie == null) || (String.IsNullOrEmpty(cookie.Value)))
                return new CASTicketResult();

            return Grant(cookie.Value, renew, warm);

        }

        //Valida através dos tokens existentes
        public CASTicketResult Grant(String ticket, Boolean renew, Boolean warm)
        {
            if (renew)
                return new CASTicketResult("Renew selected");

            CASTicketResult tgc = CASTicketResult.GetToken(Path.Combine(BasePath.FullName, "tickets"), Service, ticket);
            if (warm && !tgc.CreateByCredentials)
                return new CASTicketResult();
            else
                return tgc;
        }

        public CASTicketResult Grant(String username, String password)
        {

            //Checa se ha um ticket para este usuário
            CASTicketResult t = CASTicketResult.GetToken(Path.Combine(BasePath.FullName, "tickets"), Service, null, username);
            if ((!t.Success) && (t.UserName != username))
                t = null;

            String checkLog = "";
            LogEvent l = new LogEvent(delegate(Object sender, PluginLogType type, String text)
            {
                if (type == PluginLogType.Error)
                    checkLog += text + Environment.NewLine;
            });

            this.Log += l;
            
            CASTicketResult ret = new CASTicketResult();
            try
            {

                if (!CheckInputConfig(this.Config, true, l))
                    throw new Exception("Erro on check configurarion", new Exception(checkLog));

                ret = iGrant(t, username, password);
            }
            catch { }
            
            this.Log -= l;            

            //Se houver um ticket para este usuário utiliza os mesmos tokens
            if ((t != null) && (t.Success) && (t.UserName == ret.UserName))
            {
                ret.GrantTicket = t.GrantTicket;
                ret.LongTicket = t.LongTicket;
                ret.ProxyTicket = t.ProxyTicket;
            }

            return ret;
        }

        public void DestroyTicket(HttpCookie cookie)
        {

            if ((cookie == null) || (String.IsNullOrEmpty(cookie.Value)))
                return;

            DestroyTicket(cookie.Value, null);
        }


        public void SaveTicket(CASTicketResult ticket)
        {
            ticket.SaveToFile(Path.Combine(BasePath.FullName, "tickets"));
        }


        public void DestroyTicket(String ticket, String username)
        {
            CASTicketResult t = CASTicketResult.GetToken(Path.Combine(BasePath.FullName, "tickets"), Service, ticket, username);
            if (t.Success)
                t.Destroy(BasePath.FullName);
        }

        /*
        public void DestroyUserInfo(String username)
        {
            CASUserInfo t = CASUserInfo.GetUserInfo(Path.Combine(BasePath.FullName, "uinfo"), Service, username);
            if (t.Success)
                t.Destroy(BasePath.FullName);
        }

        public void SaveUserInfo(CASUserInfo user)
        {
            user.SaveToFile(Path.Combine(BasePath.FullName, "uinfo"));
        }
        */

        public Boolean Equal(CASConnectorBase p) { return p.GetPluginId().Equals(this.GetPluginId()); }


        public static void FillConfig(CASConnectorBase plugin, ref Dictionary<String, Object> config, String key, Object value)
        {
            /*if (!connectorConf.ContainsKey(d1[kCol]))
                connectorConf.Add(d1[kCol], d1[vCol].ToString());*/

            PluginConfigTypes type = PluginConfigTypes.String;
            List<PluginConfigFields> cfg = new List<PluginConfigFields>();
            PluginConfigFields[] tmp = plugin.GetConfigFields();
            if (tmp != null)
                cfg.AddRange(tmp);
            tmp = null;

            PluginConfigFields fCfg = cfg.Find(c => (c.Key.ToLower() == key));
            if (fCfg != null)
                type = fCfg.Type;

            switch (type)
            {
                case PluginConfigTypes.Boolean:
                case PluginConfigTypes.Uri:
                case PluginConfigTypes.Int32:
                case PluginConfigTypes.Int64:
                case PluginConfigTypes.DateTime:
                case PluginConfigTypes.String:
                case PluginConfigTypes.Directory:
                case PluginConfigTypes.Base64FileData:
                    if (!config.ContainsKey(key))
                        config.Add(key, value);
                    break;

                case PluginConfigTypes.StringList:
                case PluginConfigTypes.StringFixedList:
                    if (!config.ContainsKey(key))
                        config.Add(key, new String[0]);

                    List<String> items = new List<String>();
                    items.AddRange((String[])config[key]);

                    if (!items.Contains((String)value))
                        items.Add((String)value);

                    config[key] = items.ToArray();

                    items.Clear();
                    items = null;

                    break;

            }

        }

        protected Boolean CheckInputConfig(Dictionary<String, Object> config, Boolean checkDirectoryExists, LogEvent Log)
        {
            return CheckInputConfig(config, checkDirectoryExists, Log, true, true);
        }

        protected Boolean CheckInputConfig(Dictionary<String, Object> config, Boolean checkDirectoryExists, LogEvent Log, Boolean checkImport, Boolean checkDeploy)
        {

            if (config == null)
            {
                Log(this, PluginLogType.Error, "Config is null");
                return false;
            }

            foreach (PluginConfigFields conf in this.GetConfigFields())
            {
                if (((checkImport) && (conf.ImportRequired)) || ((checkDeploy) && (conf.DeployRequired)))
                {
                    if (!config.ContainsKey(conf.Key) || (config[conf.Key] == null) || (config[conf.Key].ToString().Trim() == ""))
                    {
                        Log(this, PluginLogType.Error, conf.Name + " not set");
                        return false;
                    }
                    else
                    {
                        //Realiza os testes de try
                        switch (conf.Type)
                        {
                            case PluginConfigTypes.Boolean:
                                try
                                {
                                    Boolean tst = Boolean.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, PluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case PluginConfigTypes.Uri:
                                try
                                {
                                    Uri tst = new Uri(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, PluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case PluginConfigTypes.Int32:
                                try
                                {
                                    Int32 tst = Int32.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, PluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case PluginConfigTypes.Int64:
                                try
                                {
                                    Int64 tst = Int64.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, PluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case PluginConfigTypes.DateTime:
                                try
                                {
                                    DateTime tst = DateTime.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, PluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case PluginConfigTypes.String:
                                //Nada
                                break;

                            case PluginConfigTypes.Directory:
                                try
                                {
                                    DirectoryInfo tst = new DirectoryInfo(config[conf.Key].ToString());
                                    if (checkDirectoryExists && !tst.Exists)
                                        throw new DirectoryNotFoundException("Directory '" + config[conf.Key] + "' not found");
                                }
                                catch (Exception ex)
                                {
                                    Log(this, PluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case PluginConfigTypes.StringList:
                            case PluginConfigTypes.StringFixedList:
                                if (config[conf.Key] is String[])
                                    return true;
                                else
                                    return false;
                                break;

                            case PluginConfigTypes.Base64FileData:
                                try
                                {
                                    Byte[] tst = Convert.FromBase64String(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(this, PluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                        }
                    }
                }
            }

            return true;
        }

    }
}
