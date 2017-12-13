using System;

namespace InstallWizard
{
    public class S0016_S01_logs : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'logs'))
                    BEGIN
		
	                    CREATE TABLE [dbo].[logs](
		                    [id] [varchar](50) NOT NULL,
		                    [date] [datetime] NOT NULL,
		                    [source] [varchar](50) NOT NULL,
		                    [key] [int] NOT NULL,
		                    [level] [int] NOT NULL,
		                    [proxy_id] [bigint] NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [context_id] [bigint] NOT NULL,
		                    [resource_id] [bigint] NOT NULL,
		                    [plugin_id] [bigint] NOT NULL,
		                    [entity_id] [bigint] NOT NULL,
		                    [identity_id] [bigint] NOT NULL,
		                    [text] [varchar](max) NOT NULL,
		                    [additional_data] [varchar](max) NULL,
		                    [executed_by_entity_id] [bigint] NOT NULL,
	                     CONSTRAINT [PK_logs] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    CREATE NONCLUSTERED INDEX [IX_logs] ON [dbo].[logs] 
	                    (
		                    [date] ASC,
		                    [key] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]

	                    CREATE NONCLUSTERED INDEX [IX_logs_1] ON [dbo].[logs] 
	                    (
		                    [date] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]

	                    CREATE NONCLUSTERED INDEX [IX_logs_date_entity] ON [dbo].[logs] 
	                    (
		                    [date] ASC,
		                    [entity_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 50) ON [PRIMARY]

                        ALTER TABLE [dbo].[logs] ADD  CONSTRAINT [DF_logs_date]  DEFAULT (getdate()) FOR [date]
                        ALTER TABLE [dbo].[logs] ADD  CONSTRAINT [DF_logs_key]  DEFAULT ((0)) FOR [key]
                        ALTER TABLE [dbo].[logs] ADD  CONSTRAINT [DF_logs_level]  DEFAULT ((0)) FOR [level]
                        ALTER TABLE [dbo].[logs] ADD  CONSTRAINT [DF_logs_executed_by_entity_id]  DEFAULT ((0)) FOR [executed_by_entity_id]

                        ALTER TABLE [dbo].[entity_timeline]  WITH CHECK ADD  CONSTRAINT [FK_entity_timeline_logs] FOREIGN KEY([log_id])
                        REFERENCES [dbo].[logs] ([id])
                        
                        ALTER TABLE [dbo].[entity_timeline] CHECK CONSTRAINT [FK_entity_timeline_logs]
                        
                        EXEC('CREATE TRIGGER [dbo].[LogsInsteadTrigger] on [dbo].[logs]
                        INSTEAD OF INSERT
                        AS
                        BEGIN
	                        /*
	                        DECLARE @id varchar(22)
	                        SELECT @id = REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(varchar(23), GETDATE(),121),''-'',''''),'' '',''''),'':'',''''),''.'','''')

	                        INSERT INTO log_seed select 1 where (select COUNT(*) from log_seed) = 0
	                        */
	
	                        UPDATE log_seed SET seed = seed + 1
	                        UPDATE log_seed SET seed = 1 where seed > 99999

	                        /*SELECT @id = @id + CAST(seed as varchar) FROM log_seed*/

                        DECLARE @seed varchar(5)
                        SELECT @seed = seed FROM log_seed with(nolock)

                          DECLARE @tmpInserted table (
		                        [id1] [varchar](17) NULL,
		                        [id2] [varchar](5) NULL,
		                        [id3] [varchar](25) NULL,
		                        [date] [datetime] NOT NULL,
		                        [source] [varchar](50) NOT NULL,
		                        [level] [int] NOT NULL,
		                        [proxy_id] [bigint] NULL  default(0),
		                        [enterprise_id] [bigint] NULL  default(0),
		                        [context_id] [bigint] NULL default(0),
		                        [resource_id] [bigint] NULL default(0),
		                        [plugin_id] [bigint] NULL default(0),
		                        [entity_id] [bigint] NULL default(0),
		                        [identity_id] [bigint] NULL default(0),
		                        [text] [varchar](max) NOT NULL,
		                        [additional_data] [varchar](max) NULL,
		                        [key] [int] NULL,
		                        [executed_by_entity_id] [bigint] NOT NULL
	                        )
  
                          INSERT INTO @tmpInserted
                                   ([id1]
                                   ,[id2]
                                   ,[id3]
                                   ,[date]
                                   ,[source]
                                   ,[key]
                                   ,[level]
                                   ,[proxy_id]
                                   ,[enterprise_id]
                                   ,[context_id]
                                   ,[resource_id]
                                   ,[plugin_id]
                                   ,[entity_id]
                                   ,[identity_id]
                                   ,[text]
                                   ,[additional_data]
                                   ,[executed_by_entity_id])
                           SELECT	CONVERT(varchar(17),REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(varchar(23), i.date,121),''-'',''''),'' '',''''),'':'',''''),''.'','''')),
			                        @seed,
			                        convert(varchar(25), CAST((ROW_NUMBER() OVER (ORDER BY i.date ASC)) AS VARCHAR)),
			                        i.date, 
			                        i.source,
			                        i.[key],
			                        i.level,
			                        proxy_id = CASE
				                         WHEN i.proxy_id is null THEN 0 
				                         ELSE i.proxy_id
			                        END,
			                        enterprise_id = CASE
				                        WHEN (i.enterprise_id is null OR i.enterprise_id = 0) AND i.context_id > 0 
					                        THEN (SELECT top 1 c.enterprise_id 
							                        FROM context c with(nolock)
							                        WHERE c.id = i.context_id)
				                         WHEN (i.enterprise_id is null OR i.enterprise_id = 0) AND i.resource_id > 0 
					                        THEN (SELECT top 1 c.enterprise_id 
							                        FROM resource r  with(nolock)
								                        inner join context c on r.context_id = c.id
							                        WHERE r.id = i.resource_id)
				                        WHEN (i.enterprise_id is null OR i.enterprise_id = 0) AND i.entity_id > 0 
					                        THEN (SELECT top 1 c.enterprise_id 
							                        FROM entity e with(nolock)
								                        inner join context c on e.context_id = c.id
							                        WHERE e.id = i.entity_id)
				                         ELSE i.enterprise_id
			                        END,
			                        context_id = CASE
				                         WHEN (i.context_id is null OR i.context_id = 0) AND i.resource_id > 0 
					                        THEN (SELECT top 1 r.context_id 
							                        FROM resource r  with(nolock)
							                        WHERE r.id = i.resource_id)
				                        WHEN (i.context_id is null OR i.context_id = 0) AND i.entity_id > 0 
					                        THEN (SELECT top 1 e.context_id 
							                        FROM entity e with(nolock)
							                        WHERE e.id = i.entity_id)
				                         ELSE i.context_id
			                        END,
			                        i.resource_id,
			                        i.plugin_id,
			                        i.entity_id,
			                        i.identity_id,
			                        i.text,
			                        i.additional_data,
			                        i.executed_by_entity_id
                           FROM inserted i


	                        --INSERT ON REAL TABLE
	                        INSERT INTO logs
                                   ([id]
                                   ,[date]
                                   ,[source]
                                   ,[key]
                                   ,[level]
                                   ,[proxy_id]
                                   ,[enterprise_id]
                                   ,[context_id]
                                   ,[resource_id]
                                   ,[plugin_id]
                                   ,[entity_id]
                                   ,[identity_id]
                                   ,[text]
                                   ,[additional_data]
                                   ,[executed_by_entity_id])
	                        SELECT min([id1] + [id2]+ [id3])
                                   ,[date]
                                   ,[source]
                                   ,[key]
                                   ,[level]
                                   ,[proxy_id]
                                   ,[enterprise_id]
                                   ,[context_id]
                                   ,[resource_id]
                                   ,[plugin_id]
                                   ,[entity_id]
                                   ,[identity_id]
                                   ,[text]
                                   ,[additional_data]
                                   ,[executed_by_entity_id]
                            FROM	@tmpInserted
                            GROUP BY
			                        [date]
                                   ,[source]
                                   ,[key]
                                   ,[level]
                                   ,[proxy_id]
                                   ,[enterprise_id]
                                   ,[context_id]
                                   ,[resource_id]
                                   ,[plugin_id]
                                   ,[entity_id]
                                   ,[identity_id]
                                   ,[text]
                                   ,[additional_data]
                                   ,[executed_by_entity_id]

	                        --INSERT ON ENTITY TIMELINE TOO
	                        INSERT INTO entity_timeline
                                   ([log_id]
                                   ,[date]
                                   ,[key]
                                   ,[entity_id]
                                   ,[identity_id]          
                                   ,[title]
                                   ,[text])
                             SELECT	min([id1] + [id2]+ [id3]),
			                        i.date, 
			                        i.[key],
			                        i.entity_id,
			                        i.identity_id,
			                        i.text,
			                        i.additional_data
                               FROM @tmpInserted i
	                        WHERE	[key] IN (1002, 1003, 1004, 1007, 1008, 1011, 1012, 1016, 1017, 1018, 1021, 1022, 1023, 1024, 1027)
	                        GROUP BY
			                        i.date, 
			                        i.[key],
			                        i.entity_id,
			                        i.identity_id,
			                        i.text,
			                        i.additional_data
                        END;')

                    END

                    INSERT INTO [db_install] ([version]) VALUES (16);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 16.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
