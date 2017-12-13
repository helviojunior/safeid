using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0009_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_sys_rbac')
                    BEGIN
                        DROP PROCEDURE sp_sys_rbac;
                    END

                    
                    EXEC ('
                        CREATE procedure [dbo].[sp_sys_rbac]
	                        @entity_id bigint,
	                        @enterprise_id bigint,
	                        @submodule varchar(50),
	                        @permission varchar(50)
                        as

                        declare @can bit
                        set @can = 0

                        /* verifica se o usuario e system admin */
                        select	@can = 1 
	                        from	sys_role r
                        inner join	sys_entity_role er
	                        on	er.role_id	= r.id
                        where	er.entity_id = @entity_id
	                        and	r.sa = 1

                        /* verifica se o usuário é enterprise admin */
                        if (@can = 0)
                        BEGIN
	                        select	@can = 1 
		                        from	sys_role r
	                        inner join	sys_entity_role er
		                        on	er.role_id	= r.id
	                        where	er.entity_id = @entity_id
		                        and	r.enterprise_id = @enterprise_id
		                        and	r.ea = 1
                        END

                        /* verifica a permissão no modulo e funcao especifica */
                        if (@can = 0)
                        BEGIN
	                        select	@can = 1
		                        from	sys_role r
	                        inner join	sys_entity_role er
		                        on	er.role_id	= r.id
	                        inner join sys_role_permission rp
		                        on	rp.role_id = r.id
	                        inner join sys_permission p
		                        on p.id = rp.permission_id
	                        inner join	sys_sub_module sm
		                        on sm.id = p.submodule_id
	                        inner join	sys_module m
		                        on	m.id = sm.module_id
	                        where	er.entity_id = @entity_id
		                        and	r.enterprise_id = @enterprise_id
		                        and	sm.[api_module] = @submodule
		                        and	p.[key] = @permission
                        END

                        select @can


                    ');

                    INSERT INTO [db_ver] ([version]) VALUES (9);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 9.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
