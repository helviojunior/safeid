using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IAM.PluginInterface;
using System.Net;
using System.Web;
using SafeTrend.Json;
using SafeTrend.Xml;

namespace SeniorRH
{

    public class AknaPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "Senior RH V1.0 Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir Senior API Beta"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/seniorrh");
        }

        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("URL do servidor", "server_uri", "URL do servidor", PluginConfigTypes.Uri, true, @"http://localhost/"));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha", PluginConfigTypes.Password, true, ""));
            conf.Add(new PluginConfigFields("Número da Empresa no Senior (NumEmp)", "numemp", "Campo NumEmp no Senior", PluginConfigTypes.String, true, ""));
            
            return conf.ToArray();
        }


        public override PluginConnectorConfigActions[] GetConfigActions()
        {

            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();
            conf.Add(new PluginConnectorConfigActions("Adição/remoção em uma lista", "group", "Adicionar/remover o usuário em uma lista de usuários", "Nome da lista", "group_name", "Nome da lista que o usuário será adicionado/removido"));

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

            SeniorAPI api = new SeniorAPI(config["username"].ToString(), config["password"].ToString(), config["numemp"].ToString(), new Uri(config["server_uri"].ToString()));

            XML.DebugMessage dbgC = new XML.DebugMessage(delegate(String data, String debug)
            {
#if DEBUG
                iLog(this, PluginLogType.Information, "## JSON Debug message: " + data + debug);

#endif
            });

            try
            {

                List<Dictionary<String, String>> users = api.GetUsers(dbgC);
                Int32 cnt = 0;
                foreach(Dictionary<String, String> u in users)
                {

                    foreach(String key in u.Keys)
                    {
                        if (!ret.fields.ContainsKey(key))
                            ret.fields.Add(key, new List<string>());

                        ret.fields[key].Add(u[key]);
                    }

                    cnt++;
                    if (cnt >= 10)
                        break;
                }



                ret.success = true;
            }
            catch (Exception ex)
            {
                iLog(this, PluginLogType.Error, ex.Message);
                ret.success = false;
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

            String lastStep = "CheckInputConfig";

            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            StringBuilder debugLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            SeniorAPI api = new SeniorAPI(config["username"].ToString(), config["password"].ToString(), config["numemp"].ToString(), new Uri(config["server_uri"].ToString()));


            XML.DebugMessage dbgC = new XML.DebugMessage(delegate(String data, String debug)
            {

                debugLog.AppendLine("######");
                debugLog.AppendLine("## JSON Debug message: " + data);
                debugLog.AppendLine(debug);

#if DEBUG
                Log2(this, PluginLogType.Debug, 0, 0, "JSON Debug message: " + data, debug);
#endif
            });

            try
            {
                
                lastStep = "Resgata os colaboradores contratados nos ultimos 365 dias";

                List<Dictionary<String, String>> users = api.GetUsers(dbgC);

                foreach (Dictionary<String, String> u in users)
                {

                    PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                    foreach (String key in u.Keys)
                    {
                        if (key.ToLower() == "numcpf")
                        {
                            package.AddProperty(key, u[key].Replace("-", "").Replace(".", "").Replace(" ", ""), "string");
                        }
                        else
                        {
                            package.AddProperty(key, u[key], "string");
                        }
                    }

                    ImportPackageUser(package);
                }

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process import (" + lastStep + "): " + ex.Message);

                Log2(this, PluginLogType.Error, 0, 0, "Error on process import: " + ex.Message, "Last step: " + lastStep);
            }
            finally
            {

                if (logType != PluginLogType.Information)
                    processLog.AppendLine(debugLog.ToString());

                Log2(this, logType, 0, 0, "Import executed", processLog.ToString());
                processLog.Clear();
                processLog = null;

                debugLog.Clear();
                debugLog = null;
            }


        }

        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            String lastStep = "CheckInputConfig";

            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            StringBuilder debugLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            SeniorAPI api = new SeniorAPI(config["username"].ToString(), config["password"].ToString(), config["numemp"].ToString(), new Uri(config["server_uri"].ToString()));


            XML.DebugMessage dbgC = new XML.DebugMessage(delegate(String data, String debug)
            {

                debugLog.AppendLine("######");
                debugLog.AppendLine("## JSON Debug message: " + data);
                debugLog.AppendLine(debug);

#if DEBUG
                Log2(this, PluginLogType.Debug, 0, 0, "JSON Debug message: " + data, debug);
#endif
            });

            try
            {

                String importID = "ProcessImportAfterDeploy-" + Guid.NewGuid().ToString();

                lastStep = "Checa CPF no pacote";

                String cpf = "";

                //Busca o e-mail nas propriedades específicas desto usuário
                foreach (PluginConnectorBasePackageData dt in package.entiyData)
                    if (dt.dataName.ToLower() == "numcpf" && !String.IsNullOrEmpty(dt.dataValue.ToLower()))
                        cpf = dt.dataValue;

                //Busca o e-mail nas propriedades específicas deste plugin
                if ((cpf == null) || (cpf == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.pluginData)
                        if (dt.dataName.ToLower() == "numcpf" && !String.IsNullOrEmpty(dt.dataValue.ToLower()))
                            cpf = dt.dataValue;
                }

                //Se não encontrou o e-mail testa nas propriedades maracas como ID
                if ((cpf == null) || (cpf == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.ids)
                        if (dt.dataName.ToLower() == "numcpf" && !String.IsNullOrEmpty(dt.dataValue.ToLower()))
                            cpf = dt.dataValue;
                }

                //Se não encontrou o e-mail testa nas propriedades gerais
                if ((cpf == null) || (cpf == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.properties)
                        if (dt.dataName.ToLower() == "numcpf" && !String.IsNullOrEmpty(dt.dataValue.ToLower()))
                            cpf = dt.dataValue;
                }

#if DEBUG
                try
                {
                    String jData = JSON.Serialize<PluginConnectorBaseDeployPackage>(package);
                    if (package.password != "")
                        jData = jData.Replace(package.password, "Replaced for user security");

                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "Package data", jData);
                }
                catch { }
#endif

                if (cpf == "")
                {
                    String jData = "";

                    try
                    {
                        jData = JSON.Serialize<PluginConnectorBaseDeployPackage>(package);
                        if (package.password != "")
                            jData = jData.Replace(package.password, "Replaced for user security");
                    }
                    catch { }

                    logType = PluginLogType.Error;
                    processLog.AppendLine("CPF (numCpf) not found in properties list. " + jData);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "CPF (numCpf) not found in properties list", jData);
                    return;
                }

                lastStep = "Resgata informações do colaborador";

                List<Dictionary<String, String>> users = api.GetUserData(cpf);

                foreach (Dictionary<String, String> u in users)
                {

                    PluginConnectorBaseImportPackageUser packageImp = new PluginConnectorBaseImportPackageUser(importID);
                    foreach (String key in u.Keys)
                    {
                        if (key.ToLower() == "numcpf")
                        {
                            packageImp.AddProperty(key, u[key].Replace("-", "").Replace(".", "").Replace(" ", ""), "string");
                        }
                        else
                        {
                            packageImp.AddProperty(key, u[key], "string");
                        }
                    }

                    ImportPackageUser(packageImp);
                }

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process import (" + lastStep + "): " + ex.Message);

                Log2(this, PluginLogType.Error, 0, 0, "Error on process import: " + ex.Message, "Last step: " + lastStep);
            }
            finally
            {

                if (logType != PluginLogType.Information)
                    processLog.AppendLine(debugLog.ToString());

                Log2(this, logType, 0, 0, "Import executed", processLog.ToString());
                processLog.Clear();
                processLog = null;

                debugLog.Clear();
                debugLog = null;
            }

        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

        }


        public override void ProcessDelete(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {


        }


        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }
}
