using System;

namespace InstallWizard
{
    public class S0014_S01_role : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'role'))
                    BEGIN

	                    CREATE TABLE [dbo].[role](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [context_id] [bigint] NOT NULL,
		                    [parent_id] [bigint] NOT NULL,
		                    [name] [varchar](200) NOT NULL,
		                    [create_date] [datetime] NOT NULL,
	                     CONSTRAINT [PK_role] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[role] ADD  CONSTRAINT [DF_role_parent_id]  DEFAULT ((0)) FOR [parent_id]
                        ALTER TABLE [dbo].[role] ADD  CONSTRAINT [DF_role_create_date]  DEFAULT (getdate()) FOR [create_date]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'identity_role'))
                    BEGIN

                        CREATE TABLE [dbo].[identity_role](
	                        [identity_id] [bigint] NOT NULL,
	                        [role_id] [bigint] NOT NULL,
	                        [auto] [bit] NOT NULL,
                         CONSTRAINT [PK_identity_role] PRIMARY KEY CLUSTERED 
                        (
	                        [identity_id] ASC,
	                        [role_id] ASC
                        )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
                        ) ON [PRIMARY]

                        ALTER TABLE [dbo].[identity_role] ADD  CONSTRAINT [DF_identity_role_auto]  DEFAULT ((0)) FOR [auto]

                        ALTER TABLE [dbo].[identity_role]  WITH CHECK ADD  CONSTRAINT [FK_identity_role_identity] FOREIGN KEY([identity_id])
                        REFERENCES [dbo].[identity] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE
                        
                        ALTER TABLE [dbo].[identity_role] CHECK CONSTRAINT [FK_identity_role_identity]
                        
                        ALTER TABLE [dbo].[identity_role]  WITH CHECK ADD  CONSTRAINT [FK_identity_role_role] FOREIGN KEY([role_id])
                        REFERENCES [dbo].[role] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE
                        
                        ALTER TABLE [dbo].[identity_role] CHECK CONSTRAINT [FK_identity_role_role]

                    END

                    INSERT INTO [db_install] ([version]) VALUES (14);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 14.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
