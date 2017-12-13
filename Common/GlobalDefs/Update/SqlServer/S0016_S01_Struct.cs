using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0016_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"


                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'enterprise_auth_par'))
                    BEGIN


	                    CREATE TABLE [dbo].[enterprise_auth_par](
		                    [enterprise_id] [bigint] NOT NULL,
		                    [plugin] [varchar](500) NOT NULL,
		                    [key] [varchar](50) NOT NULL,
		                    [value] [varchar](max) NOT NULL,
	                     CONSTRAINT [PK_enterprise_auth_par] PRIMARY KEY CLUSTERED 
	                    (
		                    [enterprise_id] ASC,
		                    [plugin] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    EXEC('ALTER TABLE [dbo].[enterprise_auth_par]  WITH CHECK ADD  CONSTRAINT [FK_enterprise_auth_par_enterprise] FOREIGN KEY([enterprise_id]) REFERENCES [dbo].[enterprise] ([id])')

	                    EXEC('ALTER TABLE [dbo].[enterprise_auth_par] CHECK CONSTRAINT [FK_enterprise_auth_par_enterprise]')

                    END

                    IF (EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'enterprise' AND  COLUMN_NAME = 'cas_service'))
                    BEGIN
                        EXEC('EXECUTE sp_rename N''dbo.enterprise.cas_service'', N''TMP_auth_plugin'', ''COLUMN''')
                        EXEC('EXECUTE sp_rename N''dbo.enterprise.TMP_auth_plugin'', N''auth_plugin'', ''COLUMN''')

                        EXEC('insert into enterprise_auth_par (enterprise_id, plugin, [key], [value])
                        select id, ''auth://iam/plugins/cas'', ''uri'', auth_plugin
                        from enterprise
                        where auth_plugin like ''%/cas%'' and auth_plugin not like ''%auth://iam/plugins%''')

                        EXEC('update enterprise
                        set auth_plugin = ''auth://iam/plugins/cas''
                        where auth_plugin like ''%/cas%'' and auth_plugin not like ''%auth://iam/plugins%''')
                    END

                    INSERT INTO [db_ver] ([version]) VALUES (16);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 16.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
