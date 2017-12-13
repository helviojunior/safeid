using System;

namespace InstallWizard
{
    public class S0038_S01_functions : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'TF' AND name = 'fn_selectRoleTree')
                    BEGIN
                        DROP FUNCTION [fn_selectRoleTree];
                    END

                    EXEC('CREATE FUNCTION [dbo].[fn_selectRoleTree]
                    (
	                    @role_id bigint
                    )
                    RETURNS 
                    @retTable TABLE 
                    (
	                    role_id bigint, 
	                    parent_id bigint, 
	                    top_id bigint, 
	                    name varchar(200), 
	                    [level] bigint, 
	                    [order_sequence] varchar(20)
                    )
                    AS
                    BEGIN
	
		
	                    ;with relation (role_id, parent_id, top_id, name, [level], [order_sequence])  
	                    as  
	                    (  
	                    select id, parent_id, id, name, 0, cast(id as varchar(20))  
	                    from role  
	                    where id = @role_id --parent_id = 0
	                    union all  
	                    select p.id, p.parent_id, r.top_id, p.name, r.[level]+1, cast(r.[order_sequence] + ''.'' + cast(p.id as varchar) as varchar(20))  
	                    from role p  
	                    inner join relation r on p.parent_id = r.role_id  
	                    where r.[level]<1024
	                    )  

	                    insert into @retTable
	                    select *
	                    from relation
	                    order by order_sequence
	                    option(maxrecursion 1024);
	
	                    RETURN 
                    END
                    ');

                    INSERT INTO [db_install] ([version]) VALUES (38);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when count(*) = 0 then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 38.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
