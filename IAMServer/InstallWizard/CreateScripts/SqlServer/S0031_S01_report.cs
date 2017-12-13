using System;

namespace InstallWizard
{
    public class S0031_S01_report : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'report'))
                    BEGIN

	                    CREATE TABLE [dbo].[report](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [title] [varchar](255) NOT NULL,
		                    [recipient] [varchar](2000) NOT NULL,
	                        CONSTRAINT [PK_report] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[report]  WITH CHECK ADD  CONSTRAINT [FK_report_enterprise] FOREIGN KEY([enterprise_id])
	                    REFERENCES [dbo].[enterprise] ([id])
	
	                    ALTER TABLE [dbo].[report] CHECK CONSTRAINT [FK_report_enterprise]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'report_schedule'))
                    BEGIN


	                    CREATE TABLE [dbo].[report_schedule](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [report_id] [bigint] NOT NULL,
		                    [schedule] [varchar](500) NOT NULL,
		                    [next] [datetime] NOT NULL,
	                        CONSTRAINT [PK_report_schedule] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[report_schedule] ADD  CONSTRAINT [DF_report_schedule_next]  DEFAULT (getdate()) FOR [next]
	
	                    ALTER TABLE [dbo].[report_schedule]  WITH CHECK ADD  CONSTRAINT [FK_report_schedule_report] FOREIGN KEY([report_id])
	                    REFERENCES [dbo].[report] ([id])

	                    ALTER TABLE [dbo].[report_schedule] CHECK CONSTRAINT [FK_report_schedule_report]

                    END



                    INSERT INTO [db_install] ([version]) VALUES (31);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 31.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
