using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0013_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_message_template'))
                    BEGIN
                        
                        CREATE TABLE [dbo].[st_message_template](
	                        [id] [bigint] IDENTITY(1,1)  NOT NULL,
	                        [enterprise_id] [bigint] NOT NULL,
	                        [key] [varchar](50) NOT NULL,
	                        [html] [bit] NOT NULL,
	                        [subject] [varchar](300) NOT NULL,
	                        [body] [varchar](max) NOT NULL,
                         CONSTRAINT [PK_st_message_template] PRIMARY KEY CLUSTERED 
                        (
	                        [id] ASC
                        )
                        );

                        ALTER TABLE [dbo].[st_message_template] ADD  CONSTRAINT [DF_st_message_template_enterprise_id]  DEFAULT ((0)) FOR [enterprise_id];

                        ALTER TABLE [dbo].[st_message_template] ADD  CONSTRAINT [DF_st_message_template_html]  DEFAULT ((1)) FOR [html];

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_messages'))
                    BEGIN
                        CREATE TABLE [dbo].[st_messages](
	                        [id] [bigint] IDENTITY(1,1) NOT NULL,
                            [enterprise_id] [bigint] NOT NULL,
	                        [key] [varchar](100) NOT NULL,
	                        [date] [datetime] NOT NULL,
	                        [status] [varchar](5) NULL,
                            [to] [varchar](500) NULL,
                            [html] [bit] NOT NULL,
	                        [subject] [varchar](300) NOT NULL,
                            [body] [varchar](max) NOT NULL
                         CONSTRAINT [PK_st_messages] PRIMARY KEY CLUSTERED 
                        (
	                        [id] ASC
                        )
                        );

                        ALTER TABLE [dbo].[st_messages]  WITH CHECK ADD  CONSTRAINT [FK_st_messages_enterprise] FOREIGN KEY([enterprise_id]) REFERENCES [dbo].[enterprise] ([id]);

                        ALTER TABLE [dbo].[st_messages] ADD  CONSTRAINT [DF_st_messages_date]  DEFAULT (getdate()) FOR [date];

                        ALTER TABLE [dbo].[st_messages] ADD  CONSTRAINT [DF_st_messages_status]  DEFAULT ('W') FOR [status];
                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_messages_status'))
                    BEGIN
                        CREATE TABLE [dbo].[st_messages_status](
	                        [message_id] [bigint] NOT NULL,
	                        [date] [datetime] NOT NULL,
	                        [error] [bit] NOT NULL,
	                        [status] [varchar](300) NOT NULL,
	                        [description] [varchar](max) NOT NULL,
                         CONSTRAINT [PK_st_messages_status] PRIMARY KEY CLUSTERED 
                        (
	                        [message_id] ASC,
	                        [date] ASC
                        )
                        );

                        ALTER TABLE [dbo].[st_messages_status]  WITH CHECK ADD  CONSTRAINT [FK_st_messages_status_st_messages] FOREIGN KEY([message_id]) REFERENCES [dbo].[st_messages] ([id]);

                        ALTER TABLE [dbo].[st_messages_status] CHECK CONSTRAINT [FK_st_messages_status_st_messages];
                        
                        ALTER TABLE [dbo].[st_messages_status] ADD  CONSTRAINT [DF_st_messages_status_date]  DEFAULT (getdate()) FOR [date];

                        ALTER TABLE [dbo].[st_messages_status] ADD  CONSTRAINT [DF_st_messages_status_error]  DEFAULT ((0)) FOR [error];
                    END


                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_messages_views'))
                    BEGIN
                        CREATE TABLE [dbo].[st_messages_views](
	                        [message_id] [bigint] NOT NULL,
	                        [date] [datetime] NOT NULL,
	                        [ip_addr] [varchar](100) NULL,
	                        [user_agent] [varchar](max) NULL
                        );

                        ALTER TABLE [dbo].[st_messages_views]  WITH CHECK ADD  CONSTRAINT [FK_st_messages_views_st_messages] FOREIGN KEY([message_id]) REFERENCES [dbo].[st_messages] ([id]);

                        ALTER TABLE [dbo].[st_messages_views] CHECK CONSTRAINT [FK_st_messages_views_st_messages];

                        ALTER TABLE [dbo].[st_messages_views] ADD  CONSTRAINT [DF_st_messages_views_date]  DEFAULT (getdate()) FOR [date];
                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_messages_links'))
                    BEGIN
                        CREATE TABLE [dbo].[st_messages_links](
							[id] [bigint] IDENTITY(1,1) NOT NULL,
	                        [message_id] [bigint] NOT NULL,
							[key] [varchar](100) NOT NULL,
	                        [link] [varchar](max) NULL,
                         CONSTRAINT [PK_st_messages_links] PRIMARY KEY CLUSTERED 
                        (
	                        [id] ASC
                        )
                        );

                        ALTER TABLE [dbo].[st_messages_links]  WITH CHECK ADD  CONSTRAINT [FK_st_messages_links_st_messages] FOREIGN KEY([message_id]) REFERENCES [dbo].[st_messages] ([id]);

                        ALTER TABLE [dbo].[st_messages_links] CHECK CONSTRAINT [FK_st_messages_links_st_messages];

                    END

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'st_messages_links_click'))
                    BEGIN
                        CREATE TABLE [dbo].[st_messages_links_click](
	                        [messages_links_id] [bigint] NOT NULL,
	                        [date] [datetime] NOT NULL,
	                        [ip_addr] [varchar](100) NULL,
	                        [user_agent] [varchar](max) NULL
                        );

                        ALTER TABLE [dbo].[st_messages_links_click]  WITH CHECK ADD  CONSTRAINT [FK_st_messages_links_click_st_messages] FOREIGN KEY([messages_links_id]) REFERENCES [dbo].[st_messages_links] ([id]);

                        ALTER TABLE [dbo].[st_messages_links_click] CHECK CONSTRAINT [FK_st_messages_links_click_st_messages];

                        ALTER TABLE [dbo].[st_messages_links_click] ADD  CONSTRAINT [DF_st_messages_links_click_date]  DEFAULT (getdate()) FOR [date];
                    END



                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_st_get_message_template')
                    BEGIN
                        DROP PROCEDURE sp_st_get_message_template;
                    END


                    EXEC ('
                       
                        CREATE procedure [dbo].[sp_st_get_message_template]
                            @enterprise_id bigint,
                            @message_key varchar(50)
                        as

                        declare @MESSAGE_ID bigint = 0;

                        /*** Tenta localizar a mensagem mais específica, ou seja alterada pelo administrador da empresa ***/
                        SELECT TOP 1	@MESSAGE_ID = id 
                            FROM		st_message_template t with(nolock) 
                                WHERE	t.enterprise_id = @enterprise_id
                                    AND	t.[key] = @message_key


                        /*** Localiza a mensagem padrão do sistema ***/
                        IF (@MESSAGE_ID = 0)
                        BEGIN
                            SELECT TOP 1	@MESSAGE_ID = id 
                            FROM		st_message_template t with(nolock) 
                                WHERE	t.enterprise_id = 0
                                    AND	t.[key] = @message_key
                        END	

                        /*** Caso não localizado retorna erro ***/
                        IF (@MESSAGE_ID = 0)
                        BEGIN
                         RAISERROR (''Not found message template'', -- Message text.
                                       16, -- Severity.
                                       1 -- State.
                                       );
                        END

                        /*** Retorna o template ***/           
                        SELECT t.*, e.last_uri
	                        FROM	st_message_template t with(nolock)
	                        CROSS	JOIN enterprise e with(nolock)
	                        WHERE	t.id = @MESSAGE_ID

                    ');


					
                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_st_new_message')
                    BEGIN
                        DROP PROCEDURE sp_st_new_message;
                    END


                    EXEC ('
                       
                        CREATE procedure [dbo].[sp_st_new_message]
                            @enterprise_id bigint,
                            @send_to varchar(300),
                            @is_html bit,
                            @subject varchar(300),
                            @body varchar(max)
                        as

                        /*** Gera a chave de identificação da mensagem ***/
                        DECLARE @KEY VARCHAR(100)
                        SET @KEY = REPLACE(CONVERT(VARCHAR(10), GETDATE(), 120),''-'','''') + ''-'' + CAST(NEWID() AS VARCHAR(50))

                        /*** Insere a mensagem ***/
                        INSERT INTO [st_messages]
                                   ([enterprise_id]
                                   ,[key]
                                   ,[date]
                                   ,[status]
                                   ,[to]
                                   ,[html]
                                   ,[subject]
                                   ,[body])
							VALUES 
									(@enterprise_id
									,@KEY
									,GETDATE()
									,''W''
									,@send_to
									,@is_html
									,replace(@subject,''|message_key|'',@KEY)
									,replace(@body,''|message_key|'',@KEY))
									
                        /*** Retorna a mensagem ***/           
                        SELECT *
	                        FROM	[st_messages]
	                        WHERE	[KEY] = @KEY

                    ');

                    

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_st_new_message_link')
                    BEGIN
                        DROP PROCEDURE sp_st_new_message_link;
                    END


                    EXEC ('
                       
                        CREATE procedure [dbo].[sp_st_new_message_link]
                            @message_id bigint,
                            @link varchar(max)
                        as


                        /*** Tenta localizar a mensagem mais específica, ou seja alterada pelo administrador da empresa ***/

                        /*** Caso não localizado retorna erro ***/
                        IF ( NOT EXISTS (SELECT 1
                            FROM		st_messages t with(nolock) 
                                WHERE	t.id = @message_id
	                        ))
	                        BEGIN
	                         RAISERROR (''Message not found'', -- Message text.
				                           16, -- Severity.
				                           1 -- State.
				                           );
	                        END

                        /*** Gera a chave de identificação do link ***/
                        DECLARE @KEY VARCHAR(100)
                        SET @KEY = REPLACE(CONVERT(VARCHAR(10), GETDATE(), 120),''-'','''') + ''-'' + CAST(NEWID() AS VARCHAR(50)) + ''-'' + cast(@message_id as varchar(50))

                        /*** Insere o link ***/
                        INSERT INTO [st_messages_links]
                                   ([message_id]
                                   ,[key]
                                   ,[link])
	                        VALUES (
                            @message_id
                            ,@KEY
                            ,@link)

                        /*** Retorna o link ***/           
                        SELECT TOP 1 [KEY]
                            FROM	[st_messages_links]
                            WHERE	[KEY] = @KEY
                        
                        
                    ');


                    INSERT INTO [db_ver] ([version]) VALUES (13);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 13.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
