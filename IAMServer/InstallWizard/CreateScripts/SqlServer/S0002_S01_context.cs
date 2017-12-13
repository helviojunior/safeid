using System;

namespace InstallWizard
{
    public class S0002_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'context'))
					BEGIN
						CREATE TABLE [dbo].[context](
							[id] [bigint] IDENTITY(1,1) NOT NULL,
							[enterprise_id] [bigint] NOT NULL,
							[name] [varchar](255) NOT NULL,
							[password_rule] [varchar](50) NULL,
							[auth_key_time] [int] NOT NULL,
							[pwd_length] [int] NOT NULL,
							[pwd_upper_case] [bit] NOT NULL,
							[pwd_lower_case] [bit] NOT NULL,
							[pwd_digit] [bit] NOT NULL,
							[pwd_symbol] [bit] NOT NULL,
							[pwd_no_name] [bit] NOT NULL,
							[create_date] [datetime] NOT NULL,
						 CONSTRAINT [PK_context] PRIMARY KEY CLUSTERED 
						(
							[id] ASC
						)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
						) ON [PRIMARY]

                        ALTER TABLE [dbo].[context] ADD  CONSTRAINT [DF_context_auth_key_time]  DEFAULT ((20)) FOR [auth_key_time]
                        ALTER TABLE [dbo].[context] ADD  CONSTRAINT [DF_context_pwd_length]  DEFAULT ((8)) FOR [pwd_length]
                        ALTER TABLE [dbo].[context] ADD  CONSTRAINT [DF_context_pwd_upper_case]  DEFAULT ((1)) FOR [pwd_upper_case]
                        ALTER TABLE [dbo].[context] ADD  CONSTRAINT [DF_context_pwd_lower_case]  DEFAULT ((1)) FOR [pwd_lower_case]
                        ALTER TABLE [dbo].[context] ADD  CONSTRAINT [DF_context_pwd_digit]  DEFAULT ((1)) FOR [pwd_digit]
                        ALTER TABLE [dbo].[context] ADD  CONSTRAINT [DF_context_pwd_symbol]  DEFAULT ((1)) FOR [pwd_symbol]
                        ALTER TABLE [dbo].[context] ADD  CONSTRAINT [DF_context_pwd_no_name]  DEFAULT ((1)) FOR [pwd_no_name]
                        ALTER TABLE [dbo].[context] ADD  CONSTRAINT [DF_context_create_date]  DEFAULT (getdate()) FOR [create_date]

					END

                    INSERT INTO [db_install] ([version]) VALUES (2);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
            
        }

        public double Serial { get { return 2.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
