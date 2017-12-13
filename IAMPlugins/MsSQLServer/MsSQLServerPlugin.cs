using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using IAM.PluginInterface;
using SafeTrend.Json;
using System.Text.RegularExpressions;

namespace MsSQLServer
{
    public class MsSQLServerPlugin : PluginConnectorBase
    {
        public override String GetPluginName() { return "Microsoft SQL Server plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com base de dados Microsoft SQLServer 2005 ou superior"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/mssqlserver");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("ConectionString", "connectionstring", "String de conexão com o banco de dados", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Tabela", "table", "Tabela", PluginConfigTypes.String, true, ","));
            //conf.Add(new PluginConfigFields("Filtro de publicação", "deploy_filter", "Colunas para filtro (somente nome das colunas separada por virgula)", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Filtro de seleção", "select_filter", "String 'where' para filtro", PluginConfigTypes.String, false, ","));
            //conf.Add(new PluginConfigFields("Coluna de login", "login_column", "Nome da coluna no banco de dados que contém o login", PluginConfigTypes.String, false, ","));
            //conf.Add(new PluginConfigFields("Coluna de senha", "password_column", "Nome da coluna no banco de dados que contém a senha", PluginConfigTypes.String, false, ","));
            //conf.Add(new PluginConfigFields("Coluna de nome", "name_column", "Nome da coluna no banco de dados que contém o nome", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Coluna de bloqueio", "lock_column", "Nome da coluna no banco de dados que contém o status do usuário (bloqueado/desbloqueado)", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Valor de bloqueado", "locked_value", "Valor a ser inserido na cooluna de status/bloqueaio em caso de bloqueado", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Valor de desbloqueado", "unlocked_value", "Valor a ser inserido na cooluna de status/bloqueaio em caso de desbloqueado", PluginConfigTypes.String, false, ","));

            conf.Add(new PluginConfigFields("SQL de pré importação", "pre_sql_imp", "Comando SQL a ser executado antes da importação. (Não pode ser Stored Procedure)", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Stored Procedure de pré importação", "pre_sp_imp", "Stored Procedure SQL a ser executada antes da importação.", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("SQL de pós importação", "pos_sql_imp", "Comando SQL a ser executado após a importação. (Não pode ser Stored Procedure)", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Stored Procedure de pós importação", "pos_sp_imp", "Stored Procedure SQL a ser executada após a importação.", PluginConfigTypes.String, false, ","));

            conf.Add(new PluginConfigFields("SQL de pré publicação", "pre_sql_deploy", "Comando SQL a ser executado antes da publicação de cada usuário. (Não pode ser Stored Procedure)", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Stored Procedure de pré publicação", "pre_sp_deploy", "Stored Procedure SQL a ser executada antes da  publicação de cada usuário.", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("SQL de pós publicação", "pos_sql_deploy", "Comando SQL a ser executado após a publicação de cada usuário. (Não pode ser Stored Procedure)", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Stored Procedure de pós publicação", "pos_sp_deploy", "Stored Procedure SQL a ser executada após a publicação de cada usuário.", PluginConfigTypes.String, false, ","));

            return conf.ToArray();
        }


        public override PluginConnectorConfigActions[] GetConfigActions()
        {
            List<String> macro = new List<string>();
            macro.Add("{login}");
            macro.Add("{full_name}");

            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();
            conf.Add(new PluginConnectorConfigActions("Executa Stored Procedure", "procedure", "Executa uma 'Stored procedure'", "Procedure", "procedure_name", "Procedure a ser executada", macro));
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


            String connectionstring = config["connectionstring"].ToString();
            String table = config["table"].ToString();
            String filter = config["select_filter"].ToString();

            MSSQLDB db = null;
            try
            {
                db = new MSSQLDB(connectionstring);
                db.openDB();

                String sql = "select top 20 * from " + table;

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

            String connectionstring = config["connectionstring"].ToString();
            String table = config["table"].ToString();
            String filter = config["select_filter"].ToString();

            MSSQLDB db = null;
            try
            {
                db = new MSSQLDB(connectionstring);
                db.openDB();

                try
                {
                    if ((config.ContainsKey("pre_sql_imp")) && (!String.IsNullOrEmpty(config["pre_sql_imp"].ToString())))
                        db.ExecuteNonQuery(config["pre_sql_imp"].ToString(), CommandType.Text, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro on execute SQL (" + config["pre_sql_imp"].ToString() + "): " + db.LastError);
                }

                try
                {
                    if ((config.ContainsKey("pre_sp_imp")) && (!String.IsNullOrEmpty(config["pre_sp_imp"].ToString())))
                        db.ExecuteNonQuery(config["pre_sp_imp"].ToString(), CommandType.StoredProcedure, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro on execute Stored Procedure (" + config["pre_sp_imp"].ToString() + "): " + db.LastError);
                }


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
                        Log2(this, PluginLogType.Error, 0, 0, "Erro ao importar o registro: " + ex.Message, "");
                        Log(this, PluginLogType.Error, "Erro ao importar o registro: " + ex.Message);
                    }
                    finally
                    {
                        package.Dispose();
                        package = null;
                    }
                }


                try
                {
                    if ((config.ContainsKey("pos_sql_imp")) && (!String.IsNullOrEmpty(config["pos_sql_imp"].ToString())))
                        db.ExecuteNonQuery(config["pos_sql_imp"].ToString(), CommandType.Text, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro on execute SQL (" + config["pos_sql_imp"].ToString() + "): " + db.LastError);
                }

                try
                {
                    if ((config.ContainsKey("pos_sp_imp")) && (!String.IsNullOrEmpty(config["pos_sp_imp"].ToString())))
                        db.ExecuteNonQuery(config["pos_sp_imp"].ToString(), CommandType.StoredProcedure, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro on execute Stored Procedure (" + config["pos_sp_imp"].ToString() + "): " + db.LastError);
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

            String connectionstring = config["connectionstring"].ToString();
            String table = config["table"].ToString();
            String lock_column = (config.ContainsKey("lock_column") ? config["lock_column"].ToString().ToLower() : null);
            String locked_value = (config.ContainsKey("locked_value") ? config["locked_value"].ToString().ToLower() : null);
            String unlocked_value = (config.ContainsKey("unlocked_value") ? config["unlocked_value"].ToString().ToLower() : null);
            //String deploy_filter = config["deploy_filter"].ToString();
            //String login_column = (config.ContainsKey("login_column") ? config["login_column"].ToString().ToLower() : null);
            //String name_column = (config.ContainsKey("name_column") ? config["name_column"].ToString().ToLower() : null);
            //String password_column = (config.ContainsKey("password_column") ? config["password_column"].ToString().ToLower() : null);

            /*conf.Add(new PluginConfigFields("ConectionString", "connectionstring", "String de conexão com o banco de dados", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Tabela", "table", "Tabela", PluginConfigTypes.String, true, ","));
            conf.Add(new PluginConfigFields("Filtro de selecção", "select_filter", "String 'where' para filtro", PluginConfigTypes.String, false, ","));
            //conf.Add(new PluginConfigFields("Filtro de publicação", "deploy_filter", "Colunas para filtro (somente nome das colunas separada por virgula)", PluginConfigTypes.String, true, ","));
            //conf.Add(new PluginConfigFields("Coluna de login", "login_column", "Nome da coluna no banco de dados que contém o login", PluginConfigTypes.String, false, ","));
            //conf.Add(new PluginConfigFields("Coluna de senha", "password_column", "Nome da coluna no banco de dados que contém a senha", PluginConfigTypes.String, false, ","));
            //conf.Add(new PluginConfigFields("Coluna de nome", "name_column", "Nome da coluna no banco de dados que contém o nome", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Coluna de bloqueio", "lock_column", "Nome da coluna no banco de dados que contém o status do usuário (bloqueado/desbloqueado)", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Valor de bloqueado", "locked_value", "Valor a ser inserido na cooluna de status/bloqueaio em caso de bloqueado", PluginConfigTypes.String, false, ","));
            conf.Add(new PluginConfigFields("Valor de desbloqueado", "unlocked_value", "Valor a ser inserido na cooluna de status/bloqueaio em caso de desbloqueado", PluginConfigTypes.String, false, ","));
            */

            MSSQLDB db = null;
            StringBuilder processLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            try
            {
                if (!String.IsNullOrEmpty(package.password))
                    processLog.AppendLine("Package contains password");
                else
                    processLog.AppendLine("Package not contains password");


                db = new MSSQLDB(connectionstring);
                db.openDB();


                List<String> prop = new List<String>();
                
                String login = package.login;

                //Monta a tabela de campos para a filtragem
                Dictionary<String, String> filter = new Dictionary<String, String>();

                //Adiciona os mapeamentos que são ID ou único para filtragem
                foreach(PluginConnectorBaseDeployPackageMapping m in  fieldMapping)
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
                SqlParameterCollection par = MSSQLDB.GetSqlParameterObject();


                //Preenche a tabela de parâmetros com os campos do where
                List<String> f1 = new List<string>();
                foreach (DataColumn dc in dtInsertSchema.Columns)
                    if (filter.ContainsKey(dc.ColumnName.ToLower()))
                        try
                        {
                            if (dc.DataType.Equals(typeof(String)))
                                par.Add("@s_" + dc.ColumnName, GetDBType(dc.DataType)).Value = Corte(filter[dc.ColumnName.ToLower()], dc.MaxLength);
                            else
                                par.Add("@s_" + dc.ColumnName, GetDBType(dc.DataType)).Value = filter[dc.ColumnName.ToLower()];

                            f1.Add(dc.ColumnName + " = @s_" + dc.ColumnName);
                        }
                        catch (Exception ex)
                        {
                            processLog.AppendLine("Column: " + dc.ColumnName + ", DataType: " + dc.DataType.ToString());
                            try
                            {
                                processLog.AppendLine("Value: " + filter[dc.ColumnName.ToLower()]);
                            }
                            catch (Exception ex1)
                            {
                                processLog.AppendLine("Value error: " + ex1.Message);
                            }
                            throw new Exception("Erro filling filter data. Column=" + dc.ColumnName + ", " + dc.DataType.ToString(), ex);
                        }


                //Monta todos os campos que serão inseridos/atualizados
                Dictionary<String, String> data = new Dictionary<String, String>();

                foreach (DataColumn dc in dtInsertSchema.Columns)
                    if (!data.ContainsKey(dc.ColumnName.ToLower()))
                        data.Add(dc.ColumnName.ToLower(), null);


                if (data.ContainsKey("locked"))
                    data["locked"] = (package.locked || package.temp_locked ? "1" : "0");

                processLog.AppendLine("lock_column = " + (String.IsNullOrEmpty(lock_column) ? "empty" : lock_column));
                processLog.AppendLine("locked_value = " + (String.IsNullOrEmpty(locked_value) ? "empty" : locked_value));
                processLog.AppendLine("unlocked_value = " + (String.IsNullOrEmpty(unlocked_value) ? "empty" : unlocked_value));

                if ((lock_column != null) && (data.ContainsKey(lock_column.ToLower())))
                {
                    if ((package.locked || package.temp_locked) && (!String.IsNullOrEmpty(locked_value)))
                        data[lock_column.ToLower()] = locked_value;
                    else if ((!package.locked && !package.temp_locked) && (!String.IsNullOrEmpty(unlocked_value)))
                        data[lock_column.ToLower()] = unlocked_value;
                    else
                        data[lock_column.ToLower()] = (package.locked || package.temp_locked ? "1" : "0");

                    //DebugLog(this, PluginLogType.Debug, package.entityId, package.identityId, "data[lock_column] = " + data[lock_column], "");
                    processLog.AppendLine("data[lock_column] = " + data[lock_column]);

                }

                String password_column = "";
                foreach (PluginConnectorBaseDeployPackageMapping m in fieldMapping)
                    if (m.isPassword && data.ContainsKey(m.dataName.ToLower()))
                    {
                        data[m.dataName.ToLower()] = package.password;
                        password_column = m.dataName.ToLower();
                    }
                    else if (m.isLogin && data.ContainsKey(m.dataName.ToLower()))
                        data[m.dataName.ToLower()] = package.login;
                    else if (m.isName && data.ContainsKey(m.dataName.ToLower()))
                        data[m.dataName.ToLower()] = package.fullName.fullName;

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




                if ((config.ContainsKey("pre_sql_deploy")) && (!String.IsNullOrEmpty(config["pre_sql_deploy"].ToString())))
                {
                    String sql = config["pre_sql_deploy"].ToString();

#if DEBUG
                    processLog.AppendLine("Preparando a execução do pré sql: " + sql);
#endif

                    try
                    {

                        Regex r = new Regex(@"{.*?}", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                        foreach (Match m in r.Matches(sql))
                        {
                            String key = m.Groups[0].Value.Trim(" {}".ToCharArray());
#if DEBUG
                            processLog.AppendLine("5. Macro {" + key + "}");
#endif

                            if (data.ContainsKey(key.ToLower()) && !String.IsNullOrEmpty(data[key.ToLower()]))
                            {
                                sql = sql.Replace("{" + key + "}", data[key.ToLower()]);
#if DEBUG
                                processLog.AppendLine("5.1. {" + key + "}: " + data[key.ToLower()]);
#endif
                            }
                            else
                            {
                                throw new Exception("Macro data for '{" + key + "}' not found or is empty");
                            }

                        }

                        try
                        {
#if DEBUG
                            processLog.AppendLine("Pré-SQL: " + sql);
#endif

                            db.ExecuteNonQuery(sql, CommandType.Text, null);

                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Erro on execute SQL (" + sql + "): " + db.LastError);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erro on execute (" + sql + "): " + ex.Message);
                    }
                }
                

                try
                {
                    if ((config.ContainsKey("pre_sp_deploy")) && (!String.IsNullOrEmpty(config["pre_sp_deploy"].ToString())))
                        db.ExecuteNonQuery(config["pre_sp_deploy"].ToString(), CommandType.StoredProcedure, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro on execute Stored Procedure (" + config["pre_sp_deploy"].ToString() + "): " + db.LastError);
                }



                LogEvent dbExecLog = new LogEvent(delegate(Object sender, PluginLogType type, String text)
                {
                    processLog.AppendLine(text);
                });

                db.OnLog += dbExecLog;

                //Verifica se o registro existe
                DataTable dtReg = db.ExecuteDataTable("select * from " + table + " where " + String.Join(" and ", f1), CommandType.Text, par);
                if (dtReg == null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on select data: " + db.LastError);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on select data: " + db.LastError, "");
                        return;
                    }


                //Preenche a tabela de parâmetros com os campos que serão inseridos/atualizados
                if (dtReg.Rows.Count > 0)
                {
                    //Existe, atualiza

                    List<String> c1 = new List<string>();
                    foreach (DataColumn dc in dtInsertSchema.Columns)
                        if (data.ContainsKey(dc.ColumnName.ToLower()))
                            try
                            {
                                if (!String.IsNullOrWhiteSpace(data[dc.ColumnName.ToLower()]))
                                {
                                    if (dc.DataType.Equals(typeof(String)))
                                        par.Add("@" + dc.ColumnName, GetDBType(dc.DataType)).Value = Corte((data[dc.ColumnName.ToLower()] != null ? data[dc.ColumnName.ToLower()] : ""), dc.MaxLength);
                                    else
                                        par.Add("@" + dc.ColumnName, GetDBType(dc.DataType)).Value = (data[dc.ColumnName.ToLower()] != null ? data[dc.ColumnName.ToLower()] : "");

                                    c1.Add(dc.ColumnName + " = @" + dc.ColumnName);
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


                    String update = "update " + table + " set  " + String.Join(", ", c1) + " where " + String.Join(" and ", f1);

                    StringBuilder tmpText = new StringBuilder();
                    try{

                        tmpText.AppendLine("ExecuteNonQuery.SQL = " + update);
                        tmpText.AppendLine("ExecuteNonQuery.Parameters " + par.Count);

                        foreach (SqlParameter p in par)
                            tmpText.AppendLine("ExecuteNonQuery.Parameters[" + p.ParameterName + "] = " + p.Value.ToString().Replace(package.password, "Replaced for user security"));

                        db.ExecuteNonQuery(update, CommandType.Text, par);
                    }
                    catch (Exception ex2)
                    {

                        processLog.AppendLine(tmpText.ToString());

                        throw new Exception("Error updating user", ex2);
                    }
                    finally
                    {
                        tmpText.Clear();
                        tmpText = null;
                    }

                    NotityChangeUser(this, package.entityId);

                    processLog.AppendLine("");

                    if (!String.IsNullOrEmpty(package.password))
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
                            try
                            {
                                if (!String.IsNullOrWhiteSpace(data[dc.ColumnName.ToLower()]))
                                {
                                    if (dc.DataType.Equals(typeof(String)))
                                        par.Add("@" + dc.ColumnName, GetDBType(dc.DataType)).Value = Corte(data[dc.ColumnName.ToLower()], dc.MaxLength);
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
                                catch(Exception ex1) {
                                    processLog.AppendLine("Value error: " + ex1.Message);
                                }
                                throw new Exception("Erro filling data. Column=" + dc.ColumnName + ", " + dc.DataType.ToString(), ex);
                            }

                    String insert = "insert into " + table + " (" + String.Join(",", c1) + ") values (" + String.Join(",", c2) + ")";

                    StringBuilder tmpText = new StringBuilder();
                    try
                    {

                        tmpText.AppendLine("ExecuteNonQuery.SQL = " + insert);
                        tmpText.AppendLine("ExecuteNonQuery.Parameters " + par.Count);

                        foreach (SqlParameter p in par)
                            tmpText.AppendLine("ExecuteNonQuery.Parameters[" + p.ParameterName + "] = " + p.Value.ToString().Replace(package.password, "Replaced for user security"));

                        db.ExecuteNonQuery(insert, CommandType.Text, par);
                    }
                    catch (Exception ex2)
                    {
                        processLog.AppendLine(tmpText.ToString());

                        throw new Exception("Error adding user", ex2);
                    }
                    finally
                    {
                        tmpText.Clear();
                        tmpText = null;
                    }

                    NotityChangeUser(this, package.entityId);

                    processLog.AppendLine("");
                    processLog.AppendLine("User added");
                }

                db.OnLog -= dbExecLog;

                //Executa as ações do RBAC
                if ((package.pluginAction != null) && (package.pluginAction.Count > 0))
                {
                    processLog.AppendLine("");
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


                try
                {
                    if ((config.ContainsKey("pos_sql_deploy")) && (!String.IsNullOrEmpty(config["pos_sql_deploy"].ToString())))
                        db.ExecuteNonQuery(config["pos_sql_deploy"].ToString(), CommandType.Text, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro on execute SQL (" + config["pos_sql_deploy"].ToString() + "): " + db.LastError);
                }

                try
                {
                    if ((config.ContainsKey("pos_sp_deploy")) && (!String.IsNullOrEmpty(config["pos_sp_deploy"].ToString())))
                        db.ExecuteNonQuery(config["pos_sp_deploy"].ToString(), CommandType.StoredProcedure, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro on execute Stored Procedure (" + config["pos_sp_deploy"].ToString() + "): " + db.LastError);
                }

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process deploy: " + ex.Message + (ex.InnerException != null ? " --> " + ex.InnerException.Message : ""));

#if DEBUG
                String debugInfo = JSON.Serialize2(new { package = package, fieldMapping = fieldMapping });
                if (package.password != "")
                    debugInfo = debugInfo.Replace(package.password, "Replaced for user security");

                processLog.AppendLine(debugInfo);
#endif

                Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on process deploy: " + ex.Message, processLog.ToString());
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

            MSSQLDB db = null;
            try
            {
                db = new MSSQLDB(connectionstring);
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
                SqlParameterCollection par = MSSQLDB.GetSqlParameterObject();


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

        private SqlDbType GetDBType(System.Type theType)
        {
            SqlParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new SqlParameter();
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
            return p1.SqlDbType;
        }

        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }

}
