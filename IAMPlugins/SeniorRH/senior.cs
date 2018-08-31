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

    public class SeniorPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "Senior RH V1.0 Plugin (Debug)"; }
        public override String GetPluginDescription() { return "Plugin para integragir Senior API Beta (Em debug, nao usar)"; }

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
                debugLog.AppendLine("## [" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] JSON Debug message: " + data);
                debugLog.AppendLine(debug);

            });

            try
            {

                lastStep = "Resgata os colaboradores contratados nos últimos 365 dias";

                List<Dictionary<String, String>> users = api.GetUsers(dbgC);

                if (users == null)
                    throw new Exception("User data is empty");


                foreach (Dictionary<String, String> u in users)
                {

                    StringBuilder userDebugLog = new StringBuilder();

                    //userDebugLog.AppendLine(debugLog.ToString());

                    try
                    {
                        userDebugLog.AppendLine("######");
                        userDebugLog.AppendLine("### User Data");
                        userDebugLog.AppendLine(JSON.Serialize<Dictionary<String, String>>(u));
                    }
                    catch { }

                    userDebugLog.AppendLine("");

                    String cNumCad = "";//Data de admissao

                    if (u.ContainsKey("numCad"))
                        cNumCad = u["numCad"];
                    else if (u.ContainsKey("numcad"))
                        cNumCad = u["numcad"];

                    PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                    userDebugLog.AppendLine("######");
                    userDebugLog.AppendLine("### Package id: " + package.pkgId);
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

                    userDebugLog.AppendLine("");

                    XML.DebugMessage userDbgC = new XML.DebugMessage(delegate(String data, String debug)
                    {

                        userDebugLog.AppendLine("######");
                        userDebugLog.AppendLine("## [" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] JSON Debug message: " + data);
                        userDebugLog.AppendLine(debug);

                    });


                    Dictionary<String, Dictionary<String, String>> cData = GetComplementatyData(api, u, userDbgC);
                    if (cData.ContainsKey(cNumCad))
                    {
                        foreach (String key in cData[cNumCad].Keys)
                        {
                            if (key.ToLower() == "numcpf")
                            {
                                package.AddProperty(key, cData[cNumCad][key].Replace("-", "").Replace(".", "").Replace(" ", ""), "string");
                            }
                            else
                            {
                                package.AddProperty(key, cData[cNumCad][key], "string");
                            }
                        }
                    }

#if DEBUG
                    Log2(this, PluginLogType.Debug, 0, 0, "Import debug log for pachage " + package.pkgId, userDebugLog.ToString());
#endif

                    ImportPackageUser(package);
                }

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process import (" + lastStep + "): " + ex.Message);


                if (ex is SafeTrend.Xml.ResultEmptyException)
                {
                    Log2(this, PluginLogType.Error, 0, 0, "Network erro or API lock error importing user data", ex.Message + Environment.NewLine + "Last step: " + lastStep);
                }
                else
                {


                    Log2(this, PluginLogType.Error, 0, 0, "Error on process import: " + ex.Message, "Last step: " + lastStep);
                }
            }
            finally
            {

#if DEBUG
                Log2(this, PluginLogType.Debug, 0, 0, "Import debug log", debugLog.ToString());
#endif

                if (logType != PluginLogType.Information)
                    processLog.AppendLine(debugLog.ToString());

                Log2(this, logType, 0, 0, "Import executed", processLog.ToString());
                processLog.Clear();
                processLog = null;

                debugLog.Clear();
                debugLog = null;
            }


        }

        private Dictionary<String, Dictionary<String, String>> GetComplementatyData(SeniorAPI api, Dictionary<String, String> user, XML.DebugMessage debugCallback = null)
        {
            Dictionary<String, Dictionary<String, String>> uData = new Dictionary<string, Dictionary<string, string>>();

            //Preeche as datas que detem contratacoes
            String numCpf = "";//Data de admissao

            if (user.ContainsKey("numCpf"))
                numCpf = user["numCpf"];
            else if (user.ContainsKey("numcpf"))
                numCpf = user["numcpf"];

            numCpf = numCpf.Replace("-", "").Replace(".", "").Replace(" ", "");

            if (!String.IsNullOrEmpty(numCpf))
            {

                try
                {
                    List<Dictionary<String, String>> usersData = api.GetComplememtaryByCPF(numCpf, debugCallback);

                    if (usersData == null)
                        return uData;

                    foreach (Dictionary<String, String> u in usersData)
                    {
                        String cNumCad = "";//Data de admissao

                        if (u.ContainsKey("numCad"))
                            cNumCad = u["numCad"];
                        else if (u.ContainsKey("numcad"))
                            cNumCad = u["numcad"];

                        if (!String.IsNullOrEmpty(cNumCad))
                        {
                            if (!uData.ContainsKey(cNumCad))
                                uData.Add(cNumCad, new Dictionary<string, string>(u));

                        }

                    }

                }
                catch (Exception ex)
                {
                    if (debugCallback != null)
                        debugCallback("Error getting complementary user data", ex.Message);

                    
                    if (ex is SafeTrend.Xml.ResultEmptyException)
                        throw ex;
                }
            }

            return uData;
        }

        private Dictionary<String, Dictionary<String, String>> GetComplementatyDataOld(SeniorAPI api, Dictionary<String, String> user, XML.DebugMessage debugCallback = null)
        {
            Dictionary<String, Dictionary<String, String>> uData = new Dictionary<string,Dictionary<string,string>>();
            List<String> dates = new List<String>();

            //Preeche as datas que detem contratacoes
            String datAdm = "";//Data de admissao

            if (user.ContainsKey("datAdm"))
                datAdm = user["datAdm"];
            else if (user.ContainsKey("datadm"))
                datAdm = user["datadm"];

            if (!String.IsNullOrEmpty(datAdm))
                if (!dates.Contains(datAdm))
                    dates.Add(datAdm);


            if (dates.Count == 0)
                return uData;

            //Resgata todos os colaboradores por cada um das datas
            foreach (String d in dates)
            {

                try
                {
                    List<Dictionary<String, String>> usersData = api.GetComplememtaryByDate(d, debugCallback);

                    if (usersData == null)
                        continue;

                    foreach (Dictionary<String, String> u in usersData)
                    {
                        String cNumCad = "";//Data de admissao

                        if (u.ContainsKey("numCad"))
                            cNumCad = u["numCad"];
                        else if (u.ContainsKey("numcad"))
                            cNumCad = u["numcad"];

                        if (!String.IsNullOrEmpty(cNumCad))
                        {
                            if (!uData.ContainsKey(cNumCad))
                                uData.Add(cNumCad, new Dictionary<string, string>(u));

                        }

                    }

                }
                catch(Exception ex) {
                    if (debugCallback != null)
                        debugCallback("Error getting complementary user data", ex.Message);

                    if (ex is SafeTrend.Xml.ResultEmptyException)
                        throw ex;
                }
            }
           
            return uData;
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

            });

            try
            {

                String importID = "ImpAfDep-" + Guid.NewGuid().ToString();

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

                List<Dictionary<String, String>> users = api.GetUserData(cpf, dbgC);

                if (users == null)
                    throw new Exception("User data is empty");

                foreach (Dictionary<String, String> u in users)
                {

                    String cNumCad = "";//Data de admissao

                    if (u.ContainsKey("numCad"))
                        cNumCad = u["numCad"];
                    else if (u.ContainsKey("numcad"))
                        cNumCad = u["numcad"];


                    PluginConnectorBaseImportPackageUser packageImp = new PluginConnectorBaseImportPackageUser(importID);
                    try
                    {
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


                        Dictionary<String, Dictionary<String, String>> cData = GetComplementatyData(api, u, dbgC);
                        if (cData.ContainsKey(cNumCad))
                        {
                            foreach (String key in cData[cNumCad].Keys)
                            {
                                if (key.ToLower() == "numcpf")
                                {
                                    packageImp.AddProperty(key, cData[cNumCad][key].Replace("-", "").Replace(".", "").Replace(" ", ""), "string");
                                }
                                else
                                {
                                    packageImp.AddProperty(key, cData[cNumCad][key], "string");
                                }
                            }
                        }


                    }
                    catch (Exception ex2)
                    {

                        processLog.AppendLine("Error: " + ex2.Message);
                        
                    }
                    finally
                    {

                        processLog.AppendLine("Import (after deploy) package generated:");
                        processLog.AppendLine("\tImport ID: " + importID);
                        processLog.AppendLine("\tPackage ID: " + packageImp.pkgId);
                        processLog.AppendLine("");
                        processLog.AppendLine("Package data:");
                        processLog.AppendLine(JSON.Serialize(packageImp));

                        ImportPackageUser(packageImp);
                    }



                }

            }
            catch (Exception ex)
            {
                
                logType = PluginLogType.Error;
                processLog.AppendLine("Error processing import (" + lastStep + "): " + ex.Message);

                if (ex is SafeTrend.Xml.ResultEmptyException)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Network erro or API lock error importing user data", ex.Message + Environment.NewLine + debugLog.ToString());
                }

                try
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error processing import after deploy: " + ex.Message, debugLog.ToString());
                }
                catch
                {
                    Log2(this, PluginLogType.Error, 0, 0, "Error processing import after deploy: " + ex.Message, debugLog.ToString());
                }
            }
            finally
            {
                
#if DEBUG
                processLog.AppendLine(debugLog.ToString());

                Log2(this, PluginLogType.Debug, 0, 0, "Import debug log", debugLog.ToString());

                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "Import debug log", debugLog.ToString());

#else
                if (logType != PluginLogType.Information)
                    processLog.AppendLine(debugLog.ToString());
#endif

                Log2(this, logType, package.entityId, package.identityId, "Import executed", processLog.ToString());

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
