using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0031_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_cleanup_logs')
                    BEGIN
                        DROP PROCEDURE sp_cleanup_logs;
                    END


					EXEC ('CREATE PROCEDURE [dbo].[sp_cleanup_logs]
                        AS
                        delete	from logs
                        where	[KEY] = 0
	                        and date < DATEADD(d,-15,getdate())
	                        and id in (
				                        select id from logs l with(nolock)
				                        where not exists (select 1 from dbo.entity_timeline t with(nolock)
									                        where t.log_id = l.id)
				                        )

						delete from [dbo].[st_package_track]
							where date < DATEADD(d,-15,getdate())

						declare @SQL nvarchar(max)
						declare @actual_recovery varchar(100)
						SELECT @actual_recovery = recovery_model_desc FROM sys.databases where database_id = DB_ID();

						SET @SQL = N''Use '' + QUOTENAME(DB_NAME()) + '';'' + CHAR(13)

						IF (@actual_recovery <> ''SIMPLE'')
							SET @SQL = @SQL + N''ALTER DATABASE '' + QUOTENAME(DB_NAME()) + '' SET RECOVERY SIMPLE;'' + CHAR(13)


						SELECT @SQL = @SQL +  N''  DBCC SHRINKFILE ('' + quotename(df.[name],'''''''') + '', 1);'' + CHAR(13)
						 from sys.database_files df
						 order by [type], name

						 IF (@actual_recovery <> ''SIMPLE'')
							SET @SQL = @SQL + N''ALTER DATABASE '' + QUOTENAME(DB_NAME()) + '' SET RECOVERY ''+ @actual_recovery + N'';'' + CHAR(13)

						--print @SQL
 
						execute (@SQL)

					');

                    INSERT INTO [db_ver] ([version]) VALUES (31);
                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 31; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
