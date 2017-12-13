using System;

namespace InstallWizard
{
    public class S0011_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [resource_id] [bigint] NOT NULL,
		                    [plugin_id] [bigint] NOT NULL,
		                    [permit_add_entity] [bit] NOT NULL,
		                    [enabled] [bit] NOT NULL,
		                    [mail_domain] [varchar](200) NOT NULL,
		                    [build_login] [bit] NOT NULL,
		                    [build_mail] [bit] NOT NULL,
		                    [enable_import] [bit] NOT NULL,
		                    [enable_deploy] [bit] NOT NULL,
		                    [order] [int] NOT NULL,
		                    [name_field_id] [bigint] NOT NULL,
		                    [mail_field_id] [bigint] NOT NULL,
		                    [login_field_id] [bigint] NOT NULL,
		                    [deploy_after_login] [bit] NOT NULL,
		                    [password_after_login] [bit] NOT NULL,
		                    [deploy_process] [bit] NOT NULL,
		                    [deploy_all] [bit] NOT NULL,
		                    [deploy_password_hash] [varchar](10) NOT NULL,
		                    [create_date] [datetime] NOT NULL,
		                    [import_groups] [bit] NOT NULL,
		                    [import_containers] [bit] NOT NULL,
	                     CONSTRAINT [PK_resource_plugin] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]
	
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_permit_add_login]  DEFAULT ((1)) FOR [permit_add_entity]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_enabled]  DEFAULT ((1)) FOR [enabled]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_build_login]  DEFAULT ((1)) FOR [build_login]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_build_mail]  DEFAULT ((1)) FOR [build_mail]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_enable_import]  DEFAULT ((0)) FOR [enable_import]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_enable_deploy]  DEFAULT ((0)) FOR [enable_deploy]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_order]  DEFAULT ((0)) FOR [order]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_deploy_after_login]  DEFAULT ((0)) FOR [deploy_after_login]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_password_after_login]  DEFAULT ((1)) FOR [password_after_login]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_deploy_process]  DEFAULT ((1)) FOR [deploy_process]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_deploy_all]  DEFAULT ((1)) FOR [deploy_all]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_deploy_password_hash]  DEFAULT ('none') FOR [deploy_password_hash]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_create_date]  DEFAULT (getdate()) FOR [create_date]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_import_groups]  DEFAULT ((0)) FOR [import_groups]
                        ALTER TABLE [dbo].[resource_plugin] ADD  CONSTRAINT [DF_resource_plugin_import_containers]  DEFAULT ((0)) FOR [import_containers]


                        ALTER TABLE [dbo].[resource_plugin]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_plugin] FOREIGN KEY([plugin_id])
                        REFERENCES [dbo].[plugin] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE

                        ALTER TABLE [dbo].[resource_plugin] CHECK CONSTRAINT [FK_resource_plugin_plugin]

                        ALTER TABLE [dbo].[resource_plugin]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_resource] FOREIGN KEY([resource_id])
                        REFERENCES [dbo].[resource] ([id])

                        ALTER TABLE [dbo].[resource_plugin] CHECK CONSTRAINT [FK_resource_plugin_resource]


	                    CREATE NONCLUSTERED INDEX [IX_resource_plugin1] ON [dbo].[resource_plugin] 
	                    (
		                    [id] ASC
	                    )
	                    INCLUDE ( [resource_id],
	                    [plugin_id]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	
	                    CREATE NONCLUSTERED INDEX [IX_resource_plugin2] ON [dbo].[resource_plugin] 
	                    (
		                    [plugin_id] ASC,
		                    [resource_id] ASC
	                    )
	                    INCLUDE ( [id]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	
                    END
					

                    INSERT INTO [db_install] ([version]) VALUES (11);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 11.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
