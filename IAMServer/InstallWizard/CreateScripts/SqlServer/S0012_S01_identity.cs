using System;

namespace InstallWizard
{
    public class S0012_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'identity'))
                    BEGIN

	                    CREATE TABLE [dbo].[identity](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [entity_id] [bigint] NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [create_date] [datetime] NOT NULL,
		                    [deleted] [bit] NOT NULL,
		                    [deleted_date] [datetime] NULL,
		                    [temp_locked] [bit] NOT NULL,
	                     CONSTRAINT [PK_identity] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[identity] ADD  CONSTRAINT [DF_identity_create_date]  DEFAULT (getdate()) FOR [create_date]
                        ALTER TABLE [dbo].[identity] ADD  CONSTRAINT [DF_identity_deleted]  DEFAULT ((0)) FOR [deleted]
                        ALTER TABLE [dbo].[identity] ADD  CONSTRAINT [DF_identity_temp_locked]  DEFAULT ((0)) FOR [temp_locked]

                        ALTER TABLE [dbo].[identity]  WITH CHECK ADD  CONSTRAINT [FK_identity_entity] FOREIGN KEY([entity_id])
                        REFERENCES [dbo].[entity] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE

                        ALTER TABLE [dbo].[identity] CHECK CONSTRAINT [FK_identity_entity]

                        ALTER TABLE [dbo].[identity]  WITH CHECK ADD  CONSTRAINT [FK_identity_resource_plugin] FOREIGN KEY([resource_plugin_id])
                        REFERENCES [dbo].[resource_plugin] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE

                        ALTER TABLE [dbo].[identity] CHECK CONSTRAINT [FK_identity_resource_plugin]

	                    CREATE NONCLUSTERED INDEX [IX_identity1] ON [dbo].[identity] 
	                    (
		                    [resource_plugin_id] ASC
	                    )
	                    INCLUDE ( [id],
	                    [entity_id]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]

	                    EXEC('
	                    CREATE TRIGGER [dbo].[IdentityAfterUpdate] ON [dbo].[identity]
	                       AFTER UPDATE
	                    AS 
	                    BEGIN
		                    INSERT	INTO deploy_now (entity_id, date)
		                    SELECT	i.entity_id, dateadd(minute,5,getdate())
			                    FROM	inserted i
			                    inner	join deleted d on d.id = i.id
			                    WHERE	(i.temp_locked <> d.temp_locked)
	                    END')

                    END
					
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'identity_block_inheritance'))
                    BEGIN

	                    CREATE TABLE [dbo].[identity_block_inheritance](
		                    [identity_id] [bigint] NOT NULL,
		                    [date] [datetime] NOT NULL
	                    ) ON [PRIMARY]

	                    CREATE NONCLUSTERED INDEX [IX_identity_block_inheritance1] ON [dbo].[identity_block_inheritance] 
	                    (
		                    [identity_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

                        ALTER TABLE [dbo].[identity_block_inheritance] ADD  CONSTRAINT [DF_identity_block_inheritance_date]  DEFAULT (getdate()) FOR [date]

                        ALTER TABLE [dbo].[identity_block_inheritance]  WITH CHECK ADD  CONSTRAINT [FK_identity_block_inheritance_identity] FOREIGN KEY([identity_id])
                        REFERENCES [dbo].[identity] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE

                        ALTER TABLE [dbo].[identity_block_inheritance] CHECK CONSTRAINT [FK_identity_block_inheritance_identity]


                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'identity_acl_ignore'))
                    BEGIN

	                    CREATE TABLE [dbo].[identity_acl_ignore](
		                    [identity_id] [bigint] NOT NULL,
		                    [start_date] [datetime] NOT NULL,
		                    [end_date] [datetime] NOT NULL
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[identity_acl_ignore] ADD  CONSTRAINT [DF_identity_acl_ignore_start_date]  DEFAULT (getdate()) FOR [start_date]
                        ALTER TABLE [dbo].[identity_acl_ignore] ADD  CONSTRAINT [DF_identity_acl_ignore_end_date]  DEFAULT (CONVERT([datetime],CONVERT([varchar](10),getdate(),(120))+' 23:59:59',(120))) FOR [end_date]

                        ALTER TABLE [dbo].[identity_acl_ignore]  WITH CHECK ADD  CONSTRAINT [FK_identity_acl_ignore_identity] FOREIGN KEY([identity_id])
                        REFERENCES [dbo].[identity] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE

                        ALTER TABLE [dbo].[identity_acl_ignore] CHECK CONSTRAINT [FK_identity_acl_ignore_identity]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (12);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 12.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
