using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IAM.PluginInterface;
using System.Net;
using System.Web;
using SafeTrend.Json;

namespace eCentry
{

    public class EmailManagerPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "IAM eCentry Email Manager V1.0 Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir eCentry Email Manager jSON API Beta"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/ecentryemailmanager");
        }

        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Domínio", "domain", "Domínio", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha", PluginConfigTypes.Password, true, ""));
            conf.Add(new PluginConfigFields("Resposta de segurança", "security_response", "Resposta de segurança", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Domínio de e-mail", "mail_domain", "Domínio de e-mail para filtro na publicação", PluginConfigTypes.String, false, ""));
            conf.Add(new PluginConfigFields("Mensagem de reativação", "mail_message", "Mensagem de reativação de e-mail em caso de solicitação de remoção de recebimento dos e-mails", PluginConfigTypes.String, false, ""));

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

                ret.fields.Add("id", new List<string>());
                ret.fields["id"].Add("Identificador interno do usuário no e-mail manager");

                ret.fields.Add("email", new List<string>());
                ret.fields["email"].Add("E-mail do usuário");

                ret.fields.Add("name", new List<string>());
                ret.fields["name"].Add("Nome do usuário");

                ret.fields.Add("description", new List<string>());
                ret.fields["description"].Add("Descrição do usuário");

                ret.fields.Add("statusemail", new List<string>());
                emUserStatusEmail[] values = (emUserStatusEmail[])Enum.GetValues(typeof(emUserStatusEmail)).Cast<emUserStatusEmail>();
                foreach (emUserStatusEmail v in values)
                    ret.fields["statusemail"].Add(v.ToString());

                ret.fields.Add("date_creation", new List<string>());
                ret.fields["date_creation"].Add("Data da criação");

                ret.fields.Add("date_modified", new List<string>());
                ret.fields["date_modified"].Add("data de modificação");

                ret.fields.Add("rating", new List<string>());
                for (Int32 r = 1; r <= 5; r++)
                    ret.fields["rating"].Add(r.ToString());

                ret.fields.Add("gender", new List<string>());
                ret.fields["gender"].Add("M");
                ret.fields["gender"].Add("F");

                ret.fields.Add("date_birth", new List<string>());
                ret.fields["date_birth"].Add("Data de aniversário do usuário");


                Uri serverUri = new Uri("http://api.emailmanager.com/");

                CookieContainer cookie = new CookieContainer();
                emLogin[] login = JSON.JsonWebRequest<emLogin[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?method=authentLogin&language=en_US&output=json&domain=" + config["domain"].ToString() + "&username=" + config["username"].ToString() + "&password=" + config["password"].ToString()), null, "", null, "GET", cookie, null);

                if ((login == null) || (login.Length == 0))
                {
                    throw new Exception("Login result is empty");
                }

                if (String.IsNullOrEmpty(login[0].apikey))
                {
                    throw new Exception("Login error: " + login[0].message);
                }

                string apiKey = login[0].apikey;

                String usersText = JSON.TextWebRequest(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contacts&language=en_US&output=json&begin=1&limit=20"), null, "", null, "GET", cookie, null);
                List<Dictionary<String, Object>> usersObject = ParseUsersJSON(usersText);

                foreach (Dictionary<String, Object> u in usersObject)
                {
                    foreach (String key in u.Keys)
                    {
                        switch (key.ToLower())
                        {
                            case "statusemail":
                                //nada
                                break;

                            default:
                                if (!ret.fields.ContainsKey(key))
                                    ret.fields.Add(key, new List<string>());

                                try
                                {
                                    ret.fields[key].Add(u[key].ToString());
                                }
                                catch { }
                                break;
                        }
                    }
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

        public List<Dictionary<String, Object>> ParseUsersJSON(String jsonData)
        {
            List<Dictionary<String, Object>> users = new List<Dictionary<String, Object>>();

            try
            {
                users = JSON.Deserialize2<List<Dictionary<String, Object>>>(jsonData);

            }
            catch
            {
                Dictionary<String, Object> user = new Dictionary<String, Object>();
                user = JSON.Deserialize2<Dictionary<String, Object>>(jsonData);
                users.Add(user);
            }

            return users;
                
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
            
            JSON.DebugMessage dbgC = new JSON.DebugMessage(delegate(String data, String debug)
            {

#if DEBUG
                Log(this, PluginLogType.Debug, "JSON Debug message: " + data + debug);
#endif
            });


            try
            {
                lastStep = "Check info";
                String mail_domain = "";//config["mail_domain"].ToString();

                if ((config.ContainsKey("mail_domain")) && (!String.IsNullOrEmpty(config["mail_domain"].ToString())))
                    mail_domain = config["mail_domain"].ToString();


                lastStep = "Auth";

                Uri serverUri = new Uri("http://api.emailmanager.com/");

                CookieContainer cookie = new CookieContainer();
                emLogin[] login = JSON.JsonWebRequest<emLogin[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?method=authentLogin&language=en_US&output=json&domain=" + config["domain"].ToString() + "&username=" + config["username"].ToString() + "&password=" + config["password"].ToString()), null, "", null, "GET", cookie, dbgC);

                if ((login == null) || (login.Length == 0))
                {
                    Log(this, PluginLogType.Error, "Login result is empty");
                    return;
                }

                if (String.IsNullOrEmpty(login[0].apikey))
                {
                    Log(this, PluginLogType.Error, "Login error: " + login[0].message);
                    return;
                }

                string apiKey = login[0].apikey;

                lastStep = "Get groups";

                emGroup[] groups = JSON.JsonWebRequest<emGroup[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=groups&folder_id=0&parent_id=0&language=en_US&output=json&limit=" + Int32.MaxValue), null, "", null, "GET", cookie, dbgC);

                if (groups != null && groups.Length == 1)
                {
                    if (groups[0].id == "")
                        throw new Exception("Error retriving groups");
                }

                lastStep = "Get Users";
                
                emContactCount[] contactCount = JSON.JsonWebRequest<emContactCount[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactCount&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                if ((contactCount != null) && (contactCount.Length > 0) && (contactCount[0].total > 0))
                {
                    Int64 total = contactCount[0].total;
                    Int64 offSet = 1;
                    Int64 size = 100;

                    do
                    {

                        String usersText = JSON.TextWebRequest(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contacts&language=en_US&output=json&begin=" + offSet + "&limit=" + size), null, "", null, "GET", cookie, dbgC);
                        List<Dictionary<String, Object>> usersObject = ParseUsersJSON(usersText);

                        ParseUsersInfo(importId, serverUri, fieldMapping, groups, usersObject, apiKey, cookie, dbgC);

                        offSet += size;

                    } while (offSet <= total);
                }
                else
                {
                    throw new Exception("Error retriving users count");
                }


            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, ex.Message);
            }
        }

        private void ParseUsersInfo(String importId, Uri serverUri, List<PluginConnectorBaseDeployPackageMapping> fieldMapping, emGroup[] groups, List<Dictionary<String, Object>> usersObject, String apiKey, CookieContainer cookie, JSON.DebugMessage dbgC)
        {

            foreach (Dictionary<String, Object> u in usersObject)
            {

                PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);

                /*
                foreach (emGroup g in groups)
                {
                    if (g.folder_id == u.folder_id)
                        package.container = g.name;
                }*/

                Int32 uID = 0;

                foreach (String key in u.Keys)
                {
                    try
                    {
                        switch (key.ToLower())
                        {
                            case "id":
                                uID = Int32.Parse(u[key].ToString());
                                package.AddProperty(key, u[key].ToString(), (fieldMapping.Exists(f => (f.dataName == key)) ? fieldMapping.Find(f => (f.dataName == key)).dataType : "string"));
                                break;


                            case "statusemail":
                                Int32 status = Int32.Parse(u[key].ToString());
                                emUserStatusEmail ems = (emUserStatusEmail)status;

                                package.AddProperty(key, ems.ToString(), (fieldMapping.Exists(f => (f.dataName == key)) ? fieldMapping.Find(f => (f.dataName == key)).dataType : "string"));
                                break;

                            default:
                                package.AddProperty(key, u[key].ToString(), (fieldMapping.Exists(f => (f.dataName == key)) ? fieldMapping.Find(f => (f.dataName == key)).dataType : "string"));
                                break;
                        }
                    }
                    catch { }
                }


                
                try
                {

                    emUserGroup[] userGroups = JSON.JsonWebRequest<emUserGroup[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactGroups&cid=" + uID + "&language=en_US&output=json&limit=" + Int32.MaxValue), null, "", null, "GET", cookie, dbgC);
                    if ((userGroups != null) && (userGroups.Length > 0))
                        foreach (emUserGroup ug in userGroups)
                            foreach (emGroup g in groups)
                                if (g.id == ug.group_id)
                                    package.AddGroup(g.name);
                }
                catch { }

                ImportPackageUser(package);
            }

        }

        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            String lastStep = "CheckInputConfig";

            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            StringBuilder importLog = new StringBuilder();

            JSON.DebugMessage dbgC = new JSON.DebugMessage(delegate(String data, String debug)
            {

                importLog.AppendLine("######");
                importLog.AppendLine("## JSON Debug message: " + data);
                importLog.AppendLine(debug);

#if DEBUG
                Log2(this, PluginLogType.Debug, package.entityId, package.identityId, "JSON Debug message: " + data, debug);
#endif
            });


            try
            {
                lastStep = "Check info";

                String email = "";
                String importId = Guid.NewGuid().ToString();

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


                if (String.IsNullOrEmpty(email))
                {
                    String jData = "";

                    try
                    {
                        jData = JSON.Serialize<PluginConnectorBaseDeployPackage>(package);
                        if (package.password != "")
                            jData = jData.Replace(package.password, "Replaced for user security");
                    }
                    catch { }

                    processLog.AppendLine("IAM Email not found in properties list. " + jData);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "IAM Email not found in properties list.", jData);
                    return;
                }



                lastStep = "Auth";

                Uri serverUri = new Uri("http://api.emailmanager.com/");

                CookieContainer cookie = new CookieContainer();
                emLogin[] login = JSON.JsonWebRequest<emLogin[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?method=authentLogin&language=en_US&output=json&domain=" + config["domain"].ToString() + "&username=" + config["username"].ToString() + "&password=" + config["password"].ToString()), null, "", null, "GET", cookie, dbgC);

                if ((login == null) || (login.Length == 0))
                {
                    processLog.AppendLine("Login result is empty");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Login result is empty", "");
                    return;
                }

                if (String.IsNullOrEmpty(login[0].apikey))
                {
                    processLog.AppendLine("Login error: " + login[0].message);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Login error: " + login[0].message, "");
                    return;
                }

                string apiKey = login[0].apikey;

                lastStep = "Get groups";

                emGroup[] groups = JSON.JsonWebRequest<emGroup[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=groups&folder_id=0&parent_id=0&language=en_US&output=json&limit=" + Int32.MaxValue), null, "", null, "GET", cookie, dbgC);

                if (groups != null && groups.Length == 1)
                {
                    if (groups[0].id == "")
                        throw new Exception("Error retriving groups");
                }

                lastStep = "Get User";

                String usersText = JSON.TextWebRequest(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contacts&email=" + email + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);

                List<Dictionary<String, Object>> usersObject = new List<Dictionary<string, object>>();
                try
                {
                    usersObject = ParseUsersJSON(usersText);
                }
                catch (Exception ex2)
                {
                    throw new Exception("Error persing JSON data (" + ex2.Message + ")");
                }

                try
                {
                    ParseUsersInfo(importId, serverUri, fieldMapping, groups, usersObject, apiKey, cookie, dbgC);
                }
                catch (Exception ex2)
                {
                    throw new Exception("Error persing user info (" + ex2.Message + ")");
                }

            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process import after deploy: " + ex.Message, "Last step: " + lastStep + Environment.NewLine + processLog.ToString() + importLog.ToString());
            }
            finally
            {

                processLog.Clear();
                processLog = null;

                importLog.Clear();
                importLog = null;
            }
        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            String lastStep = "CheckInputConfig";
            
            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            StringBuilder debugLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;


            JSON.DebugMessage dbgC = new JSON.DebugMessage(delegate(String data, String debug)
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

                //http://api.emailmanager.com/1.0/?method=authentLogin&domain=<subdomínio>&username=<usuário>&password=<senha>
                Uri serverUri = new Uri("http://api.emailmanager.com/");

                CookieContainer cookie = new CookieContainer();
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

                if (groups != null && groups.Length == 1)
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
                */

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
                emUserStatusEmail emailStatus = emUserStatusEmail.OK;

                emUser[] user = JSON.JsonWebRequest<emUser[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contacts&email=" + email + "&language=en_US&output=json"), null, "", null, "GET", cookie, dbgC);
                if ((user != null) && (user.Length > 0) && (!String.IsNullOrEmpty(user[0].id)))
                {
                    //Encontrou
                    userId = user[0].id;
                    emailStatus = user[0].statusemail;

                    processLog.AppendLine("User found: id = " + userId + ", e-mail = " + user[0].email + ", name = " + user[0].name);
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
                        userId = user[0].id;*/
                }

                if (userId == null)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Unknow erro on add user");
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Unknow erro on add user", "");
                    return;
                }


                if (emailStatus != emUserStatusEmail.OK)
                {
                    String msg = "";
                    switch (emailStatus)
                    {
                        case emUserStatusEmail.HardError:
                            msg = "Permanent error";
                            break;

                        case emUserStatusEmail.TempError:
                            msg = "Temporary error";
                            break;

                        case emUserStatusEmail.NotExists:
                            msg = "Not exists";
                            break;

                        case emUserStatusEmail.UserAbuseRequested:
                            msg = "User made an abuse report";
                            break;

                        case emUserStatusEmail.UserExclusionRequested:
                            msg = "User made an exclusion request";
                            break;
                    }

                    processLog.AppendLine("E-mail status error: " + msg);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "E-mail status error: " + msg, "");

                    try
                    {
                        //Tenta realizar a remoção do usuário da lista de exclusão
                        if ((email.ToLower().IndexOf(mail_domain.ToLower()) > 0) && ((emailStatus == emUserStatusEmail.UserExclusionRequested) || (emailStatus == emUserStatusEmail.UserAbuseRequested)))
                        {
                            String message = "";
                            try
                            {
                                message = config["mail_message"].ToString();
                            }
                            catch { }

                            if (String.IsNullOrEmpty(message))
                                message = "Solicitamos a reativação do recebimento de mensagens deste sistema";

                            Int32 count = 1;
                            String tmp = "";
                            CookieContainer cookie2 = new CookieContainer();
                            tmp = JSON.TextWebRequest(new Uri("http://" + config["domain"].ToString() + ".emailmanager.com/System/Auth/handler"), "username=" + config["username"].ToString() + "&password=" + config["password"].ToString(), "application/x-www-form-urlencoded", null, "POST", cookie2, dbgC);
                            
                            do
                            {
                                System.Threading.Thread.Sleep(count * 2);
                                
                                Int32 count2 = 1;
                                do
                                {
                                    tmp = JSON.TextWebRequest(new Uri("http://" + config["domain"].ToString() + ".emailmanager.com/System/Security-question/handler"), "securityResponse=" + config["security_response"].ToString(), "application/x-www-form-urlencoded", null, "POST", cookie2, dbgC);
                                    count2++;
                                
                                } while ((count2 < 5) && (tmp.ToUpper() == "ERROR"));

                                if (tmp.ToUpper() == "ERROR")
                                    throw new Exception("Incorrect security answer, please check Resouce x Plugin configurarion.");

                                tmp = JSON.TextWebRequest(new Uri("http://" + config["domain"].ToString() + ".emailmanager.com/"), "", "", null, "GET", cookie2, dbgC);

                                if (tmp.ToLower().IndexOf("pergunta") > 0)
                                {
                                    count++;
                                    continue;
                                }

                                tmp = JSON.TextWebRequest(new Uri("http://" + config["domain"].ToString() + ".emailmanager.com/BlackList/invite"), "id=" + userId + "&ac=1&message=" + HttpUtility.UrlEncode(message), "application/x-www-form-urlencoded", null, "POST", cookie2, dbgC);

                                count++;
                                
                            } while ((count < 5) && (tmp.ToLower().IndexOf("pergunta") > 0));

                            if (tmp.ToLower().IndexOf("pergunta") > 0)//Mesmo tendo a pergunta força uma tentativa 
                                tmp = JSON.TextWebRequest(new Uri("http://" + config["domain"].ToString() + ".emailmanager.com/BlackList/invite"), "id=" + userId + "&ac=1&message=" + HttpUtility.UrlEncode(message), "application/x-www-form-urlencoded", null, "POST", cookie2, dbgC);

                            emInviteResponse resp = JSON.Deserialize<emInviteResponse>(tmp);
                            if ((resp != null) && (resp.success))
                                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "Blacklist removal invite sent", "");
                            else
                                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Blacklist removal invite NOT sent", "");

                            //Resposta incorreta
                        }
                    }
                    catch (Exception ex1)
                    {
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Blacklist removal invite NOT sent", ex1.Message + debugLog.ToString());
                    }
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

                processLog.AppendLine("User updated on Email Manager");*/


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
                */

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


                //Verifica o status do contato
                userUpdate = JSON.JsonWebRequest<emUserCreate[]>(new Uri(serverUri.Scheme + "://" + serverUri.Host + ":" + serverUri.Port + "/1.0/?apikey=" + apiKey + "&method=contactInfo&cid=" + userId + "&language=en_US&" + userExtraData + "&output=json"), null, "", null, "GET", cookie, dbgC);
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
                catch { }
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



        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }
}
