using System;

namespace InstallWizard
{
    public class S0003_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

					IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'entity'))
					BEGIN

						CREATE TABLE [dbo].[entity](
							[id] [bigint] IDENTITY(1,1) NOT NULL,
							[context_id] [bigint] NOT NULL,
							[alias] [varchar](30) NOT NULL,
							[login] [varchar](50) NULL,
							[full_name] [varchar](300) NOT NULL,
							[password] [varchar](2000) NULL,
							[create_date] [datetime] NOT NULL,
							[change_password] [datetime] NOT NULL,
							[locked] [bit] NOT NULL,
							[recovery_code] [varchar](50) NULL,
							[last_login] [datetime] NULL,
							[must_change_password] [bit] NOT NULL,
							[deleted] [bit] NOT NULL,
							[deleted_date] [datetime] NULL,
						 CONSTRAINT [PK_entity] PRIMARY KEY CLUSTERED 
						(
							[id] ASC
						)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
						) ON [PRIMARY]

                        ALTER TABLE [dbo].[entity] ADD  CONSTRAINT [DF_entity_create_date]  DEFAULT (getdate()) FOR [create_date]
                        ALTER TABLE [dbo].[entity] ADD  CONSTRAINT [DF_entity_change_password]  DEFAULT ('1970-01-01 00:00:00') FOR [change_password]
                        ALTER TABLE [dbo].[entity] ADD  CONSTRAINT [DF_entity_locked]  DEFAULT ((0)) FOR [locked]
                        ALTER TABLE [dbo].[entity] ADD  CONSTRAINT [DF_entity_must_change_password]  DEFAULT ((0)) FOR [must_change_password]
                        ALTER TABLE [dbo].[entity] ADD  CONSTRAINT [DF_entity_deleted]  DEFAULT ((0)) FOR [deleted]

						CREATE NONCLUSTERED INDEX [IX_entity2] ON [dbo].[entity] 
						(
							[context_id] ASC,
							[alias] ASC,
							[full_name] ASC,
							[create_date] ASC
						)
						INCLUDE ( [id]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
						
						CREATE NONCLUSTERED INDEX [IX_entity3] ON [dbo].[entity] 
						(
							[deleted] ASC
						)
						INCLUDE ( [context_id],
						[locked],
						[last_login]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
						
						CREATE NONCLUSTERED INDEX [IX_entity4] ON [dbo].[entity] 
						(
							[id] ASC
						)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]

                        CREATE NONCLUSTERED INDEX [IX_entity5]
                        ON [dbo].[entity] ([context_id])
                        INCLUDE ([id],[full_name],[login],[password],[locked],[deleted])

                        ALTER TABLE [dbo].[entity]  WITH CHECK ADD  CONSTRAINT [FK_entity_context] FOREIGN KEY([context_id])
                        REFERENCES [dbo].[context] ([id])
                        
                        ALTER TABLE [dbo].[entity] CHECK CONSTRAINT [FK_entity_context]

                        EXEC ('
                            CREATE TRIGGER [dbo].[EntityAfterUpdate] ON [dbo].[entity]
                               AFTER UPDATE
                            AS 
                            BEGIN
	                            INSERT	INTO deploy_now (entity_id, date)
	                            SELECT	i.id, dateadd(minute,5,getdate())
		                            FROM	inserted i
		                            inner	join deleted d on d.id = i.id
		                            WHERE	(i.locked <> d.locked)
			                            OR	(i.deleted <> d.deleted)
                            END
                        ')


					END

                    INSERT INTO [db_install] ([version]) VALUES (3);


                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 3.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
