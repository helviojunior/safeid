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
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace SafeTrend.WebAPI
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
    public abstract class APIBase
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
        public abstract Object Process(DbBase database, Int64 enterpriseId, String method, String auth, Dictionary<String, Object> parameters);

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
                List<Type> types = new List<Type>();

                //Seleciona todos os tipos de todos os assemblies carregados
                //Filtrado se é classe e pelo nome do método desejado
                foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    try
                    {
                        types.AddRange(from t in asm.GetTypes()
                                       where t.IsClass && t.Name.ToLower() == mp[0]
                                       select t
                                        );
                    }
                    catch { }

                foreach (Type type in types)
                {
                    if (!type.IsClass) continue;

                    if (type.Name.ToLower() == mp[0])
                    {
                        Type baseType = type.BaseType;
                        while (baseType != null)
                        {
                            if (baseType.Equals(typeof(APIBase)))
                            {
                                object obj = Activator.CreateInstance(type);
                                APIBase t = (APIBase)obj;
                                return t;
                            }
                            baseType = baseType.BaseType;
                        }
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
        public abstract AccessControl ValidateCtrl(DbBase database, String method, String auth, Dictionary<String, Object> parameters, ExternalAccessControl extCtrl);

        /*
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
        }*/
        


        /*
        public String GetDBConfig(SqlConnection conn, String key)
        {
            DataTable dt = Select(conn, "select * from server_config with(nolock) where data_name = '" + key + "'");
            if ((dt == null) || (dt.Rows.Count == 0))
                return "";

            return dt.Rows[0]["data_value"].ToString();
        }
        */

    }
}
