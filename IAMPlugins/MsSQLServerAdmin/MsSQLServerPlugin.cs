using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using IAM.PluginInterface;
using SafeTrend.Json;

namespace MsSQLServerAdmin
{
    public class MsSQLServerAdminPlugin : PluginConnectorBase
    {
        public override String GetPluginName() { return "Microsoft SQL Server (Admin users) plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com banco de dados Microsoft SQLServer 2005 ou superior"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/MsSQLServerAdmin");
        }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Servidor", "server", "IP ou nome do servidor para conexão", PluginConfigTypes.String, true, @""));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário para conexão", PluginConfigTypes.String, true, @""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha para conexão", PluginConfigTypes.Password, true, @""));

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


            String connectionstring = "Data Source=" + config["server"].ToString() + ";Initial Catalog=master;User Id=" + config["username"].ToString() + ";Password='" + config["password"].ToString() + "';";

            MSSQLDB db = null;
            try
            {
                db = new MSSQLDB(connectionstring);
                db.openDB();

                String sql = "SELECT name AS Login_Name FROM sys.server_principals  WHERE TYPE IN ('S') and name not like '%##%' ORDER BY name, type_desc";

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


            String connectionstring = "Data Source=" + config["server"].ToString() + ";Initial Catalog=master;User Id=" + config["username"].ToString() + ";Password='" + config["password"].ToString() + "';";

            MSSQLDB db = null;
            try
            {
                db = new MSSQLDB(connectionstring);
                db.openDB();

                String sql = "SELECT name AS Login_Name FROM sys.server_principals  WHERE TYPE IN ('S') and name not like '%##%' ORDER BY name, type_desc";

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


            String connectionstring = "Data Source=" + config["server"].ToString() + ";Initial Catalog=master;User Id=" + config["username"].ToString() + ";Password='" + config["password"].ToString() + "';";

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

                LogEvent dbExecLog = new LogEvent(delegate(Object sender, PluginLogType type, String text)
                {
                    processLog.AppendLine(text);
                });

                db.OnLog += dbExecLog;

                //Verifica se o registro existe
                DataTable dtReg = db.ExecuteDataTable("SELECT name AS Login_Name FROM sys.server_principals  WHERE TYPE IN ('S') and name = '" + package.login + "'", CommandType.Text, null);
                if (dtReg == null)
                    {
                        logType = PluginLogType.Error;
                        processLog.AppendLine("Error on select data: " + db.LastError);
                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on select data: " + db.LastError, "");
                        return;
                    }


                //Preenche a tabela de parâmetros com os campos que serão inseridos/atualizados
                if (dtReg.Rows.Count == 0)
                {
                    //Não existe, cria
                    String tmpPwd = IAM.Password.RandomPassword.Generate(20);
                    tmpPwd = tmpPwd.Replace("'", "");
                    tmpPwd = tmpPwd.Replace(".", "");
                    tmpPwd = tmpPwd.Replace("\\", "");
                    tmpPwd = tmpPwd.Replace("[", "");
                    tmpPwd = tmpPwd.Replace("]", "");

                    if (package.password == "")
                        processLog.AppendLine("User not found in AD and IAM Password not found in properties list, creating a random password (" + tmpPwd + ")");

                    String insert = "CREATE LOGIN [" + package.login + "] WITH PASSWORD=N'" + tmpPwd + "', DEFAULT_DATABASE=[master], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF";

                    StringBuilder tmpText = new StringBuilder();
                    try
                    {

                        tmpText.AppendLine("ExecuteNonQuery.SQL = " + insert);

                        db.ExecuteNonQuery(insert, CommandType.Text, null);
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


                if (package.password != "")
                {
                    String insert = "ALTER LOGIN [" + package.login + "] WITH PASSWORD=N'" + package.password + "'";

                    StringBuilder tmpText = new StringBuilder();
                    try
                    {

                        tmpText.AppendLine("ExecuteNonQuery.SQL = " + insert);

                        db.ExecuteNonQuery(insert, CommandType.Text, null);
                    }
                    catch (Exception ex2)
                    {
                        String sPs = "";
                        try
                        {
                            PasswordStrength ps = CheckPasswordStrength(package.password, package.fullName.fullName);

                            sPs += "Length = " + package.password.Length + Environment.NewLine;
                            sPs += "Contains Uppercase? " + ps.HasUpperCase + Environment.NewLine;
                            sPs += "Contains Lowercase? " + ps.HasLowerCase + Environment.NewLine;
                            sPs += "Contains Symbol? " + ps.HasSymbol + Environment.NewLine;
                            sPs += "Contains Number? " + ps.HasDigit + Environment.NewLine;
                            sPs += "Contains part of the name/username? " + ps.HasNamePart + Environment.NewLine;

                        }
                        catch { }

                        Log2(this, PluginLogType.Error, package.entityId, package.identityId, "Error on set user password, check the password complexity rules", ex2.Message + (ex2.InnerException != null ? " " + ex2.InnerException.Message : "") + Environment.NewLine + sPs);
                        return;
                    }
                    finally
                    {
                        tmpText.Clear();
                        tmpText = null;
                    }
                }

                NotityChangeUser(this, package.entityId);

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


                if (package.password != "")
                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User updated with password", "");
                else
                    Log2(this, PluginLogType.Information, package.entityId, package.identityId, "User updated without password", "");

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
