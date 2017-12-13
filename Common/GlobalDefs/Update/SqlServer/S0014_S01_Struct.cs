using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0014_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    
                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'v' AND name = 'vw_entity_mails')
                    BEGIN
                        DROP VIEW vw_entity_mails;
                    END

                    EXEC ('
                       
                        CREATE view [dbo].[vw_entity_mails]
                        as
                        select	e.id entity_id,
		                        c.name context_name, 
		                        c.id context_id,
		                        e.full_name, 
		                        e.login, 
		                        e.create_date, 
		                        e.last_login, 
		                        v.value mail, 
		                        e.locked
                        from entity e with(nolock) 
                        inner join	context c
	                        on	c.id = e.context_id
                        left join (
	                        select i.entity_id, m.data_name, ife.value, f.data_type, r.name, f.name field_name
	                        from identity_field ife with(nolock) 
	                        inner join [identity] i on ife.identity_id = i.id 
	                        inner join entity e with(nolock) on i.entity_id = e.id 
	                        inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id 
	                        inner join resource r with(nolock) on r.id = rp.resource_id and r.context_id = e.context_id 
	                        inner join resource_plugin_mapping m with(nolock) on m.resource_plugin_id = rp.id and m.field_id = ife.field_id 
	                        inner join field f with(nolock) on ife.field_id = f.id
	                        where m.field_id = rp.mail_field_id
                        ) v on v.entity_id = e.id
                        where e.deleted = 0 and v.value is not null
                        group by e.id, c.name, c.id, e.full_name, e.login, e.create_date, e.last_login, v.value, e.locked

                        UNION

                        select		e.id entity_id, c.name context_name, c.id context_id, e.full_name, e.login, e.create_date, e.last_login, efe.value mail, e.locked
	                        from	entity e
	                        inner	join context c
		                        on	c.id = e.context_id
	                        inner	join entity_field efe
		                        on	efe.entity_id = e.id
                        where e.deleted = 0
                        group by  e.id, c.name, c.id, e.full_name, e.login, e.create_date, e.last_login, efe.value, e.locked

                    ')

                    INSERT INTO [db_ver] ([version]) VALUES (14);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 14.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
