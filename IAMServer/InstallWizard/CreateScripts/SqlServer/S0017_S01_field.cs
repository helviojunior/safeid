using System;

namespace InstallWizard
{
    public class S0017_S01_field : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'field'))
                    BEGIN
		
	                    CREATE TABLE [dbo].[field](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [name] [varchar](100) NOT NULL,
		                    [data_type] [varchar](50) NOT NULL,
		                    [public] [bit] NOT NULL,
		                    [user] [bit] NULL,
	                     CONSTRAINT [PK_fields] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]


	                    ALTER TABLE [dbo].[field] ADD  CONSTRAINT [DF_field_data_type]  DEFAULT ('string') FOR [data_type]
	                    ALTER TABLE [dbo].[field] ADD  CONSTRAINT [DF_field_public]  DEFAULT ((0)) FOR [public]
	                    ALTER TABLE [dbo].[field] ADD  CONSTRAINT [DF_field_user]  DEFAULT ((0)) FOR [user]

	                    ALTER TABLE [dbo].[field]  WITH CHECK ADD  CONSTRAINT [FK_field_enterprise] FOREIGN KEY([enterprise_id])
	                    REFERENCES [dbo].[enterprise] ([id])
	
	                    ALTER TABLE [dbo].[field] CHECK CONSTRAINT [FK_field_enterprise]
	

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'identity_field'))
                    BEGIN

	                    CREATE TABLE [dbo].[identity_field](
		                    [identity_id] [bigint] NOT NULL,
		                    [field_id] [bigint] NOT NULL,
		                    [value] [varchar](800) NOT NULL
	                    ) ON [PRIMARY]

	                    CREATE NONCLUSTERED INDEX [IX_identity_field_1] ON [dbo].[identity_field] 
	                    (
		                    [identity_id] ASC,
		                    [field_id] ASC,
		                    [value] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]

	                    CREATE NONCLUSTERED INDEX [IX_identity_field1] ON [dbo].[identity_field] 
	                    (
		                    [field_id] ASC,
		                    [value] ASC
	                    )
	                    INCLUDE ( [identity_id]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]

	                    CREATE NONCLUSTERED INDEX [IX_identity_field2] ON [dbo].[identity_field] 
	                    (
		                    [field_id] ASC,
		                    [identity_id] ASC
	                    )
	                    INCLUDE ( [value]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]

	                    CREATE NONCLUSTERED INDEX [IX_identity_field4] ON [dbo].[identity_field] 
	                    (
		                    [identity_id] ASC
	                    )
	                    INCLUDE ( [field_id],
	                    [value]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]


	                    ALTER TABLE [dbo].[identity_field]  WITH NOCHECK ADD  CONSTRAINT [FK_identity_field_field] FOREIGN KEY([field_id])
	                    REFERENCES [dbo].[field] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[identity_field] CHECK CONSTRAINT [FK_identity_field_field]
	
	                    ALTER TABLE [dbo].[identity_field]  WITH NOCHECK ADD  CONSTRAINT [FK_identity_field_identity] FOREIGN KEY([identity_id])
	                    REFERENCES [dbo].[identity] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[identity_field] CHECK CONSTRAINT [FK_identity_field_identity]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'entity_field'))
                    BEGIN
		
	                    CREATE TABLE [dbo].[entity_field](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [entity_id] [bigint] NOT NULL,
		                    [field_id] [bigint] NOT NULL,
		                    [value] [varchar](500) NOT NULL,
	                     CONSTRAINT [PK_entity_field] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[entity_field]  WITH CHECK ADD  CONSTRAINT [FK_entity_field_entity] FOREIGN KEY([entity_id])
	                    REFERENCES [dbo].[entity] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[entity_field] CHECK CONSTRAINT [FK_entity_field_entity]
	
	
	                    ALTER TABLE [dbo].[entity_field]  WITH CHECK ADD  CONSTRAINT [FK_entity_field_field2] FOREIGN KEY([field_id])
	                    REFERENCES [dbo].[field] ([id])
	
	                    ALTER TABLE [dbo].[entity_field] CHECK CONSTRAINT [FK_entity_field_field2]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (17);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 17.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
