using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IAM.PluginInterface;
using System.Net;
using System.Web;
using SafeTrend.Json;

namespace Nexcore
{

    public class NCallPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "NexCoore NCall+ V1.0 Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir Nexcore NCall+"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/ncall");
        }

        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Servidor", "server", "URL do servidor", PluginConfigTypes.Uri, true, ""));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha", PluginConfigTypes.Password, true, ""));
            conf.Add(new PluginConfigFields("Supervisor", "supervisor", "Usuário integrados se tornarão supervisores?", PluginConfigTypes.Boolean, false, ""));

            return conf.ToArray();
        }


        public override PluginConnectorConfigActions[] GetConfigActions()
        {

            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();
            
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

            try
            {

                iLog(this, PluginLogType.Error, "Operação não implementada neste plugin");

                //if (!ret.fields.ContainsKey(property.PropertyName))
                //    ret.fields.Add(property.PropertyName, new List<string>());

                //ret.fields[property.PropertyName].Add(tmp2.ToString("yyyy-MM-dd HH:mm:ss"));

                //ret.success = true;
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
            //Plugin sem processo de importação
        }

        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            //Não implementado
        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            String lastStep = "CheckInputConfig";


            JSON.DebugMessage dbgC = new JSON.DebugMessage(delegate(String data, String debug)
            {
#if DEBUG
                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "JSON Debug message: " + data, debug);
#endif
            });

            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {
                lastStep = "Check info";

                String container = "";

                Uri apiUri = GetNcallUriFromConfig(config);

                if ((package.fullName == null) || (package.fullName.fullName.Trim() == ""))
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
                    processLog.AppendLine("IAM Full Name not found in properties list. " + jData);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Full Name not found in properties list", jData);
                    return;
                }

                lastStep = "Auth";

                //http://api.emailmanager.com/1.0/?method=authentLogin&domain=<subdomínio>&username=<usuário>&password=<senha>
                Uri serverUri = new Uri(apiUri, "/ncall/controle.php");

                CookieContainer cookie = new CookieContainer();
                String ret = JSON.TextWebRequest(new Uri(apiUri, "/ncall/controle.php"), "proxacao=login&params=" + HttpUtility.UrlEncode("usuario=" + config["username"] + "|senha=" + MD5Checksum(config["password"].ToString())) + "&usuario=" + config["username"] + "&senhaLogin=" + MD5Checksum(config["password"].ToString()), "application/x-www-form-urlencoded", null, "POST", cookie, dbgC);

                //Tenta localizar texto de que o login foi com sucesso
                if (ret.ToLower().IndexOf("troncomonitor.php") <= 0)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Login result is empty");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Login result is empty", "");
                    return;
                }
                

                String tst = "";

                /*
                emLogin[] login = JSON.JsonWebRequest<emLogin[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?method=authentLogin&language=en_US&output=json&domain=" + config["domain"].ToString() + "&username=" + config["username"].ToString() + "&password=" + config["password"].ToString()), null, "", null, "GET", cookie, dbgC);

                if ((login == null) || (login.Length == 0))
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Login result is empty");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Login result is empty", "");
                    return;
                }

                if (String.IsNullOrEmpty(login[0].apikey))
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Login error: " + login[0].message);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Login error: " + login[0].message, "");
                    return;
                }

                string apiKey = login[0].apikey;

                //Lista todas as pastas do sistema
                //editionFolders 
                //Esta parte não foi implementada pois a API não está funcionando
                //emailMonitorLogin[] login2 = JSON.JsonWebRequest<emailMonitorLogin[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=editionFolders&parent_id=0&language=en_US&output=json"), null, "", null, "GET", cookie);

                //Cria o 'container', se não houver
                //emailMonitorLogin[] login3 = JSON.JsonWebRequest<emailMonitorLogin[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=editionFolderCreate&parent_id=0&name="+ container +"&language=en_US&output=json"), null, "", null, "GET", cookie);

                lastStep = "Get groups";

                //Lista os grupos para vinculação de 'Role', caso o usuário não esteja em nenhuma role não será adicionado
                //groups 

                emGroup[] groups = JSON.JsonWebRequest<emGroup[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=groups&folder_id=0&parent_id=0&language=en_US&output=json&limit="+ Int32.MaxValue), null, "", null, "GET", cookie, dbgC);

                if (groups.Length == 1)
                {
                    if (groups[0].id == "")
                        throw new Exception("Error retriving groups");
                }

                
                /*
                //Exclui todos os grupos com nome SafeIDUsers
                if ((groups != null) && (groups.Length > 0))
                    foreach (emGroup g in groups)
                        if ((!String.IsNullOrEmpty(g.name)) && (g.name.ToLower() == "iamusers") && (Int32.Parse( g.id) > 55))
                        {
                            Object tst = JSON.JsonWebRequest<Object>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=groupDelete&group_id=" + g.id + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                            Console.WriteLine("Deleting " + g.id);
                        }
                * /

                String baseGrpId = "0";
                if ((groups != null) && (groups.Length > 0))
                    foreach (emGroup g in groups)
                        if ((!String.IsNullOrEmpty(g.name)) && (g.name.ToLower() == container.ToLower()))
                            baseGrpId = g.id;

                List<String> dbg = new List<string>();
                if ((groups != null) && (groups.Length > 0))
                    foreach (emGroup g in groups)
                        dbg.Add(g.ToString());


                if (baseGrpId == "0")
                {
                    //Cria o grupo Base com o nome do container
                    emGroupCreate[] grpCreate = JSON.JsonWebRequest<emGroupCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=groupCreate&folder_id=0&parent_id=0&name=" + container + "&description=" + container + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                    if ((grpCreate != null) && (grpCreate.Length > 0) && (!String.IsNullOrEmpty(grpCreate[0].id)))
                        baseGrpId = grpCreate[0].id;
                    else
                        baseGrpId = "0";

                    groups = JSON.JsonWebRequest<emGroup[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=groups&folder_id=0&parent_id=0&language=en_US&output=json&limit=" + Int32.MaxValue), null, "", null, "GET", cookie, dbgC);
                }

                dbg = new List<string>();
                if ((groups != null) && (groups.Length > 0))
                    foreach (emGroup g in groups)
                        dbg.Add(g.ToString());


                lastStep = "Get User";
                //Verifica se o usuário existe
                String userId = null;
                emUser[] user = JSON.JsonWebRequest<emUser[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contacts&email=" + email + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                if ((user != null) && (user.Length > 0) && (!String.IsNullOrEmpty(user[0].id)))
                {
                    //Encontrou
                    userId = user[0].id;

                }
                else
                {
                    lastStep = "Create User";

                    if ((package.locked) || (package.temp_locked))
                    {
                        logType = PluginLogType.Warning;
                        processLog.AppendLine("User not found in Mail Manager and user is locked. Accound not created");
                        Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found in Mail Manager and user is locked. Accound not created", "");
                        return;
                    }

                    //Cria
                    emUserCreate[] userCreate = JSON.JsonWebRequest<emUserCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactCreate&name=" + HttpUtility.UrlEncode(package.fullName.fullName) + "&email=" + email + "&groups_id=" + baseGrpId + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                    if ((userCreate != null) && (userCreate.Length > 0) && (!String.IsNullOrEmpty(userCreate[0].cid)))
                        userId = userCreate[0].cid;

                    processLog.AppendLine("User created on Email Manager");

                    /*
                    user = JSON.JsonWebRequest<emUser[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contacts&email=" + email + "&language=en_US&output=json"), null, "", null, "GET", cookie);
                    if ((user != null) && (user.Length > 0) && (!String.IsNullOrEmpty(user[0].id)))
                        userId = user[0].id;* /
                }

                if (userId == null)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Unknow erro on add user");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unknow erro on add user", "");
                    return;
                }

                //Atualiza os campos personalizados do usuario
                Dictionary<String, String> extraData = new Dictionary<String, String>();

                foreach (PluginConnectorBaseDeployPackageMapping m in fieldMapping)
                    if ((m.dataName.ToLower().IndexOf("extra_") != -1) && !extraData.ContainsKey(m.dataName.ToLower()))
                        extraData.Add(m.dataName.ToLower(), null);
                
                foreach (PluginConnectorBasePackageData dt in package.importsPluginData)
                    if (extraData.ContainsKey(dt.dataName.ToLower()) && extraData[dt.dataName.ToLower()] == null)
                    {
                        extraData[dt.dataName.ToLower()] = dt.dataValue;
#if DEBUG
                        processLog.AppendLine("1. extraData[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }

                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (extraData.ContainsKey(dt.dataName.ToLower()) && extraData[dt.dataName.ToLower()] == null)
                    {
                        extraData[dt.dataName.ToLower()] = dt.dataValue;
#if DEBUG
                        processLog.AppendLine("2. extraData[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }

                foreach (PluginConnectorBasePackageData dt in package.properties)
                    if (extraData.ContainsKey(dt.dataName.ToLower()) && extraData[dt.dataName.ToLower()] == null)
                    {
                        extraData[dt.dataName.ToLower()] = dt.dataValue;
#if DEBUG
                        processLog.AppendLine("3. extraData[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }


                String userExtraData = "";
                foreach (String k in extraData.Keys)
                {

                    if (userExtraData != "") userExtraData += "&";
                    userExtraData += k + "=" + HttpUtility.UrlEncode(extraData[k]);

#if DEBUG
                    processLog.AppendLine("4. extraData[" + k + "] = " + extraData[k]);
#endif
                }
                
                //emUserCreate[] userUpdate = JSON.JsonWebRequest<emUserCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactUpdate&cid=" + userId + "&name=" + HttpUtility.UrlEncode(package.fullName.fullName) + "&email=" + email + "&groups_id=" + baseGrpId + "&language=en_US&" + userExtraData + "&output=json"), null, "", null, "GET", cookie, dbgC);

                /*
                 * //Desabilitado este ponto de atualiza;c'ao e transferido para uma unica atualizacao final, juntamente com os grupos
                emUserCreate[] userUpdate = JSON.JsonWebRequest<emUserCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactUpdate&cid=" + userId + "&name=" + HttpUtility.UrlEncode(package.fullName.fullName) + "&email=" + email + "&language=en_US&" + userExtraData + "&output=json"), null, "", null, "GET", cookie, dbgC);
                if ((userUpdate != null) && (userUpdate.Length > 0) && (userUpdate[0].code != 0))
                {
                    processLog.AppendLine("Error updating user info: " + userUpdate[0].message);
                }

                processLog.AppendLine("User updated on Email Manager");* /


                lastStep = "Check groups/roles";
                List<String> grpIds = new List<String>();
                List<String> grpIdsRemove = new List<String>();
                grpIds.Add(baseGrpId);
                Boolean rebuildGrpList = false;

                //Busca os grupos que este usuário fará parte
                if ((package.pluginAction != null) && (package.pluginAction.Count > 0))
                {
                    foreach (PluginConnectorBaseDeployPackageAction act in package.pluginAction)
                        try
                        {
                            processLog.AppendLine("Role: " + act.roleName + " (" + act.actionType.ToString() + ") " + act.ToString());

                            switch (act.actionKey.ToLower())
                            {
                                case "group":
                                    if (act.actionType == PluginActionType.Add)
                                    {
                                        String grpAddId = null;
                                        if ((groups != null) && (groups.Length > 0))
                                            foreach (emGroup g in groups)
                                                if ((!String.IsNullOrEmpty(g.name)) && (g.name.ToLower() == act.actionValue.ToLower()))
                                                {
                                                    grpAddId = g.id;
                                                    grpIds.Add(grpAddId);
                                                }

                                        if (grpAddId == null)
                                        {
                                            emGroupCreate[] grpCreate = JSON.JsonWebRequest<emGroupCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=groupCreate&folder_id=0&parent_id=0&name=" + act.actionValue + "&description=" + act.actionValue + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                                            if ((grpCreate != null) && (grpCreate.Length > 0) && (!String.IsNullOrEmpty(grpCreate[0].id)))
                                            {
                                                rebuildGrpList = true;
                                                grpAddId = grpCreate[0].id;
                                                grpIds.Add(grpAddId);
                                                processLog.AppendLine("User added in group " + act.actionValue + " by role " + act.roleName);
                                            }
                                        }
                                        else
                                        {
                                            processLog.AppendLine("User added in group " + act.actionValue + " by role " + act.roleName);
                                        }

                                    }
                                    else if (act.actionType == PluginActionType.Remove)
                                    {
                                        if ((groups != null) && (groups.Length > 0))
                                            foreach (emGroup g in groups)
                                                if ((!String.IsNullOrEmpty(g.name)) && (g.name.ToLower() == act.actionValue.ToLower()))
                                                {
                                                    grpIdsRemove.Add(g.id);
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
                        }
                }


                //Remove o usuário dos grupos criados erroneamente
                //Remove de todos os grupos iniciados com "\" ou com o mesmo nome do container
                /*
                if ((!String.IsNullOrEmpty(package.container)) && (groups != null) && (groups.Length > 0))
                    foreach (emGroup g in groups)
                        if ((!String.IsNullOrEmpty(g.name)) && ((g.name.ToLower() == package.container.ToLower()) || (g.name.ToLower().Substring(0, 1) == "\\")))
                        {
                            grpIdsRemove.Add(g.id);
                            processLog.AppendLine("User removed from group " + package.container + " by container rule");
                        }
                * /

                grpIds.Remove("0");//Remove o grupo "zero" pois a API não o aceita

                lastStep = "Rebuild groups";
                if (rebuildGrpList) //Como alguns grupos fram criados, recarrega a listagem de grupos
                    groups = JSON.JsonWebRequest<emGroup[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=groups&folder_id=0&parent_id=0&language=en_US&output=json&limit=" + Int32.MaxValue), null, "", null, "GET", cookie, dbgC);


                lastStep = "Check final groups";
                //Checa a listagem de grupos deste usuário, remove somente os que foram explicitamente definidos pelo IM
                //Mantendo os grupos que foram adicionados pela console do mail manager
                List<String> finalGrps = new List<String>();
                finalGrps.AddRange(grpIds);

                emUserGroup[] userGroups = JSON.JsonWebRequest<emUserGroup[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactGroups&cid=" + userId + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                if ((userGroups != null) && (userGroups.Length > 0))
                    foreach (emUserGroup g in userGroups)
                        if ((!finalGrps.Contains(g.group_id)) && (!grpIdsRemove.Contains(g.group_id)))
                            finalGrps.Add(g.group_id);

                if (!finalGrps.Contains(baseGrpId))//Mantém o grupo base
                    finalGrps.Add(baseGrpId);

                finalGrps.Remove("0");//Remove o grupo "zero" pois a API não o aceita

                lastStep = "Update user info";

                //Atualiza as informações do usuário
                //A atualização somente dos grupos
                //JSON.JsonWebRequest<emUserCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactUpdate&cid=" + userId + "&groups_id=" + String.Join(",", finalGrps) + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);

                emUserCreate[] userUpdate = JSON.JsonWebRequest<emUserCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactUpdate&cid=" + userId + "&name=" + HttpUtility.UrlEncode(package.fullName.fullName) + "&email=" + email + "&groups_id=" + String.Join(",", finalGrps) + "&language=en_US&" + userExtraData + "&output=json"), null, "", null, "GET", cookie, dbgC);
                if ((userUpdate != null) && (userUpdate.Length > 0) && (userUpdate[0].code != 0))
                {
                    processLog.AppendLine("Error updating user info: " + userUpdate[0].message);
                }
                else
                {
                    processLog.AppendLine("User updated");
                }

                try
                {
                    lastStep = "Groups info";

                    //Texto informativo com grupos do usuário
                    List<String> grpName = new List<String>();
                    if ((groups != null) && (groups.Length > 0))
                        foreach (emGroup g in groups)
                            if (finalGrps.Contains(g.id) && (!grpName.Contains(g.name)))
                                grpName.Add(g.name);

                    processLog.AppendLine("User groups: " + (grpName.Count == 0 ? "None" : String.Join(", ", grpName)));

                    grpName.Clear();
                    grpName = null;

                }
                catch { }

                try
                {

                    lastStep = "End";

                    finalGrps.Clear();
                    finalGrps = null;

                    grpIds.Clear();
                    grpIds = null;

                    Array.Clear(groups, 0, groups.Length);
                    groups = null;

                    Array.Clear(userGroups, 0, userGroups.Length);
                    userGroups = null;
                }
                catch { }*/
            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process deploy (" + lastStep + "): " + ex.Message);
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message, "Last step: " + lastStep);
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

            JSON.DebugMessage dbg = new JSON.DebugMessage(delegate(String data, String debug)
            {
#if DEBUG
                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "JSON Debug message: " + data, debug);
#endif
            });

            if (!CheckInputConfig(config, true, Log))
                return;

            //contactDelete 


            String lastStep = "CheckInputConfig";

            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {
                lastStep = "Check info";

                String container = package.container;

                if (String.IsNullOrEmpty(container))
                    container = "IAMUsers";

                //Este plugin estava gerando milhares de listas indevidamente devido ao container
                //Desta forma foi fixado o container como sempre Sendo SafeIDUsers
                container = "IAMUsers";

                String email = "";



                String mail_domain = "";//config["mail_domain"].ToString();

                if ((config.ContainsKey("mail_domain")) && (!String.IsNullOrEmpty(config["mail_domain"].ToString())))
                    mail_domain = config["mail_domain"].ToString();

                //Busca o e-mail nas propriedades específicas deste plugin
                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                        email = dt.dataValue;

                //Se não encontrou o e-mail testa nas propriedades maracas como ID
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.ids)
                        if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                            email = dt.dataValue;
                }

                //Se não encontrou o e-mail testa nas propriedades gerais
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.properties)
                        if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                            email = dt.dataValue;
                }

                //Se não encontrou nenhum e-mail do dominio principal adiciona qualquer outro e-mail
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.properties)
                        if (dt.dataValue.ToLower().IndexOf("@") > 1)
                            email = dt.dataValue;
                }


                if (email == "")
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
                    processLog.AppendLine("IAM Email not found in properties list. " + jData);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Email not found in properties list.", jData);
                    return;
                }



                if ((package.fullName == null) || (package.fullName.fullName.Trim() == ""))
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
                    processLog.AppendLine("IAM Full Name not found in properties list. " + jData);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Full Name not found in properties list", jData);
                    return;
                }

                lastStep = "Auth";

                //http://api.emailmanager.com/1.0/?method=authentLogin&domain=<subdomínio>&username=<usuário>&password=<senha>
                Uri serverUri = new Uri("http://api.emailmanager.com/");

                CookieContainer cookie = new CookieContainer();
                emLogin[] login = JSON.JsonWebRequest<emLogin[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?method=authentLogin&language=en_US&output=json&domain=" + config["domain"].ToString() + "&username=" + config["username"].ToString() + "&password=" + config["password"].ToString()), null, "", null, "GET", cookie, dbg);

                if ((login == null) || (login.Length == 0))
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Login result is empty");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Login result is empty", "");
                    return;
                }

                if (String.IsNullOrEmpty(login[0].apikey))
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Login error: " + login[0].message);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Login error: " + login[0].message, "");
                    return;
                }

                string apiKey = login[0].apikey;


                JSON.DebugMessage dbgC = new JSON.DebugMessage(delegate(String data, String debug)
                {
#if DEBUG
                    Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "JSON Debug message: " + data, debug);
#endif
                });


                lastStep = "Get User";
                //Verifica se o usuário existe
                String userId = null;
                emUser[] user = JSON.JsonWebRequest<emUser[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contacts&email=" + email + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                if ((user != null) && (user.Length > 0) && (!String.IsNullOrEmpty(user[0].id)))
                {
                    //Encontrou
                    userId = user[0].id;

                }
                                
                if (userId == null)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("User not found");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "User not found", "");
                    return;
                }

                //Atualiza os campos personalizados do usuario

                emUserCreate[] userUpdate = JSON.JsonWebRequest<emUserCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactCancel&cid=" + userId + "&language=en_US&extra_89=teste001&output=json"), null, "", null, "GET", cookie, dbgC);
                if ((userUpdate != null) && (userUpdate.Length > 0) && (userUpdate[0].code != 0))
                {
                    processLog.AppendLine("Error cancelling user info: " + userUpdate[0].message);
                }

                processLog.AppendLine("User canceled on Email Manager");

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process delete (" + lastStep + "): " + ex.Message);
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process delete: " + ex.Message, "Last step: " + lastStep);
            }
            finally
            {
                Log2(this, logType, package.entityId, package.identityId, "Delete executed", processLog.ToString());
                processLog.Clear();
                processLog = null;
            }


        }

        private Uri GetNcallUriFromConfig(Dictionary<String, Object> config)
        {
            try
            {
                Uri tmp1 = new Uri(config["server"].ToString());

                return new Uri(tmp1.Scheme + "://" + tmp1.Host + (!tmp1.IsDefaultPort ? ":" + tmp1.Port : ""));

            }
            catch (Exception ex)
            {
                throw new Exception("Erro building NCall Uri", ex);
            }
        }


        private static String MD5Checksum(String data)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower();
        }


        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }
}
