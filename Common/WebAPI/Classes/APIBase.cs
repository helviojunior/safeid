/// 
/// @file APIBase.cs
/// <summary>
/// Implementações da classe APIBase. 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 15/10/2013
/// $Id: APIBase.cs, v1.0 2013/10/15 Helvio Junior $

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.Data.SqlClient;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.WebAPI
{
   
    /// <summary>
    /// Definição do delegate de erro
    /// </summary>
    public delegate void Error(ErrorType type, String data, String debug, Dictionary<String, Object> additionalReturn);

    /// <summary>
    /// Definição do delegate de autenticação externa
    /// </summary>
    public delegate AccessControl ExternalAccessControl(String method, String auth, AccessControl preCtrl, Dictionary<String, Object> parameters);


    /// <summary>
    /// Definição do retorno para autenticação externa
    /// </summary>
    public class AccessControl
    {
        public Boolean Result { get; set; }
        public Int64 EntityId { get; set; }

        public AccessControl(Int64 entityId, Boolean result)
        {
            this.EntityId = entityId;
            this.Result = result;
        }
    }


    /// <summary>
    /// Classe abstrata utilizada como base para todas as requisiçoes da API
    /// As classes de processamento obrigatoriamente serão derivadas desta classe
    /// </summary>
    internal abstract class APIBase : IAMDatabase
    {
        /// <summary>
        /// Método abstrato de processamentoda requisição
        /// A classe derivada deverá implementa-lo, e todo o processamento da requisição será nele
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="enterpriseId">ID da empresa</param>
        /// <param name="method">String com o método que deverá ser processado</param>
        /// <param name="auth">String com a chave de autenticação.</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        public abstract Object Process(SqlConnection sqlConnection, Int64 enterpriseId, String method, String auth, Dictionary<String, Object> parameters);

        /// <summary>
        /// Método abstrato utilizando o delegate Error
        /// </summary>
        public abstract event Error Error;

        /// <summary>
        /// Método abstrato utilizando o delegate ExternalAccessControl
        /// </summary>
        public abstract event ExternalAccessControl ExternalAccessControl;

        public AccessControl Acl { get; protected set; }

        /// <summary>
        /// Método stático que realiza a criação da instância da classe derivada que precessará o método em questão
        /// O método passado pela requisição JSON será no seguinte formato 'classe.metodo' ex.: 'user.login'
        /// Onde 'user' é o nome da classe obrigatoriamente derivada daesta classe 'APIBase' e 
        /// 'login' é o método que a classe 'user' deverá executar.
        /// 
        /// Esta chamada fará os seguintes passos:
        /// 1 - Varredura em todos os assembly contidos nesta dll
        /// 2 - Busca todos que são derivados desta classe 'APIBase'
        /// 3 - Verifica se o nome da classe é igual ao nome passado no parâmetro 'method'
        /// </summary>
        /// <param name="method">Método que deverá ser executado no formato classe.metodo</param>
        public static APIBase CreateInstance(String method)
        {
            method = method.ToLower();
            String[] mp = method.Split(".".ToCharArray(), 2);

            if (mp.Length != 2)
                return null;

            try
            {
                Assembly assembly = Assembly.GetAssembly(typeof(APIBase));

                Type[] classes = assembly.GetTypes();

                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsClass) continue;

                    if ((type.BaseType.Equals(typeof(APIBase))) && (type.Name.ToLower() == mp[0]))
                    {
                        object obj = Activator.CreateInstance(type);
                        APIBase t = (APIBase)obj;
                        return t;
                    }

                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("PluginManager error: " + ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Método stático que realiza a validação de autenticação do usuário
        /// 
        /// Esta chamada fará os seguintes passos:
        /// 1 - Realiza a verificação interna com base na chave de autenticação passada no parâmetro 'auth'
        /// 2 - Verifica se está habilitado a autenticação externa (parâmetro extCtrl != null)
        /// 3 - Se está habilitado executa a validação externa através do delagate 'ExternalAccessControl'
        /// 4 - Se não retorna o resultado da autenticação interna
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="method">String com o método que deverá ser processado</param>
        /// <param name="auth">String com a chave de autenticação.</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        /// <param name="extCtrl">Delegate 'ExternalAccessControl' para a autenticação externa</param>
        internal AccessControl ValidateCtrl(SqlConnection sqlConnection, String method, String auth, Dictionary<String, Object> parameters, ExternalAccessControl extCtrl)
        {

            Boolean ret = false;
            Int64 entityId = 0;

            if (String.IsNullOrWhiteSpace(auth))
            {
                ret = false;
            }
            else
            {
                DataTable tmp = ExecuteDataTable(sqlConnection, String.Format("select e.id entity_id, ea.*, e.locked from entity_auth ea inner join entity e with(nolock) on ea.entity_id = e.id where e.deleted = 0 and ea.auth_key = '{0}' and end_date > getdate()", auth), CommandType.Text, null);
                if ((tmp == null) || (tmp.Rows.Count == 0))
                {
                    ret = false;
                }
                else if ((Boolean)tmp.Rows[0]["locked"])
                {
                    ret = false;
                    entityId = (Int64)tmp.Rows[0]["entity_id"];
                }
                else
                {
                    //Existe a chave e está válida
                    //Deve ser implementado aqui o RBAC
                    ret = true;
                    entityId = (Int64)tmp.Rows[0]["entity_id"];
                }

                tmp.Dispose();
            }

            if (extCtrl != null)
            {
                //Transfere a responsabilidade da autenticação para a chamada externa
                //Passa como parametro a decisão que foi tomada até agora
                this.Acl = extCtrl(method, auth, new AccessControl(entityId, ret), parameters);
            }
            else
            {
                this.Acl = new AccessControl(entityId, ret);
            }

            return this.Acl;
        }

        /*
        public static void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text)
        {
            AddUserLog(conn, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", null);
        }

        public static void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData)
        {
            AddUserLog(conn, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, null);
        }


        public static void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, Int64 executedByEntityId)
        {
            AddUserLog(conn, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", executedByEntityId, null);
        }

        public static void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId)
        {
            AddUserLog(conn, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, null);
        }


        public static void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, SqlTransaction transaction)
        {
            AddUserLog(conn, key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, 0, transaction);
        }

        public static void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId, SqlTransaction transaction)
        {
            DbParameterCollection par = new DbParameterCollection();
            par.Add("@date", System.Data.SqlDbType.DateTime).Value = (date.HasValue ? date.Value : DateTime.Now);
            par.Add("@source", typeof(String), source.Length).Value = source;
            par.Add("@key", typeof(Int32)).Value = (Int32)key;
            par.Add("@level", typeof(Int32)).Value = (Int32)level;
            par.Add("@proxy_id", typeof(Int64)).Value = proxyId;
            par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextId;
            par.Add("@resource_id", typeof(Int64)).Value = resourceId;
            par.Add("@plugin_id", typeof(Int64)).Value = pluginId;
            par.Add("@entity_id", typeof(Int64)).Value = entityId;
            par.Add("@identity_id", typeof(Int64)).Value = identityId;
            par.Add("@text", typeof(String), text.Length).Value = text;
            par.Add("@additional_data", typeof(String), additionalData.Length).Value = additionalData;
            par.Add("@executed_by_entity_id", typeof(Int64)).Value = executedByEntityId;

            ExecuteNonQuery(conn, "insert into logs ([date] ,[source] ,[key] ,[level] ,[proxy_id] ,[enterprise_id] ,[context_id] ,[resource_id] ,[plugin_id] ,[entity_id] ,[identity_id] ,[text] ,[additional_data], [executed_by_entity_id]) values (@date ,@source ,@key ,@level ,@proxy_id ,@enterprise_id ,@context_id ,@resource_id, @plugin_id ,@entity_id ,@identity_id ,@text ,@additional_data, @executed_by_entity_id)", par, System.Data.CommandType.Text, transaction);

        }


        public static DataTable Select(SqlConnection conn, String sql)
        {
            return Select(conn, sql, null, null);
        }

        public static DataTable Select(SqlConnection conn, String sql, SqlTransaction transaction)
        {
            return Select(conn, sql, null, transaction);
        }

        public static DataTable Select(SqlConnection conn, String sql, DbParameterCollectionParameters, SqlTransaction transaction)
        {
            SqlCommand cmd = new SqlCommand(sql, conn);
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


                if (transaction != null)
                    cmd.Transaction = transaction;


                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();

                da.Fill(ds, "data");

                return ds.Tables["data"];

            }
            finally
            {
                cmd = null;
            }
        }

        static public DbParameterCollectionGetSqlParameterObject()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }

        public static void ExecuteNonQuery(SqlConnection conn, String sql, DbParameterCollectionparameters, CommandType commandType)
        {
            ExecuteNonQuery(conn, sql, parameters, commandType, null);
        }

        public static void ExecuteNonQuery(SqlConnection conn, String sql, DbParameterCollectionparameters, CommandType commandType, SqlTransaction transaction)
        {

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

                if (transaction != null)
                    cmd.Transaction = transaction;

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
                //if (conn != null) conn.Close();
                //if (conn != null) conn.Dispose();

                cmd = null;
            }
        }



        public static Object ExecuteScalar(SqlConnection conn, String command, CommandType commandType, DbParameterCollectionParameters)
        {
            return ExecuteScalar(conn, command, commandType, Parameters, null);
        }

        public static Object ExecuteScalar(SqlConnection conn, String command, CommandType commandType, DbParameterCollectionParameters, SqlTransaction trans)
        {
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

                return cmd.ExecuteScalar();
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
            }
        }



        public static DataTable ExecuteDataTable(SqlConnection conn, String command, CommandType commandType, DbParameterCollectionParameters)
        {
            return ExecuteDataTable(conn, command, commandType, Parameters, null);
        }

        public static DataTable ExecuteDataTable(SqlConnection conn, String command, CommandType commandType, DbParameterCollectionParameters, SqlTransaction trans)
        {
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
            }
        }*/


        public String GetDBConfig(SqlConnection conn, String key)
        {
            DataTable dt = Select(conn, "select * from server_config with(nolock) where data_name = '" + key + "'");
            if ((dt == null) || (dt.Rows.Count == 0))
                return "";

            return dt.Rows[0]["data_value"].ToString();
        }


    }
}
