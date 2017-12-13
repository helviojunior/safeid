using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0015_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_workflow'))
                    BEGIN

	                    CREATE TABLE [dbo].[st_workflow](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [context_id] [bigint] NOT NULL,
		                    [name] [varchar](300) NOT NULL,
		                    [description] [varchar](max) NOT NULL,
		                    [owner_id] [bigint] NOT NULL,
		                    [type] [varchar](20) NOT NULL,
		                    [create_date] [datetime] NOT NULL,
		                    [change_date] [datetime] NOT NULL,
		                    [enabled] [bit] NOT NULL,
		                    [deleted] [bit] NOT NULL,
		                    [deprecated] [bit] NOT NULL,
		                    [original_id] [bigint] NOT NULL,
		                    [version] [bigint] NOT NULL,
	                        CONSTRAINT [PK_st_workflow] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )
	                    );

	                    ALTER TABLE [dbo].[st_workflow]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_context] FOREIGN KEY([context_id])REFERENCES [dbo].[context] ([id]);

	                    ALTER TABLE [dbo].[st_workflow] CHECK CONSTRAINT [FK_st_workflow_context];
	
	                    ALTER TABLE [dbo].[st_workflow]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_entity] FOREIGN KEY([owner_id]) REFERENCES [dbo].[entity] ([id]);
	
	                    ALTER TABLE [dbo].[st_workflow] CHECK CONSTRAINT [FK_st_workflow_entity];
	
	                    ALTER TABLE [dbo].[st_workflow] ADD  CONSTRAINT [DF_st_workflow_create_date]  DEFAULT (getdate()) FOR [create_date];

	                    ALTER TABLE [dbo].[st_workflow] ADD  CONSTRAINT [DF_st_workflow_change_date]  DEFAULT (getdate()) FOR [change_date];

	                    ALTER TABLE [dbo].[st_workflow] ADD  CONSTRAINT [DF_st_workflow_enabled]  DEFAULT ((1)) FOR [enabled];

	                    ALTER TABLE [dbo].[st_workflow] ADD  CONSTRAINT [DF_st_workflow_deleted]  DEFAULT ((0)) FOR [deleted];

	                    ALTER TABLE [dbo].[st_workflow] ADD  CONSTRAINT [DF_st_workflow_deprecated]  DEFAULT ((0)) FOR [deprecated];

	                    ALTER TABLE [dbo].[st_workflow] ADD  CONSTRAINT [DF_st_workflow_original_id]  DEFAULT ((0)) FOR [original_id];

	                    ALTER TABLE [dbo].[st_workflow] ADD  CONSTRAINT [DF_st_workflow_version]  DEFAULT ((1)) FOR [version];

	                    EXEC ('

		                    CREATE TRIGGER [dbo].[st_WorkflowUpdate] ON [dbo].[st_workflow]
		                        INSTEAD OF UPDATE
		                    AS 

			                    DECLARE @id bigint

			                    -- Cursor para percorrer os nomes dos objetos 
			                    DECLARE cursor_inserted CURSOR FOR
				                    SELECT id FROM inserted

			                    -- Abrindo Cursor para leitura
			                    OPEN cursor_inserted

			                    -- Lendo a próxima linha
			                    FETCH NEXT FROM cursor_inserted INTO @id

			                    -- Percorrendo linhas do cursor (enquanto houverem)
			                    WHILE @@FETCH_STATUS = 0
			                    BEGIN

			    
					                    /*** Verifica se o workflow ja foi utilizado ***/
					                    IF NOT (EXISTS (SELECT 1 FROM [st_workflow_request] WHERE workflow_id = @id))
					                    BEGIN
						                    /*** O workflow nunca foi usado, pode ser atualizado ***/
							                    UPDATE [st_workflow]
							                        SET [context_id] = i.context_id
								                        ,[name] = i.name
								                        ,[description] = i.description
								                        ,[owner_id] = i.owner_id
								                        ,[type] = i.type
								                        ,[create_date] = i.create_date
								                        ,[change_date] = GETDATE()
								                        ,[enabled] = i.enabled
								                        ,[deleted] = i.deleted
								                        ,[deprecated] = i.deprecated
								                        ,[original_id] = i.original_id
								                        ,[version] = i.version
							                    FROM	inserted i
							                    WHERE	i.id = st_workflow.id
								                    AND i.id = @id

					                    END
					                    ELSE
					                    BEGIN

						                    /*** Atualiza o registro antigo como descontinuado ***/
						                    UPDATE	st_workflow
							                    SET	[deprecated] = 1
						                    WHERE	id = @id

						                    /*** Exclui todos os outros registros não utilizados (filhos deste mesmo workflow) ***/
						                    DELETE FROM [st_workflow]
							                    WHERE	id in	(
								                    SELECT		id 
									                    FROM	[st_workflow] w
									                    WHERE	original_id = @id
										                    AND	NOT (EXISTS (SELECT 1 FROM [st_workflow_request] w1 WHERE w1.workflow_id = w.id))
								                    )
						

						                    /*** Atualiza o registro antigo ***/
						                    INSERT INTO [st_workflow]
								                        ([context_id]
								                        ,[name]
								                        ,[description]
								                        ,[owner_id]
								                        ,[type]
								                        ,[create_date]
								                        ,[change_date]
								                        ,[enabled]
								                        ,[deleted]
								                        ,[deprecated]
								                        ,[original_id]
								                        ,[version])
							                    SELECT i.context_id
									                    ,i.name
									                    ,i.description
									                    ,i.owner_id
									                    ,i.type
									                    ,GETDATE()
									                    ,i.create_date
									                    ,i.enabled
									                    ,i.deleted
									                    ,0
									                    ,case when i.original_id > 0 then i.original_id else i.id end
									                    ,(i.[version] + 1)
							                    FROM	inserted i
							                    WHERE	i.id = @id
					                    END
				    

				                    -- Lendo a próxima linha
				                    FETCH NEXT FROM cursor_inserted INTO @id
			                    END

			                    -- Fechando Cursor para leitura
			                    CLOSE cursor_inserted

			                    -- Desalocando o cursor
			                    DEALLOCATE cursor_inserted 

	                    ');

                    END

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_st_new_workflow')
                    BEGIN
                        DROP PROCEDURE sp_st_new_workflow;
                    END

                    EXEC ('
   
	                    CREATE procedure [dbo].[sp_st_new_workflow]
		                    @context_id bigint,
		                    @name varchar(300),
		                    @description varchar(50),
		                    @owner bigint,
		                    @type varchar(20),
		                    @enabled bit
	                    as

	                    DECLARE @DATE DATETIME;
	                    SET @DATE = GETDATE()


	                    /*** Caso não localizado retorna erro ***/
	                    IF NOT (EXISTS (SELECT 1 FROM entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id WHERE c.id = @context_id and e.id = @owner))
	                    BEGIN
	                        RAISERROR (''Owner entity not exists or not a chield of this enterprise'', -- Message text.
				                        16, -- Severity.
				                        1 -- State.
				                        );
	                    END


	                    INSERT INTO [st_workflow]
			                        ([context_id]
			                        ,[name]
			                        ,[description]
			                        ,[owner_id]
			                        ,[type]
			                        ,[create_date]
			                        ,[enabled])
		                        VALUES
				                    (@context_id
				                    ,@name
				                    ,@description
				                    ,@owner
				                    ,@type
				                    ,@DATE
				                    ,@enabled)

	                    SELECT	w.*
	                    FROM		[st_workflow] w
	                    WHERE	w.context_id	= @context_id
		                    AND	w.name			= @name
		                    AND	w.owner_id		= @owner
		                    AND	w.[type]		= @type
		                    AND	w.create_date	= @DATE
                    ');

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_workflow_access_entity'))
                    BEGIN
	
	                    CREATE TABLE [dbo].[st_workflow_access_entity](
		                    [workflow_id] [bigint] NOT NULL,
		                    [entity_id] [bigint] NOT NULL,
	                        CONSTRAINT [PK_st_workflow_access_entity] PRIMARY KEY CLUSTERED 
	                    (
		                    [workflow_id] ASC,
		                    [entity_id] ASC
	                    )
	                    );

	                    ALTER TABLE [dbo].[st_workflow_access_entity]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_access_entity_role] FOREIGN KEY([entity_id]) REFERENCES [dbo].[entity] ([id]);

	                    ALTER TABLE [dbo].[st_workflow_access_entity] CHECK CONSTRAINT [FK_st_workflow_access_entity_role];

	                    ALTER TABLE [dbo].[st_workflow_access_entity]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_access_entity_st_workflow] FOREIGN KEY([workflow_id]) REFERENCES [dbo].[st_workflow] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;

	                    ALTER TABLE [dbo].[st_workflow_access_entity] CHECK CONSTRAINT [FK_st_workflow_access_entity_st_workflow];

                    END


                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_workflow_access_role'))
                    BEGIN
	
	                    CREATE TABLE [dbo].[st_workflow_access_role](
		                    [workflow_id] [bigint] NOT NULL,
		                    [role_id] [bigint] NOT NULL,
	                        CONSTRAINT [PK_st_workflow_access_role] PRIMARY KEY CLUSTERED 
	                    (
		                    [workflow_id] ASC,
		                    [role_id] ASC
	                    )
	                    );

	                    ALTER TABLE [dbo].[st_workflow_access_role]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_access_role_role] FOREIGN KEY([role_id]) REFERENCES [dbo].[role] ([id]);

	                    ALTER TABLE [dbo].[st_workflow_access_role] CHECK CONSTRAINT [FK_st_workflow_access_role_role];

	                    ALTER TABLE [dbo].[st_workflow_access_role]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_access_role_st_workflow] FOREIGN KEY([workflow_id]) REFERENCES [dbo].[st_workflow] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;

	                    ALTER TABLE [dbo].[st_workflow_access_role] CHECK CONSTRAINT [FK_st_workflow_access_role_st_workflow];
		
                    END


                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_workflow_activity'))
                    BEGIN

	                    CREATE TABLE [dbo].[st_workflow_activity](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [workflow_id] [bigint] NOT NULL,
		                    [name] [varchar](100) NOT NULL,
		                    [escalation_days] [int] NOT NULL,
		                    [expiration_days] [int] NOT NULL,
		                    [auto_deny] [bigint] NULL,
		                    [auto_approval] [bigint] NULL,
		                    [execution_order] [int] NOT NULL,
	                        CONSTRAINT [PK_st_workflow_activity] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )
	                    );

	                    ALTER TABLE [dbo].[st_workflow_activity]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_activity_filters] FOREIGN KEY([auto_deny]) REFERENCES [dbo].[filters] ([id]);

	                    ALTER TABLE [dbo].[st_workflow_activity] CHECK CONSTRAINT [FK_st_workflow_activity_filters];

	                    ALTER TABLE [dbo].[st_workflow_activity]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_activity_filters1] FOREIGN KEY([auto_approval]) REFERENCES [dbo].[filters] ([id]);

	                    ALTER TABLE [dbo].[st_workflow_activity] CHECK CONSTRAINT [FK_st_workflow_activity_filters1];

	                    ALTER TABLE [dbo].[st_workflow_activity]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_activity_st_workflow] FOREIGN KEY([workflow_id]) REFERENCES [dbo].[st_workflow] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;

	                    ALTER TABLE [dbo].[st_workflow_activity] CHECK CONSTRAINT [FK_st_workflow_activity_st_workflow];

	                    ALTER TABLE [dbo].[st_workflow_activity] ADD  CONSTRAINT [DF_st_workflow_activity_execution_order]  DEFAULT ((0)) FOR [execution_order];
		
                    END


                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_workflow_activity_manual_approval'))
                    BEGIN

	                    CREATE TABLE [dbo].[st_workflow_activity_manual_approval](
		                    [workflow_activity_id] [bigint] NOT NULL,
		                    [entity_approver] [bigint] NULL,
		                    [role_approver] [bigint] NULL
	                    );

	                    ALTER TABLE [dbo].[st_workflow_activity_manual_approval]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_activity_manual_approval_entity] FOREIGN KEY([entity_approver]) REFERENCES [dbo].[entity] ([id]);

	                    ALTER TABLE [dbo].[st_workflow_activity_manual_approval] CHECK CONSTRAINT [FK_st_workflow_activity_manual_approval_entity];

	                    ALTER TABLE [dbo].[st_workflow_activity_manual_approval]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_activity_manual_approval_role] FOREIGN KEY([role_approver]) REFERENCES [dbo].[role] ([id]);

	                    ALTER TABLE [dbo].[st_workflow_activity_manual_approval] CHECK CONSTRAINT [FK_st_workflow_activity_manual_approval_role];

	                    ALTER TABLE [dbo].[st_workflow_activity_manual_approval]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_activity_manual_approval_st_workflow_activity] FOREIGN KEY([workflow_activity_id]) REFERENCES [dbo].[st_workflow_activity] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;

	                    ALTER TABLE [dbo].[st_workflow_activity_manual_approval] CHECK CONSTRAINT [FK_st_workflow_activity_manual_approval_st_workflow_activity];

                    END


                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_workflow_request'))
                    BEGIN

	                    CREATE TABLE [dbo].[st_workflow_request](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [entity_id] [bigint] NOT NULL,
		                    [workflow_id] [bigint] NOT NULL,
		                    [create_date] [datetime] NOT NULL,
		                    [status] [int] NOT NULL,
		                    [description] [varchar](max) NOT NULL,
		                    [start_date] [datetime] NULL,
		                    [end_date] [datetime] NULL,
		                    [deployed] [bit] NOT NULL,
	                        CONSTRAINT [PK_st_workflow_request] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )
	                    );

	                    ALTER TABLE [dbo].[st_workflow_request]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_request_entity] FOREIGN KEY([entity_id]) REFERENCES [dbo].[entity] ([id]);
	                    ALTER TABLE [dbo].[st_workflow_request] CHECK CONSTRAINT [FK_st_workflow_request_entity];
	                    ALTER TABLE [dbo].[st_workflow_request]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_request_st_workflow] FOREIGN KEY([workflow_id]) REFERENCES [dbo].[st_workflow] ([id]);
	                    ALTER TABLE [dbo].[st_workflow_request] CHECK CONSTRAINT [FK_st_workflow_request_st_workflow];
	                    ALTER TABLE [dbo].[st_workflow_request]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_request_st_workflow_request] FOREIGN KEY([id]) REFERENCES [dbo].[st_workflow_request] ([id]);
	                    ALTER TABLE [dbo].[st_workflow_request] CHECK CONSTRAINT [FK_st_workflow_request_st_workflow_request];
	                    ALTER TABLE [dbo].[st_workflow_request] ADD  CONSTRAINT [DF_st_workflow_request_create_date]  DEFAULT (getdate()) FOR [create_date];
	                    ALTER TABLE [dbo].[st_workflow_request] ADD  CONSTRAINT [DF_st_workflow_request_deployed]  DEFAULT ((0)) FOR [deployed];

                    END



                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_workflow_request_status'))
                    BEGIN

	                    CREATE TABLE [dbo].[st_workflow_request_status](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [workflow_request_id] [bigint] NOT NULL,
		                    [date] [datetime] NOT NULL,
		                    [status] [int] NOT NULL,
		                    [description] [varchar](max) NOT NULL,
		                    [executed_by_entity_id] [bigint] NOT NULL,
		                    [activity_id] [bigint] NOT NULL,
	                        CONSTRAINT [PK_st_workflow_request_status] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )
	                    );

	                    ALTER TABLE [dbo].[st_workflow_request_status]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_request_status_entity] FOREIGN KEY([executed_by_entity_id]) REFERENCES [dbo].[entity] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;
	                    ALTER TABLE [dbo].[st_workflow_request_status] CHECK CONSTRAINT [FK_st_workflow_request_status_entity];
	                    ALTER TABLE [dbo].[st_workflow_request_status]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_request_status_st_workflow_activity] FOREIGN KEY([activity_id]) REFERENCES [dbo].[st_workflow_activity] ([id]);
	                    ALTER TABLE [dbo].[st_workflow_request_status] CHECK CONSTRAINT [FK_st_workflow_request_status_st_workflow_activity];
	                    ALTER TABLE [dbo].[st_workflow_request_status]  WITH CHECK ADD  CONSTRAINT [FK_st_workflow_request_status_st_workflow_request] FOREIGN KEY([workflow_request_id]) REFERENCES [dbo].[st_workflow_request] ([id]) ON UPDATE CASCADE ON DELETE CASCADE;
	                    ALTER TABLE [dbo].[st_workflow_request_status] CHECK CONSTRAINT [FK_st_workflow_request_status_st_workflow_request];
	                    ALTER TABLE [dbo].[st_workflow_request_status] ADD  CONSTRAINT [DF_st_workflow_request_status_date]  DEFAULT (getdate()) FOR [date];
	                    ALTER TABLE [dbo].[st_workflow_request_status] ADD  CONSTRAINT [DF_st_workflow_request_status_status]  DEFAULT ((0)) FOR [status];

                    END

                    IF (NOT EXISTS (SELECT 1 FROM st_message_template WHERE [key] = 'access_request_admin'))
                    BEGIN
	                    INSERT INTO st_message_template (enterprise_id, [key], html, subject, body) VALUES (0, 'access_request_admin', 1, 'Nova requisição de acesso', '<html><body>workflow_name = %workflow_name%<br />user_name = %user_name%<br />user_login = %user_login%<br />user_id = %user_id%<br />admin_id = %admin_id%<br />approval_link = <a href=%approval_link%>Aprovar</a><br />deny_link = <a href=%deny_link%>Negar</a><br />description = %description%</body></html>');
                    END

                    IF (NOT EXISTS (SELECT 1 FROM st_message_template WHERE [key] = 'access_request'))
                    BEGIN
	                    INSERT INTO st_message_template (enterprise_id, [key], html, subject, body) VALUES (0, 'access_request', 1, 'Requisição de acesso', '<html><body><p>Olá, %user_name%,</p><p>Recebemos sua requisição de acesso e ela se encontra em processo de aprovação.</p><p>Você será informado(a) por e-mail sobre o andamento da requisição.</p><p><strong>Requisição realizada: </strong>%workflow_name%</p><p><strong>Passos de aprovação: </strong></p>%steps%</body></html>');
                    END

                    IF (NOT EXISTS (SELECT 1 FROM st_message_template WHERE [key] = 'access_request_changed'))
                    BEGIN
	                    INSERT INTO st_message_template (enterprise_id, [key], html, subject, body) VALUES (0, 'access_request_changed', 1, 'Requisição de acesso alterada', '<html><body><p>Olá, %user_name%,</p><p>Ocorreu uma alteração no status da sua requisição de acesso.</p><p><strong>Requisição realizada: </strong>%workflow_name%</p><p><strong>Alteração: </strong></p><p style=padding-left: 30px;>%change%</p></body></html>');
                    END


                    IF (select count(*) from sys_sub_module where id = 15) = 0
                    BEGIN
                        SET IDENTITY_INSERT sys_sub_module ON;
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(15, 2, 'workflow', 'workflow', 'Workflow');
                        SET IDENTITY_INSERT sys_sub_module OFF;

                        SET IDENTITY_INSERT [sys_permission] ON;
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (34, 5, 'accessrequest','Criar nova requisição de acesso');                        
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (35, 15, 'new','Criar novo');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (36, 15, 'change','Alterar dados');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (37, 15, 'list','Listar todos');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (38, 15, 'search','Buscar todos');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (39, 15, 'delete','Excluir');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (40, 15, 'get','Visualizar');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (41, 15, 'accessrequestlist','Listagem de requisições de acesso');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (42, 15, 'getaccessrequest','Visualizar requisição de acesso');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (43, 15, 'accessrequestallow','Aprovar requisição de acesso');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (44, 15, 'accessrequestrevoke','Revogar requisição de acesso');
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (45, 15, 'accessrequestdeny','Negar requisição de acesso');
                        SET IDENTITY_INSERT [sys_permission] OFF;
                    END


                    INSERT INTO [db_ver] ([version]) VALUES (15);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 15.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
