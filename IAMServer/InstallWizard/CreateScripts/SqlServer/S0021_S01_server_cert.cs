using System;

namespace InstallWizard
{
    public class S0021_S01_server_cert : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'server_cert'))
                    BEGIN
	                    CREATE TABLE [dbo].[server_cert](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [server_cert] [varchar](max) NOT NULL,
		                    [server_pkcs12_cert] [varchar](max) NOT NULL,
	                     CONSTRAINT [PK_server_cert] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]
	
                    END


                    INSERT INTO [db_install] ([version]) VALUES (21);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 21.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
