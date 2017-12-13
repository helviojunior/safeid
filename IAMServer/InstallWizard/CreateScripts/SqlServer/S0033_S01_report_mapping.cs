using System;

namespace InstallWizard
{
    public class S0033_S01_report_mapping : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'report_mapping'))
                    BEGIN

	                    CREATE TABLE [dbo].[report_mapping](
		                    [report_id] [bigint] NOT NULL,
		                    [field_id] [bigint] NOT NULL,
		                    [order] [int] NOT NULL,
	                     CONSTRAINT [PK_report_mapping] PRIMARY KEY CLUSTERED 
	                    (
		                    [report_id] ASC,
		                    [field_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[report_mapping]  WITH CHECK ADD  CONSTRAINT [FK_report_mapping_field] FOREIGN KEY([field_id])
	                    REFERENCES [dbo].[field] ([id])
	
	                    ALTER TABLE [dbo].[report_mapping] CHECK CONSTRAINT [FK_report_mapping_field]
	
	                    /****** Object:  ForeignKey [FK_report_mapping_report]    Script Date: 07/07/2014 10:31:06 ******/
	                    ALTER TABLE [dbo].[report_mapping]  WITH CHECK ADD  CONSTRAINT [FK_report_mapping_report] FOREIGN KEY([report_id])
	                    REFERENCES [dbo].[report] ([id])
	
	                    ALTER TABLE [dbo].[report_mapping] CHECK CONSTRAINT [FK_report_mapping_report]

                    END






                    INSERT INTO [db_install] ([version]) VALUES (33);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 33.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
