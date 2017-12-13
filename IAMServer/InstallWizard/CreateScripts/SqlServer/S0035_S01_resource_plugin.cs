using System;

namespace InstallWizard
{
    public class S0035_S01_resource_plugin : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_par'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_par](
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [key] [varchar](50) NOT NULL,
		                    [value] [varchar](max) NOT NULL
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_par]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_par_resource_plugin] FOREIGN KEY([resource_plugin_id])
	                    REFERENCES [dbo].[resource_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[resource_plugin_par] CHECK CONSTRAINT [FK_resource_plugin_par_resource_plugin]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_role'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_role](
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [role_id] [bigint] NOT NULL,
	                     CONSTRAINT [PK_resource_plugin_role] PRIMARY KEY CLUSTERED 
	                    (
		                    [resource_plugin_id] ASC,
		                    [role_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_role]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_role_resource_plugin] FOREIGN KEY([resource_plugin_id])
	                    REFERENCES [dbo].[resource_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE

	                    ALTER TABLE [dbo].[resource_plugin_role] CHECK CONSTRAINT [FK_resource_plugin_role_resource_plugin]

	                    ALTER TABLE [dbo].[resource_plugin_role]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_role_role] FOREIGN KEY([role_id])
	                    REFERENCES [dbo].[role] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE

	                    ALTER TABLE [dbo].[resource_plugin_role] CHECK CONSTRAINT [FK_resource_plugin_role_role]


                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_mapping'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_mapping](
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [field_id] [bigint] NOT NULL,
		                    [data_name] [varchar](50) NOT NULL,
		                    [is_id] [bit] NOT NULL,
		                    [is_password] [bit] NOT NULL,
		                    [is_property] [bit] NOT NULL,
		                    [is_unique_property] [bit] NOT NULL,
	                     CONSTRAINT [PK_plugin_mapping] PRIMARY KEY CLUSTERED 
	                    (
		                    [resource_plugin_id] ASC,
		                    [field_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_mapping] ADD  CONSTRAINT [DF_resource_plugin_mapping_is_id]  DEFAULT ((0)) FOR [is_id]
	                    ALTER TABLE [dbo].[resource_plugin_mapping] ADD  CONSTRAINT [DF_resource_plugin_mapping_is_password]  DEFAULT ((0)) FOR [is_password]
	                    ALTER TABLE [dbo].[resource_plugin_mapping] ADD  CONSTRAINT [DF_resource_plugin_mapping_is_property]  DEFAULT ((1)) FOR [is_property]
	                    ALTER TABLE [dbo].[resource_plugin_mapping] ADD  CONSTRAINT [DF_resource_plugin_mapping_is_unique_property]  DEFAULT ((0)) FOR [is_unique_property]

	                    ALTER TABLE [dbo].[resource_plugin_mapping]  WITH CHECK ADD  CONSTRAINT [FK_plugin_mapping_resource_plugin] FOREIGN KEY([resource_plugin_id])
	                    REFERENCES [dbo].[resource_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[resource_plugin_mapping] CHECK CONSTRAINT [FK_plugin_mapping_resource_plugin]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_lock_filter'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_lock_filter](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [filter_id] [bigint] NOT NULL,
	                     CONSTRAINT [PK_resource_plugin_lock_filter] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_lock_filter]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_lock_filter_filters] FOREIGN KEY([filter_id])
	                    REFERENCES [dbo].[filters] ([id])
	
	                    ALTER TABLE [dbo].[resource_plugin_lock_filter] CHECK CONSTRAINT [FK_resource_plugin_lock_filter_filters]

	                    ALTER TABLE [dbo].[resource_plugin_lock_filter]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_lock_filter_resource_plugin] FOREIGN KEY([resource_plugin_id])
	                    REFERENCES [dbo].[resource_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[resource_plugin_lock_filter] CHECK CONSTRAINT [FK_resource_plugin_lock_filter_resource_plugin]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_ignore_filter'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_ignore_filter](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [filter_id] [bigint] NOT NULL,
	                     CONSTRAINT [PK_resource_plugin_ignore_filter] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]


	                    ALTER TABLE [dbo].[resource_plugin_ignore_filter]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_ignore_filter_filters] FOREIGN KEY([filter_id])
	                    REFERENCES [dbo].[filters] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[resource_plugin_ignore_filter] CHECK CONSTRAINT [FK_resource_plugin_ignore_filter_filters]
	
	                    ALTER TABLE [dbo].[resource_plugin_ignore_filter]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_ignore_filter_resource_plugin] FOREIGN KEY([resource_plugin_id])
	                    REFERENCES [dbo].[resource_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE

	                    ALTER TABLE [dbo].[resource_plugin_ignore_filter] CHECK CONSTRAINT [FK_resource_plugin_ignore_filter_resource_plugin]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_fetch'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_fetch](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [request_date] [datetime] NOT NULL,
		                    [response_date] [datetime] NULL,
		                    [success] [bit] NULL,
		                    [json_data] [varchar](max) NULL,
	                     CONSTRAINT [PK_plugin_FETCH] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_fetch] ADD  CONSTRAINT [DF_resource_plugin_fetch_request_date]  DEFAULT (getdate()) FOR [request_date]

	                    ALTER TABLE [dbo].[resource_plugin_fetch]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_fetch_resource_plugin] FOREIGN KEY([resource_plugin_id])
	                    REFERENCES [dbo].[resource_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE

	                    ALTER TABLE [dbo].[resource_plugin_fetch] CHECK CONSTRAINT [FK_resource_plugin_fetch_resource_plugin]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_schedule'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_schedule](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [schedule] [varchar](500) NOT NULL,
		                    [next] [datetime] NULL,
	                     CONSTRAINT [PK_plugin_schedule] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_schedule] ADD  CONSTRAINT [DF_resource_plugin_schedule_next]  DEFAULT (getdate()) FOR [next]

	                    ALTER TABLE [dbo].[resource_plugin_schedule]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_schedule_resource_plugin] FOREIGN KEY([resource_plugin_id])
	                    REFERENCES [dbo].[resource_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[resource_plugin_schedule] CHECK CONSTRAINT [FK_resource_plugin_schedule_resource_plugin]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_role_time_acl'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_role_time_acl](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [role_id] [bigint] NOT NULL,
		                    [time_acl] [varchar](500) NOT NULL,
	                     CONSTRAINT [PK_resource_plugin_role_time_acl] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_role_time_acl]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_role_time_acl_resource_plugin_role] FOREIGN KEY([resource_plugin_id], [role_id])
	                    REFERENCES [dbo].[resource_plugin_role] ([resource_plugin_id], [role_id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[resource_plugin_role_time_acl] CHECK CONSTRAINT [FK_resource_plugin_role_time_acl_resource_plugin_role]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_role_filter'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_role_filter](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [role_id] [bigint] NOT NULL,
		                    [filter_id] [bigint] NOT NULL,
	                     CONSTRAINT [PK_resource_plugin_role_filter] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_role_filter]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_role_filter_filters] FOREIGN KEY([filter_id])
	                    REFERENCES [dbo].[filters] ([id])
	
	                    ALTER TABLE [dbo].[resource_plugin_role_filter] CHECK CONSTRAINT [FK_resource_plugin_role_filter_filters]

	                    ALTER TABLE [dbo].[resource_plugin_role_filter]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_role_filter_resource_plugin_role] FOREIGN KEY([resource_plugin_id], [role_id])
	                    REFERENCES [dbo].[resource_plugin_role] ([resource_plugin_id], [role_id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[resource_plugin_role_filter] CHECK CONSTRAINT [FK_resource_plugin_role_filter_resource_plugin_role]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'resource_plugin_role_action'))
                    BEGIN

	                    CREATE TABLE [dbo].[resource_plugin_role_action](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [role_id] [bigint] NOT NULL,
		                    [action_key] [varchar](200) NOT NULL,
		                    [action_add_value] [varchar](500) NOT NULL,
		                    [action_del_value] [varchar](500) NULL,
		                    [additional_data] [varchar](max) NULL,
	                     CONSTRAINT [PK_resource_plugin_role_action] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[resource_plugin_role_action]  WITH CHECK ADD  CONSTRAINT [FK_resource_plugin_role_action_resource_plugin_role] FOREIGN KEY([resource_plugin_id], [role_id])
	                    REFERENCES [dbo].[resource_plugin_role] ([resource_plugin_id], [role_id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[resource_plugin_role_action] CHECK CONSTRAINT [FK_resource_plugin_role_action_resource_plugin_role]

                    END

					

                    INSERT INTO [db_install] ([version]) VALUES (35);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 35.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
