using System;

namespace InstallWizard
{
    public class S0006_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

					IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'languages'))
                    BEGIN

	                    CREATE TABLE [dbo].[languages](
	                    [language] [varchar](10) NOT NULL,
	                    [name] [varchar](50) NOT NULL,
	                     CONSTRAINT [PK_languages] PRIMARY KEY CLUSTERED 
	                    (
		                    [language] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    INSERT INTO languages ([language], [name]) VALUES('pt-BR', 'Português (Brasil)')
	                    INSERT INTO languages ([language], [name]) VALUES('en-US','English')

                    END

                    INSERT INTO [db_install] ([version]) VALUES (6);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 6.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
