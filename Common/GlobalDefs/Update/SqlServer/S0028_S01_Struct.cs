using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0028_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'backup_schedule'))
                    BEGIN

	                    CREATE TABLE [dbo].[backup_schedule](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [schedule] [varchar](500) NOT NULL,
		                    [next] [datetime] NOT NULL,
	                        CONSTRAINT [PK_backup_schedule] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[backup_schedule] ADD  CONSTRAINT [DF_backup_schedule_next]  DEFAULT (getdate()) FOR [next]
	
                        INSERT INTO [backup_schedule] ([schedule], [next]) VALUES('{ ''trigger'':''Dialy'', ''startdate'':''2012-01-01'', ''triggertime'':''00:10:00'', ''repeat'':''0'' }', '1970-01-01 00:00:00');

                    END

                    INSERT INTO [db_ver] ([version]) VALUES (28);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 28; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
