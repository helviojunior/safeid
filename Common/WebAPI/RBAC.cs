/// 
/// @file RBAC.cs
/// <summary>
/// Implementações de controle de acesso baseado em perfil (Role Based Access Control). 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 27/10/2013
/// $Id: RBAC.cs, v1.0 2013/10/27 Helvio Junior $

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.Data;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace SafeTrend.WebAPI
{
    /// <summary>
    /// Classe estática utilizada para RBAC
    /// </summary>
    public abstract class RBAC
    {
        /// <summary>
        /// Método para verificação de permissão (se o usuário em questão é admin)
        /// Este método executa a store procedure 'sp_sys_rbac_admin' que fará toda a regra de negócio
        /// </summary>
        /// <param name="conn">Conexão com o banco de dados MS-SQL</param>
        /// <param name="entityId">ID do usuário que se deseja verificar a permissão</param>
        /// <param name="enterpriseId">ID do empresa que se deseja verificar a permissão</param>
        /// <returns>Retorna true ou false</returns>
        public abstract Boolean UserAdmin(DbBase database, Int64 entityId, Int64 enterpriseId);
            

        
        /// <summary>
        /// Método para verificação de permissão de execução de perfil
        /// Este método executa a store procedure 'sp_sys_rbac' que fará toda a regra de negócio
        /// </summary>
        /// <param name="conn">Conexão com o banco de dados MS-SQL</param>
        /// <param name="entityId">ID do usuário que se deseja verificar a permissão</param>
        /// <param name="enterpriseId">ID do empresa que se deseja verificar a permissão</param>
        /// <param name="module">Módulo do sistema</param>
        /// <param name="permission">Permissão desejada</param>
        /// <returns>Retorna true ou false</returns>
        public abstract Boolean UserCan(DbBase database, Int64 entityId, Int64 enterpriseId, String module, String permission);

    }

    /// <summary>
    /// Classe estática utilizada para RBAC
    /// </summary>
    public static class RBACOld
    {
        /// <summary>
        /// Método para verificação de permissão (se o usuário em questão é admin)
        /// Este método executa a store procedure 'sp_sys_rbac_admin' que fará toda a regra de negócio
        /// </summary>
        /// <param name="conn">Conexão com o banco de dados MS-SQL</param>
        /// <param name="entityId">ID do usuário que se deseja verificar a permissão</param>
        /// <param name="enterpriseId">ID do empresa que se deseja verificar a permissão</param>
        /// <returns>Retorna true ou false</returns>
        public static Boolean UserAdmin(SqlConnection conn, Int64 entityId, Int64 enterpriseId)
        {
            
            DbParameterCollection par = null;
            
            try
            {
                using (SqlBase db = new SqlBase(conn))
                {
                    par = new DbParameterCollection();
                    par.Add("@entity_id", typeof(Int64)).Value = entityId;
                    par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;

                    return db.ExecuteScalar<Boolean>("sp_sys_rbac_admin", CommandType.StoredProcedure, par, null);
                }

            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                par = null;
            }
        }


        /// <summary>
        /// Método para verificação de permissão de execução de perfil
        /// Este método executa a store procedure 'sp_sys_rbac' que fará toda a regra de negócio
        /// </summary>
        /// <param name="conn">Conexão com o banco de dados MS-SQL</param>
        /// <param name="entityId">ID do usuário que se deseja verificar a permissão</param>
        /// <param name="enterpriseId">ID do empresa que se deseja verificar a permissão</param>
        /// <param name="module">Módulo do sistema</param>
        /// <param name="permission">Permissão desejada</param>
        /// <returns>Retorna true ou false</returns>
        public static Boolean UserCan(DbBase database, Int64 entityId, Int64 enterpriseId, String module, String permission)
        {

            DbParameterCollection par = null;
            try
            {
                par = new DbParameterCollection();
                par.Add("@entity_id", typeof(Int64)).Value = entityId;
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@submodule", typeof(String), module.Length).Value = module;
                par.Add("@permission", typeof(String), permission.Length).Value = permission;

                return database.ExecuteScalar<Boolean>("sp_sys_rbac", CommandType.StoredProcedure, par, null);
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                par = null;
            }
        }


    }
}
