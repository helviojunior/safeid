using System;

namespace InstallWizard
{
    public class S0022_S01_sys_role : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'sys_role_permission'))
                    BEGIN
	
		
	                    CREATE TABLE [dbo].[sys_role_permission](
		                    [role_id] [bigint] NOT NULL,
		                    [permission_id] [bigint] NOT NULL,
	                     CONSTRAINT [PK_sys_role_permission] PRIMARY KEY CLUSTERED 
	                    (
		                    [role_id] ASC,
		                    [permission_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'sys_role'))
                    BEGIN
	

	                    CREATE TABLE [dbo].[sys_role](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [parent_id] [bigint] NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [name] [varchar](200) NOT NULL,
		                    [sa] [bit] NOT NULL,
		                    [ea] [bit] NOT NULL,
		                    [create_date] [datetime] NOT NULL,
	                     CONSTRAINT [PK_sys_role] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]
	
	                    ALTER TABLE [dbo].[sys_role] ADD  CONSTRAINT [DF_sys_role_parent_id]  DEFAULT ((0)) FOR [parent_id]
	                    ALTER TABLE [dbo].[sys_role] ADD  CONSTRAINT [DF_sys_role_enterprise_id]  DEFAULT ((0)) FOR [enterprise_id]
	                    ALTER TABLE [dbo].[sys_role] ADD  CONSTRAINT [DF_sys_role_sa]  DEFAULT ((0)) FOR [sa]
	                    ALTER TABLE [dbo].[sys_role] ADD  CONSTRAINT [DF_sys_role_ea]  DEFAULT ((0)) FOR [ea]
	                    ALTER TABLE [dbo].[sys_role] ADD  CONSTRAINT [DF_sys_role_create_date]  DEFAULT (getdate()) FOR [create_date]
	
	
                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'sys_entity_role'))
                    BEGIN
	
	
	                    CREATE TABLE [dbo].[sys_entity_role](
		                    [entity_id] [bigint] NOT NULL,
		                    [role_id] [bigint] NOT NULL,
	                     CONSTRAINT [PK_sys_entity_role] PRIMARY KEY CLUSTERED 
	                    (
		                    [entity_id] ASC,
		                    [role_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]
	
	                    ALTER TABLE [dbo].[sys_entity_role]  WITH CHECK ADD  CONSTRAINT [FK_sys_entity_role_entity] FOREIGN KEY([entity_id])
	                    REFERENCES [dbo].[entity] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE

	                    ALTER TABLE [dbo].[sys_entity_role] CHECK CONSTRAINT [FK_sys_entity_role_entity]
	
	                    ALTER TABLE [dbo].[sys_entity_role]  WITH CHECK ADD  CONSTRAINT [FK_sys_entity_role_sys_role] FOREIGN KEY([role_id])
	                    REFERENCES [dbo].[sys_role] ([id])

	                    ALTER TABLE [dbo].[sys_entity_role] CHECK CONSTRAINT [FK_sys_entity_role_sys_role]
	
	                    INSERT INTO [sys_role] ([parent_id],[enterprise_id],[name],[sa],[ea]) values (0,0,'System Admin',1,0)

                    END


                    INSERT INTO [db_install] ([version]) VALUES (22);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 22.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
