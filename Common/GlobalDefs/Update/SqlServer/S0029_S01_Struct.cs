using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0029_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'st_mail_rule'))
                    BEGIN

                        CREATE TABLE [dbo].[st_mail_rule](
	                        [id] [bigint] IDENTITY(1,1) NOT NULL,
	                        [context_id] [bigint] NOT NULL,
	                        [name] [varchar](300) NOT NULL,
	                        [rule] [varchar](300) NOT NULL,
	                        [order] [int] NOT NULL,
                         CONSTRAINT [PK_st_mail_rule] PRIMARY KEY CLUSTERED 
                        (
	                        [id] ASC
                        )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
                        ) ON [PRIMARY]

                        ALTER TABLE [dbo].[st_mail_rule]  WITH CHECK ADD  CONSTRAINT [FK_st_mail_rule_context] FOREIGN KEY([context_id]) REFERENCES [dbo].[context] ([id])

                        ALTER TABLE [dbo].[st_mail_rule] CHECK CONSTRAINT [FK_st_mail_rule_context]

                        INSERT INTO [st_mail_rule] ([context_id],[name],[rule],[order]) SELECT [context_id],[name],[rule],[order] FROM [login_rule]
                    END

                    INSERT INTO [db_ver] ([version]) VALUES (29);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 29; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
