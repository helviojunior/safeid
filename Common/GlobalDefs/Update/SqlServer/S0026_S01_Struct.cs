using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0026_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'code_plugin_par'))
                    BEGIN
	                    CREATE TABLE [code_plugin_par](
	                        [enterprise_id] [bigint] NOT NULL,
	                        [file_name] [varchar](200) NOT NULL,
	                        [uri] [varchar](500) NOT NULL,
	                        [key] [varchar](50) NOT NULL,
	                        [value] [varchar](max) NOT NULL,
	                        CONSTRAINT [FK_code_plugin_par_enterprise] FOREIGN KEY([enterprise_id])
	                        REFERENCES [dbo].[enterprise] ([id])
	                        ON UPDATE CASCADE
	                        ON DELETE CASCADE
                        );
                    END

                    INSERT INTO [db_ver] ([version]) VALUES (26);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 26; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
