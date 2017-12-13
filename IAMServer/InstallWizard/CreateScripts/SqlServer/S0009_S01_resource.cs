using System;

namespace InstallWizard
{
    public class S0009_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [context_id] [bigint] NOT NULL,
		                    [proxy_id] [bigint] NOT NULL,
		                    [name] [varchar](50) NOT NULL,
		                    [enabled] [bit] NOT NULL,
		                    [create_date] [datetime] NOT NULL,
	                     CONSTRAINT [PK_resource] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[resource] ADD  CONSTRAINT [DF_resource_enabled]  DEFAULT ((1)) FOR [enabled]
                        ALTER TABLE [dbo].[resource] ADD  CONSTRAINT [DF_resource_create_date]  DEFAULT (getdate()) FOR [create_date]

                        ALTER TABLE [dbo].[resource]  WITH CHECK ADD  CONSTRAINT [FK_resource_context] FOREIGN KEY([context_id])
                        REFERENCES [dbo].[context] ([id])

                        ALTER TABLE [dbo].[resource] CHECK CONSTRAINT [FK_resource_context]

                        ALTER TABLE [dbo].[resource]  WITH CHECK ADD  CONSTRAINT [FK_resource_proxy] FOREIGN KEY([proxy_id])
                        REFERENCES [dbo].[proxy] ([id])

                        ALTER TABLE [dbo].[resource] CHECK CONSTRAINT [FK_resource_proxy]

                        CREATE NONCLUSTERED INDEX [IX_resource1] ON [dbo].[resource] 
                        (
	                        [id] ASC
                        )
                        INCLUDE ( [context_id]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

                    END
					

                    INSERT INTO [db_install] ([version]) VALUES (9);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 9.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
