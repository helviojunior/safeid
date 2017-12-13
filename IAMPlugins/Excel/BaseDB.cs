using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Reflection;
using System.IO;
using IAM.PluginInterface;

namespace Excel
{
    public abstract class BaseDB : IDisposable
    {
        private OleDbConnection i_cn;
        private Boolean i_Opened;
        private String i_ConnectionString;

        private Int32 i_timeout = 300;
        private String i_lastError = "";

        public OleDbConnection conn { get { return i_cn; } }
        public String LastError { get { return i_lastError; } }
        //public Boolean isOpenned { get { return i_Opened; } }
        public Int32 Timeout { get { return i_timeout; } set { i_timeout = value; } }

        public event LogEvent OnLog;

        public BaseDB()
        {
            i_Opened = false;
        }

        public BaseDB(FileInfo fileName)
        {
            i_Opened = false;

            switch (fileName.Extension.ToLower())
            {
                case ".xls":
                    i_ConnectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=\"{0}\";Extended Properties=\"Excel 8.0;HDR=YES\";", fileName.FullName);
                    break;

                case ".xlsx":
                    i_ConnectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=\"{0}\";Extended Properties=\"Excel 12.0 xml;HDR=YES\";", fileName.FullName);
                    break;
            }
        }

        private void DebugLog(PluginLogType type, String text)
        {
#if DEBUG
            if (OnLog != null)
                OnLog(this, type, text);
#endif
        }


        public OleDbConnection createAndOpenDB(String tableName, DataColumnCollection columns)
        {
            List<String> columnNames = new List<string>();
            foreach (DataColumn c in columns)
                if (!columnNames.Contains(c.ColumnName))
                    columnNames.Add(c.ColumnName);

            return createAndOpenDB(tableName, columnNames);
        }


        public OleDbConnection createAndOpenDB(String tableName, List<String> columnNames)
        {

            List<String> cols = new List<string>();
            foreach (String c in columnNames)
                cols.Add(c + " CHAR(255)");

            string createTableScript = "CREATE TABLE [" + tableName + "] (";
            createTableScript += String.Join(",", cols);
            createTableScript += ")";

            using (OleDbConnection conObj = new OleDbConnection(i_ConnectionString))
            {
                using (OleDbCommand cmd = new OleDbCommand(createTableScript, conObj))
                {
                    conObj.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            columnNames.Clear();
            columnNames = null;

            i_cn = new OleDbConnection(i_ConnectionString);
            if (i_cn.State == ConnectionState.Closed)
            {
                i_cn.Open();
            }

            i_Opened = true;
            return i_cn;
        }


        public OleDbConnection openDB()
        {
            i_cn = new OleDbConnection(i_ConnectionString);
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
            OleDbDataAdapter adp = new OleDbDataAdapter("SELECT * FROM [" + tableName + "$]", i_cn);
            adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            tst = adp.FillSchema(tst, SchemaType.Source);

            if (tst == null)
            {
                tst = new DataTable();
                adp = new OleDbDataAdapter("SELECT * FROM [" + tableName + "]", i_cn);
                adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                tst = adp.FillSchema(tst, SchemaType.Source);
            }

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

        public DataTable Select(String SQL, Int32 startRecod, Int32 maxRecord)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }


            return Select(SQL, i_cn, null, startRecod, maxRecord);
        }

        public DataTable Select(String SQL, OleDbTransaction transaction)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            return Select(SQL, i_cn, transaction);
        }

        public DataTable Select(String SQL, OleDbTransaction transaction, Int32 startRecod, Int32 maxRecord)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            return Select(SQL, i_cn, transaction, startRecod, maxRecord);
        }

        public DataTable Select(String SQL, OleDbConnection conn)
        {
            return Select(SQL, conn, null);
        }

        public DataTable Select(String SQL, OleDbConnection conn, OleDbTransaction transaction)
        {
            return Select(SQL, conn, transaction, -1, -1);
        }

        public DataTable Select(String SQL, OleDbConnection conn, OleDbTransaction transaction, Int32 startRecod, Int32 maxRecord)
        {
            i_lastError = "";

            if ((conn == null) || (conn.State == ConnectionState.Closed))
            {
                i_lastError = "Connection is null";
                return null;
            }

            OleDbCommand select = null;
            OleDbDataAdapter da = null;
            DataSet ds = null;
            try
            {
                select = new OleDbCommand(SQL, conn);

                select.CommandTimeout = i_timeout;

                if (transaction != null)
                    select.Transaction = transaction;

                select.CommandType = CommandType.Text;

                da = new OleDbDataAdapter(select);
                ds = new DataSet();
                if ((startRecod > 0) && (maxRecord > 0))
                    da.Fill(ds, startRecod, maxRecord, "data");
                else
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

        public DataTable selectAllFrom(String tableName, String filter)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            return selectAllFrom(tableName, filter, i_cn);
        }

        public DataTable selectAllFrom(String tableName, String filter, OleDbConnection conn)
        {
            String SQL = "SELECT * " +
                         "FROM [" + tableName + "]";

            if ((filter != null) && (filter != ""))
                SQL += " WHERE " + filter;

            return Select(SQL, conn);
        }

        public static OleDbParameterCollection GetSqlParameterObject()
        {
            return (OleDbParameterCollection)typeof(OleDbParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }

        public void Insert(String insertSQL, OleDbParameterCollection Parameters)
        {
            OleDbConnection conn = new OleDbConnection(i_ConnectionString);
            conn.Open();

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            OleDbCommand cmd = null;
            OleDbDataReader dr = null;
            try
            {
                cmd = new OleDbCommand(insertSQL, conn);
                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (OleDbParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.OleDbType, par.Size).Value = par.Value;
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

        public void Insert2(String insertSQL, OleDbParameterCollection Parameters)
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

            DebugLog(PluginLogType.Debug, "insertSQL.SQL = " + insertSQL);

            OleDbCommand cmd = new OleDbCommand(insertSQL, i_cn);
            OleDbDataReader dr = null;
            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (OleDbParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.OleDbType, par.Size).Value = par.Value;
                    }
                }

                DebugLog(PluginLogType.Debug, "Insert2.Parameters " + cmd.Parameters.Count);

                foreach (OleDbParameter p in cmd.Parameters)
                    DebugLog(PluginLogType.Debug, "Insert2.Parameters[" + p.ParameterName + "] = " + p.Value);

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

        public Object ExecuteScalar(String command, CommandType commandType, OleDbParameterCollection Parameters)
        {
            return ExecuteScalar(command, commandType, Parameters, null);
        }

        public Object ExecuteScalar(String command, CommandType commandType, OleDbParameterCollection Parameters, OleDbTransaction trans)
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

            OleDbCommand cmd = new OleDbCommand(command, i_cn);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = i_timeout;

            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (OleDbParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.OleDbType, par.Size).Value = par.Value;
                        cmd.CommandText = cmd.CommandText.Replace(par.ParameterName, "?");
                    }
                }

                if (trans != null)
                    cmd.Transaction = trans;

                DebugLog(PluginLogType.Debug, "ExecuteScalar.SQL = " + cmd.CommandText);

                DebugLog(PluginLogType.Debug, "ExecuteScalar.Parameters " + cmd.Parameters.Count);

                foreach (OleDbParameter p in cmd.Parameters)
                    DebugLog(PluginLogType.Debug, "ExecuteScalar.Parameters[" + p.ParameterName + "] = " + p.Value);

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


        public DataTable ExecuteDataTable(String command, CommandType commandType, OleDbParameterCollection Parameters)
        {
            return ExecuteDataTable(command, commandType, Parameters, null);
        }

        public DataTable ExecuteDataTable(String command, CommandType commandType, OleDbParameterCollection Parameters, OleDbTransaction trans)
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

            OleDbCommand cmd = new OleDbCommand(command, i_cn);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = i_timeout;

            OleDbDataAdapter da = null;
            DataSet ds = null;
            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (OleDbParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.OleDbType, par.Size).Value = par.Value;
                        cmd.CommandText = cmd.CommandText.Replace(par.ParameterName, "?");
                    }
                }

                if (trans != null)
                    cmd.Transaction = trans;


                DebugLog(PluginLogType.Debug, "ExecuteDataTable.SQL = " + cmd.CommandText);

                DebugLog(PluginLogType.Debug, "ExecuteDataTable.Parameters " + cmd.Parameters.Count);

                foreach (OleDbParameter p in cmd.Parameters)
                    DebugLog(PluginLogType.Debug, "ExecuteDataTable.Parameters[" + p.ParameterName + "] = " + p.Value);


                da = new OleDbDataAdapter(cmd);
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


        public void ExecuteNonQuery(String command, CommandType commandType, OleDbParameterCollection Parameters)
        {
            ExecuteNonQuery(command, commandType, Parameters, null);
        }

        public void ExecuteNonQuery(String command, CommandType commandType, OleDbParameterCollection Parameters, OleDbTransaction trans)
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

            OleDbCommand cmd = new OleDbCommand(command, i_cn);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = i_timeout;

            String debug = "";

            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (OleDbParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.OleDbType, par.Size).Value = par.Value;
                        cmd.CommandText = cmd.CommandText.Replace(par.ParameterName, "?");
                    }
                }

                if (trans != null)
                    cmd.Transaction = trans;


                DebugLog(PluginLogType.Debug, "ExecuteNonQuery.SQL = " + cmd.CommandText);

                DebugLog(PluginLogType.Debug, "ExecuteNonQuery.Parameters " + cmd.Parameters.Count);

                foreach (OleDbParameter p in cmd.Parameters)
                    DebugLog(PluginLogType.Debug, "ExecuteNonQuery.Parameters[" + p.ParameterName + "] = " + p.Value);

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
