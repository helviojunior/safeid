using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallWizard
{
    public class S0001_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'db_install'))
                    BEGIN
	                    CREATE TABLE [db_install] (
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
