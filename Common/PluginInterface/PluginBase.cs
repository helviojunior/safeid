using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;

namespace IAM.PluginInterface
{

    public abstract class PluginBase : IDisposable
    {
        // Informacoes do plugin
        public abstract Uri GetPluginId(); //interface://IAM/plugins/pluginbase
        public abstract String GetPluginName();
        public abstract PluginConfigFields[] GetConfigFields();
        public abstract String GetPluginDescription();

        public abstract Boolean ValidateConfigFields(Dictionary<String, Object> config, Boolean checkDirectoryExists, LogEvent Log, Boolean checkImport, Boolean checkDeploy);

        public abstract event LogEvent Log;
        public abstract event LogEvent2 Log2;

        public Boolean Equal(PluginBase p) { return p.GetPluginId().Equals(this.GetPluginId()); }

        public void Dispose()
        {

        }

        public static void FillConfig(PluginBase plugin, ref Dictionary<String, Object> config, String key, Object value)
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
                case PluginConfigTypes.Password:
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
                            case PluginConfigTypes.Password:
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
