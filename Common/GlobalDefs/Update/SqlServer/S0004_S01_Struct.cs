using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0004_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_clone_resourceplugin')
                    BEGIN
                        DROP PROCEDURE sp_clone_resourceplugin;
                    END


                    EXEC ('CREATE PROCEDURE [dbo].[sp_clone_resourceplugin]
	                    @enterprise_id bigint,
	                    @resource_plugin_id bigint
                    AS
                    BEGIN
                    /**** Declara as variáveis ****/
                    DECLARE @create_date datetime
                    DECLARE @new_id bigint

                    SET @create_date = GETDATE()

                    /**** Cria o novo registro ****/
                    INSERT INTO [resource_plugin]
                                ([resource_id]
                                ,[plugin_id]
                                ,[permit_add_entity]
                                ,[enabled]
                                ,[mail_domain]
                                ,[build_login]
                                ,[build_mail]
                                ,[enable_import]
                                ,[enable_deploy]
                                ,[order]
                                ,[name_field_id]
                                ,[mail_field_id]
                                ,[login_field_id]
                                ,[deploy_after_login]
                                ,[password_after_login]
                                ,[deploy_process]
                                ,[deploy_all]
                                ,[deploy_password_hash]
                                ,[create_date]
                                ,[import_groups]
                                ,[import_containers])
                    SELECT	rp.resource_id
                                ,rp.plugin_id
                                ,rp.permit_add_entity
                                ,0
                                ,rp.mail_domain
                                ,rp.build_login
                                ,rp.build_mail
                                ,rp.enable_import
                                ,rp.enable_deploy
                                ,rp.[order]
                                ,rp.name_field_id
                                ,rp.mail_field_id
                                ,rp.login_field_id
                                ,rp.deploy_after_login
                                ,rp.password_after_login
                                ,rp.deploy_process
                                ,rp.deploy_all
                                ,rp.deploy_password_hash
                                ,@create_date
                                ,rp.import_groups
                                ,rp.import_containers
                    FROM	resource_plugin rp with(nolock)
                    INNER	JOIN resource r with(nolock) on rp.resource_id = r.id
                    INNER	JOIN context c with(nolock) on r.context_id = c.id
	                    WHERE	rp.id			= @resource_plugin_id
		                    AND	c.enterprise_id	= @enterprise_id
		
                    /**** Seleciona o novo registro ****/
                    SELECT @new_id = rp.id
                    FROM	resource_plugin rp with(nolock)
                    INNER	JOIN resource_plugin rpo with(nolock)
	                    ON	rp.create_date			= @create_date
	                    AND	rp.resource_id			= rpo.resource_id
	                    AND	rp.plugin_id			= rpo.plugin_id

                    /**** Copia os registros da tabela resource_plugin_ignore_filter ****/
                    INSERT INTO resource_plugin_ignore_filter (resource_plugin_id, filter_id)
                    SELECT DISTINCT @new_id
                            ,filter_id
                        FROM resource_plugin_ignore_filter
	                    WHERE	resource_plugin_id = @resource_plugin_id


                    /**** Copia os registros da tabela resource_plugin_lock_filter ****/
                    INSERT INTO resource_plugin_lock_filter (resource_plugin_id, filter_id)
                    SELECT DISTINCT @new_id
                            ,filter_id
                        FROM resource_plugin_lock_filter
	                    WHERE	resource_plugin_id = @resource_plugin_id


                    /**** Copia os registros da tabela resource_plugin_mapping ****/
                    INSERT INTO resource_plugin_mapping (
	                        resource_plugin_id
                            ,field_id
                            ,data_name
                            ,is_id
                            ,is_password
                            ,is_property
                            ,is_unique_property)
                    SELECT DISTINCT @new_id
                            ,field_id
                            ,data_name
                            ,is_id
                            ,is_password
                            ,is_property
                            ,is_unique_property
                        FROM resource_plugin_mapping
	                    WHERE	resource_plugin_id = @resource_plugin_id


                    /**** Copia os registros da tabela resource_plugin_par ****/
                    INSERT INTO resource_plugin_par (
	                        resource_plugin_id
                            ,[key]
                            ,value)
                    SELECT DISTINCT @new_id
                            ,[key]
                            ,value
                        FROM resource_plugin_par
	                    WHERE	resource_plugin_id = @resource_plugin_id

                    /**** Copia os registros da tabela resource_plugin_role ****/
                    INSERT INTO resource_plugin_role (
                            resource_plugin_id
                            ,role_id)
                    SELECT DISTINCT @new_id
                            ,role_id
                        FROM resource_plugin_role
	                    WHERE	resource_plugin_id = @resource_plugin_id


                    /**** Copia os registros da tabela resource_plugin_role_action ****/
                    INSERT INTO resource_plugin_role_action (
                            resource_plugin_id
                            ,role_id
                            ,action_key
                            ,action_add_value
                            ,action_del_value
                            ,additional_data)
                    SELECT DISTINCT @new_id
                            ,role_id
                            ,action_key
                            ,action_add_value
                            ,action_del_value
                            ,additional_data
                        FROM resource_plugin_role_action
	                    WHERE	resource_plugin_id = @resource_plugin_id


                    /**** Copia os registros da tabela resource_plugin_role_filter ****/
                    INSERT INTO resource_plugin_role_filter (
                            resource_plugin_id
                            ,role_id
                            ,filter_id)
                    SELECT DISTINCT @new_id
                            ,role_id
                            ,filter_id
                        FROM resource_plugin_role_filter
	                    WHERE	resource_plugin_id = @resource_plugin_id


                    /**** Copia os registros da tabela resource_plugin_role_time_acl ****/
                    INSERT INTO resource_plugin_role_time_acl ( 
                            resource_plugin_id
                            ,role_id
                            ,time_acl)
                    SELECT DISTINCT @new_id
                            ,role_id
                            ,time_acl
                        FROM resource_plugin_role_time_acl
	                    WHERE	resource_plugin_id = @resource_plugin_id


                    /**** Copia os registros da tabela resource_plugin_schedule ****/
                    INSERT INTO resource_plugin_schedule (
	                        resource_plugin_id
                            ,schedule
                            ,next)
                    SELECT DISTINCT @new_id
                            ,schedule
                            ,next
                        FROM resource_plugin_schedule
	                    WHERE	resource_plugin_id = @resource_plugin_id


                    /**** Retorna as informações do resource x plugin clonado  ****/
                    SELECT	*
                    FROM	resource_plugin rp with(nolock)
	                    WHERE	id = @new_id

                    END');

                    INSERT INTO [db_ver] ([version]) VALUES (4);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 4.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
