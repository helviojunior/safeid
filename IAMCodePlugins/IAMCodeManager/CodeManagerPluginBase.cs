using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;

namespace IAM.CodeManager
{
    public abstract class CodeManagerPluginBase
    {
        // Informacoes do plugin
        public abstract Uri GetPluginId(); //interface://IAM/codeplugins/pluginbase
        public abstract String GetPluginName();
        public abstract String GetPluginPrefix();
        public abstract String GetPluginDescription();

        public abstract CodePluginConfigFields[] GetConfigFields();

        public abstract Boolean ValidateConfigFields(Dictionary<String, Object> config);

        public abstract List<CodeData> ParseData(List<String> inputData);

        public event CodeLogEvent OnLog;

        public Boolean Equal(CodeManagerPluginBase p) { return p.GetPluginId().Equals(this.GetPluginId()); }

        public abstract Boolean iSendCode(Dictionary<String, Object> config, CodeData target, String code);

        public Boolean SendCode(Dictionary<String, Object> config, List<String> inputData, String codeHash, String code)
        {
            if (!ValidateConfigFields(config))
                return false;

            List<CodeData> iData = ParseData(inputData);
            foreach (CodeData c in iData)
                if (c.DataId.ToUpper() == codeHash.ToUpper())
                    return iSendCode(config, c, code);

            return false;
        }

        public void Log(CodePluginLogType type, String text)
        {
            if (OnLog != null)
                OnLog(this, type, text);
        }

        
        public static CodeManagerPluginBase GetPluginByData(List<CodeManagerPluginBase> plugins, List<String> inputData, String codeHash)
        {
            foreach (CodeManagerPluginBase p in plugins)
            {
                List<CodeData> pData = p.ParseData(inputData);
                if (pData.Exists(p1 => (p1.DataId.ToUpper() == codeHash.ToUpper())))
                    return p;
            }

            return null;
        }


        public static void FillConfig(CodeManagerPluginBase plugin, ref Dictionary<String, Object> config, String key, Object value)
        {

            CodePluginConfigTypes type = CodePluginConfigTypes.String;
            List<CodePluginConfigFields> cfg = new List<CodePluginConfigFields>();
            CodePluginConfigFields[] tmp = plugin.GetConfigFields();
            if (tmp != null)
                cfg.AddRange(tmp);
            tmp = null;

            CodePluginConfigFields fCfg = cfg.Find(c => (c.Key.ToLower() == key));
            if (fCfg != null)
                type = fCfg.Type;

            switch (type)
            {
                case CodePluginConfigTypes.Boolean:
                case CodePluginConfigTypes.Uri:
                case CodePluginConfigTypes.Int32:
                case CodePluginConfigTypes.Int64:
                case CodePluginConfigTypes.DateTime:
                case CodePluginConfigTypes.String:
                case CodePluginConfigTypes.Base64FileData:
                case CodePluginConfigTypes.Password:
                    if (!config.ContainsKey(key))
                        config.Add(key, value);
                    break;

            }

        }

        protected Boolean CheckConfig(Dictionary<String, Object> config)
        {

            if (config == null)
            {
                Log(CodePluginLogType.Error, "Config is null");
                return false;
            }

            foreach (CodePluginConfigFields conf in this.GetConfigFields())
            {
                if (conf.Required)
                {
                    if (!config.ContainsKey(conf.Key) || (config[conf.Key] == null) || (config[conf.Key].ToString().Trim() == ""))
                    {
                        Log(CodePluginLogType.Error, conf.Name + " not set");
                        return false;
                    }
                    else
                    {
                        //Realiza os testes de try
                        switch (conf.Type)
                        {
                            case CodePluginConfigTypes.Boolean:
                                try
                                {
                                    Boolean tst = Boolean.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(CodePluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case CodePluginConfigTypes.Uri:
                                try
                                {
                                    Uri tst = new Uri(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(CodePluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case CodePluginConfigTypes.Int32:
                                try
                                {
                                    Int32 tst = Int32.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(CodePluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case CodePluginConfigTypes.Int64:
                                try
                                {
                                    Int64 tst = Int64.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(CodePluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case CodePluginConfigTypes.DateTime:
                                try
                                {
                                    DateTime tst = DateTime.Parse(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(CodePluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
                                    return false;
                                }
                                break;

                            case CodePluginConfigTypes.String:
                            case CodePluginConfigTypes.Password:
                                //Nada
                                break;

                            case CodePluginConfigTypes.Base64FileData:
                                try
                                {
                                    Byte[] tst = Convert.FromBase64String(config[conf.Key].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Log(CodePluginLogType.Error, "Error on try value of '" + conf.Name + "': " + ex.Message);
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
