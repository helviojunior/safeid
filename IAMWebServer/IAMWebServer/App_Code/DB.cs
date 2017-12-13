using System;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Reflection;
using IAM.GlobalDefs;
using System.Web.Hosting;
using SafeTrend.Data;


    /// <summary>
    /// Summary description for DB
    /// </summary>
public class DB : SqlBaseStatic
{

    static public void AddLinkCount(Page page)
    {
        try
        {

            Int64 id = 0;

            if (!String.IsNullOrWhiteSpace((String)page.RouteData.Values["id"]))
                Int64.TryParse((String)page.RouteData.Values["id"], out id);

            //Não selecinou nenhum item, ou seja menu principal
            if (id == 0)
            {
                //Opções para gravação dos links na base
                Int64 enterpriseId = 0;
                Int64 userId = 0;
                String submodule = "dashboard";

                if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                    enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

                if ((page.Session["login"]) != null && (page.Session["login"] is LoginData))
                    userId = ((LoginData)page.Session["login"]).Id;

                if (userId > 0 && enterpriseId > 0)
                {

                    String path = page.Request.ServerVariables["PATH_INFO"].ToLower();
                    String ApplicationVirtualPath = HostingEnvironment.ApplicationVirtualPath;
                    path = path.Substring(ApplicationVirtualPath.Length); //Corta o virtual path

                    if (path.IndexOf("_admin") == 0)
                        return;

                    if (!String.IsNullOrWhiteSpace((String)page.RouteData.Values["module"]))
                        submodule = (String)page.RouteData.Values["module"];

                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                    par.Add("@entity_id", typeof(Int64)).Value = userId;
                    par.Add("@module", typeof(String)).Value = "admin";
                    par.Add("@submodule", typeof(String)).Value = submodule;
                    par.Add("@path", typeof(String)).Value = path;

                    ExecuteNonQuery("sp_insert_link_count", System.Data.CommandType.StoredProcedure, par);
                }
            }
        }
        catch { }
    }


    static public SqlConnection GetConnection()
    {

        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        conn.Open();

        return conn;
    }

    static public String GetConnectionString()
    {
        return ConfigurationManager.AppSettings["conectionInfo"];
    }

    /******************************************
    *** Reimplementa os métodos, porém utilizando a conexão local *******/


    static public void AddUserLog( LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text)
    {
        AddUserLog( key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", null);
    }

    static public void AddUserLog( LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData)
    {
        AddUserLog( key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, null);
    }

    static public void AddUserLog( LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, Int64 executedByEntityId)
    {
        AddUserLog( key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", executedByEntityId, null);
    }

    static public void AddUserLog( LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId)
    {
        AddUserLog( key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, null);
    }


    static public void AddUserLog( LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, SqlTransaction transaction)
    {
        AddUserLog( key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, 0, transaction);
    }

    static public void AddUserLog(LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId, SqlTransaction transaction)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            AddUserLog(i_cn, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, transaction);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public DataTable GetSchema(String tableName)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            return GetSchema(i_cn, tableName);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public DataTable Select(String SQL)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            return Select(i_cn, SQL);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public DataTable Select(String SQL, SqlTransaction transaction)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            return Select(i_cn, SQL, transaction);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }


    static public void BulkCopy(DataTable source, String table)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            BulkCopy(i_cn, source, table);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public void BulkCopy(DataTable source, String table, SqlTransaction trans)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            BulkCopy(i_cn, source, table, trans);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public DataTable selectAllFrom(String tableName, String filter)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            return selectAllFrom(i_cn, tableName, filter);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public void Insert(String insertSQL, DbParameterCollection Parameters)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            Insert(i_cn, insertSQL, Parameters);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public void Insert2(String insertSQL, DbParameterCollection Parameters)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            Insert(i_cn, insertSQL, Parameters);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public Object ExecuteScalar(String command, CommandType commandType, DbParameterCollection Parameters)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            return ExecuteScalar(i_cn, command, commandType, Parameters);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public Object ExecuteScalar(String command, CommandType commandType, DbParameterCollection Parameters, SqlTransaction trans)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            return ExecuteScalar(i_cn, command, commandType, Parameters, trans);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }


    static public DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection Parameters)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            return ExecuteDataTable(i_cn, command, commandType, Parameters);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection Parameters, SqlTransaction trans)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            return ExecuteDataTable(i_cn, command, commandType, Parameters, trans);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }


    static public void ExecuteNonQuery(String command, CommandType commandType, DbParameterCollection Parameters)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            ExecuteNonQuery(i_cn, command, commandType, Parameters);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }

    static public void ExecuteNonQuery(String command, CommandType commandType, DbParameterCollection Parameters, SqlTransaction trans)
    {
        SqlConnection i_cn = GetConnection();
        try
        {
            ExecuteNonQuery(i_cn, command, commandType, Parameters, trans);
        }
        finally
        {
            if (i_cn != null) i_cn.Close();
            if (i_cn != null) i_cn.Dispose();
            i_cn = null;
        }
    }


    /*

    static public Object ExecuteSQLScalar(String sql, DbParameterCollection parameters, CommandType commandType)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        conn.Open();
            
        return ExecuteSQLScalar(conn, sql, parameters, commandType, null);
    }

    static public Object ExecuteSQLScalar(String sql, DbParameterCollection parameters, CommandType commandType, SqlTransaction trans)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        conn.Open();

        return ExecuteSQLScalar(conn, sql, parameters, commandType, trans);
    }


    static public Object ExecuteSQLScalar(SqlConnection conn, String sql, DbParameterCollection parameters, CommandType commandType, SqlTransaction trans)
    {
        Object ret = null;

        if (conn.State == ConnectionState.Closed)
            conn.Open();

        SqlCommand cmd = null;
        SqlDataReader dr = null;
        try
        {

            cmd = new SqlCommand(sql, conn);
            cmd.CommandType = commandType;
            if (parameters != null)
            {
                cmd.Parameters.Clear();
                foreach (SqlParameter par in parameters)
                {
                    cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                }
            }

            if (trans != null)
                cmd.Transaction = trans;

            ret = cmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (cmd != null) cmd.Dispose();
            if (dr != null) dr.Close();
            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();

            cmd = null;
        }

        return ret;
    }

    static public void ExecuteSQL(String sql, DbParameterCollection parameters, CommandType commandType)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        conn.Open();

        ExecuteSQL(conn, sql, parameters, commandType, null);
    }

    static public void ExecuteSQL(String sql, DbParameterCollection parameters, CommandType commandType, SqlTransaction trans)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        conn.Open();

        ExecuteSQL(conn, sql, parameters, commandType, trans);
    }

    static public void ExecuteSQL(SqlConnection conn, String sql, DbParameterCollection parameters, CommandType commandType, SqlTransaction trans)
    {
        if (conn.State == ConnectionState.Closed)
            conn.Open();

        SqlCommand cmd = null;
        SqlDataReader dr = null;
        try
        {
            cmd = new SqlCommand(sql, conn);
            cmd.CommandType = commandType;
            if (parameters != null)
            {
                cmd.Parameters.Clear();
                foreach (SqlParameter par in parameters)
                {
                    cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                }
            }

            if (trans != null)
                cmd.Transaction = trans;

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (cmd != null) cmd.Dispose();
            if (dr != null) dr.Close();
            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();

            cmd = null;
        }
    }


    static public DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection Parameters)
    {
        return ExecuteDataTable(command, commandType, Parameters, null);
    }

    static public DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection Parameters, SqlTransaction trans)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        conn.Open();

        return ExecuteDataTable(conn, command, commandType, Parameters, trans);
    }

    static public DataTable ExecuteDataTable(SqlConnection conn, String command, CommandType commandType, DbParameterCollection Parameters, SqlTransaction trans)
    {
        if (conn.State == ConnectionState.Closed)
            conn.Open();

        Int16 step = 0;
        while ((step < 10) && (conn.State == ConnectionState.Connecting))
        {
            System.Threading.Thread.Sleep(100);
        }

        SqlCommand cmd = new SqlCommand(command, conn);
        cmd.CommandType = commandType;

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
            throw ex;
        }
        finally
        {
            if (Parameters != null) Parameters.Clear();
            Parameters = null;

            if (cmd != null) cmd.Dispose();
            cmd = null;

            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();

        }
    }




    static public DataTable Select(String sql)
    {
        return Select(sql, null);
    }

    static public DataTable Select(SqlConnection conn, String sql)
    {
        return Select(conn, sql, null, null);
    }

    static public DataTable Select(String sql, DbParameterCollection Parameters)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        try
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            return Select(conn, sql, Parameters, null);
        }
        finally
        {
            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();

        }
    }

    static public DataTable Select(SqlConnection conn, String sql, DbParameterCollection Parameters, SqlTransaction trans)
    {
        SqlCommand cmd = new SqlCommand(sql, conn);
        SqlDataReader dr = null;
        try
        {

            //dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            //this._dg.DataSource = dr;
            //this._dg.DataBind();

            if (Parameters != null)
            {
                cmd.Parameters.Clear();
                foreach (SqlParameter par in Parameters)
                {
                    cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                }
            }


            if (trans != null)
                cmd.Transaction = trans;

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();

            da.Fill(ds, "data");

            return ds.Tables["data"];

        }
        finally
        {
            if (dr != null) dr.Close();

            cmd = null;
        }
    }

    static public DataTable Select(String sql, Int32 startRecord, Int32 maxRecords)
    {
        return Select(sql, startRecord, maxRecords, null);
    }

    static public DataTable Select(String sql, Int32 startRecord, Int32 maxRecords, DbParameterCollection Parameters)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        SqlCommand cmd = new SqlCommand(sql, conn);
        SqlDataReader dr = null;
        try
        {

            conn.Open();
            //dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            //this._dg.DataSource = dr;
            //this._dg.DataBind();


            if (Parameters != null)
            {
                cmd.Parameters.Clear();
                foreach (SqlParameter par in Parameters)
                {
                    cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                }
            }

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();

            da.Fill(ds, startRecord, maxRecords, "data");

            return ds.Tables["data"];

        }
        finally
        {
            if (dr != null) dr.Close();
            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();

            cmd = null;
        }
    }

    static public void InsertDebug(Page page, String text)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        SqlCommand cmd = new SqlCommand("INSERT INTO debug (page, [client], [text]) values (@page, @client, @text);", conn);
        SqlDataReader dr = null;
        try
        {
            conn.Open();

            cmd.Parameters.Clear();
            if (page == null)
            {
                cmd.Parameters.Add("@page", typeof(String), 255).Value = "";

                if (text == null)
                    cmd.Parameters.Add("@text", typeof(String), 255).Value = "null";
                else
                    cmd.Parameters.Add("@text", typeof(String), text.Length).Value = text;

                cmd.Parameters.Add("@client", typeof(String), 255).Value = "";

            }
            else
            {
                cmd.Parameters.Add("@page", typeof(String), 255).Value = page.Request.Params["PATH_INFO"];

                if (text == null)
                    cmd.Parameters.Add("@text", typeof(String), 255).Value = "null";
                else
                    cmd.Parameters.Add("@text", typeof(String), text.Length).Value = text;

                if (page.Request.Params["REMOTE_ADDR"] == null)
                    cmd.Parameters.Add("@client", typeof(String), 255).Value = "";
                else
                    cmd.Parameters.Add("@client", typeof(String), 50).Value = page.Request.Params["REMOTE_ADDR"];
            }
            cmd.ExecuteNonQuery();
        }
        catch { }
        finally
        {
            if (dr != null) dr.Close();
            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();

            cmd = null;
        }
    }

    static public void InsertDebug(HttpContext context, String text)
    {
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        SqlCommand cmd = new SqlCommand("INSERT INTO debug (page, [client], [text]) values (@page, @client, @text);", conn);
        SqlDataReader dr = null;
        try
        {
            conn.Open();

            cmd.Parameters.Clear();
            if (context == null)
            {
                cmd.Parameters.Add("@page", typeof(String), 255).Value = "";

                if (text == null)
                    cmd.Parameters.Add("@text", typeof(String), 255).Value = "null";
                else
                    cmd.Parameters.Add("@text", typeof(String), text.Length).Value = text;

                cmd.Parameters.Add("@client", typeof(String), 255).Value = "";

            }
            else
            {
                cmd.Parameters.Add("@page", typeof(String), 255).Value = context.Request.Params["PATH_INFO"];

                if (text == null)
                    cmd.Parameters.Add("@text", typeof(String), 255).Value = "null";
                else
                    cmd.Parameters.Add("@text", typeof(String), text.Length).Value = text;

                if (context.Request.Params["REMOTE_ADDR"] == null)
                    cmd.Parameters.Add("@client", typeof(String), 255).Value = "";
                else
                    cmd.Parameters.Add("@client", typeof(String), 50).Value = context.Request.Params["REMOTE_ADDR"];
            }
            cmd.ExecuteNonQuery();
        }
        catch { }
        finally
        {
            if (dr != null) dr.Close();
            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();

            cmd = null;
        }
    }

    static public void InsertTrack(Page page)
    {


        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["conectionInfo"]);
        SqlCommand cmd = new SqlCommand("INSERT INTO track ([client], client_id, page, page_querystring, [referer], referer_querystring) values (@client, @client_id, @page, @page_querystring , @referer, @referer_querystring);", conn);
        SqlDataReader dr = null;
        try
        {
            conn.Open();

            cmd.Parameters.Clear();

            if (page.Request.Params["REMOTE_ADDR"] == null)
                cmd.Parameters.Add("@client", typeof(String), 255).Value = "";
            else
                cmd.Parameters.Add("@client", typeof(String), 50).Value = page.Request.Params["REMOTE_ADDR"];

            Int64 clientID = 0;
            if ((page.Session["user_info"] != null) && (page.Session["user_info"] is System.Data.DataRow))
                clientID = (Int64)((DataRow)page.Session["user_info"])["id"];

            cmd.Parameters.Add("@client_id", typeof(String), 50).Value = clientID;

            String sPage = page.Request.Params["PATH_INFO"];
            String sQuerystring = "";

            if (page.Request.QueryString.Keys.Count > 0)
            {
                sQuerystring += "?";

                foreach (String key in page.Request.QueryString.Keys)
                {
                    sQuerystring += key + "=" + page.Request.QueryString[key] + "&";
                }
            }

            sQuerystring = sQuerystring.Trim("&=".ToCharArray());

            cmd.Parameters.Add("@page", typeof(String), 255).Value = sPage;
            cmd.Parameters.Add("@page_querystring", typeof(String), 2000).Value = sQuerystring;

            if (page.Request.Params["HTTP_REFERER"] == null)
            {
                cmd.Parameters.Add("@referer", typeof(String), 2000).Value = "";
                cmd.Parameters.Add("@referer_querystring", typeof(String), 2000).Value = "";
            }
            else
            {
                Uri referer = new Uri(page.Request.Params["HTTP_REFERER"]);
                try
                {
                    cmd.Parameters.Add("@referer", typeof(String), 2000).Value = referer.Scheme + "://" + referer.Host + (referer.Port != 80 ? ":" + referer.Port : "") + referer.AbsolutePath;
                }
                catch
                {
                    cmd.Parameters.Add("@referer", typeof(String), 2000).Value = referer.AbsoluteUri;
                }
                cmd.Parameters.Add("@referer_querystring", typeof(String), 2000).Value = referer.Query;
            }

            cmd.ExecuteNonQuery();
        }
        catch { }
        finally
        {
            if (dr != null) dr.Close();
            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();

            cmd = null;
        }
    }*/



}

