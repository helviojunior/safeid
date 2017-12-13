using System;

namespace InstallWizard
{
    public class S0007_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'enterprise'))
                    BEGIN

	                    CREATE TABLE [dbo].[enterprise](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [name] [varchar](200) NOT NULL,
		                    [fqdn] [varchar](2000) NOT NULL,
		                    [server_pkcs12_cert] [varchar](max) NULL,
		                    [server_cert] [varchar](max) NULL,
		                    [client_pkcs12_cert] [varchar](max) NULL,
		                    [language] [varchar](10) NOT NULL,
		                    [create_date] [datetime] NOT NULL,
		                    [auth_plugin] [varchar](500) NOT NULL,
	                     CONSTRAINT [PK_enterprise_1] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[enterprise] ADD  CONSTRAINT [DF_enterprise_language]  DEFAULT ('pt-BR') FOR [language]
                        ALTER TABLE [dbo].[enterprise] ADD  CONSTRAINT [DF_enterprise_create_date]  DEFAULT (getdate()) FOR [create_date]


                        ALTER TABLE [dbo].[enterprise]  WITH CHECK ADD  CONSTRAINT [FK_enterprise_languages] FOREIGN KEY([language])
                        REFERENCES [dbo].[languages] ([language])

                        ALTER TABLE [dbo].[enterprise] CHECK CONSTRAINT [FK_enterprise_languages]


                    END
					
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'enterprise_fqdn_alias'))
                    BEGIN

	                    CREATE TABLE [dbo].[enterprise_fqdn_alias](
		                    [fqdn] [varchar](900) NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
	                     CONSTRAINT [PK_enterprise_fqdn_alias] PRIMARY KEY CLUSTERED 
	                    (
		                    [fqdn] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[enterprise_fqdn_alias]  WITH CHECK ADD  CONSTRAINT [FK_enterprise_fqdn_alias_enterprise] FOREIGN KEY([enterprise_id])
	                    REFERENCES [dbo].[enterprise] ([id])

	                    ALTER TABLE [dbo].[enterprise_fqdn_alias] CHECK CONSTRAINT [FK_enterprise_fqdn_alias_enterprise]

                    END


                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_workflow'))
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

	                    ALTER TABLE [dbo].[enterprise_auth_par]  WITH CHECK ADD  CONSTRAINT [FK_enterprise_auth_par_enterprise] FOREIGN KEY([enterprise_id])
	                    REFERENCES [dbo].[enterprise] ([id])

	                    ALTER TABLE [dbo].[enterprise_auth_par] CHECK CONSTRAINT [FK_enterprise_auth_par_enterprise]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (7);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 7.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
