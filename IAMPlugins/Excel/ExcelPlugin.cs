using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.IO;
using IAM.PluginInterface;
using SafeTrend.Json;

namespace Excel
{
    public class ExcelPlugin : PluginConnectorBase
    {
        public override String GetPluginName() { return "Microsoft Excel spreadsheet connector"; }
        public override String GetPluginDescription() { return "Plugin para integragir com arquivos Excel connector"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/excel");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Dir. Importação", "import_folder", "Diretório de importação", PluginConfigTypes.String, true, @"c:\IAMProxy\excelimport"));
            conf.Add(new PluginConfigFields("Nome da planilha", "sheet", "Nome da planilha a ser importada", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Coluna de bloqueio", "lock_column", "Nome da coluna no banco de dados que contém o status do usuário (bloqueado/desbloqueado)", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Valor de bloqueado", "locked_value", "Valor a ser inserido na cooluna de status/bloqueaio em caso de bloqueado", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Valor de desbloqueado", "unlocked_value", "Valor a ser inserido na cooluna de status/bloqueaio em caso de desbloqueado", PluginConfigTypes.String, false, ","));
            

            return conf.ToArray();
        }


        public override PluginConnectorConfigActions[] GetConfigActions()
        {
            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();
            //conf.Add(new PluginConnectorConfigActions("Executa Stored Procedure", "procedure", "Executa uma 'Stored procedure'", "Procedure", "procedure_name", "Procedure a ser executada", macro));
            //conf.Add(new PluginConnectorConfigActions("Executa SQL", "sql", "Executa uma sql (não pode ser procedure)", "SQL", "sql", "Comando SQL a ser executado", macro));

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


            String table = config["sheet"].ToString();
            table = table.Trim("$ []".ToCharArray());
                        
            try
            {

                
                DirectoryInfo importDir = null; ;
                try
                {
                    importDir = new DirectoryInfo(config["import_folder"].ToString());
                    if (!importDir.Exists)
                        throw new DirectoryNotFoundException();
                }
                catch (Exception ex)
                {
                    iLog(this, PluginLogType.Error, "Erro ao localizar o diretório de importação (" + config["import_folder"].ToString() + "): " + ex.Message);
                    return ret;
                }

                foreach (FileInfo f in importDir.GetFiles("*.xls"))
                {


                    iLog(this, PluginLogType.Information, "Iniciando mapeamento do arquivo '" + f.Name + "'");

                    OdbcDB db = null;
                    try
                    {
                        db = new OdbcDB(f);
                        db.openDB();


                        iLog(this, PluginLogType.Error, "Listatando schema da tabela...");
                        DataTable dtSchema = db.GetSchema(table);

                        if (dtSchema == null)
                            throw new Exception("Erro ao listar o schema da tabela: " + db.LastError);

                        try
                        {
                            foreach (DataColumn dc in dtSchema.Columns)
                            {
                                if (!ret.fields.ContainsKey(dc.ColumnName))
                                {

                                    try
                                    {
                                        ret.fields.Add(dc.ColumnName, new List<string>());

                                        iLog(this, PluginLogType.Information, "Column " + dc.ColumnName + ": DataType=" + dc.DataType.ToString() + ", MaxLength=" + dc.MaxLength + ", AllowDBNull=" + dc.AllowDBNull);
                                    }
                                    catch (Exception ex)
                                    {
                                        iLog(this, PluginLogType.Information, "Column " + dc.ColumnName + ": error on get data -> " + ex.Message);
                                    }

                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            iLog(this, PluginLogType.Error, "Erro ao listar as colunas: " + ex.Message);
                        }


                        String sql = "select * from [" + table + "$]";

                        DataTable dtSource = db.Select(sql, 0, 10);

                        if (dtSource == null)
                            throw new Exception("Erro on select: " + db.LastError);

                        try
                        {
                            foreach (DataColumn dc in dtSource.Columns)
                            {
                                if (!ret.fields.ContainsKey(dc.ColumnName))
                                    ret.fields.Add(dc.ColumnName, new List<string>());
                            }

                        }
                        catch (Exception ex)
                        {
                            iLog(this, PluginLogType.Error, "Erro ao listar as colunas: " + ex.Message);
                        }

                        Int32 qty = 0;

                        foreach (DataRow dr in dtSource.Rows)
                        {
                            qty++;

                            if (qty < 10)
                                break;

                            String regId = Guid.NewGuid().ToString();

                            try
                            {
                                foreach (DataColumn dc in dtSource.Columns)
                                {
                                    if (!ret.fields.ContainsKey(dc.ColumnName))
                                        ret.fields.Add(dc.ColumnName, new List<string>());

                                    ret.fields[dc.ColumnName].Add(dr[dc.ColumnName].ToString());
                                }

                            }
                            catch (Exception ex)
                            {
                                iLog(this, PluginLogType.Error, "Erro ao importar o registro: " + ex.Message);
                            }
                        }

                        ret.success = true;
                    }
                    catch (Exception ex)
                    {
                        iLog(this, PluginLogType.Error, "Falha ao mapear os dados do arquivo '" + f.Name + "': " + ex.Message);
                    }
                    finally
                    {
                        if (db != null)
                            db.closeDB();
                    }
                }
                                
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
            if (!CheckInputConfig(config, true, Log))
                return;


            String table = config["sheet"].ToString();
            table = table.Trim("$ []".ToCharArray());

            try
            {

                DirectoryInfo importDir = null; ;
                try
                {
                    importDir = new DirectoryInfo(config["import_folder"].ToString());
                    if (!importDir.Exists)
                        throw new DirectoryNotFoundException();
                }
                catch (Exception ex)
                {
                    Log2(this, PluginLogType.Error, 0, 0, "Erro ao localizar o diretório de importação (" + config["import_folder"].ToString() + ")", ex.Message);
                    return;
                }

                foreach (FileInfo f in importDir.GetFiles("*.xls"))
                {

                    OdbcDB db = null;
                    try
                    {
                        db = new OdbcDB(f);
                        db.openDB();

                        String sql = "select * from [" + table + "$]";

                        DataTable dtSource = db.Select(sql);

                        if (dtSource == null)
                            throw new Exception("Erro on select: " + db.LastError);

                        foreach (DataRow dr in dtSource.Rows)
                        {
                            PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                            try
                            {
                                foreach (DataColumn dc in dtSource.Columns)
                                    package.AddProperty(dc.ColumnName, dr[dc.ColumnName].ToString(), dc.DataType.ToString());

                                ImportPackageUser(package);
                            }
                            catch (Exception ex)
                            {
                                Log(this, PluginLogType.Error, "Erro ao importar o registro: " + ex.Message);
                            }
                            finally
                            {
                                package.Dispose();
                                package = null;
                            }
                        }


                    }
                    catch (Exception ex)
                    {
                        Log2(this, PluginLogType.Error, 0, 0, "Falha ao mapear os dados do arquivo '" + f.Name + "'", ex.Message);
                    }
                    finally
                    {
                        if (db != null)
                            db.Dispose();
                    }


                    f.MoveTo(f.FullName + ".imported");

                    Log(this, PluginLogType.Information, "Importação do arquivo '" + f.Name + "' concluida");

                }


            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, 0, 0, "Error on process import: " + ex.Message, "");
                Log(this, PluginLogType.Error, ex.Message);
            }

        }

        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            //Não implementado
        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            if (!CheckInputConfig(config, true, Log))
                return;

            DirectoryInfo importDir = null; ;
            try
            {
                importDir = new DirectoryInfo(Path.Combine(config["import_folder"].ToString(), "out"));
                if (!importDir.Exists)
                    importDir.Create();
            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, 0, 0, "Erro ao criar o diretório de importação (" + config["import_folder"].ToString() + "\\Out\\)", ex.Message);
                return;
            }

            FileInfo f = new FileInfo(Path.Combine(importDir.FullName, "export" + DateTime.Now.ToString("yyyyMMddHHmmss-ffffff") + ".xlsx"));

            if (!f.Directory.Exists)
                f.Directory.Create();


            String table = "Export " + DateTime.Now.ToString("HHmmss");

            String lock_column = (config.ContainsKey("lock_column") ? config["lock_column"].ToString().ToLower() : null);
            String locked_value = (config.ContainsKey("locked_value") ? config["locked_value"].ToString().ToLower() : null);
            String unlocked_value = (config.ContainsKey("unlocked_value") ? config["unlocked_value"].ToString().ToLower() : null);

            OdbcDB db = null;
            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {

                List<String> columnNames = new List<String>();
                foreach (PluginConnectorBaseDeployPackageMapping m in fieldMapping)
                    if (!columnNames.Contains(m.dataName))
                        columnNames.Add(m.dataName);

                if ((!String.IsNullOrEmpty(lock_column)) && (!columnNames.Contains(lock_column)))
                    columnNames.Add(lock_column);

                db = new OdbcDB(f);
                db.createAndOpenDB(table, columnNames);

                List<String> prop = new List<String>();

                String login = package.login;

                //Resgata a restutura da tabela de destino
                DataTable dtInsertSchema = db.GetSchema(table);

                table = dtInsertSchema.TableName;

                //Monta o where
                OleDbParameterCollection par = OdbcDB.GetSqlParameterObject();

                //Monta todos os campos que serão inseridos/atualizados
                Dictionary<String, String> data = new Dictionary<String, String>();

                foreach (DataColumn dc in dtInsertSchema.Columns)
                    if (!data.ContainsKey(dc.ColumnName.ToLower()))
                        data.Add(dc.ColumnName.ToLower(), null);

                if (data.ContainsKey("locked"))
                    data["locked"] = (package.locked || package.temp_locked ? "1" : "0");

                DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "lock_column = " + (String.IsNullOrEmpty(lock_column) ? "empty" : lock_column), "");
                DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "locked_value = " + (String.IsNullOrEmpty(locked_value) ? "empty" : locked_value), "");
                DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "unlocked_value = " + (String.IsNullOrEmpty(unlocked_value) ? "empty" : unlocked_value), "");

                if ((lock_column != null) && (data.ContainsKey(lock_column)))
                {
                    if ((package.locked || package.temp_locked) && (!String.IsNullOrEmpty(locked_value)))
                        data[lock_column] = locked_value;
                    else if ((!package.locked && !package.temp_locked) && (!String.IsNullOrEmpty(unlocked_value)))
                        data[lock_column] = unlocked_value;
                    else
                        data[lock_column] = (package.locked || package.temp_locked ? "1" : "0");

                    DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "data[lock_column] = " + data[lock_column], "");

                }

                String password_column = "";
                foreach (PluginConnectorBaseDeployPackageMapping m in fieldMapping)
                    if (m.isPassword && data.ContainsKey(m.dataName))
                    {
                        data[m.dataName] = package.password;
                        password_column = m.dataName;
                    }
                    else if (m.isLogin && data.ContainsKey(m.dataName))
                        data[m.dataName] = package.login;
                    else if (m.isName && data.ContainsKey(m.dataName))
                        data[m.dataName] = package.fullName.fullName;

                /*if (login_column != null && data.ContainsKey(login_column))
                    data[login_column] = package.login;

                if (password_column != null && data.ContainsKey(password_column))
                    data[password_column] = package.password;

                if (name_column != null && data.ContainsKey(name_column))
                    data[name_column] = package.fullName.fullName;*/


                foreach (PluginConnectorBasePackageData dt in package.importsPluginData)
                    if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                    {
                        data[dt.dataName.ToLower()] = dt.dataValue;
                        //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "1. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("1. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }

                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                    {
                        data[dt.dataName.ToLower()] = dt.dataValue;
                        //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "2. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("2. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }

                foreach (PluginConnectorBasePackageData dt in package.properties)
                    if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                    {
                        data[dt.dataName.ToLower()] = dt.dataValue;
                        //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "3. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("3. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }


                foreach (String k in data.Keys)
                {
                    //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "4. data[" + k + "] = " + data[k], "");
#if DEBUG
                    processLog.AppendLine("4. data[" + k + "] = " + data[k]);
#endif
                }


                LogEvent dbExecLog = new LogEvent(delegate(Object sender, PluginLogType type, String text)
                {
                    processLog.AppendLine(text);
                });


                //Não existe, cria

                if ((data.ContainsKey(password_column)) && (package.password == ""))
                {
                    package.password = IAM.Password.RandomPassword.Generate(16);
                    data[password_column] = package.password;
                    processLog.AppendLine("User not found in AD and IAM Password not found in properties list, creating a random password (" + package.password + ")");
                }

                //Limpa os parâmetros
                par.Clear();

                List<String> c1 = new List<string>();
                List<String> c2 = new List<string>();
                foreach (DataColumn dc in dtInsertSchema.Columns)
                    if (data.ContainsKey(dc.ColumnName.ToLower()))
                    {
                        if (!String.IsNullOrWhiteSpace(data[dc.ColumnName.ToLower()]))
                        {
                            if (dc.DataType.Equals(typeof(String)))
                            {
                                String txt = Corte((data[dc.ColumnName.ToLower()] != null ? data[dc.ColumnName.ToLower()] : ""), dc.MaxLength);
                                par.Add("@" + dc.ColumnName, GetDBType(dc.DataType), txt.Length).Value = txt;
                            }
                            else
                                par.Add("@" + dc.ColumnName, GetDBType(dc.DataType)).Value = data[dc.ColumnName.ToLower()];

                            c1.Add(dc.ColumnName);
                            c2.Add("@" + dc.ColumnName);
                        }
                    }


                foreach (OleDbParameter p in par)
                {
                    //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "6. par[" + p.ParameterName + "] = " + p.Value, "");
#if DEBUG
                    processLog.AppendLine("6. par[" + p.ParameterName + "] = " + p.Value);
#endif
                }
                
                String insert = "insert into [" + table + "] (" + String.Join(",", c1) + ") values (" + String.Join(",", c2) + ")";

                db.OnLog += dbExecLog;
                db.ExecuteNonQuery(insert, CommandType.Text, par);
                db.OnLog -= dbExecLog;

                NotityChangeUser(this, package.entityId);

                processLog.AppendLine("User added");
                
                /*
                //Executa as ações do RBAC
                if ((package.pluginAction != null) && (package.pluginAction.Count > 0))
                {
                    foreach (PluginConnectorBaseDeployPackageAction act in package.pluginAction)
                        try
                        {
                            switch (act.actionKey.ToLower())
                            {
                                case "procedure":
                                    String sql1 = act.actionValue.Replace("{login}", package.login).Replace("{full_name}", package.fullName.fullName);
                                    db.ExecuteNonQuery(sql1, CommandType.StoredProcedure, null);
                                    break;

                                case "sql":
                                    String sql2 = act.actionValue.Replace("{login}", package.login).Replace("{full_name}", package.fullName.fullName);
                                    db.ExecuteNonQuery(sql2, CommandType.Text, null);
                                    break;

                                default:
                                    processLog.AppendLine("Action not recognized: " + act.actionKey);
                                    Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "Action not recognized: " + act.actionKey, "");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            processLog.AppendLine("Error on execute action (" + act.actionKey + "): " + ex.Message);
                            Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on execute action (" + act.actionKey + "): " + ex.Message, "");
                        }
                }*/

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process deploy: " + ex.Message);

                String debugInfo = JSON.Serialize2(new { package = package, fieldMapping = fieldMapping });
                if (package.password != "")
                    debugInfo = debugInfo.Replace(package.password, "Replaced for user security");

                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message, debugInfo);
            }
            finally
            {
                if (db != null)
                    db.Dispose();

                Log2(this, logType, package.entityId, package.identityId, "Deploy executed", processLog.ToString());
                processLog.Clear();
                processLog = null;
            }
        }

        public void DebugLog(object sender, PluginLogType type, long entityId, long identityId, string text, string additionalData)
        {
#if DEBUG
            Log2(sender, type, entityId, identityId, text, additionalData);
#endif
        }

        public override void ProcessDelete(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            return;
        }

        private String Corte(String text, Int32 len)
        {
            
            if (String.IsNullOrWhiteSpace(text))
                return "";

            if (len <= 0)
                return text;

            if (text.Length <= len)
                return text;

            return text.Substring(0,len);
        }

        private OleDbType GetDBType(System.Type theType)
        {
            OleDbParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new OleDbParameter();
            tc = System.ComponentModel.TypeDescriptor.GetConverter(p1.DbType);
            if (tc.CanConvertFrom(theType))
            {
                p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
            }
            else
            {
                //Try brute force
                try
                {
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
                }
                catch (Exception ex)
                {
                    //Do Nothing
                }
            }
            return p1.OleDbType;
        }

        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }

}
