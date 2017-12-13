using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0008_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'v' AND name = 'vw_entity_all_data')
                    BEGIN
                        DROP VIEW vw_entity_all_data;
                    END

                    
                    EXEC ('
                    CREATE view [dbo].[vw_entity_all_data]
                    as
	                    select e.*, c.enterprise_id, i.id identity_id, f.name, r.name resource_name, ife.field_id, ife.value, rp.id resource_plugin_id, rp.resource_id, rp.plugin_id
	                    ,container_id = isnull((select c1.id from container c1 with(nolock) inner join entity_container ec  with(nolock) on ec.container_id = c1.id where ec.entity_id = e.id),0)
	                    from entity e  with(nolock)
	                    inner join context c with(nolock) on e.context_id = c.id
	                    inner join [identity] i with(nolock) on i.entity_id = e.id
	                    inner join identity_field ife with(nolock) on ife.identity_id = i.id
	                    inner join field f with(nolock) on ife.field_id = f.id
	                    inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id
	                    inner join resource r with(nolock) on r.id = rp.resource_id

	                    UNION

	                    select e.*, c.enterprise_id, 0, f.name, ''Entity Data'', efe.field_id, efe.value, 0, 0, 0
	                    ,container_id = isnull((select c1.id from container c1 with(nolock) inner join entity_container ec  with(nolock) on ec.container_id = c1.id where ec.entity_id = e.id),0)
	                    from entity e  with(nolock)
	                    inner join context c with(nolock) on e.context_id = c.id
	                    inner join entity_field efe with(nolock) on efe.entity_id = e.id
	                    inner join field f with(nolock) on efe.field_id = f.id
                    ');

                    EXEC sp_rebuild_views;

                    INSERT INTO [db_ver] ([version]) VALUES (8);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 8.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
