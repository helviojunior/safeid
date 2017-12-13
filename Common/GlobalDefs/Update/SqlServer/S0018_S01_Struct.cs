using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0018_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    
                    
                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_eligible_identity')
                    BEGIN
                        DROP VIEW [vw_eligible_identity];
                    END


                    EXEC('CREATE VIEW [dbo].[vw_eligible_identity]
                    as
                    select	e.id, 
		                    identity_id =	case when not min(i1.identity_id) is null then min(i1.identity_id)
						                    else min(i2.identity_id) end
                    from	entity e
                    left join (select i.entity_id, i.id identity_id from [identity] i
                    inner join resource_plugin rp on i.resource_plugin_id = rp.id and rp.permit_add_entity = 1) i1
                    on i1.entity_id = e.id
                    left join (select i.entity_id, i.id identity_id from [identity] i
                    inner join resource_plugin rp on i.resource_plugin_id = rp.id) i2
                    on i2.entity_id = e.id
                    group by e.id
                    having case when not min(i1.identity_id) is null then min(i1.identity_id)
						                    else min(i2.identity_id) end is not null

                    ')


                    INSERT INTO [db_ver] ([version]) VALUES (18);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 18.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
