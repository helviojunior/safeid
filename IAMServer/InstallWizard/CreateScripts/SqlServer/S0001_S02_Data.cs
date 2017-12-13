using System;

namespace InstallWizard
{
    public class S0001_S01_Data : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    INSERT INTO [db_install] ([version]) VALUES (1);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when count(*) = 0 then 1 ELSE 0 END FROM [db_install]"; }
        }

        public double Serial { get { return 1.2; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
