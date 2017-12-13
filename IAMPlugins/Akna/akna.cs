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

namespace akna
{

    public class AknaPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "IAM Akna V1.0 Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir Akna API Beta"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/akna");
        }

        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha", PluginConfigTypes.Password, true, ""));
            conf.Add(new PluginConfigFields("Domínio de e-mail", "mail_domain", "Domínio de e-mail para filtro na publicação", PluginConfigTypes.String, false, ""));
            
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

            try
            {

                ret.fields.Add("email", new List<string>());
                ret.fields["email"].Add("E-mail do usuário");

                ret.fields.Add("sequencia", new List<string>());
                ret.fields["sequencia"].Add("Sequencia do usuário");

                ret.fields.Add("nome", new List<string>());
                ret.fields["nome"].Add("Nome do usuário");

                ret.fields.Add("idade", new List<string>());
                ret.fields["idade"].Add("Idade do usuário");

                ret.fields.Add("data_nascimento", new List<string>());
                ret.fields["data_nascimento"].Add("Data de nascimento do usuário");

                ret.fields.Add("sexo", new List<string>());
                ret.fields["sexo"].Add("Sexo do usuário");
                //ret.fields["sexo"].Add("M");
                //ret.fields["sexo"].Add("F");

                ret.fields.Add("endereco", new List<string>());
                ret.fields["endereco"].Add("Endereço do usuário");

                ret.fields.Add("complemento", new List<string>());
                ret.fields["complemento"].Add("Complemento do endereço do usuário");

                ret.fields.Add("bairro", new List<string>());
                ret.fields["bairro"].Add("Bairro do endereço do usuário");

                ret.fields.Add("cidade", new List<string>());
                ret.fields["cidade"].Add("Cidade do usuário");

                ret.fields.Add("estado", new List<string>());
                ret.fields["estado"].Add("Estado do usuário");

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
            
        }

        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            
        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            String lastStep = "CheckInputConfig";
            
            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            StringBuilder debugLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            AknaAPI akna = new AknaAPI(config["username"].ToString(), config["password"].ToString());


            XML.DebugMessage dbgC = new XML.DebugMessage(delegate(String data, String debug)
            {

                debugLog.AppendLine("######");
                debugLog.AppendLine("## JSON Debug message: " + data);
                debugLog.AppendLine(debug);

#if DEBUG
                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "JSON Debug message: " + data, debug);
#endif
            });


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

                //Busca o e-mail nas propriedades específicas desto usuário
                foreach (PluginConnectorBasePackageData dt in package.entiyData)
                    if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                        email = dt.dataValue;

                //Busca o e-mail nas propriedades específicas deste plugin
                if ((email == null) || (email == ""))
                {
                    foreach (PluginConnectorBasePackageData dt in package.pluginData)
                        if (dt.dataValue.ToLower().IndexOf("@" + mail_domain) > 1)
                            email = dt.dataValue;
                }

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

                Uri serverUri = new Uri("https://api.akna.com.br/emkt/int/integracao.php");

                CookieContainer cookie = new CookieContainer();

                lastStep = "Get groups";

                String tst = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<MAIN><FUNC TRANS=\"\" KEY=\"0ea001e9ca76917fcfaffacf5bad\"><RETURN ID=\"01\">Usuário e/ou senha inválidos</RETURN></FUNC></MAIN>";

                AknaListResponse tst2 = SafeTrend.Xml.XML.Deserialize<AknaListResponse>(tst);

                AknaListResponse listas = akna.GetData<AknaListResponse>("<main><emkt trans=\"11.02\"></emkt></main>", cookie, dbgC);

                //AknaListResponse listas = JSON.JsonWebRequest<AknaListResponse>(serverUri, getPostData(config["username"].ToString(), config["password"].ToString(), "<main><emkt trans=\"11.02\"></emkt></main>"), "application/x-www-form-urlencoded", null, "POST", cookie, dbgC);

                if ((listas == null) || (listas.EMKT == null) || (listas.EMKT.Listas == null) || (listas.EMKT.Listas.Count == 0))
                {
                    logType = PluginLogType.Error;

                    if ((listas != null) && (listas.FUNC != null) && (listas.FUNC._return != null) && (!String.IsNullOrEmpty(listas.FUNC._return[0].value)))
                        throw new Exception("Error retriving groups: " + listas.FUNC._return[0].value);
                    else
                        throw new Exception("Error retriving groups");

                }

                List<String> dbg = new List<string>();
                if ((listas.EMKT.Listas != null) && (listas.EMKT.Listas.Count > 0))
                    foreach (AknaListResponse.aknaLista.aknaListaItem l in listas.EMKT.Listas)
                        dbg.Add("Lista: " + l.name.ToString());


                
                lastStep = "Check groups/roles";
                List<String> grpIds = new List<String>();
                List<String> grpIdsRemove = new List<String>();
                
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
                                        if ((listas.EMKT.Listas != null) && (listas.EMKT.Listas.Count > 0))
                                            foreach (AknaListResponse.aknaLista.aknaListaItem l in listas.EMKT.Listas)
                                                if ((!String.IsNullOrEmpty(l.name)) && (l.name.ToLower() == act.actionValue.ToLower()))
                                                {
                                                    grpAddId = l.name;
                                                    grpIds.Add(grpAddId);
                                                }

                                        if (grpAddId == null)
                                        {
                                            processLog.AppendLine("List '"+ act.actionValue +"' not exists yet, creating...");
                                        }

                                        try
                                        {

                                            StringBuilder putXML = new StringBuilder();
                                            putXML.Append("<main><emkt trans=\"11.05\">");
                                            putXML.Append("<nome>" + act.actionValue + "</nome>");
                                            putXML.Append("<substituir>N</substituir>");
                                            putXML.Append("<destinatario codigo=\"" + package.login + "\">");
                                            putXML.Append("<nome>" + package.fullName.fullName + "</nome>");
                                            putXML.Append("<email>" + email + "</email>");

                                            putXML.Append("</destinatario></emkt></main>");

                                            AknaCommandResponse cmd = akna.GetData<AknaCommandResponse>(putXML.ToString(), cookie, dbgC);

                                            if ((cmd == null) || (cmd.EMKT == null) || (cmd.EMKT._return == null) || (cmd.EMKT._return.Count == 0) || (cmd.EMKT._return[0].id != "00"))
                                            {

                                                if ((cmd != null) && (cmd.EMKT != null) && (cmd.EMKT._return != null) && (!String.IsNullOrEmpty(cmd.EMKT._return[0].value)))
                                                    throw new Exception("Adding group " + act.actionValue + " by role " + act.roleName + ": " + cmd.EMKT._return[0].value);
                                                else
                                                    throw new Exception("Adding group " + act.actionValue + " by role " + act.roleName);

                                            }

                                            processLog.AppendLine("User added in group " + act.actionValue + " by role " + act.roleName);

                                        }
                                        catch (Exception ex)
                                        {

                                            StringBuilder putXML = new StringBuilder();
                                            putXML.Append("<main><emkt trans=\"11.05\">");
                                            putXML.Append("<nome>" + act.actionValue + "</nome>");
                                            putXML.Append("<substituir>N</substituir>");
                                            putXML.Append("<destinatario codigo=\"" + package.login + "\">");
                                            putXML.Append("<nome>" + package.fullName.fullName + "</nome>");
                                            putXML.Append("<email>" + email + "</email>");
                                            putXML.Append("</destinatario></emkt></main>");

                                            AknaCommandResponse cmd = akna.GetData<AknaCommandResponse>(putXML.ToString(), cookie, dbgC);

                                            if ((cmd == null) || (cmd.EMKT == null) || (cmd.EMKT._return == null) || (cmd.EMKT._return.Count == 0) || (cmd.EMKT._return[0].id != "00"))
                                            {

                                                if ((cmd != null) && (cmd.EMKT != null) && (cmd.EMKT._return != null) && (!String.IsNullOrEmpty(cmd.EMKT._return[0].value)))
                                                    throw new Exception("Adding group " + act.actionValue + " by role " + act.roleName + ": " + cmd.EMKT._return[0].value);
                                                else
                                                    throw new Exception("Adding group " + act.actionValue + " by role " + act.roleName);

                                            }

                                            processLog.AppendLine("User added in group " + act.actionValue + " by role " + act.roleName);
                                        }
                                    }
                                    else if (act.actionType == PluginActionType.Remove)
                                    {
                                        /*if ((groups != null) && (groups.Length > 0))
                                            foreach (emGroup g in groups)
                                                if ((!String.IsNullOrEmpty(g.name)) && (g.name.ToLower() == act.actionValue.ToLower()))
                                                {
                                                    grpIdsRemove.Add(g.id);
                                                    processLog.AppendLine("User removed from group " + act.actionValue + " by role " + act.roleName);
                                                }*/
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


            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process deploy (" + lastStep + "): " + ex.Message);

                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message, "Last step: " + lastStep);
            }
            finally
            {

                if (logType != PluginLogType.Information)
                    processLog.AppendLine(debugLog.ToString());

                Log2(this, logType, package.entityId, package.identityId, "Deploy executed", processLog.ToString());
                processLog.Clear();
                processLog = null;

                debugLog.Clear();
                debugLog = null;
            }

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
