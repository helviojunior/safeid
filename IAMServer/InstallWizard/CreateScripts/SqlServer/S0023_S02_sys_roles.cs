using System;

namespace InstallWizard
{
    public class S0023_S02_sys_roles : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'sys_module'))
                    BEGIN

	                    CREATE TABLE [dbo].[sys_module](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [key] [varchar](50) NOT NULL,
		                    [name] [varchar](200) NOT NULL,
	                     CONSTRAINT [PK_sys_module] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]
	
                        SET IDENTITY_INSERT sys_module ON
                        INSERT INTO sys_module (id, [key], name) VALUES(1,'autoservice','Autoserviço')
                        INSERT INTO sys_module (id, [key], name) VALUES(2,'admin','Admin')
                        SET IDENTITY_INSERT sys_module OFF

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'sys_sub_module'))
                    BEGIN

	                    CREATE TABLE [dbo].[sys_sub_module](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [module_id] [bigint] NOT NULL,
		                    [key] [varchar](50) NOT NULL,
		                    [api_module] [varchar](50) NULL,
		                    [name] [varchar](200) NOT NULL,
	                     CONSTRAINT [PK_sys_sub_module] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[sys_sub_module]  WITH CHECK ADD  CONSTRAINT [FK_sys_sub_module_sys_module] FOREIGN KEY([module_id])
	                    REFERENCES [dbo].[sys_module] ([id])
	
	                    ALTER TABLE [dbo].[sys_sub_module] CHECK CONSTRAINT [FK_sys_sub_module_sys_module]

                        SET IDENTITY_INSERT sys_sub_module ON
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(1, 2, 'dasboard', null, 'Dasboard');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(2, 2, 'enterprise', 'enterprise', 'Empresa');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(3, 2, 'context', 'context', 'Contexto');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(4, 2, 'system_roles', 'sysroles', 'Funções de sistema');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(5, 2, 'users', 'users', 'Usuários');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(6, 2, 'roles', 'roles', 'Funções de usuário');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(7, 2, 'plugin', 'plugin', 'Plugin');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(8, 2, 'proxy', 'proxy', 'Proxy');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(9, 2, 'resource', 'resource', 'Recursos');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(10, 2, 'field', 'fields', 'Campos');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(11, 2, 'resource_plugin', 'resourceplugin', 'Recurso x Plugin');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(12, 2, 'service_status', null, 'Status dos serviços');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(13, 2, 'filter', 'filter', 'Filtros');
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(14, 2, 'license', 'license', 'Licença');
                        SET IDENTITY_INSERT sys_sub_module OFF

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'sys_permission'))
                    BEGIN


	                    CREATE TABLE [dbo].[sys_permission](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [submodule_id] [bigint] NOT NULL,
		                    [key] [varchar](50) NOT NULL,
		                    [name] [varchar](200) NOT NULL,
	                     CONSTRAINT [PK_sys_permission] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]
	
	                    ALTER TABLE [dbo].[sys_permission]  WITH CHECK ADD  CONSTRAINT [FK_sys_permission_sys_sub_module] FOREIGN KEY([submodule_id])
	                    REFERENCES [dbo].[sys_sub_module] ([id])
	
	                    ALTER TABLE [dbo].[sys_permission] CHECK CONSTRAINT [FK_sys_permission_sys_sub_module]
			

                        SET IDENTITY_INSERT [sys_permission] ON
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (1, 2, 'change','Alterar configurações gerais')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (2, 3, 'new','Criar novo')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (3, 3, 'get','Visualizar')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (4, 3, 'list','Listar todos')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (5, 3, 'search','Buscar todos')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6, 3, 'change','Alterar dados')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (7, 3, 'delete','Excluir')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (8, 4, 'new','Criar novo')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (9, 4, 'get','Visualizar')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (10, 4, 'list','Listar todos')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11, 4, 'search','Buscar todos')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (12, 4, 'permissions','Listar permissões')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (13, 4, 'permissionstree','Listar arvore de permissões')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (14, 4, 'users','Listar usuários')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (15, 4, 'change','Alterar dados')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16, 4, 'delete','Excluir')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (17, 4, 'changepermissions','Alterar permissões')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (18, 4, 'deleteuser','Excluir usuário')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (19, 4, 'deleteallusers','Excluir todos os usuário')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (20, 4, 'adduser','Inserir usuário')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (21, 5, 'get','Visualizar')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (22, 5, 'list','Listar todos')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (23, 5, 'search','Buscar todos')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (24, 5, 'deleteidentity','Excluir identidade')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (25, 5, 'unlockidentity','Desbloquear identidade')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (26, 5, 'resetpassword','Redefinir senha para o padrão do sistema')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (27, 5, 'changepassword','Alterar senha (digitando a nova senha)')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (28, 5, 'deploy','Iniciar publicação dos dados')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (29, 5, 'lock','Bloquear')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (30, 5, 'unlock','Desbloquear')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (31, 5, 'delete','Excluir')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (32, 5, 'logs','Visualizar logs')
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (33, 14, 'info','Informações de licenciamento');
                        SET IDENTITY_INSERT [sys_permission] OFF

                    END

	
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'sys_link_count'))
                    BEGIN

	                    CREATE TABLE [dbo].[sys_link_count](
		                    [module_id] [bigint] NOT NULL,
		                    [submodule_id] [bigint] NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [entity_id] [bigint] NOT NULL,
		                    [path] [varchar](5000) NOT NULL,
		                    [count] [bigint] NOT NULL
	                    ) ON [PRIMARY]


	                    ALTER TABLE [dbo].[sys_link_count] ADD  CONSTRAINT [DF_link_count_count]  DEFAULT ((0)) FOR [count]

	                    ALTER TABLE [dbo].[sys_link_count]  WITH CHECK ADD  CONSTRAINT [FK_sys_link_count_sys_module] FOREIGN KEY([module_id])
	                    REFERENCES [dbo].[sys_module] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[sys_link_count] CHECK CONSTRAINT [FK_sys_link_count_sys_module]
	
	                    ALTER TABLE [dbo].[sys_link_count]  WITH CHECK ADD  CONSTRAINT [FK_sys_link_count_sys_sub_module] FOREIGN KEY([submodule_id])
	                    REFERENCES [dbo].[sys_sub_module] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[sys_link_count] CHECK CONSTRAINT [FK_sys_link_count_sys_sub_module]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (23);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 23.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
