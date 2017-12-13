using System;

namespace InstallWizard
{
    public class S0030_S01_login_rule : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'login_rule'))
                    BEGIN

	                    CREATE TABLE [dbo].[login_rule](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [context_id] [bigint] NOT NULL,
		                    [name] [varchar](50) NOT NULL,
		                    [rule] [varchar](300) NOT NULL,
		                    [order] [int] NOT NULL,
	                     CONSTRAINT [PK_login_rule] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]
		
	                    ALTER TABLE [dbo].[login_rule]  WITH CHECK ADD  CONSTRAINT [FK_login_rule_context] FOREIGN KEY([context_id])
	                    REFERENCES [dbo].[context] ([id])
	
	                    ALTER TABLE [dbo].[login_rule] CHECK CONSTRAINT [FK_login_rule_context]
		
                    END


                    INSERT INTO [db_install] ([version]) VALUES (30);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 30.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
