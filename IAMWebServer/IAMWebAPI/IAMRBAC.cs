using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SafeTrend.WebAPI;
using System.Data;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;
using IAM.GlobalDefs;

namespace IAM.WebAPI
{
    public class IAMRBAC : RBAC, IDisposable
    {
        public Boolean HasAdminConsole(DbBase database, Int64 entityId, Int64 enterpriseId)
        {
            if ((!(database is IAMDatabase)) && (!(database is SqlBase)))
                throw new Exception("Invalid database type. Expected IAMDatabase or SqlBase");

            DbParameterCollection par = null;
            Boolean ret = false;

            try
            {
                par = new DbParameterCollection();
                par.Add("@entity_id", typeof(Int64)).Value = entityId;
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;

                ret = database.ExecuteScalar<Boolean>("sp_sys_rbac_admin", CommandType.StoredProcedure, par, null);

                if (ret)
                    return ret;

                //Verifica se há alguma permissão administrativa
                ret = database.ExecuteScalar<Boolean>("SELECT case when count(*) = 0 then cast(0 as bit) ELSE cast(1 as bit) END from sys_entity_role sel with(nolock) inner join sys_role sr with(nolock) on sr.id = sel.role_id inner join sys_role_permission rp with(nolock) on sr.id = rp.role_id inner join sys_permission p with(nolock) on p.id = rp.permission_id where sel.entity_id = @entity_id and sr.enterprise_id = @enterprise_id", CommandType.Text, par, null);

            }
            catch (Exception ex)
            {
                ret = false;
            }
            finally
            {
                par = null;
            }

            return ret;
        }

        public override Boolean UserAdmin(DbBase database, Int64 entityId, Int64 enterpriseId)
        {
            if ((!(database is IAMDatabase)) && (!(database is SqlBase)))
                throw new Exception("Invalid database type. Expected IAMDatabase or SqlBase");

            DbParameterCollection par = null;

            try
            {
                par = new DbParameterCollection();
                par.Add("@entity_id", typeof(Int64)).Value = entityId;
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;

                return database.ExecuteScalar<Boolean>("sp_sys_rbac_admin", CommandType.StoredProcedure, par, null);

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

        public override Boolean UserCan(DbBase database, Int64 entityId, Int64 enterpriseId, String module, String permission)
        {
            if ((!(database is IAMDatabase)) && (!(database is SqlBase)))
                throw new Exception("Invalid database type. Expected IAMDatabase or SqlBase");

            DbParameterCollection par = null;
            try
            {
                String[] parts = permission.ToLower().Split(".".ToCharArray(), 2);

                par = new DbParameterCollection();
                par.Add("@entity_id", typeof(Int64)).Value = entityId;
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@submodule", typeof(String)).Value = parts[0];
                par.Add("@permission", typeof(String)).Value = parts[1];

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

        public void Dispose()
        {

        }

    }
}
