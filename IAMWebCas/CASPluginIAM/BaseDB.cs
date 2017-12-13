using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace CASPluginIAM
{
    public abstract class BaseDB : IDisposable
    {
        private String i_server;
        private String i_username;
        private String i_password;
        private String i_dbname;
        private SqlConnection i_cn;
        private Boolean i_Opened;
        private String i_ConnectionString;

        private Int32 i_timeout = 300;
        private String i_lastError = "";

        public SqlConnection conn { get { return i_cn; } }
        public String LastError { get { return i_lastError; } }
        //public Boolean isOpenned { get { return i_Opened; } }
        public Int32 Timeout { get { return i_timeout; } set { i_timeout = value; } }

        public BaseDB()
        {
            i_Opened = false;
        }

        public BaseDB(String server, String dbName) :
            this(server, dbName, false) { }

        public BaseDB(String server, String dbName, Boolean useSharedMemory)
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

        public BaseDB(String server, String dbName, String username, String password)
        {
            i_Opened = false;
            i_server = server;
            i_username = username;
            i_password = password;
            i_dbname = dbName;
            i_ConnectionString = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3};", i_server, i_dbname, i_username, i_password);
        }

        public BaseDB(String connectionString)
        {
            i_ConnectionString = connectionString;
        }

        public SqlConnection openDB()
        {
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

        public DataTable GetSchema(String tableName)
        {

            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }


            DataTable tst = new DataTable();
            SqlDataAdapter adp = new SqlDataAdapter("SELECT top 0 * FROM " + tableName, i_cn);
            adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            tst = adp.FillSchema(tst, SchemaType.Source);

            return tst;
            //return Select("select top 0 * from " + tableName);
        }

        public DataTable Select(String SQL)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }


            return Select(SQL, i_cn, null);
        }

        public DataTable Select(String SQL, SqlTransaction transaction)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            return Select(SQL, i_cn, transaction);
        }

        public DataTable Select(String SQL, SqlConnection conn)
        {
            return Select(SQL, conn, null);
        }

        public DataTable Select(String SQL, SqlConnection conn, SqlTransaction transaction)
        {
            i_lastError = "";

            if ((conn == null) || (conn.State == ConnectionState.Closed))
            {
                i_lastError = "Connection is null";
                return null;
            }

            SqlCommand select = null;
            SqlDataAdapter da = null;
            DataSet ds = null;
            try
            {
                select = new SqlCommand(SQL, conn);

                select.CommandTimeout = i_timeout;

                if (transaction != null)
                    select.Transaction = transaction;

                select.CommandType = CommandType.Text;

                da = new SqlDataAdapter(select);
                ds = new DataSet();
                da.Fill(ds, "data");
                da.Dispose();

                DataTable tmp = ds.Tables["data"];

                return tmp;
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message;

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

        public void BulkCopy(DataTable source, String table)
        {
            BulkCopy(source, table, null);
        }

        public void BulkCopy(DataTable source, String table, SqlTransaction trans)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            BulkCopy(source, table, i_cn, trans);

        }

        public void BulkCopy(DataTable source, String table, SqlConnection conn, SqlTransaction trans)
        {
            if (trans == null)
            {
                using (SqlBulkCopy bulk = new SqlBulkCopy(conn))
                {
                    bulk.DestinationTableName = table;
                    bulk.WriteToServer(source);
                }
            }
            else
            {
                using (SqlBulkCopy bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, trans))
                {
                    bulk.DestinationTableName = table;
                    bulk.WriteToServer(source);
                }
            }
        }

        public DataTable selectAllFrom(String tableName, String filter)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            return selectAllFrom(tableName, filter, i_cn);
        }

        public DataTable selectAllFrom(String tableName, String filter, SqlConnection conn)
        {
            String SQL = "SELECT * " +
                         "FROM [" + tableName + "]";

            if ((filter != null) && (filter != ""))
                SQL += " WHERE " + filter;

            return Select(SQL, conn);
        }

        public static SqlParameterCollection GetSqlParameterObject()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }

        public void Insert(String insertSQL, SqlParameterCollection Parameters)
        {
            SqlConnection conn = new SqlConnection(i_ConnectionString);
            conn.Open();

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            SqlCommand cmd = null;
            SqlDataReader dr = null;
            try
            {
                cmd = new SqlCommand(insertSQL, conn);
                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message;
                throw ex;
            }
            finally
            {
                if (Parameters != null) Parameters.Clear();
                Parameters = null;

                if (cmd != null) cmd.Dispose();
                if (dr != null) dr.Close();
                if (conn != null) conn.Close();
                if (conn != null) conn.Dispose();

                cmd = null;
            }
        }

        public void Insert2(String insertSQL, SqlParameterCollection Parameters)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            SqlCommand cmd = new SqlCommand(insertSQL, i_cn);
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

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message;
                throw ex;
            }
            finally
            {
                if (Parameters != null) Parameters.Clear();
                Parameters = null;

                if (cmd != null) cmd.Dispose();
                if (dr != null) dr.Close();

                cmd = null;
            }
        }

        public Object ExecuteScalar(String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            return ExecuteScalar(command, commandType, Parameters, null);
        }

        public Object ExecuteScalar(String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            SqlCommand cmd = new SqlCommand(command, i_cn);
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

                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message;
                throw ex;
            }
            finally
            {
                if (Parameters != null) Parameters.Clear();
                Parameters = null;

                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }


        public DataTable ExecuteDataTable(String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            return ExecuteDataTable(command, commandType, Parameters, null);
        }

        public DataTable ExecuteDataTable(String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            SqlCommand cmd = new SqlCommand(command, i_cn);
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

                da = new SqlDataAdapter(cmd);
                ds = new DataSet();
                da.Fill(ds, "data");
                da.Dispose();

                DataTable tmp = ds.Tables["data"];

                return tmp;

            }
            catch (Exception ex)
            {
                i_lastError = ex.Message;
                throw ex;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }


        public void ExecuteNonQuery(String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            ExecuteNonQuery(command, commandType, Parameters, null);
        }

        public void ExecuteNonQuery(String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            SqlCommand cmd = new SqlCommand(command, i_cn);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = i_timeout;

            String debug = "";

            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                    {
                        debug += "DECLARE " + par.ParameterName + " " + par.SqlDbType.ToString();
                        if (par.Size > 0)
                            debug += "(" + par.Size + ")";
                        debug += Environment.NewLine;
                        debug += "SET " + par.ParameterName + " = " + par.Value + Environment.NewLine;

                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                if (trans != null)
                    cmd.Transaction = trans;

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message;
                throw ex;
            }
            finally
            {
                if (Parameters != null) Parameters.Clear();
                Parameters = null;

                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }

    }
}
