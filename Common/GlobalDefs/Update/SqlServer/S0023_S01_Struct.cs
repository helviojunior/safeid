using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0023_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'entity_keys'))
                    BEGIN

                        CREATE TABLE [dbo].[entity_keys](
	                        [context_id] [bigint] NOT NULL,
                            [entity_id] [bigint] NOT NULL,
	                        [identity_id] [bigint] NULL,
	                        [resource_plugin_id] [bigint] NOT NULL,
	                        [field_id] [bigint] NOT NULL,
	                        [value] [varchar](800) NOT NULL,
	                        [src] [varchar](80) NOT NULL
                        )

                        EXEC('ALTER TABLE dbo.entity_keys ADD CONSTRAINT FK_entity_keys_context FOREIGN KEY(context_id) REFERENCES dbo.context(id) ON UPDATE  CASCADE ON DELETE  CASCADE ');
                        EXEC('ALTER TABLE dbo.entity_keys ADD CONSTRAINT FK_entity_keys_entity FOREIGN KEY(entity_id) REFERENCES dbo.entity(id) ON UPDATE  CASCADE ON DELETE  CASCADE ');
                        EXEC('ALTER TABLE dbo.entity_keys ADD CONSTRAINT FK_entity_keys_field FOREIGN KEY (field_id) REFERENCES dbo.field(id) ON UPDATE  CASCADE ON DELETE  CASCADE ');
                        EXEC('ALTER TABLE dbo.entity_keys ADD CONSTRAINT FK_entity_keys_resource_plugin FOREIGN KEY (resource_plugin_id) REFERENCES dbo.resource_plugin(id) ON UPDATE  CASCADE ON DELETE  CASCADE ');
                        EXEC('ALTER TABLE dbo.entity_keys ADD CONSTRAINT FK_entity_keys_identity FOREIGN KEY(identity_id) REFERENCES dbo.[identity](id)');
                        EXEC('CREATE NONCLUSTERED INDEX [IX_entity_keys1] ON [dbo].[entity_keys] ([field_id],[value])');
                    END

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_rebuild_entity_keys')
                    BEGIN
                        DROP PROCEDURE sp_rebuild_entity_keys;
                    END


					EXEC ('CREATE PROCEDURE sp_rebuild_entity_keys
					as
                    --EXCLUI TODOS OS DADOS TA TABELA
					delete from entity_keys

					--Insere na tabela todos os Logins
					insert into entity_keys
					select 
							e.context_id,
							e.id, 
							i.id,
							i.resource_plugin_id,
							rp.login_field_id, 
							e.login,
							''k1 login''
					from	entity e with(nolock)
						inner	join resource r with(nolock)
							on	r.context_id = e.context_id
						inner	join resource_plugin rp with(nolock)
							on	r.id = rp.resource_id
						inner	join [identity] i  with(nolock)
							on e.id = i.entity_id
						where	not exists (select	1 
											from	entity_keys ei  with(nolock)
											where	ei.entity_id = e.id
												and ei.identity_id = i.id
												and ei.field_id = rp.login_field_id
												and ei.value = e.login)
						group by 
								e.context_id,
								e.id, 
								i.id,
								i.resource_plugin_id,
								rp.login_field_id, 
								e.login

					--Insere na tabela todos os valores que são chave da tabela identity_field
					insert into entity_keys
					select 
							e.context_id,
							e.id, 
							i.id,
							i.resource_plugin_id,
							ife.field_id, 
							ife.value,
                            ''k1 identity field''
					from	entity e with(nolock)
						inner	join [identity] i with(nolock)
							on e.id = i.entity_id
						inner	join resource_plugin rp  with(nolock)
							on i.resource_plugin_id = rp.id
						inner	join resource_plugin_mapping rpm   with(nolock)
							on	rp.id = rpm.resource_plugin_id
							and (rpm.is_id = 1 or rpm.is_unique_property = 1)
						inner	join [identity_field] ife  with(nolock)
							on	ife.identity_id = i.id
							and	ife.field_id = rpm.field_id	
						where	not exists (select	1 
											from	entity_keys ei  with(nolock)
											where	ei.entity_id = e.id
												and ei.identity_id = i.id
												and ei.field_id = ife.field_id
												and ei.value = ife.value)
                            and rp.permit_add_entity = 1
						group by 
								e.context_id,
								e.id, 
								i.id,
								i.resource_plugin_id,
								ife.field_id, 
								ife.value 

					--Insere na tabela todos os valores que são chave da tabela entity_field
					insert into entity_keys
					select 
							e.context_id,
							e.id, 
							NULL,
							rp.id, 
							efe.field_id, 
							efe.value,
							''k1 entity field''
					from	entity e with(nolock)
						inner join resource r   with(nolock)
							on r.context_id = e.context_id
						inner	join resource_plugin rp  with(nolock)
							on r.id = rp.resource_id
						inner	join resource_plugin_mapping rpm   with(nolock)
							on	rp.id = rpm.resource_plugin_id
							and (rpm.is_id = 1 or rpm.is_unique_property = 1)
						inner	join [entity_field] efe  with(nolock)
							on	efe.field_id = rpm.field_id
							and	efe.entity_id = e.id
						where	not exists (select	1 
											from	entity_keys ei  with(nolock)
											where	ei.entity_id = e.id
												and ei.identity_id is NULL
                                                and ei.resource_plugin_id = rp.id
												and ei.field_id = efe.field_id
												and ei.value = efe.value)
						group by 
								e.context_id,
								e.id,
                                rp.id, 
								efe.field_id, 
								efe.value 
                           

                        DBCC DBREINDEX (""dbo.entity_keys"", "" "", 70)
					');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_rebuild_entity_keys2')
                    BEGIN
                        DROP PROCEDURE sp_rebuild_entity_keys2;
                    END


					EXEC ('CREATE PROCEDURE sp_rebuild_entity_keys2
							@entity_id bigint
						as

						--Insere na tabela todos os Logins
						insert into entity_keys
						select 
								e.context_id,
								e.id, 
								i.id,
								i.resource_plugin_id,
								rp.login_field_id, 
								e.login,
                                ''k2 login''
						from	entity e with(nolock)
							inner	join resource r with(nolock)
								on	r.context_id = e.context_id
							inner	join resource_plugin rp with(nolock)
								on	r.id = rp.resource_id
							inner	join [identity] i  with(nolock)
								on e.id = i.entity_id
							where	not exists (select	1 
												from	entity_keys ei  with(nolock)
												where	ei.entity_id = e.id
													and ei.identity_id = i.id
													and ei.field_id = rp.login_field_id
													and ei.value = e.login)
									and e.id = @entity_id
							group by 
									e.context_id,
									e.id, 
									i.id,
									i.resource_plugin_id,
									rp.login_field_id, 
									e.login

						--Insere na tabela todos os valores que são chave da tabela identity_field
						insert into entity_keys
						select 
								e.context_id,
								e.id, 
								i.id,
								i.resource_plugin_id,
								ife.field_id, 
								ife.value,
                                ''k2 identity field''
						from	entity e with(nolock)
							inner	join [identity] i with(nolock)
								on e.id = i.entity_id
							inner	join resource_plugin rp  with(nolock)
								on i.resource_plugin_id = rp.id
							inner	join resource_plugin_mapping rpm   with(nolock)
								on	rp.id = rpm.resource_plugin_id
								and (rpm.is_id = 1 or rpm.is_unique_property = 1)
							inner	join [identity_field] ife  with(nolock)
								on	ife.identity_id = i.id
								and	ife.field_id = rpm.field_id	
							where	not exists (select	1 
												from	entity_keys ei  with(nolock)
												where	ei.entity_id = e.id
													and ei.identity_id = i.id
													and ei.field_id = ife.field_id
													and ei.value = ife.value)
									and e.id = @entity_id
                                    and rp.permit_add_entity = 1
							group by 
									e.context_id,
									e.id, 
									i.id,
									i.resource_plugin_id,
									ife.field_id, 
									ife.value 

						--Insere na tabela todos os valores que são chave da tabela entity_field
						insert into entity_keys
						select 
							    e.context_id,
							    e.id, 
							    NULL,
							    rp.id, 
							    efe.field_id, 
							    efe.value,
							    ''k2 entity field''
					    from	entity e with(nolock)
						    inner join resource r   with(nolock)
							    on r.context_id = e.context_id
						    inner	join resource_plugin rp  with(nolock)
							    on r.id = rp.resource_id
						    inner	join resource_plugin_mapping rpm   with(nolock)
							    on	rp.id = rpm.resource_plugin_id
							    and (rpm.is_id = 1 or rpm.is_unique_property = 1)
						    inner	join [entity_field] efe  with(nolock)
							    on	efe.field_id = rpm.field_id
							    and	efe.entity_id = e.id
						    where	not exists (select	1 
											    from	entity_keys ei  with(nolock)
											    where	ei.entity_id = e.id
												    and ei.identity_id is NULL
                                                    and ei.resource_plugin_id = rp.id
												    and ei.field_id = efe.field_id
												    and ei.value = efe.value)
							    and e.id = @entity_id
						    group by 
								    e.context_id,
								    e.id,
                                    rp.id,
								    efe.field_id, 
								    efe.value 
                           ');

                    INSERT INTO [db_ver] ([version]) VALUES (23);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 23; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
