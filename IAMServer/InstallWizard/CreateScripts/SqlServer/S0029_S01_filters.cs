using System;

namespace InstallWizard
{
    public class S0029_S01_filters : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'filters'))
                    BEGIN

	                    CREATE TABLE [dbo].[filters](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [name] [varchar](300) NOT NULL,
		                    [create_date] [datetime] NOT NULL,
	                     CONSTRAINT [PK_filters_1] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[filters] ADD  CONSTRAINT [DF_filters_create_date]  DEFAULT (getdate()) FOR [create_date]
	                    ALTER TABLE [dbo].[filters]  WITH CHECK ADD  CONSTRAINT [FK_filters_enterprise] FOREIGN KEY([enterprise_id])
	                    REFERENCES [dbo].[enterprise] ([id])
	
	                    ALTER TABLE [dbo].[filters] CHECK CONSTRAINT [FK_filters_enterprise]
	

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'filters_conditions'))
                    BEGIN

	                    CREATE TABLE [dbo].[filters_conditions](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [filter_id] [bigint] NOT NULL,
		                    [group_id] [varchar](30) NOT NULL,
		                    [group_selector] [varchar](3) NOT NULL,
		                    [field_id] [bigint] NOT NULL,
		                    [text] [varchar](500) NOT NULL,
		                    [condition] [varchar](30) NOT NULL,
		                    [selector] [varchar](30) NOT NULL,
	                     CONSTRAINT [PK_filters] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]
	
	                    ALTER TABLE [dbo].[filters_conditions] ADD  CONSTRAINT [DF_filter_condition_group_selector]  DEFAULT ('OR') FOR [group_selector]
	                    ALTER TABLE [dbo].[filters_conditions] ADD  CONSTRAINT [DF_filter_condition_condition]  DEFAULT ('equal') FOR [condition]
	                    ALTER TABLE [dbo].[filters_conditions] ADD  CONSTRAINT [DF_filter_condition_selector]  DEFAULT ('OR') FOR [selector]

	                    ALTER TABLE [dbo].[filters_conditions]  WITH CHECK ADD  CONSTRAINT [FK_filters_conditions_filters] FOREIGN KEY([filter_id])
	                    REFERENCES [dbo].[filters] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE

	                    ALTER TABLE [dbo].[filters_conditions] CHECK CONSTRAINT [FK_filters_conditions_filters]

	                    ALTER TABLE [dbo].[filters_conditions]  WITH CHECK ADD  CONSTRAINT [FK_filters_field] FOREIGN KEY([field_id])
	                    REFERENCES [dbo].[field] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE

	                    ALTER TABLE [dbo].[filters_conditions] CHECK CONSTRAINT [FK_filters_field]

                    END

                    INSERT INTO [db_install] ([version]) VALUES (29);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 29.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
