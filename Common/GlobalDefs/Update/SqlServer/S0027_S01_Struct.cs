using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0027_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    IF (NOT EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'resource_plugin' AND  COLUMN_NAME = 'priority'))
                    BEGIN
                        ALTER TABLE dbo.resource_plugin ADD priority int NOT NULL CONSTRAINT DF_resource_plugin_priority DEFAULT 1
                    END

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_collector_imports_regs')
                    BEGIN
                        DROP VIEW [vw_collector_imports_regs];
                    END

                    EXEC('CREATE view [dbo].[vw_collector_imports_regs]
                    as

                    /****** Sempre usar  with(nolock) para evitar deadlock no engine ******/

                    select 
	                    ci.*,
	                    c.enterprise_id,
	                    c.id as context_id,
	                    rp.resource_id,
	                    rp.plugin_id,
	                    p.uri plugin_uri,
	                    rp.build_login,
	                    rp.build_mail,
	                    rp.priority
                    from collector_imports ci with(nolock)
	                    inner	join resource_plugin rp with(nolock)
		                    on	ci.resource_plugin_id = rp.id
	                    inner	join resource r with(nolock)
		                    on	r.id = rp.resource_id
	                    inner	join plugin p with(nolock)
		                    on	p.id = rp.plugin_id
	                    inner	join context c with(nolock)
		                    on	c.id = r.context_id
                    where ci.[status] = ''F''
                    ');


                    INSERT INTO [db_ver] ([version]) VALUES (27);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 27; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
