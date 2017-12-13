using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0001_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'db_ver'))
                    BEGIN
	                    CREATE TABLE [db_ver] (
		                    [version] BigInt Not Null,
		                    [date] Datetime Not Null DEFAULT GETDATE()
	                    );
                    END

                    ";
            }
        }

        public string Precondition
        {
            get { return null; }
        }

        public double Serial { get { return 1.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
