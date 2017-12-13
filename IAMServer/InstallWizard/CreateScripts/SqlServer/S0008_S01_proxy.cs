using System;

namespace InstallWizard
{
    public class S0008_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'proxy'))
                    BEGIN

	                    CREATE TABLE [dbo].[proxy](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [name] [varchar](255) NOT NULL,
		                    [create_date] [datetime] NOT NULL,
		                    [config] [bit] NOT NULL,
		                    [last_sync] [datetime] NULL,
		                    [address] [varchar](128) NULL,
		                    [version] [varchar](20) NULL,
	                     CONSTRAINT [PK_proxy] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[proxy] ADD  CONSTRAINT [DF_proxy_create_date]  DEFAULT (getdate()) FOR [create_date]
                        ALTER TABLE [dbo].[proxy] ADD  CONSTRAINT [DF_proxy_config]  DEFAULT ((0)) FOR [config]

                        ALTER TABLE [dbo].[proxy]  WITH CHECK ADD  CONSTRAINT [FK_proxy_enterprise] FOREIGN KEY([enterprise_id])
                        REFERENCES [dbo].[enterprise] ([id])

                        ALTER TABLE [dbo].[proxy] CHECK CONSTRAINT [FK_proxy_enterprise]


                    END
					

                    INSERT INTO [db_install] ([version]) VALUES (8);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 8.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
