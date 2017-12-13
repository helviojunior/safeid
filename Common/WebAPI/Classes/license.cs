/// 
/// @file License.cs
/// <summary>
/// Implementações da classe License. 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 24/11/2013
/// $Id: License.cs, v1.0 2013/11/24 Helvio Junior $

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using IAM.Config;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using IAM.License;

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe APIInfo, derivada da classe APIBase
    /// Implementa os métodos apiinfo.*
    /// </summary>
    internal class License : APIBase
    {
        public override event Error Error;
        public override event ExternalAccessControl ExternalAccessControl;

        private Int64 _enterpriseId;

        /// <summary>
        /// Método de processamentoda requisição
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="enterpriseId">ID da empresa</param>
        /// <param name="method">String com o método que deverá ser processado</param>
        /// <param name="auth">String com a chave de autenticação.</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        public override Object Process(SqlConnection sqlConnection, Int64 enterpriseId, String method, String auth, Dictionary<String, Object> parameters)
        {
            base.Connection = sqlConnection;

            method = method.ToLower();
            String[] mp = method.Split(".".ToCharArray(), 2);

            if (mp.Length != 2)
                return null;

            if (this.GetType().Name.ToLower() != mp[0])
                return null;

            AccessControl ac = ValidateCtrl(sqlConnection, method, auth, parameters, ExternalAccessControl);
            
            if (!ac.Result)
            {
                Error(ErrorType.InvalidParameters, "Not authorized", "", null);
                return null;
            }

            switch (mp[1])
            {
                case "info":
                    Dictionary<String, Object> result = new Dictionary<String, Object>();

                    LicenseControl lic = LicenseChecker.GetLicenseData(sqlConnection, null, enterpriseId);

                    result.Add("hasLicense", lic.Valid);
                    if (lic.Valid)
                    {
                        result.Add("used", lic.Count);
                        result.Add("available", lic.Entities);
                    }

                    return result;

                    break;

                default:
                    Error(ErrorType.InvalidRequest, "JSON-rpc method is unknow.", "", null);
                    return null;
                    break;
            }

            return null;
        }

    }
}
