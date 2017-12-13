using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Odbc;
using IAM.PluginInterface;
using SafeTrend.Json;

namespace ODBC
{
    public class OdbcPlugin : PluginConnectorBase
    {
        public override String GetPluginName() { return "Microsoft ODBC connector"; }
        public override String GetPluginDescription() { return "Plugin para integragir com base de dados Microsoft ODBC connector"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/odbc");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("DSN de sistema", "system_dsn", "Nome do DSN (Data Source Name) de sistema", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário de autenticação", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha de autenticação", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Tabela", "table", "Tabela", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Filtro de seleção", "select_filter", "String 'where' para filtro", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Coluna de bloqueio", "lock_column", "Nome da coluna no banco de dados que contém o status do usuário (bloqueado/desbloqueado)", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Valor de bloqueado", "locked_value", "Valor a ser inserido na cooluna de status/bloqueaio em caso de bloqueado", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Valor de desbloqueado", "unlocked_value", "Valor a ser inserido na cooluna de status/bloqueaio em caso de desbloqueado", PluginConfigTypes.String, false, ","));
            
            return conf.ToArray();
        }


        public override PluginConnectorConfigActions[] GetConfigActions()
        {
            List<String> macro = new List<string>();
            macro.Add("{login}");
            macro.Add("{full_name}");

            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();
            //conf.Add(new PluginConnectorConfigActions("Executa Stored Procedure", "procedure", "Executa uma 'Stored procedure'", "Procedure", "procedure_name", "Procedure a ser executada", macro));
            conf.Add(new PluginConnectorConfigActions("Executa SQL", "sql", "Executa uma sql (não pode ser procedure)", "SQL", "sql", "Comando SQL a ser executado", macro));

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


            String table = config["table"].ToString();
            String filter = config["select_filter"].ToString();

            OdbcDB db = null;
            try
            {
                db = new OdbcDB(config["system_dsn"].ToString(), (config.ContainsKey("username") ? config["username"].ToString() : ""), (config.ContainsKey("password") ? config["password"].ToString() : ""));
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


                String sql = "select * from " + table;

                if (!String.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.Trim();
                    if (filter.IndexOf("where") != 0)
                        filter = "where " + filter;

                    sql = sql + " " + filter;
                }

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

                foreach (DataRow dr in dtSource.Rows)
                {
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

            List<String> prop = new List<String>();

            String table = config["table"].ToString();
            String filter = config["select_filter"].ToString();

            OdbcDB db = null;
            try
            {
                db = new OdbcDB(config["system_dsn"].ToString(), (config.ContainsKey("username") ? config["username"].ToString() : ""), (config.ContainsKey("password") ? config["password"].ToString() : ""));
                db.openDB();

                String sql = "select * from " + table;

                if (!String.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.Trim();
                    if (filter.IndexOf("where") != 0)
                        filter = "where " + filter;

                    sql = sql + " " + filter;
                }

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
                Log2(this, PluginLogType.Error, 0, 0, "Error on process import: " + ex.Message, "");
                Log(this, PluginLogType.Error, ex.Message);
            }
            finally
            {
                if (db != null)
                    db.Dispose();
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

            String table = config["table"].ToString();
            String lock_column = (config.ContainsKey("lock_column") ? config["lock_column"].ToString().ToLower() : null);
            String locked_value = (config.ContainsKey("locked_value") ? config["locked_value"].ToString().ToLower() : null);
            String unlocked_value = (config.ContainsKey("unlocked_value") ? config["unlocked_value"].ToString().ToLower() : null);

            OdbcDB db = null;
            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {
                db = new OdbcDB(config["system_dsn"].ToString(), (config.ContainsKey("username") ? config["username"].ToString() : ""), (config.ContainsKey("password") ? config["password"].ToString() : ""));
                db.openDB();

                List<String> prop = new List<String>();

                String login = package.login;

                //Monta a tabela de campos para a filtragem
                Dictionary<String, String> filter = new Dictionary<String, String>();

                //Adiciona os mapeamentos que são ID ou único para filtragem
                foreach (PluginConnectorBaseDeployPackageMapping m in fieldMapping)
                    if ((m.isId || m.isUnique) && !filter.ContainsKey(m.dataName.ToLower()))
                        filter.Add(m.dataName.ToLower(), null);

                foreach (PluginConnectorBaseDeployPackageMapping m in fieldMapping)
                    if (m.isLogin && filter.ContainsKey(m.dataName.ToLower()))
                        filter[m.dataName.ToLower()] = package.login;
                    else if (m.isName && filter.ContainsKey(m.dataName))
                        filter[m.dataName.ToLower()] = package.fullName.fullName;


                //Verifica se a coluna do login é uma coluna da filtragem
                /*
                foreach (String f in deploy_filter.Trim(", ".ToCharArray()).Split(",".ToCharArray()))
                    if (!filter.ContainsKey(f.ToLower().Trim()))
                        filter.Add(f.ToLower().Trim(), null);

                if (login_column != null && filter.ContainsKey(login_column))
                    filter[login_column] = package.login;

                if (name_column != null && filter.ContainsKey(name_column))
                    filter[name_column] = package.fullName.fullName;*/


                foreach (PluginConnectorBasePackageData dt in package.importsPluginData)
                    if (filter.ContainsKey(dt.dataName.ToLower()) && filter[dt.dataName.ToLower()] == null)
                        filter[dt.dataName.ToLower()] = dt.dataValue;

                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (filter.ContainsKey(dt.dataName.ToLower()) && filter[dt.dataName.ToLower()] == null)
                        filter[dt.dataName.ToLower()] = dt.dataValue;

                foreach (PluginConnectorBasePackageData dt in package.properties)
                    if (filter.ContainsKey(dt.dataName.ToLower()) && filter[dt.dataName.ToLower()] == null)
                        filter[dt.dataName.ToLower()] = dt.dataValue;


                //Verifica se algum campo da filtragem é nulo
                foreach (String k in filter.Keys)
                    if (filter[k] == null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Deploy filter column data of '" + k + "' not found");

                        String debugInfo = JSON.Serialize2(new { package = package, fieldMapping = fieldMapping });
                        if (package.password != "")
                            debugInfo = debugInfo.Replace(package.password, "Replaced for user security");

                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Deploy filter column data of '" + k + "' not found", debugInfo);
                        return;
                    }

                //Resgata a restutura da tabela de destino
                DataTable dtInsertSchema = db.GetSchema(table);

                //Monta o where
                OdbcParameterCollection par = OdbcDB.GetSqlParameterObject();


                //Preenche a tabela de parâmetros com os campos do where
                List<String> f1 = new List<string>();
                foreach (DataColumn dc in dtInsertSchema.Columns)
                    if (filter.ContainsKey(dc.ColumnName.ToLower()))
                    {
                        if (dc.DataType.Equals(typeof(String)))
                            par.Add("@s_" + dc.ColumnName, GetDBType(dc.DataType)).Value = Corte(filter[dc.ColumnName.ToLower()], dc.MaxLength);
                        else
                            par.Add("@s_" + dc.ColumnName, GetDBType(dc.DataType)).Value = filter[dc.ColumnName.ToLower()];
                        f1.Add(dc.ColumnName + " = @s_" + dc.ColumnName);
                    }


                //Monta todos os campos que serão inseridos/atualizados
                Dictionary<String, String> data = new Dictionary<String, String>();

                foreach (DataColumn dc in dtInsertSchema.Columns)
                {
                    if (dc.AutoIncrement){
                        processLog.AppendLine("Field " + dc.ColumnName + " ignored because it was indicated with an AutoIncrement fiend");
                    }
                    else if (!data.ContainsKey(dc.ColumnName.ToLower()))
                    { //Nao adiciona coluna que é autoincremento (ID)
                        data.Add(dc.ColumnName.ToLower(), null);
                    }
                }


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



#if DEBUG
                processLog.AppendLine("1. Entity Data");
#endif

                foreach (PluginConnectorBasePackageData dt in package.entiyData)
                    if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                    {
                        data[dt.dataName.ToLower()] = dt.dataValue;
                        //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "1. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("1. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }


#if DEBUG
                processLog.AppendLine("2. Import Plugin Data");
#endif

                foreach (PluginConnectorBasePackageData dt in package.importsPluginData)
                    if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                    {
                        data[dt.dataName.ToLower()] = dt.dataValue;
                        //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "1. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("2. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }

#if DEBUG
                processLog.AppendLine("3. Plugin Data");
#endif

                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                    {
                        data[dt.dataName.ToLower()] = dt.dataValue;
                        //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "2. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("3. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }

#if DEBUG
                processLog.AppendLine("4. Properties");
#endif

                foreach (PluginConnectorBasePackageData dt in package.properties)
                    if (data.ContainsKey(dt.dataName.ToLower()) && data[dt.dataName.ToLower()] == null)
                    {
                        data[dt.dataName.ToLower()] = dt.dataValue;
                        //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "3. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue, "");
#if DEBUG
                        processLog.AppendLine("4. data[" + dt.dataName.ToLower() + "] = " + dt.dataValue);
#endif
                    }


#if DEBUG
                processLog.AppendLine("5. Final data");
#endif

                foreach (String k in data.Keys)
                {
                    //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "4. data[" + k + "] = " + data[k], "");
#if DEBUG
                    processLog.AppendLine("5. data[" + k + "] = " + data[k]);
#endif
                }



                //Verifica se o registro existe
                DataTable dtReg = db.ExecuteDataTable("select * from " + table + " where " + String.Join(" and ", f1), CommandType.Text, par);
                if (dtReg == null)
                {
                    logType = PluginLogType.Error;
                    processLog.AppendLine("Error on select data: " + db.LastError);
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on select data: " + db.LastError, "");
                    return;
                }


                LogEvent dbExecLog = new LogEvent(delegate(Object sender, PluginLogType type, String text)
                {
                    processLog.AppendLine(text);
                });


                //Preenche a tabela de parâmetros com os campos que serão inseridos/atualizados
                if (dtReg.Rows.Count > 0)
                {
                    //Existe, atualiza

                    List<String> c1 = new List<string>();
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
                                    par.Add("@" + dc.ColumnName, GetDBType(dc.DataType)).Value = (data[dc.ColumnName.ToLower()] != null ? data[dc.ColumnName.ToLower()] : "");

                                c1.Add(dc.ColumnName + " = @" + dc.ColumnName);
                            }
                        }



                    foreach (OdbcParameter p in par)
                    {
#if DEBUG
                        processLog.AppendLine("5. par[" + p.ParameterName + "] = " + p.Value);
#endif
                    }


                    String update = "update " + table + " set  " + String.Join(", ", c1) + " where " + String.Join(" and ", f1);

                    db.OnLog += dbExecLog;
                    db.ExecuteNonQuery(update, CommandType.Text, par);
                    db.OnLog -= dbExecLog;

                    NotityChangeUser(this, package.entityId);

                    if (package.password != "")
                        processLog.AppendLine("User updated with password");
                    else
                        processLog.AppendLine("User updated without password");
                }
                else
                {
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
                            try
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
                            catch (Exception ex)
                            {
                                processLog.AppendLine("Column: " + dc.ColumnName + ", DataType: " + dc.DataType.ToString());
                                try
                                {
                                    processLog.AppendLine("Value: " + data[dc.ColumnName.ToLower()]);
                                }
                                catch (Exception ex1)
                                {
                                    processLog.AppendLine("Value error: " + ex1.Message);
                                }
                                throw new Exception("Erro filling data. Column=" + dc.ColumnName + ", " + dc.DataType.ToString(), ex);
                            }

                        }


                    foreach (OdbcParameter p in par)
                    {
                        //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "6. par[" + p.ParameterName + "] = " + p.Value, "");
#if DEBUG
                        processLog.AppendLine("6. par[" + p.ParameterName + "] = " + p.Value);
#endif
                    }

                    String insert = "insert into " + table + " (" + String.Join(",", c1) + ") values (" + String.Join(",", c2) + ")";

                    db.OnLog += dbExecLog;
                    db.ExecuteNonQuery(insert, CommandType.Text, par);
                    db.OnLog -= dbExecLog;

                    NotityChangeUser(this, package.entityId);

                    processLog.AppendLine("User added");
                }

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
                }

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

            if (!CheckInputConfig(config, true, Log))
                return;

            String connectionstring = config["connectionstring"].ToString();
            String table = config["table"].ToString();
            String deploy_filter = config["deploy_filter"].ToString();
            String login_column = (config.ContainsKey("login_column") ? config["login_column"].ToString().ToLower() : null);
            String name_column = (config.ContainsKey("name_column") ? config["name_column"].ToString().ToLower() : null);
            String password_column = (config.ContainsKey("password_column") ? config["password_column"].ToString().ToLower() : null);

            OdbcDB db = null;
            try
            {
                db = new OdbcDB(connectionstring);
                db.openDB();

                List<String> prop = new List<String>();

                String login = package.login;

                //Monta a tabela de campos para a filtragem
                Dictionary<String, String> filter = new Dictionary<String, String>();

                //Verifica se a coluna do login é uma coluna da filtragem
                foreach (String f in deploy_filter.Trim(", ".ToCharArray()).Split(",".ToCharArray()))
                    if (!filter.ContainsKey(f.ToLower().Trim()))
                        filter.Add(f.ToLower().Trim(), null);

                if (login_column != null && filter.ContainsKey(login_column))
                    filter[login_column] = package.login;

                if (name_column != null && filter.ContainsKey(name_column))
                    filter[name_column] = package.fullName.fullName;


                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    if (filter.ContainsKey(dt.dataName.ToLower()) && filter[dt.dataName.ToLower()] == null)
                        filter[dt.dataName.ToLower()] = dt.dataValue;

                foreach (PluginConnectorBasePackageData dt in package.properties)
                    if (filter.ContainsKey(dt.dataName.ToLower()) && filter[dt.dataName.ToLower()] == null)
                        filter[dt.dataName.ToLower()] = dt.dataValue;

                //Verifica se algum campo da filtragem é nulo
                foreach (String k in filter.Keys)
                    if (filter[k] == null)
                    {
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Deploy filter column data of '" + k + "' not found", "");
                        return;
                    }

                //Resgata a restutura da tabela de destino
                DataTable dtInsertSchema = db.GetSchema(table);

                //Monta o where
                OdbcParameterCollection par = OdbcDB.GetSqlParameterObject();


                //Preenche a tabela de parâmetros com os campos do where
                List<String> f1 = new List<string>();
                foreach (DataColumn dc in dtInsertSchema.Columns)
                    if (filter.ContainsKey(dc.ColumnName.ToLower()))
                    {
                        if (dc.DataType.Equals(typeof(String)))
                            par.Add("@s_" + dc.ColumnName, GetDBType(dc.DataType)).Value = Corte(filter[dc.ColumnName.ToLower()], dc.MaxLength);
                        else
                            par.Add("@s_" + dc.ColumnName, GetDBType(dc.DataType)).Value = filter[dc.ColumnName.ToLower()];
                        f1.Add(dc.ColumnName + " = @s_" + dc.ColumnName);
                    }


                //Verifica se o registro existe
                DataTable dtReg = db.ExecuteDataTable("select * from " + table + " where " + String.Join(" and ", f1), CommandType.Text, par);
                if (dtReg == null)
                {
                    Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on select data: " + db.LastError, "");
                    return;
                }


                //Preenche a tabela de parâmetros com os campos que serão inseridos/atualizados
                if (dtReg.Rows.Count == 0)
                {
                    Log2(this, PluginLogType.Warning, package.entityId, package.identityId, "User not found", "");
                    return;
                }


                String update = "delete from " + table + " where " + String.Join(" and ", f1);

                db.ExecuteNonQuery(update, CommandType.Text, par);

                NotityDeletedUser(this, package.entityId, package.identityId);

                Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User deleted", "");
            }
            catch (Exception ex)
            {
                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message, "");
            }
            finally
            {
                if (db != null)
                    db.Dispose();
            }
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

        private String GetCnName(String cn)
        {
            return cn.Split(",".ToCharArray())[0].Replace("cn=", "").Replace("CN=", "");
        }

        /*
        private String Where(DataColumn dc, String value)
        {
            String ret = dc.ColumnName + " = ";
            if ((dc.DataType.Equals(typeof(Int16))) || (dc.DataType.Equals(typeof(Int32))) || (dc.DataType.Equals(typeof(Int64))))
                ret += value;
            else
                ret += "'" + value + "'";

            return ret;
        }*/

        private OdbcType GetDBType(System.Type theType)
        {
            OdbcParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new OdbcParameter();
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
            return p1.OdbcType;
        }

        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }

}
