using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0030_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'st_package_track'))
                    BEGIN

                        CREATE TABLE [dbo].[st_package_track](
	                        [id] [bigint] IDENTITY(1,1) NOT NULL,
	                        [entity_id] [bigint] NOT NULL  DEFAULT ((0)),
	                        [date] [datetime] NOT NULL DEFAULT (getdate()),
	                        [flow] [varchar](20) NOT NULL,
                            [package_id] [varchar](100) NOT NULL,
                            [filename] [varchar](500) NOT NULL,
	                        [package] [varchar](max) NOT NULL,
                         CONSTRAINT [PK_st_package_track] PRIMARY KEY CLUSTERED 
                        (
	                        [id] ASC
                        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

                    END
        
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'st_package_track_history'))
                    BEGIN

                        CREATE TABLE [dbo].[st_package_track_history](
	                        [package_id] [bigint] NOT NULL,
	                        [date] [datetime] NOT NULL  DEFAULT (getdate()),
	                        [source] [varchar](200) NOT NULL,
	                        [text] [varchar](max) NOT NULL
                        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]


                        ALTER TABLE [dbo].[st_package_track_history]  WITH CHECK ADD  CONSTRAINT [FK_st_package_track_history_st_package_track] FOREIGN KEY([package_id])
                        REFERENCES [dbo].[st_package_track] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE

                        ALTER TABLE [dbo].[st_package_track_history] CHECK CONSTRAINT [FK_st_package_track_history_st_package_track]

                    END


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_package_track')
                    BEGIN
                        DROP PROCEDURE sp_new_package_track;
                    END


					EXEC ('CREATE procedure [dbo].[sp_new_package_track]
	                        @entity_id bigint,
                            @date datetime,
	                        @flow varchar(20),
	                        @package_id varchar(50),
	                        @filename varchar(500),
	                        @package varchar(max)
                        as

                        insert into st_package_track ([entity_id], [date] ,[flow] ,[package_id] ,[filename] ,[package]) 
                        values (@entity_id, @date ,@flow ,@package_id, @filename ,@package)

                        SELECT	id 
                        FROM		st_package_track t
                        WHERE	t.entity_id		= @entity_id
	                        AND	t.date			= @date
	                        AND	t.package_id	= @package_id
	                        AND	t.filename		= @filename
					');

                    INSERT INTO [db_ver] ([version]) VALUES (30);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 30; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
