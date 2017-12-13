using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0019_S02_Data : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    	
                    
                    UPDATE [sys_sub_module] SET [api_module] = 'systemrole' WHERE [key] = 'system_roles';

                    SET IDENTITY_INSERT sys_permission ON;

                    DECLARE @tmp_table TABLE(
	                    [id] [bigint] NULL,
	                    [old_id] [bigint] NOT NULL,
	                    [submodule_id] [bigint] NOT NULL,
	                    [key] [varchar](50) NOT NULL,
	                    [name] [varchar](200) NOT NULL
	                    )

                    insert into @tmp_table ([old_id], [submodule_id], [key], [name])
                    select [id], [submodule_id], [key], [name] from sys_permission

                    update @tmp_table set id = 2001 WHERE submodule_id = 2 AND [key] = 'change'
                    update @tmp_table set id = 3001 WHERE submodule_id = 3 AND [key] = 'new'
                    update @tmp_table set id = 3002 WHERE submodule_id = 3 AND [key] = 'get'
                    update @tmp_table set id = 3003 WHERE submodule_id = 3 AND [key] = 'list'
                    update @tmp_table set id = 3004 WHERE submodule_id = 3 AND [key] = 'search'
                    update @tmp_table set id = 3005 WHERE submodule_id = 3 AND [key] = 'change'
                    update @tmp_table set id = 3006 WHERE submodule_id = 3 AND [key] = 'delete'
                    update @tmp_table set id = 4001 WHERE submodule_id = 4 AND [key] = 'new'
                    update @tmp_table set id = 4002 WHERE submodule_id = 4 AND [key] = 'get'
                    update @tmp_table set id = 4003 WHERE submodule_id = 4 AND [key] = 'list'
                    update @tmp_table set id = 4004 WHERE submodule_id = 4 AND [key] = 'search'
                    update @tmp_table set id = 4005 WHERE submodule_id = 4 AND [key] = 'permissions'
                    update @tmp_table set id = 4006 WHERE submodule_id = 4 AND [key] = 'permissionstree'
                    update @tmp_table set id = 4007 WHERE submodule_id = 4 AND [key] = 'users'
                    update @tmp_table set id = 4008 WHERE submodule_id = 4 AND [key] = 'change'
                    update @tmp_table set id = 4009 WHERE submodule_id = 4 AND [key] = 'delete'
                    update @tmp_table set id = 4010 WHERE submodule_id = 4 AND [key] = 'changepermissions'
                    update @tmp_table set id = 4011 WHERE submodule_id = 4 AND [key] = 'deleteuser'
                    update @tmp_table set id = 4012 WHERE submodule_id = 4 AND [key] = 'deleteallusers'
                    update @tmp_table set id = 4013 WHERE submodule_id = 4 AND [key] = 'adduser'
                    update @tmp_table set id = 5001 WHERE submodule_id = 5 AND [key] = 'get'
                    update @tmp_table set id = 5002 WHERE submodule_id = 5 AND [key] = 'list'
                    update @tmp_table set id = 5003 WHERE submodule_id = 5 AND [key] = 'search'
                    update @tmp_table set id = 5004 WHERE submodule_id = 5 AND [key] = 'deleteidentity'
                    update @tmp_table set id = 5005 WHERE submodule_id = 5 AND [key] = 'unlockidentity'
                    update @tmp_table set id = 5006 WHERE submodule_id = 5 AND [key] = 'resetpassword'
                    update @tmp_table set id = 5007 WHERE submodule_id = 5 AND [key] = 'changepassword'
                    update @tmp_table set id = 5008 WHERE submodule_id = 5 AND [key] = 'deploy'
                    update @tmp_table set id = 5009 WHERE submodule_id = 5 AND [key] = 'lock'
                    update @tmp_table set id = 5010 WHERE submodule_id = 5 AND [key] = 'unlock'
                    update @tmp_table set id = 5011 WHERE submodule_id = 5 AND [key] = 'delete'
                    update @tmp_table set id = 5012 WHERE submodule_id = 5 AND [key] = 'logs'
                    update @tmp_table set id = 5013 WHERE submodule_id = 5 AND [key] = 'accessrequest'
                    update @tmp_table set id = 14001 WHERE submodule_id = 14 AND [key] = 'info'
                    update @tmp_table set id = 15001 WHERE submodule_id = 15 AND [key] = 'new'
                    update @tmp_table set id = 15002 WHERE submodule_id = 15 AND [key] = 'change'
                    update @tmp_table set id = 15003 WHERE submodule_id = 15 AND [key] = 'list'
                    update @tmp_table set id = 15004 WHERE submodule_id = 15 AND [key] = 'search'
                    update @tmp_table set id = 15005 WHERE submodule_id = 15 AND [key] = 'delete'
                    update @tmp_table set id = 15006 WHERE submodule_id = 15 AND [key] = 'get'
                    update @tmp_table set id = 15007 WHERE submodule_id = 15 AND [key] = 'accessrequestlist'
                    update @tmp_table set id = 15008 WHERE submodule_id = 15 AND [key] = 'getaccessrequest'
                    update @tmp_table set id = 15009 WHERE submodule_id = 15 AND [key] = 'accessrequestallow'
                    update @tmp_table set id = 15010 WHERE submodule_id = 15 AND [key] = 'accessrequestrevoke'
                    update @tmp_table set id = 15011 WHERE submodule_id = 15 AND [key] = 'accessrequestdeny'

                    DELETE FROM @tmp_table
                    WHERE old_id = id or id is null

                    INSERT INTO [sys_permission] ([id], [submodule_id], [key], [name])
                    SELECT [id], [submodule_id], [key], [name] 
	                    FROM @tmp_table

                    UPDATE	[sys_role_permission]
	                    SET permission_id = t.id
                    FROM	[sys_role_permission] p 
	                    INNER JOIN @tmp_table t
		                    ON t.old_id = p.permission_id

                    DELETE FROM [sys_permission]
                    WHERE id IN (SELECT [old_id] FROM @tmp_table)


                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6001, 6, 'new','Criar novo');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6002, 6, 'get','Visualizar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6003, 6, 'list','Listar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6004, 6, 'search','Buscar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6005, 6, 'change','Alterar dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6006, 6, 'delete','Excluir');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6007, 6, 'deleteallusers','Excluir todos os membros');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6008, 6, 'deleteuser','Excluir membros');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (6009, 6, 'adduser','Adicionar membros');

                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (7001, 7, 'new','Criar novo');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (7002, 7, 'get','Visualizar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (7003, 7, 'list','Listar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (7004, 7, 'search','Buscar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (7005, 7, 'change','Alterar dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (7006, 7, 'delete','Excluir');

                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (8001, 8, 'new','Criar novo');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (8002, 8, 'get','Visualizar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (8003, 8, 'list','Listar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (8004, 8, 'search','Buscar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (8005, 8, 'change','Alterar dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (8006, 8, 'delete','Excluir');

                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (9001, 9, 'new','Criar novo');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (9002, 9, 'get','Visualizar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (9003, 9, 'list','Listar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (9004, 9, 'search','Buscar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (9005, 9, 'change','Alterar dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (9006, 9, 'delete','Excluir');

                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (10001, 10, 'new','Criar novo');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (10002, 10, 'get','Visualizar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (10003, 10, 'list','Listar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (10004, 10, 'search','Buscar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (10005, 10, 'change','Alterar dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (10006, 10, 'delete','Excluir');

                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11001, 11, 'new','Criar novo');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11002, 11, 'get','Visualizar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11003, 11, 'list','Listar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11004, 11, 'search','Buscar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11005, 11, 'change','Alterar dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11006, 11, 'delete','Excluir');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11007, 11, 'clone','Visualizar utilizações');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11008, 11, 'parameters','Visualizar parâmetros do plugin');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11009, 11, 'mapping','Visualizar mapeamento de campos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11010, 11, 'lockrules','Visualizar regras de bloqueio');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11011, 11, 'ignore','Visualizar regras para desconsiderar registros na importação');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11012, 11, 'roles','Visualizar perfis vinculados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11013, 11, 'schedules','Visualizar agendamentos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11014, 11, 'fieldsfetch','Visualizar busca automática de dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11015, 11, 'newfetch','Nova busca automática de dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11016, 11, 'deletefetch','Excluit busca automática de dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11017, 11, 'changeparameters','Alterar parâmetros do plugin');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11018, 11, 'changemapping','Alterar mapeamento de campos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11019, 11, 'changerole','Alterar perfis vinculados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11020, 11, 'changelockrules','Alterar regras de bloqueio');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11021, 11, 'changeignore','Alterar regras para desconsiderar registros na importação');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11022, 11, 'changeschedules','Alterar agendamentos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11023, 11, 'enable','Habilitar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11024, 11, 'disable','Desabilitar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11025, 11, 'deploy','Publicar agora');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11026, 11, 'identity','Visualizar identidades vinculadas');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11027, 11, 'addidentity','Adicionar identidade');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (11028, 11, 'deleteidentity','Excluir identidade');

                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (13001, 13, 'new','Criar novo');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (13002, 13, 'get','Visualizar');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (13003, 13, 'list','Listar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (13004, 13, 'search','Buscar todos');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (13005, 13, 'change','Alterar dados');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (13006, 13, 'delete','Excluir');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (13007, 13, 'use','Visualizar utilizações');

                    SET IDENTITY_INSERT sys_permission OFF;


                    IF (select count(*) from sys_sub_module where id = 16) = 0
                    BEGIN
                        SET IDENTITY_INSERT sys_sub_module ON;
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(16, 2, 'container', 'container', 'Pastas');
                        SET IDENTITY_INSERT sys_sub_module OFF;

                        SET IDENTITY_INSERT [sys_permission] ON;
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16001, 16, 'new','Criar novo');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16002, 16, 'get','Visualizar');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16003, 16, 'list','Listar todos');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16004, 16, 'search','Buscar todos');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16005, 16, 'change','Alterar dados');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16006, 16, 'delete','Excluir');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16007, 16, 'deleteallusers','Excluir membros');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (16008, 16, 'adduser','Adicionar membros');
                        SET IDENTITY_INSERT [sys_permission] OFF;
                    END


                    IF (select count(*) from sys_sub_module where id = 17) = 0
                    BEGIN
                        SET IDENTITY_INSERT sys_sub_module ON;
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(17, 2, 'logs', 'logs', 'Logs');
                        SET IDENTITY_INSERT sys_sub_module OFF;

                        SET IDENTITY_INSERT [sys_permission] ON;
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (17001, 17, 'get','Visualizar');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (17002, 17, 'list','Listar todos');
	                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (17003, 17, 'search','Buscar todos');
                        SET IDENTITY_INSERT [sys_permission] OFF;
                    END



                    INSERT INTO [db_ver] ([version]) VALUES (19);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 19.2; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
