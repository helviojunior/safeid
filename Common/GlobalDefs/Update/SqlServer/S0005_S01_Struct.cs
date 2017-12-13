using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0005_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'container'))
                    BEGIN
	                    
                        CREATE TABLE [dbo].[container](
	                        [id] [bigint] IDENTITY(1,1) NOT NULL,
	                        [parent_id] [bigint] NOT NULL CONSTRAINT [DF_container_parent_id]  DEFAULT (0),
	                        [context_id] [bigint] NOT NULL,
	                        [name] [varchar](500) NOT NULL,
	                        [create_date] [datetime] NOT NULL CONSTRAINT [DF_container_create_date]  DEFAULT (getdate()),
                            CONSTRAINT [PK_container] PRIMARY KEY CLUSTERED 
                            (
	                            [id] ASC
                            )
                        );

	                    ALTER TABLE [dbo].[container]  WITH CHECK ADD CONSTRAINT [FK_container_context] FOREIGN KEY([context_id]) REFERENCES [dbo].[context] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;
                    END


                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'entity_container'))
                    BEGIN
	                    
                        CREATE TABLE [dbo].[entity_container](
	                        [entity_id] [bigint] NOT NULL,
	                        [container_id] [bigint] NOT NULL,
                            [auto] [bit] NOT NULL  CONSTRAINT [DF_entity_container_auto] DEFAULT (1)
                        );

                        ALTER TABLE [dbo].[entity_container]  WITH CHECK ADD CONSTRAINT [FK_entity_container_entity] FOREIGN KEY([entity_id]) REFERENCES [dbo].[entity] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;

                        ALTER TABLE [dbo].[entity_container]  WITH CHECK ADD CONSTRAINT [FK_entity_container_container] FOREIGN KEY([container_id]) REFERENCES [dbo].[container] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;

                    END

                    INSERT INTO [db_ver] ([version]) VALUES (5);


                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 5.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
