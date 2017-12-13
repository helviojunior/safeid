using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace IAM.SQLDefs
{

    public enum UserLogLevel
    {
        Debug = 0,
        Trace = 100,
        Info = 200,
        Warning = 300,
        Error = 400,
        Fatal = 500
    }


    public enum LogKey
    {
        Undefined = 0,
        Certificate_Error = 1,
        Encrypt_Error = 2,
        Dencrypt_Error = 3,
        Licence_error = 4,
        Debug = 5,
        Deploy = 6,
        Inbound = 7,
        Engine = 8,
        Watchdog = 9,

        /* User logs 1000 */
        User_Logged = 1001,
        User_NewRecoveryCode = 1002,
        User_PasswordChanged = 1003,
        User_PasswordReseted = 1004,
        User_WrongUserAndPassword = 1005,
        User_WrongPassword = 1006,
        User_Locked = 1007,
        User_Unlocked = 1008,
        User_Deploy = 1009,
        User_DeployMark = 1010,
        User_Deleted = 1011,
        User_Undeleted = 1012,
        User_AccessDenied = 1013,
        User_Update = 1014,
        User_ImportError = 1015,
        User_Added = 1016,
        User_IdentityRoleBind = 1017,
        User_IdentityRoleUnbind = 1018,
        User_ImportInfo = 1019,
        User_WrongTicket = 1020,
        User_IdentityNew = 1021,
        User_IdentityDeleted = 1022,
        User_SystemRoleBind = 1023,
        User_SystemRoleUnbind = 1024,
        User_TempLocked = 1025,
        User_TempUnlocked = 1026,
        User_PasswordCreated = 1027,
        User_PropertyChanged = 1028,

        /* API logs 2000 */
        API_Error = 2001,
        Proxy_Event = 2002,
        Plugin_Event = 2003,
        API_Log = 2004,

        /* Autoservice logs 3000 */

        /* Role 4000 */
        Role_Deploy = 4001,
        Role_Deleted = 4002,
        Role_Inserted = 4003,
        Role_Changed = 4004,

        /* Import 5000 */
        Import = 5001,

        /* Role 6000 */
        Context_Deleted = 6001,
        Context_Inserted = 6002,
        Context_Changed = 6003,

        /* Plugin 7000 */
        Plugin_Deleted = 7001,
        Plugin_Inserted = 7002,
        Plugin_Changed = 7003,

        /* Proxy 8000 */
        Proxy_Deleted = 8001,
        Proxy_Inserted = 8002,
        Proxy_Changed = 8003,

        /* Resource 9000 */
        Resource_Deleted = 9001,
        Resource_Inserted = 9002,
        Resource_Changed = 9003,

        /* Resource 9000 */
        ResourcePlugin_Deleted = 10001,
        ResourcePlugin_Inserted = 10002,
        ResourcePlugin_Changed = 10003,
        ResourcePluginParameters_Changed = 10004,
        ResourcePluginMapping_Changed = 10005,
        ResourcePluginRole_Changed = 10006,
        ResourcePluginIdentity_Changed = 10007,
        ResourcePluginLockExpression_Changed = 10008,
        ResourcePluginLockSchedule_Changed = 10009,
        ResourcePluginDeploy = 10009,

        /* Field 11000 */
        Field_Deleted = 11001,
        Field_Inserted = 11002,
        Field_Changed = 11003,

        /* System roles 12000 */
        SystemRole_Deploy = 12001,
        SystemRole_Deleted = 12002,
        SystemRole_Inserted = 12003,
        SystemRole_Changed = 12004,
        SystemRolePermission_Changed = 12005,


        /* Filter 13000 */
        Filter_Deleted = 13001,
        Filter_Inserted = 13002,
        Filter_Changed = 13003,


    }

    public class SqlBaseStatic
    {

        /******************************************
        *** Reimplementa os métodos, porém de forma stática *******/

        static public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", null);
        }

        static public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, null);
        }

        static public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, Int64 executedByEntityId)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", executedByEntityId, null);
        }

        static public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, null);
        }


        static public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, SqlTransaction transaction)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, 0, transaction);
        }


        static public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId, SqlTransaction transaction)
        {
            SqlBase st = new SqlBase();
            try
            {
                st.AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, transaction);
            }
            finally
            {
                st = null;
            }
        }

        static public DataTable GetSchema(SqlConnection connection, String tableName)
        {
            SqlBase st = new SqlBase();
            try
            {
                return st.GetSchema(connection, tableName);
            }
            finally
            {
                st = null;
            }
        }

        static public DataTable Select(SqlConnection connection, String SQL)
        {
            SqlBase st = new SqlBase();
            try
            {
                return st.Select(connection, SQL);
            }
            finally
            {
                st = null;
            }
        }

        static public DataTable Select(SqlConnection connection, String SQL, SqlTransaction transaction)
        {
            SqlBase st = new SqlBase();
            try
            {
                return st.Select(connection, SQL, transaction);
            }
            finally
            {
                st = null;
            }
        }


        static public void BulkCopy(SqlConnection connection, DataTable source, String table)
        {
            SqlBase st = new SqlBase();
            try
            {
                BulkCopy(connection, source, table);
            }
            finally
            {
                st = null;
            }
        }

        static public void BulkCopy(SqlConnection connection, DataTable source, String table, SqlTransaction trans)
        {
            SqlBase st = new SqlBase();
            try
            {
                BulkCopy(connection, source, table, trans);
            }
            finally
            {
                st = null;
            }
        }

        static public DataTable selectAllFrom(SqlConnection connection, String tableName, String filter)
        {
            SqlBase st = new SqlBase();
            try
            {
                return st.selectAllFrom(connection, tableName, filter);
            }
            finally
            {
                st = null;
            }
        }

        static public void Insert(SqlConnection connection, String insertSQL, SqlParameterCollection Parameters)
        {
            SqlBase st = new SqlBase();
            try
            {
                Insert(connection, insertSQL, Parameters);
            }
            finally
            {
                st = null;
            }
        }

        static public void Insert2(SqlConnection connection, String insertSQL, SqlParameterCollection Parameters)
        {
            SqlBase st = new SqlBase();
            try
            {
                Insert(connection, insertSQL, Parameters);
            }
            finally
            {
                st = null;
            }
        }

        static public Object ExecuteScalar(SqlConnection connection, String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            SqlBase st = new SqlBase();
            try
            {
                return st.ExecuteScalar(connection, command, commandType, Parameters);
            }
            finally
            {
                st = null;
            }
        }

        static public Object ExecuteScalar(SqlConnection connection, String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            SqlBase st = new SqlBase();
            try
            {
                return st.ExecuteScalar(connection, command, commandType, Parameters, trans);
            }
            finally
            {
                st = null;
            }
        }


        static public DataTable ExecuteDataTable(SqlConnection connection, String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            SqlBase st = new SqlBase();
            try
            {
                return st.ExecuteDataTable(connection, command, commandType, Parameters);
            }
            finally
            {
                st = null;
            }
        }

        static public DataTable ExecuteDataTable(SqlConnection connection, String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            SqlBase st = new SqlBase();
            try
            {
                return st.ExecuteDataTable(connection, command, commandType, Parameters, trans);
            }
            finally
            {
                st = null;
            }
        }


        static public void ExecuteNonQuery(SqlConnection connection, String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            SqlBase st = new SqlBase();
            try
            {
                st.ExecuteNonQuery(connection, command, commandType, Parameters);
            }
            finally
            {
                st = null;
            }
        }

        static public void ExecuteNonQuery(SqlConnection connection, String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            SqlBase st = new SqlBase();
            try
            {
                st.ExecuteNonQuery(connection, command, commandType, Parameters, trans);
            }
            finally
            {
                st = null;
            }
        }


        public static SqlParameterCollection GetSqlParameterObject()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }

    }

    [Serializable()]
    public class SqlBase
    {
        protected Boolean i_debug;
        protected Int32 i_timeout;
        protected String i_lastError;

        public String LastDBError { get { return i_lastError; } }
        public Boolean Debug { get { return i_debug; } set { i_debug = value; } }
        public Int32 Timeout { get { return i_timeout; } set { i_timeout = value; } }

        public SqlBase()
        {
            this.i_timeout = 300;
        }

        public static SqlParameterCollection GetSqlParameterObject()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }

        public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", null);
        }

        public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, null);
        }

        public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, Int64 executedByEntityId)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", executedByEntityId, null);
        }

        public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, null);
        }


        public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, SqlTransaction transaction)
        {
            AddUserLog(connection, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, 0, transaction);
        }

        public void AddUserLog(SqlConnection connection, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId, SqlTransaction transaction)
        {
            SqlParameterCollection par = GetSqlParameterObject();
            par.Add("@date", System.Data.SqlDbType.DateTime).Value = (date.HasValue ? date.Value : DateTime.Now);
            par.Add("@source", System.Data.SqlDbType.VarChar, source.Length).Value = source;
            par.Add("@key", typeof(Int32)).Value = (Int32)key;
            par.Add("@level", typeof(Int32)).Value = (Int32)level;
            par.Add("@proxy_id", System.Data.SqlDbType.BigInt).Value = proxyId;
            par.Add("@enterprise_id", System.Data.SqlDbType.BigInt).Value = enterpriseId;
            par.Add("@context_id", System.Data.SqlDbType.BigInt).Value = contextId;
            par.Add("@resource_id", System.Data.SqlDbType.BigInt).Value = resourceId;
            par.Add("@plugin_id", System.Data.SqlDbType.BigInt).Value = pluginId;
            par.Add("@entity_id", System.Data.SqlDbType.BigInt).Value = entityId;
            par.Add("@identity_id", System.Data.SqlDbType.BigInt).Value = identityId;
            par.Add("@text", System.Data.SqlDbType.VarChar, text.Length).Value = text;
            par.Add("@additional_data", System.Data.SqlDbType.VarChar, additionalData.Length).Value = additionalData;
            par.Add("@executed_by_entity_id", System.Data.SqlDbType.BigInt).Value = executedByEntityId;

            ExecuteNonQuery(connection, "insert into logs ([date] ,[source] ,[key] ,[level] ,[proxy_id] ,[enterprise_id] ,[context_id] ,[resource_id] ,[plugin_id] ,[entity_id] ,[identity_id] ,[text] ,[additional_data], [executed_by_entity_id]) values (@date ,@source ,@key ,@level ,@proxy_id ,@enterprise_id ,@context_id ,@resource_id, @plugin_id ,@entity_id ,@identity_id ,@text ,@additional_data, @executed_by_entity_id)", System.Data.CommandType.Text, par, transaction);

        }

        private String DebugQuery(SqlConnection connection,String source, SqlCommand command, SqlTransaction trans)
        {
            return DebugQuery(connection,source, command, trans, false);
        }

        private String DebugQuery(SqlConnection connection,String source, SqlCommand command, SqlTransaction trans, Boolean force)
        {

            if (!i_debug && !force)
                return "";

            String debug = "/****** Source:  " + source + "    Script Date: " + DateTime.Now.ToString("yyyy-mm-dd HH:mm:ss.ffffff") + " ******/" + Environment.NewLine;

            if (command.Parameters != null)
            {
                foreach (SqlParameter par in command.Parameters)
                {

                    debug += "DECLARE " + par.ParameterName + " " + par.SqlDbType.ToString();
                    if (par.Size > 0)
                        debug += "(" + par.Size + ")";
                    debug += Environment.NewLine;
                    switch (par.SqlDbType)
                    {
                        case SqlDbType.BigInt:
                        case SqlDbType.Int:
                        case SqlDbType.SmallInt:
                        case SqlDbType.Bit:
                            debug += "SET " + par.ParameterName + " = " + par.Value + Environment.NewLine;
                            break;

                        default:
                            debug += "SET " + par.ParameterName + " = '" + par.Value + "'" + Environment.NewLine;
                            break;
                    }

                }
                debug += Environment.NewLine;
            }

            if (command.CommandType == CommandType.StoredProcedure)
                debug += "EXEC ";

            debug += command.CommandText + Environment.NewLine;

            debug += "GO" + Environment.NewLine + Environment.NewLine;

            if (!i_debug)
                return debug;

            SqlCommand cmd = new SqlCommand("insert into debug (text) values(@text)", connection);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = i_timeout;
            cmd.Parameters.Add("@text", SqlDbType.VarChar).Value = debug;

            try
            {

                if (trans != null)
                    cmd.Transaction = trans;

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                cmd = null;
            }

            return debug;
        }

        public DataTable GetSchema(SqlConnection connection, String tableName)
        {

            if ((connection == null) || (connection.State == ConnectionState.Closed))
            {
                i_lastError = "Connection is null";
                return null;
            }

            DataTable tst = new DataTable();
            SqlDataAdapter adp = new SqlDataAdapter("SELECT top 0 * FROM " + tableName, connection);
            adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            tst = adp.FillSchema(tst, SchemaType.Source);

            return tst;
            //return Select("select top 0 * from " + tableName);
        }


        public DataTable Select(SqlConnection connection, String SQL)
        {
            return Select(connection, SQL, null);
        }

        public DataTable Select(SqlConnection connection, String SQL, SqlTransaction transaction)
        {
            i_lastError = "";

            if ((connection == null) || (connection.State == ConnectionState.Closed))
            {
                i_lastError = "Connection is null";
                return null;
            }

            SqlCommand select = null;
            SqlDataAdapter da = null;
            DataSet ds = null;
            try
            {
                select = new SqlCommand(SQL, connection);

                select.CommandTimeout = i_timeout;

                if (transaction != null)
                    select.Transaction = transaction;

                select.CommandType = CommandType.Text;

                DebugQuery(connection,"BaseDB.Select", select, transaction);

                da = new SqlDataAdapter(select);
                ds = new DataSet();
                da.Fill(ds, "data");
                da.Dispose();

                DataTable tmp = ds.Tables["data"];

                return tmp;
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery(connection,"BaseDB.Select", select, transaction, true);

                return null;
            }
            finally
            {
                if (select != null) select.Dispose();
                select = null;

                if (da != null) da.Dispose();
                da = null;

                if (ds != null) ds.Dispose();
                ds = null;
            }
        }

        public void BulkCopy(SqlConnection connection, DataTable source, String table)
        {
            BulkCopy(connection, source, table, null);
        }

        public void BulkCopy(SqlConnection connection, DataTable source, String table, SqlTransaction trans)
        {
            if (trans == null)
            {
                using (SqlBulkCopy bulk = new SqlBulkCopy(connection))
                {
                    bulk.DestinationTableName = table;
                    bulk.WriteToServer(source);
                }
            }
            else
            {
                using (SqlBulkCopy bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans))
                {
                    bulk.DestinationTableName = table;
                    bulk.WriteToServer(source);
                }
            }
        }

        public DataTable selectAllFrom(SqlConnection connection, String tableName, String filter)
        {
            String SQL = "SELECT * " +
                         "FROM [" + tableName + "]";

            if ((filter != null) && (filter != ""))
                SQL += " WHERE " + filter;

            return Select(connection, SQL);
        }

        public void Insert(SqlConnection connection, String insertSQL, SqlParameterCollection Parameters)
        {

            SqlCommand cmd = null;
            SqlDataReader dr = null;
            try
            {
                cmd = new SqlCommand(insertSQL, connection);
                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                DebugQuery(connection,"BaseDB.Insert", cmd, null);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery(connection,"BaseDB.Insert", cmd, null, true);
                throw ex;
            }
            finally
            {
                //if (Parameters != null) Parameters.Clear();
                //Parameters = null;

                if (cmd != null) cmd.Dispose();
                if (dr != null) dr.Close();

                cmd = null;
            }
        }

        public void Insert2(SqlConnection connection, String insertSQL, SqlParameterCollection Parameters)
        {

            SqlCommand cmd = new SqlCommand(insertSQL, connection);
            SqlDataReader dr = null;
            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                DebugQuery(connection,"BaseDB.Insert2", cmd, null);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery(connection,"BaseDB.Insert2", cmd, null, true);
                throw ex;
            }
            finally
            {
                //if (Parameters != null) Parameters.Clear();
                //Parameters = null;

                if (cmd != null) cmd.Dispose();
                if (dr != null) dr.Close();

                cmd = null;
            }
        }

        public Object ExecuteScalar(SqlConnection connection,String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            return ExecuteScalar(connection, command, commandType, Parameters, null);
        }

        public Object ExecuteScalar(SqlConnection connection,String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {

            SqlCommand cmd = new SqlCommand(command, connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = i_timeout;

            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                }

                if (trans != null)
                    cmd.Transaction = trans;

                DebugQuery(connection,"BaseDB.ExecuteScalar", cmd, trans);
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery(connection,"BaseDB.ExecuteScalar", cmd, trans, true);
                throw ex;
            }
            finally
            {
                //if (Parameters != null) Parameters.Clear();
                //Parameters = null;

                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }

        public DataTable ExecuteDataTable(SqlConnection connection, String command)
        {
            return ExecuteDataTable(connection, command, CommandType.Text, null);
        }

        public DataTable ExecuteDataTable(SqlConnection connection,String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            return ExecuteDataTable(connection, command, commandType, Parameters, null);
        }

        public DataTable ExecuteDataTable(SqlConnection connection,String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {

            SqlCommand cmd = new SqlCommand(command, connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = i_timeout;

            SqlDataAdapter da = null;
            DataSet ds = null;
            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                }

                if (trans != null)
                    cmd.Transaction = trans;

                DebugQuery(connection,"BaseDB.ExecuteDataTable", cmd, trans);

                da = new SqlDataAdapter(cmd);
                ds = new DataSet();
                da.Fill(ds, "data");
                da.Dispose();

                DataTable tmp = ds.Tables["data"];

                return tmp;

            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery(connection,"BaseDB.ExecuteDataTable", cmd, trans, true);
                throw ex;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }


        public void ExecuteNonQuery(SqlConnection connection,String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            ExecuteNonQuery(connection,command, commandType, Parameters, null);
        }

        public void ExecuteNonQuery(SqlConnection connection,String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            SqlCommand cmd = new SqlCommand(command, connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = i_timeout;

            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                }

                if (trans != null)
                    cmd.Transaction = trans;


                DebugQuery(connection,"BaseDB.ExecuteNonQuery", cmd, trans);


                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery(connection,"BaseDB.ExecuteNonQuery", cmd, trans, true);
                throw ex;
            }
            finally
            {
                //if (Parameters != null) Parameters.Clear();
                //Parameters = null;

                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }


    }
}
