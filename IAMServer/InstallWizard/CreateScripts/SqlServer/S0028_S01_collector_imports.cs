using System;

namespace InstallWizard
{
    public class S0028_S01_collector_imports : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'collector_imports_old'))
                    BEGIN

		
	                    CREATE TABLE [dbo].[collector_imports_old](
		                    [date] [datetime] NOT NULL,
		                    [file_name] [varchar](500) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [import_id] [varchar](50) NOT NULL,
		                    [package_id] [varchar](50) NOT NULL,
		                    [package] [varchar](max) NOT NULL,
		                    [status] [varchar](2) NOT NULL
	                    ) ON [PRIMARY]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'collector_imports'))
                    BEGIN


	                    CREATE TABLE [dbo].[collector_imports](
		                    [date] [datetime] NOT NULL,
		                    [file_name] [varchar](500) NOT NULL,
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [import_id] [varchar](50) NOT NULL,
		                    [package_id] [varchar](50) NOT NULL,
		                    [package] [varchar](max) NOT NULL,
		                    [status] [varchar](2) NOT NULL
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[collector_imports] ADD  CONSTRAINT [DF_collector_imports_status]  DEFAULT ('I') FOR [status]

	                    CREATE NONCLUSTERED INDEX [IX_collector_imports_filename] ON [dbo].[collector_imports] 
	                    (
		                    [file_name] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
	
	                    CREATE NONCLUSTERED INDEX [IX_collector_imports_status] ON [dbo].[collector_imports] 
	                    (
		                    [status] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
	
	                    CREATE NONCLUSTERED INDEX [IX_collector_imports1] ON [dbo].[collector_imports] 
	                    (
		                    [status] ASC,
		                    [resource_plugin_id] ASC,
		                    [import_id] ASC,
		                    [package_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
	
	
	                    CREATE NONCLUSTERED INDEX [IX_collector_imports2] ON [dbo].[collector_imports] 
	                    (
		                    [resource_plugin_id] ASC,
		                    [import_id] ASC,
		                    [package_id] ASC,
		                    [status] ASC,
		                    [date] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
	
	
	                    CREATE NONCLUSTERED INDEX [IX_collector_imports3] ON [dbo].[collector_imports] 
	                    (
		                    [status] ASC
	                    )
	                    INCLUDE ( [date],
	                    [file_name],
	                    [resource_plugin_id],
	                    [import_id],
	                    [package_id]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
	
	
	                    CREATE NONCLUSTERED INDEX [IX_collector_imports4] ON [dbo].[collector_imports] 
	                    (
		                    [status] ASC,
		                    [resource_plugin_id] ASC
	                    )
	                    INCLUDE ( [date],
	                    [file_name],
	                    [import_id],
	                    [package_id],
	                    [package]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
	
	
	                    CREATE NONCLUSTERED INDEX [IX_collector_imports5] ON [dbo].[collector_imports] 
	                    (
		                    [status] ASC,
		                    [resource_plugin_id] ASC
	                    )
	                    INCLUDE ( [date],
	                    [file_name],
	                    [import_id],
	                    [package_id],
	                    [package]) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 70) ON [PRIMARY]
	
	                    EXEC('CREATE TRIGGER [dbo].[CollectorimportsDeteleTrigger] on [dbo].[collector_imports]
	                    AFTER DELETE
	                    AS
	                    BEGIN
		
		                    INSERT INTO dbo.collector_imports_old
		                    SELECT [date]
		                      ,[file_name]
		                      ,[resource_plugin_id]
		                      ,[import_id]
		                      ,[package_id]
		                      ,[package]
		                      ,case when d.status = ''F'' then ''D'' else d.status end
		                    FROM deleted d
		
	                    END')

                    END



                    INSERT INTO [db_install] ([version]) VALUES (28);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 28.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
