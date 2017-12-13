using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using IAM.GlobalDefs;
using IAM.SQLDefs;

namespace IAM.SQLDB
{
    public class MSSQLDB : SqlBase, IDisposable
    {
        private Int32 i_pid;
        private String i_server;
        private String i_username;
        private String i_password;
        private String i_dbname;
        private SqlConnection i_cn;
        private Boolean i_Opened;
        private String i_ConnectionString;

        private Boolean i_externalConn = false;

        public SqlConnection conn { get { return i_cn; } }

        public MSSQLDB()
        {
            i_Opened = false;
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            base.i_timeout = 300;
        }

        public MSSQLDB(String server, String dbName) :
            this(server, dbName, false) { }

        public MSSQLDB(String server, String dbName, Boolean useSharedMemory)
            : this()
        {
            i_Opened = false;
            i_server = server;
            i_username = "";
            i_password = "";
            i_dbname = dbName;
            if (useSharedMemory)
                i_ConnectionString = string.Format("Data Source={0};Initial Catalog={1};Integrated Security=True;Network Library=dbmslpcn;", i_server, i_dbname);
            else
                i_ConnectionString = string.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;", i_server, i_dbname);
        }

        public MSSQLDB(String server, String dbName, String username, String password)
            : this()
        {
            i_Opened = false;
            i_server = server;
            i_username = username;
            i_password = password;
            i_dbname = dbName;
            i_ConnectionString = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3};", i_server, i_dbname, i_username, i_password);
        }

        public MSSQLDB(String connectionString)
            : this()
        {
            i_ConnectionString = connectionString;
        }

        public MSSQLDB(SqlConnection connection)
            : this()
        {
            i_cn = connection;
            i_externalConn = true;
            i_Opened = true;
            base.i_timeout = 300;
        }

        public SqlConnection openDB()
        {
            if (i_externalConn)
                return i_cn;

            
            i_cn = new SqlConnection(i_ConnectionString);
            if (i_cn.State == ConnectionState.Closed)
            {
                i_cn.Open();
            }

            i_Opened = true;
            return i_cn;
        }

        public void Dispose()
        {
            i_ConnectionString = null;
            closeDB();
        }

        public void closeDB()
        {
            i_Opened = false;
            try
            {
                i_cn.Close();
                i_cn = null;
            }
            catch { }
        }
        
        
        public void AddUserLog( LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text)
        {
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", null);
        }

        public void AddUserLog( LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData)
        {
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, null);
        }

        public void AddUserLog(LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, SqlTransaction transaction)
        {
            AddUserLog(i_cn, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, transaction);
        }

        public void ServiceStatus(String serviceName, String additionsData, SqlTransaction transaction)
        {
            SqlParameterCollection par = GetSqlParameterObject();
            par.Add("@name", typeof(String)).Value = serviceName;
            par.Add("@data", typeof(String)).Value = additionsData;
            
            ExecuteNonQuery(i_cn, "sp_service_status", System.Data.CommandType.StoredProcedure, par, transaction);

        }

        /******************************************
        *** Reimplementa os métodos, porém utilizando a conexão local *******/

        public DataTable GetSchema(String tableName)
        {
            return GetSchema(i_cn, tableName);
        }

        public DataTable Select(String SQL)
        {
            return Select(i_cn, SQL);
        }

        public DataTable Select(String SQL, SqlTransaction transaction)
        {
            return Select(i_cn, SQL, transaction);
        }


        public void BulkCopy(DataTable source, String table)
        {
            BulkCopy(i_cn, source, table);
        }

        public void BulkCopy(DataTable source, String table, SqlTransaction trans)
        {
            BulkCopy(i_cn, source, table, trans);
        }

        public DataTable selectAllFrom(String tableName, String filter)
        {
            return selectAllFrom(i_cn, tableName, filter);
        }

        public void Insert(String insertSQL, SqlParameterCollection Parameters)
        {
            Insert(i_cn, insertSQL, Parameters);
        }

        public void Insert2(String insertSQL, SqlParameterCollection Parameters)
        {
            Insert(i_cn, insertSQL, Parameters);
        }

        public Object ExecuteScalar(String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            return ExecuteScalar(i_cn, command, commandType, Parameters);
        }

        public Object ExecuteScalar(String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            return ExecuteScalar(i_cn, command, commandType, Parameters, trans);
        }


        public DataTable ExecuteDataTable(String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            return ExecuteDataTable(i_cn, command, commandType, Parameters);
        }

        public DataTable ExecuteDataTable(String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            return ExecuteDataTable(i_cn, command, commandType, Parameters, trans);
        }


        public void ExecuteNonQuery(String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            ExecuteNonQuery(i_cn, command, commandType, Parameters);
        }

        public void ExecuteNonQuery(String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            ExecuteNonQuery(i_cn, command, commandType, Parameters, trans);
        }

    }
}
