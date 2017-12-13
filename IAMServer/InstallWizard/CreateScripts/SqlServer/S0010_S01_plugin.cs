using System;

namespace InstallWizard
{
    public class S0010_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'plugin'))
                    BEGIN

		
	                    CREATE TABLE [dbo].[plugin](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [name] [varchar](255) NOT NULL,
		                    [scheme] [varchar](50) NOT NULL,
		                    [uri] [varchar](500) NOT NULL,
		                    [assembly] [varchar](100) NOT NULL,
		                    [create_date] [datetime] NOT NULL,
	                     CONSTRAINT [PK_plugin] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[plugin] ADD  CONSTRAINT [DF_plugin_enterprise_id]  DEFAULT ((0)) FOR [enterprise_id]
                        ALTER TABLE [dbo].[plugin] ADD  CONSTRAINT [DF_plugin_create_date]  DEFAULT (getdate()) FOR [create_date]

	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'CSV','connector','connector://iam/plugins/csvplugin','Plugincsv.dll')
	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'Active Directory','connector','connector://IAM/plugins/activedirectory','ActiveDirectory.dll')
	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'Google Apps','connector','connector://IAM/plugins/GoogleAdmin','GoogleAdmin.dll')
	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'cPanel V2 Plugin','connector','connector://iam/plugins/cpanelv2','cpanelv2.dll')
	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'eCentry Email Manager V1.0 Plugin','connector','connector://iam/plugins/ecentryemailmanager','ecentryemailmanager.dll')
	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'Microsoft Windows Plugin','connector','connector://iam/plugins/windows','Windows.dll')
	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'MS SQL Server','connector','connector://iam/plugins/mssqlserver','MsSQLServer.dll')
	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'Microsoft ODBC connect','connector','connector://iam/plugins/odbc','odbc.dll')
	                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly]) VALUES (0,'Linux SSH Plugin','connector','connector://iam/plugins/linux','linux.dll')

                    END
					
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'proxy_plugin'))
                    BEGIN

	                    CREATE TABLE [dbo].[proxy_plugin](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [proxy_id] [bigint] NOT NULL,
		                    [plugin_id] [bigint] NOT NULL,
		                    [enabled] [bit] NOT NULL,
	                     CONSTRAINT [PK_proxy_plugin_1] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[proxy_plugin] ADD  CONSTRAINT [DF_proxy_plugin_enabled]  DEFAULT ((1)) FOR [enabled]

	                    ALTER TABLE [dbo].[proxy_plugin]  WITH CHECK ADD  CONSTRAINT [FK_proxy_plugin_plugin] FOREIGN KEY([plugin_id])
	                    REFERENCES [dbo].[plugin] ([id])
	
	                    ALTER TABLE [dbo].[proxy_plugin] CHECK CONSTRAINT [FK_proxy_plugin_plugin]
	
	                    ALTER TABLE [dbo].[proxy_plugin]  WITH CHECK ADD  CONSTRAINT [FK_proxy_plugin_proxy] FOREIGN KEY([proxy_id])
	                    REFERENCES [dbo].[proxy] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[proxy_plugin] CHECK CONSTRAINT [FK_proxy_plugin_proxy]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'proxy_plugin_par'))
                    BEGIN

	                    CREATE TABLE [dbo].[proxy_plugin_par](
		                    [proxy_plugin_id] [bigint] NOT NULL,
		                    [key] [varchar](50) NOT NULL,
		                    [value] [varchar](max) NOT NULL
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[proxy_plugin_par]  WITH CHECK ADD  CONSTRAINT [FK_proxy_plugin_par_proxy_plugin] FOREIGN KEY([proxy_plugin_id])
	                    REFERENCES [dbo].[proxy_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[proxy_plugin_par] CHECK CONSTRAINT [FK_proxy_plugin_par_proxy_plugin]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (10);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 10.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
