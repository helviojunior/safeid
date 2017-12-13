/// 
/// @file APIBase.cs
/// <summary>
/// Implementações da classe IAMAPIBase. 
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
using IAM.GlobalDefs;

namespace SafeTrend.WebAPI
{
    /// <summary>
    /// Classe APIInfo, derivada da classe APIBase
    /// Implementa os métodos apiinfo.*
    /// </summary>
    internal abstract class IAMAPIBase : APIBase
    {
        internal Int64 _enterpriseId;
        //public override event Error Error;
        //public override event ExternalAccessControl ExternalAccessControl;

        public abstract Object iProcess(IAMDatabase database, Int64 enterpriseId, String method, String auth, Dictionary<String, Object> parameters);

        /// <summary>
        /// Método de processamentoda requisição
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="enterpriseId">ID da empresa</param>
        /// <param name="method">String com o método que deverá ser processado</param>
        /// <param name="auth">String com a chave de autenticação.</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        public override Object Process(DbBase database, Int64 enterpriseId, String method, String auth, Dictionary<String, Object> parameters)
        {
            if ((!(database is IAMDatabase)) && (!(database is SqlBase)))
                throw new Exception("Invalid database type. Expected IAMDatabase or SqlBase");

            this._enterpriseId = enterpriseId;

            method = method.ToLower();
            String[] mp = method.Split(".".ToCharArray(), 2);

            if (mp.Length != 2)
                return null;

            if (this.GetType().Name.ToLower() != mp[0])
                return null;

            return iProcess((IAMDatabase)database, enterpriseId, method, auth, parameters);
        }

        public override AccessControl ValidateCtrl(DbBase database, String method, String auth, Dictionary<String, Object> parameters, ExternalAccessControl extCtrl)
        {

            Boolean ret = false;
            Int64 entityId = 0;

            if (String.IsNullOrWhiteSpace(auth))
            {
                ret = false;
            }
            else
            {
                DataTable tmp = database.ExecuteDataTable(String.Format("select e.id entity_id, ea.*, e.locked from entity_auth ea inner join entity e with(nolock) on ea.entity_id = e.id where e.deleted = 0 and ea.auth_key = '{0}' and end_date > getdate()", auth), CommandType.Text, null);
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
        

    }
}
