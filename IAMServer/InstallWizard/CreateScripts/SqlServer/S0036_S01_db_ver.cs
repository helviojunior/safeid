using System;

namespace InstallWizard
{
    public class S0036_S01_db_ver : ICreateScript
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

                    INSERT INTO [db_install] ([version]) VALUES (36);

                    ";
            }
        }

        public string Precondition
        {
            get { return null; }
        }

        public double Serial { get { return 36.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
