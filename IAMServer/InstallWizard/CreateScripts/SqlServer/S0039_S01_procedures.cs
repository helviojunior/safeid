using System;

namespace InstallWizard
{
    public class S0039_S01_procedures : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    
                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_entity')
                    BEGIN
                        DROP PROCEDURE [sp_new_entity];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_entity] 
	                    @context_id bigint,
	                    @alias varchar(200),
                        @login varchar(50),
                        @full_name varchar(300),
                        @password varchar(2000)
                    AS
                    BEGIN

	                    DECLARE @DATE datetime = GETDATE()

	                    --Insere a role
	                    INSERT INTO [entity]
                               ([context_id]
                               ,[alias]
                               ,[login]
                               ,[full_name]
                               ,[password]
                               ,[create_date]
                               ,[change_password]
                               ,[locked]
                               ,[recovery_code]
                               ,[last_login]
                               ,[must_change_password]
                               ,[deleted]
                               ,[deleted_date])
                         VALUES
                               (@context_id
                               ,@alias
                               ,@login
                               ,@full_name
                               ,@password
                               ,@DATE
                               ,@DATE
                               ,0
                               ,''''
                               ,null
                               ,0
                               ,0
                               ,null)
	
	                    --Busca os dados da empresa
	                    SELECT		id
		                    FROM	[entity] with(nolock)
		                    WHERE	context_id				= @context_id
			                    AND	alias					= @alias
			                    AND	login					= @login
			                    AND	full_name				= @full_name
			                    AND	create_date				= @DATE
		
                    END
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_insert_identity_role')
                    BEGIN
                        DROP PROCEDURE [sp_insert_identity_role];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_insert_identity_role]
	                    @identity_id bigint,
	                    @role_id bigint
                    AS
                    BEGIN
	                    if (select COUNT(*) from identity_role i  with(nolock)
	                    where i.identity_id = @identity_id and i.role_id = @role_id) = 0
	                    begin
		                    insert into identity_role (identity_id, role_id, [auto])
		                    values (@identity_id, @role_id, 1);
		
		                    select cast(1 as bit);
	                    end
	                    else
	                    begin
		                    select cast(0 as bit);
	                    end
	
                    END
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_get_chart_data')
                    BEGIN
                        DROP PROCEDURE [sp_get_chart_data];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_get_chart_data]
	                    @enterpriseId BIGINT,
	                    @dStart DATETIME,
	                    @dEnd DATETIME,
	                    @key int
                    as 

                    declare @dates table (
                      d DATETIME,
                      PRIMARY KEY (d)
                    )

                    --DECLARE @dStart DATETIME = dateadd(MONTH,-1,getdate())
                    --DECLARE @dEnd DATETIME = getdate()

                    --set @dStart = dateadd(day,-1,getdate())

                    DECLARE @dIncr DATETIME = @dStart

                    --Calcula os ranges possíveis para poder preencher com valores zero quando não houver registro
                    IF (DATEDIFF(year,@dStart,@dEnd) > 1)
                    BEGIN
	                    WHILE ( @dIncr < @dEnd )
	                    BEGIN
	                      INSERT INTO @dates (d) VALUES( @dIncr )
	                      SELECT @dIncr = DATEADD(MONTH, 1, @dIncr )
	                    END
                    END
                    ELSE IF (DATEDIFF(year,@dStart,@dEnd) = 1)
                    BEGIN
	                    WHILE ( @dIncr < @dEnd )
	                    BEGIN
	                      INSERT INTO @dates (d) VALUES( @dIncr )
	                      SELECT @dIncr = DATEADD(DAY, 1, @dIncr )
	                    END
                    END
                    ELSE IF (DATEDIFF(MONTH,@dStart,@dEnd) >= 1)
                    BEGIN
	                    WHILE ( @dIncr < @dEnd )
	                    BEGIN
	                      INSERT INTO @dates (d) VALUES( @dIncr )
	                      SELECT @dIncr = convert(datetime,convert(varchar(10), DATEADD(day, 1, @dIncr ), 120),120)
	                    END
                    END
                    ELSE IF (DATEDIFF(DAY,@dStart,@dEnd) >= 3)
                    BEGIN
	                    WHILE ( @dIncr < @dEnd )
	                    BEGIN
	                      INSERT INTO @dates (d) VALUES( @dIncr )
	                      SELECT @dIncr = convert(datetime,convert(varchar(10), DATEADD(day, 1, @dIncr ), 120),120)
	                    END
                    END
                    ELSE IF (DATEDIFF(DAY,@dStart,@dEnd) >= 1)
                    BEGIN
	                    WHILE ( @dIncr < @dEnd )
	                    BEGIN
	                      INSERT INTO @dates (d) VALUES( @dIncr )
	                      SELECT @dIncr = convert(datetime,convert(varchar(13), DATEADD(day, 1, @dIncr ), 120) + '':00:00'',120)
	                    END
                    END
                    ELSE
                    BEGIN
	                    WHILE ( @dIncr < @dEnd )
	                    BEGIN
	                      INSERT INTO @dates (d) VALUES( @dIncr )
	                      SELECT @dIncr = convert(datetime,convert(varchar(16), DATEADD(MINUTE, 5, @dIncr ), 120) + '':00'',120)
	                    END
                    END

                    --select * from @dates

                    declare @logs table (
                      qty int,
                      date DATETIME 
                    )

                    insert into @logs
                    /*SELECT 
		                    qty = COUNT(l.[key]),
		                    date = CASE WHEN (DATEDIFF(year,@dStart,@dEnd) > 1) THEN convert(datetime,convert(varchar(7), l.DATE, 120),120)
		                    WHEN (DATEDIFF(year,@dStart,@dEnd) = 1) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(MONTH,@dStart,@dEnd) >= 1) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(DAY,@dStart,@dEnd) >= 1) THEN convert(datetime,convert(varchar(13), l.DATE, 120) + '':00:00'',120)
		                    ELSE convert(datetime,convert(varchar(16), l.DATE, 120) + '':00'',120) END
	                    FROM logs l with(nolock)
	                    where l.enterprise_id = @enterpriseId and l.[key] = @key and l.date between @dStart and @dEnd
	                    GROUP BY CASE WHEN (DATEDIFF(year,@dStart,@dEnd) > 1) THEN convert(datetime,convert(varchar(7), l.DATE, 120),120)
		                    WHEN (DATEDIFF(year,@dStart,@dEnd) = 1) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(MONTH,@dStart,@dEnd) >= 1) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(DAY,@dStart,@dEnd) >= 1) THEN convert(datetime,convert(varchar(13), l.DATE, 120) + '':00:00'',120)
		                    ELSE convert(datetime,convert(varchar(16), l.DATE, 120) + '':00'',120) END
	                    having COUNT(l.[key]) > 0*/

                    SELECT 
		                    qty = COUNT(l.[key]),
		                    date = CASE WHEN (DATEDIFF(year,@dStart,@dEnd) > 1) THEN convert(datetime,convert(varchar(7), l.DATE, 120),120)
		                    WHEN (DATEDIFF(year,@dStart,@dEnd) = 1) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(MONTH,@dStart,@dEnd) >= 1) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(DAY,@dStart,@dEnd) >= 3) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(DAY,@dStart,@dEnd) >= 1) THEN convert(datetime,convert(varchar(13), l.DATE, 120) + '':00:00'',120)
		                    ELSE convert(datetime,convert(varchar(16), l.DATE, 120) + '':00'',120) END
	                    FROM logs l with(nolock)
	                    where l.enterprise_id = @enterpriseId and l.[key] = @key and l.date between @dStart and @dEnd
	                    GROUP BY CASE WHEN (DATEDIFF(year,@dStart,@dEnd) > 1) THEN convert(datetime,convert(varchar(7), l.DATE, 120),120)
		                    WHEN (DATEDIFF(year,@dStart,@dEnd) = 1) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(MONTH,@dStart,@dEnd) >= 1) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(DAY,@dStart,@dEnd) >= 3) THEN convert(datetime,convert(varchar(10), l.DATE, 120),120)
		                    WHEN (DATEDIFF(DAY,@dStart,@dEnd) >= 1) THEN convert(datetime,convert(varchar(13), l.DATE, 120) + '':00:00'',120)
		                    ELSE convert(datetime,convert(varchar(16), l.DATE, 120) + '':00'',120) END
	                    having COUNT(l.[key]) > 0


                    SELECT 
	                    qty = isnull(SUM(qty),0),
	                    date = d.d
                    FROM @logs l
                    RIGHT JOIN @dates d
	                    ON d.d = l.date
                    GROUP BY d.d
                    ORDER BY d.d
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_change_login')
                    BEGIN
                        DROP PROCEDURE [sp_change_login];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_change_login] 
	                    @entity_id bigint,
	                    @old_login varchar(50),
	                    @new_login varchar(50)
                    AS
                    BEGIN
                    begin tran
	
	                    update entity set login = @new_login where id = @entity_id
	
	                    update		identity_field 
		                    set		value = @new_login
		                    where	value = @old_login
			                    and field_id in (
				                    select rp.login_field_id 
					                    from [resource_plugin] rp
					                    inner join [resource] r 
						                    on rp.resource_id = r.id
					                    inner join context c
						                    on r.context_id = c.id
					                    inner join entity e
						                    on e.context_id = c.id
					                    where e.id = @entity_id
				                    )
			                    and	identity_id in (
					                    select		id
						                    from	[identity] i
						                    where	i.entity_id = @entity_id
				                    )
	
	                    update		entity_field 
		                    set		value = @new_login
		                    where	value = @old_login
			                    and	entity_id = @entity_id
			                    and field_id in (
				                    select rp.login_field_id 
					                    from [resource_plugin] rp
					                    inner join [resource] r 
						                    on rp.resource_id = r.id
					                    inner join context c
						                    on r.context_id = c.id
					                    inner join entity e
						                    on e.context_id = c.id
					                    where e.id = @entity_id
				                    )
	
	
	
                    commit tran	
                    END


                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_change_mail')
                    BEGIN
                        DROP PROCEDURE [sp_change_login];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_change_mail] 
	                    @entity_id bigint,
	                    @old_mail varchar(50),
	                    @new_mail varchar(50)
                    AS
                    BEGIN
                    begin tran
	
	                    
	                    update entity set login = @new_mail where id = @entity_id
	
	                    update		identity_field 
		                    set		value = @new_mail
		                    where	value = @old_mail
			                    and field_id in (
				                    select rp.mail_field_id 
					                    from [resource_plugin] rp
					                    inner join [resource] r 
						                    on rp.resource_id = r.id
					                    inner join context c
						                    on r.context_id = c.id
					                    inner join entity e
						                    on e.context_id = c.id
					                    where e.id = @entity_id
				                    )
			                    and	identity_id in (
					                    select		id
						                    from	[identity] i
						                    where	i.entity_id = @entity_id
				                    )
	
	                    update		entity_field 
		                    set		value = @new_mail
		                    where	value = @old_mail
			                    and	entity_id = @entity_id
			                    and field_id in (
				                    select rp.mail_field_id 
					                    from [resource_plugin] rp
					                    inner join [resource] r 
						                    on rp.resource_id = r.id
					                    inner join context c
						                    on r.context_id = c.id
					                    inner join entity e
						                    on e.context_id = c.id
					                    where e.id = @entity_id
				                    )
	
	
	
                    commit tran	
                    END
                    ');

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_reindex_imports')
                    BEGIN
                        DROP PROCEDURE [sp_reindex_imports];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_reindex_imports]
                    as
                    DBCC DBREINDEX (''dbo.collector_imports'', '' '', 70)
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_reindex_entity')
                    BEGIN
                        DROP PROCEDURE [sp_reindex_entity];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_reindex_entity]
                    as
                    DBCC DBREINDEX (''dbo.identity_field'', '' '', 70)
                    DBCC DBREINDEX (''dbo.entity'', '' '', 70)
                    DBCC DBREINDEX (''dbo.identity'', '' '', 70)
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_rebuild_views')
                    BEGIN
                        DROP PROCEDURE [sp_rebuild_views];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_rebuild_views]
                    as

                    DECLARE @view_name AS NVARCHAR(500);

                    DECLARE views_cursor CURSOR FOR 
                        SELECT TABLE_SCHEMA + ''.'' +TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_TYPE = ''VIEW'' 
                        AND OBJECTPROPERTY(OBJECT_ID(TABLE_NAME), ''IsMsShipped'') = 0 
                        ORDER BY TABLE_SCHEMA,TABLE_NAME 

                    OPEN views_cursor 

                    FETCH NEXT FROM views_cursor 
                    INTO @view_name 

                    WHILE (@@FETCH_STATUS <> -1) 
                    BEGIN
                        BEGIN TRY
                            EXEC sp_refreshview @view_name;
                            PRINT @view_name;
                        END TRY
                        BEGIN CATCH
                            PRINT ''Error during refreshing view '' + @view_name + ''.'';
                        END CATCH;

                        FETCH NEXT FROM views_cursor 
                        INTO @view_name 
                    END 

                    CLOSE views_cursor; 
                    DEALLOCATE views_cursor;
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_insert_entity_to_role')
                    BEGIN
                        DROP PROCEDURE [sp_insert_entity_to_role];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_insert_entity_to_role]
	                    @enterprise_id bigint,
	                    @role_id bigint,
	                    @entity_id bigint
                    AS
                    BEGIN


	                    DECLARE @INSERT TABLE (
		                    [identity_id] [bigint] NOT NULL,
		                    [role_id] [bigint] NOT NULL,
		                    [auto] [bit] NOT NULL
	                    )
	
	                    --insert into identity_role (identity_id, role_id, auto) 
	                    insert into @INSERT
	                    select		identity_id, @role_id, 0 
		                    from	vw_eligible_identity 
	                    where	id = @entity_id
		                    AND	not exists(select		1 
						                    from	identity_role ir 
							                    inner	join [identity] i 
								                    on	ir.identity_id = i.id 
							                    where	ir.role_id = @role_id 
								                    and i.entity_id = @entity_id) 
	
	
	                    INSERT INTO identity_role (identity_id, role_id, auto) 
	                    SELECT * FROM @INSERT
	
	                    IF (SELECT COUNT(*) FROM @INSERT) > 0
	                    BEGIN
		                    INSERT INTO deploy_now (entity_id) values(@entity_id)
	                    END
	
	                    SELECT TOP	1 i.[identity_id], r.name role_name
		                    FROM	@INSERT i
		                    inner	join role r
			                    on	r.id = i.role_id
	
                    END
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_sys_rbac_admin')
                    BEGIN
                        DROP PROCEDURE [sp_sys_rbac_admin];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_sys_rbac_admin]
	                    @entity_id bigint,
	                    @enterprise_id bigint
                    as

                    declare @admin bit
                    set @admin = 0

                    /* verifica se o usuario e system admin */
                    select	@admin = 1 
	                    from	sys_role r
                    inner join	sys_entity_role er
	                    on	er.role_id	= r.id
                    where	er.entity_id = @entity_id
	                    and	r.sa = 1

                    /* verifica se o usuário é enterprise admin */
                    if (@admin = 0)
                    BEGIN
	                    select	@admin = 1 
		                    from	sys_role r
	                    inner join	sys_entity_role er
		                    on	er.role_id	= r.id
	                    where	er.entity_id = @entity_id
		                    and	r.enterprise_id = @enterprise_id
		                    and	r.ea = 1
                    END

                    select @admin
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_user_statistics')
                    BEGIN
                        DROP PROCEDURE [sp_user_statistics];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_user_statistics]
	                    @enterpriseId bigint
                    as

                    declare @tbl table (qty bigint, locked bit, legged bit);

                    insert into @tbl
	                    select	COUNT(*) qty, 
			                    e.locked, 
			                    logged = case when e.last_login is not null then CAST(1 as bit) else 0 end
	                    from	entity e with(nolock)
		                    inner	join context c with(nolock) on e.context_id = c.id
		                    where		e.deleted = 0
			                    and		c.enterprise_id = @enterpriseId
		                    group by	e.locked, case when e.last_login is not null then CAST(1 as bit) else 0 end

                    select 
	                    total = isnull((select SUM(qty) from @tbl), 0),
	                    locked = isnull((select SUM(qty) from @tbl where locked = 1), 0),
	                    logged = isnull((select SUM(qty) from @tbl where legged = 1), 0)
                    ');

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_auth_key')
                    BEGIN
                        DROP PROCEDURE [sp_new_auth_key];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_auth_key]
	                    @entityId bigint
                    as

                    declare @key varchar(50)

                    /* Usar datas UTC para compatibilidade com sistemas integrados */

                    /* Remove as chaves antigas, se houver */
                    delete from entity_auth
	                    where	entity_id	= @entityId 
		                    and end_date	<= dateadd(mi, -1, GETUTCDATE())
		
                    /* Verifica se ha alguma chame ativa para este usuário */
                    select		@key = auth_key
	                    from	entity_auth  with(nolock)
	                    where	entity_id	= @entityId 
		                    and end_date	>= GETUTCDATE()

                    if (@key is null)
                    begin

	                    /* deleta as chaves ativas */
	                    delete from entity_auth
		                    where	entity_id	= @entityId

	                    insert into entity_auth (entity_id, auth_key, start_date, end_date) 
	                    select	e.id,
			                    CONVERT(varchar(50), NEWID()),
			                    GETUTCDATE(),
			                    DATEADD(mi, c.auth_key_time, GETUTCDATE())
	                    from		entity e with(nolock)
		                    inner	join context c with(nolock)
			                    on	c.id = e.context_id
		                    where	e.id = @entityId

                    end

                    /* Retorna a tabela da entidade + a chave */
                    select e.*, ea.auth_key, ea.start_date, ea.end_date from entity e with(nolock) inner join entity_auth ea with(nolock) on e.id = ea.entity_id
                    where e.id = @entityId
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_service_status')
                    BEGIN
                        DROP PROCEDURE [sp_service_status];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_service_status]
	                    @name varchar(200),
	                    @data varchar(max)
                    as

                    /* Insere o identity, caso não exista */
                    insert into service_status (service_name, additional_data)
                    select		@name, @data
	                    where	not exists (select		1 from service_status with(nolock)
							                    where 	service_name = @name)

                    /* Atualiza o registro */
                    update		service_status
		                    set	last_status = GETDATE(),
			                    additional_data = @data
	                    where	service_name = @name
                    ');



                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_sys_role')
                    BEGIN
                        DROP PROCEDURE [sp_new_sys_role];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_sys_role] 
	                    @enterprise_id bigint,
	                    @name varchar(200),
	                    @parent_id bigint = 0,
	                    @system_admin bit,
	                    @enterprise_admin bit
                    AS
                    BEGIN

	                    DECLARE @DATE datetime = GETDATE()

	                    --Insere a role
	                    INSERT INTO [sys_role]
                               ([parent_id]
                               ,[enterprise_id]
                               ,[name]
                               ,[sa]
                               ,[ea])
                         VALUES
                               (@parent_id
                               ,@enterprise_id
                               ,@name
                               ,@system_admin
                               ,@enterprise_admin)
	
	                    --Busca os dados da empresa
	                    SELECT		id
		                    FROM	[sys_role]
		                    WHERE	name				= @name
			                    AND	parent_id			= @parent_id
			                    AND	enterprise_id		= @enterprise_id
			                    AND	sa					= @system_admin
			                    AND	ea					= @enterprise_admin
		
                    END
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_role')
                    BEGIN
                        DROP PROCEDURE [sp_new_role];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_role]
	                    @enterprise_id bigint,
	                    @context_id bigint,
	                    @parent_id bigint,
	                    @role_name varchar(50)
                    as

                    DECLARE @DATE DATETIME;
                    SET @DATE = GETDATE()

                    INSERT INTO role (context_id, parent_id, [name], [create_date])
                    VALUES (@context_id, @parent_id, @role_name, @DATE)

                    SELECT	r.*, c.enterprise_id, 0 AS entity_qty
                    FROM		role r
	                    inner	join context c
		                    on	c.id = r.context_id
                    WHERE	r.context_id	= @context_id
	                    AND	r.parent_id		= @parent_id
	                    AND	r.name			= @role_name
	                    AND	r.create_date	= @DATE
                    ');



                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_context')
                    BEGIN
                        DROP PROCEDURE [sp_new_context];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_context]
	                    @enterprise_id bigint,
	                    @name varchar(255),
	                    @password_rule varchar(50),
	                    @pwd_length integer,
	                    @pwd_upper_case bit,
	                    @pwd_lower_case bit,
	                    @pwd_digit bit,
	                    @pwd_symbol bit,
	                    @pwd_no_name bit
                    as

                    DECLARE @DATE DATETIME;
                    SET @DATE = GETDATE()

                    INSERT INTO [context]
                               ([enterprise_id]
                               ,[name]
                               ,[password_rule]
                               ,[auth_key_time]
                               ,[pwd_length]
                               ,[pwd_upper_case]
                               ,[pwd_lower_case]
                               ,[pwd_digit]
                               ,[pwd_symbol]
                               ,[pwd_no_name]
                               ,[create_date])
                         VALUES
			                    (@enterprise_id
			                    ,@name
			                    ,@password_rule
			                    ,20
			                    ,@pwd_length
			                    ,@pwd_upper_case
			                    ,@pwd_lower_case
			                    ,@pwd_digit
			                    ,@pwd_symbol
			                    ,@pwd_no_name
			                    ,@DATE)

                    SELECT	c.*, 0 as entity_qty
                    FROM		context c
                    WHERE	c.enterprise_id = @enterprise_id
	                    AND	c.name			= @name
	                    AND	c.password_rule	= @password_rule
	                    AND	c.pwd_length	= @pwd_length
	                    AND	c.pwd_upper_case= @pwd_upper_case
	                    AND	c.pwd_lower_case= @pwd_lower_case
	                    AND	c.pwd_digit		= @pwd_digit
	                    AND	c.pwd_symbol	= @pwd_symbol
	                    AND	c.pwd_no_name	= @pwd_no_name
	                    AND	c.create_date	= @DATE
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_insert_plugin')
                    BEGIN
                        DROP PROCEDURE [sp_insert_plugin];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_insert_plugin] 
	                    @scheme varchar(50),
	                    @uri varchar(500),
	                    @assembly varchar(100)
                    AS
                    BEGIN
	                    if (select COUNT(*) from plugin 
	                    where [scheme] = @scheme and uri = @uri and [assembly] = @assembly) = 0
	                    begin
		                    insert into plugin ([scheme], [uri], [assembly])
		                    values (@scheme, @uri, @assembly);
	                    end
	
                    END
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_migrate_imported2')
                    BEGIN
                        DROP PROCEDURE [sp_migrate_imported2];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_migrate_imported2] 
	                    @resource_plugin_id bigint,
                        @import_id varchar(40),
                        @package_id varchar(40),
                        @status varchar(2),
                        @new_status varchar(2)
                    AS
                    BEGIN

	                    DELETE FROM collector_imports
		                    WHERE status = @status 
		                    and resource_plugin_id = @resource_plugin_id
		                    and  import_id = @import_id
		                    and package_id = @package_id
		
                    END
                    ');



                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_migrate_imported')
                    BEGIN
                        DROP PROCEDURE [sp_migrate_imported];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_migrate_imported]
                    as
                    --insert into collector_imports_old select * from collector_imports where status in (''i'',''e'',''le'')
                    delete from collector_imports where status in (''i'',''e'',''le'')
                    delete from collector_imports_old where date < DATEADD(day,-7,getdate())
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_sys_rbac')
                    BEGIN
                        DROP PROCEDURE [sp_sys_rbac];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_sys_rbac]
	                    @entity_id bigint,
	                    @enterprise_id bigint,
	                    @submodule varchar(50),
	                    @permission varchar(50)
                    as

                    declare @can bit
                    set @can = 0

                    /* verifica se o usuario e system admin */
                    select	@can = 1 
	                    from	sys_role r
                    inner join	sys_entity_role er
	                    on	er.role_id	= r.id
                    where	er.entity_id = @entity_id
	                    and	r.sa = 1

                    /* verifica se o usuário é enterprise admin */
                    if (@can = 0)
                    BEGIN
	                    select	@can = 1 
		                    from	sys_role r
	                    inner join	sys_entity_role er
		                    on	er.role_id	= r.id
	                    where	er.entity_id = @entity_id
		                    and	r.enterprise_id = @enterprise_id
		                    and	r.ea = 1
                    END

                    /* verifica a permissão no modulo e funcao especifica */
                    if (@can = 0)
                    BEGIN
	                    select	@can = 1 
		                    from	sys_role r
	                    inner join	sys_entity_role er
		                    on	er.role_id	= r.id
	                    inner join sys_role_permission rp
		                    on	rp.role_id = r.id
	                    inner join sys_permission p
		                    on p.id = rp.permission_id
	                    inner join	sys_module m
		                    on	m.id = p.submodule_id
	                    where	er.entity_id = @entity_id
		                    and	r.enterprise_id = @enterprise_id
		                    and	m.[key] = @submodule
		                    and	p.[key] = @permission
                    END

                    select @can
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_enterprise')
                    BEGIN
                        DROP PROCEDURE [sp_new_enterprise];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_enterprise] 
	                    @name varchar(200),
	                    @fqdn varchar(2000),
	                    @server_pkcs12_cert varchar(max),
	                    @server_cert varchar(max),
	                    @client_pkcs12_cert varchar(max),
	                    @language varchar(10),
	                    @auth_plugin varchar(500)
                    AS
                    BEGIN

	                    DECLARE @DATE datetime = GETDATE()

	                    --Insere a empresa
	                    INSERT INTO [enterprise]
                               ([name]
                               ,[fqdn]
                               ,[server_pkcs12_cert]
                               ,[server_cert]
                               ,[client_pkcs12_cert]
                               ,[language]
                               ,[create_date]
                               ,[auth_plugin])
                         VALUES
                               (@name
                               ,@fqdn
                               ,@server_pkcs12_cert
                               ,@server_cert
                               ,@client_pkcs12_cert
                               ,@language
                               ,@DATE
                               ,@auth_plugin)
	
	                    --Busca os dados da empresa
	                    SELECT		id
		                    FROM	[enterprise]
		                    WHERE	name				= @name
			                    AND	fqdn				= @fqdn
			                    AND	server_pkcs12_cert	= @server_pkcs12_cert
			                    AND	server_cert			= @server_cert
			                    AND	client_pkcs12_cert	= @client_pkcs12_cert
			                    AND	language			= @language
			                    AND	create_date			= @DATE
			                    AND	auth_plugin			= @auth_plugin
		
                    END
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_process_logs')
                    BEGIN
                        DROP PROCEDURE [sp_process_logs];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_process_logs]
                    as

	                    DECLARE @tmpTable TABLE(
		                    [date] [datetime] NOT NULL,
		                    [source] [varchar](50) NOT NULL,
		                    [key] [int] NOT NULL,
		                    [level] [int] NOT NULL,
		                    [proxy_id] [bigint] NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [context_id] [bigint] NOT NULL,
		                    [resource_id] [bigint] NOT NULL,
		                    [plugin_id] [bigint] NOT NULL,
		                    [entity_id] [bigint] NOT NULL,
		                    [identity_id] [bigint] NOT NULL,
		                    [text] [varchar](max) NOT NULL,
		                    [additional_data] [varchar](max) NULL)

	                    INSERT INTO @tmpTable
                               ([date]
                               ,[source]
                               ,[key]
                               ,[level]
                               ,[proxy_id]
                               ,[enterprise_id]
                               ,[context_id]
                               ,[resource_id]
                               ,[plugin_id]
                               ,[entity_id]
                               ,[identity_id]
                               ,[text]
                               ,[additional_data])
		                    SELECT	i.date, 
				                    i.source,
				                    i.[key],
				                    CASE
					                    WHEN (i.type is null OR i.type = '''') THEN 0
					                    WHEN ISNUMERIC(i.type) = 1 THEN CAST(i.type as int)
					                    ELSE 0
				                    END,
				                    CASE
					                    WHEN (i.proxy_id is null OR i.proxy_id = 0) AND i.proxy_name is not null
						                    THEN (SELECT top 1 p.id
								                    FROM proxy p
								                    WHERE p.enterprise_id = i.enterprise_id and p.name = i.proxy_name)
					                     ELSE 0
				                    END,
				                    i.enterprise_id,
				                    0,
				                    i.resource_id,
				                    CASE
					                    WHEN (i.plugin_id is null OR i.plugin_id = 0) AND i.plugin_uri is not null
						                    THEN (SELECT top 1 p.id
								                    FROM plugin p
								                    WHERE p.uri = i.plugin_uri)
					                     ELSE i.plugin_id
				                    END,
				                    i.entity_id,
				                    i.identity_id,
				                    i.text,
				                    i.additional_data
                           FROM logs_imports i

	                    DELETE l
	                    FROM logs_imports l
	                    INNER JOIN @tmpTable t
		                    ON	l.date		= t.date
		                    AND	l.source	= t.source
		                    AND	l.[key]		= t.[key]
		                    AND	l.enterprise_id		= t.enterprise_id
		                    AND	l.resource_id		= t.resource_id
		                    AND	l.entity_id		= t.entity_id
		                    AND	l.identity_id		= t.identity_id
		                    AND	l.text		= t.text

	                    INSERT INTO logs
                               ([date]
                               ,[source]
                               ,[key]
                               ,[level]
                               ,[proxy_id]
                               ,[enterprise_id]
                               ,[context_id]
                               ,[resource_id]
                               ,[plugin_id]
                               ,[entity_id]
                               ,[identity_id]
                               ,[text]
                               ,[additional_data])
                        SELECT * FROM @tmpTable
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_filter')
                    BEGIN
                        DROP PROCEDURE [sp_new_filter];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_filter]
	                    @enterprise_id bigint,
	                    @filter_name varchar(300)
                    as

                    INSERT INTO filters (enterprise_id, [name])
                    VALUES (@enterprise_id, @filter_name)

                    SELECT	f.*
                    FROM		filters f
                    WHERE	f.enterprise_id	= @enterprise_id
	                    AND	f.[name]		= @filter_name
                    ');



                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_field')
                    BEGIN
                        DROP PROCEDURE [sp_new_field];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_field]
	                    @enterprise_id bigint,
	                    @field_name varchar(100),
	                    @data_type varchar(50),
	                    @public bit,
	                    @user bit
                    as

                    INSERT INTO field (enterprise_id, [name], [data_type], [public], [user])
                    VALUES (@enterprise_id, @field_name, @data_type, @public, @user)

                    SELECT	f.*
                    FROM		field f
                    WHERE	f.enterprise_id	= @enterprise_id
	                    AND	f.[name]		= @field_name
	                    AND	f.[public]		= @public
	                    AND	f.[user]		= @user
	                    AND	f.[data_type]	= @data_type
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_insert_link_count')
                    BEGIN
                        DROP PROCEDURE [sp_insert_link_count];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_insert_link_count]
	                    @enterprise_id bigint,
	                    @entity_id bigint,
	                    @module varchar(50),
	                    @submodule varchar(50),
	                    @path varchar(5000)
                    as

                    DECLARE @kmodule bigint
                    DECLARE @ksubmodule bigint

                    SELECT @kmodule = id FROM sys_module WHERE [key] = @module
                    SELECT @ksubmodule = id FROM sys_sub_module WHERE [key] = @submodule

                    INSERT INTO sys_link_count (module_id, submodule_id, enterprise_id, entity_id, [path])
                    SELECT		@kmodule, @ksubmodule, @enterprise_id, @entity_id, @path
	                    WHERE	NOT EXISTS (SELECT		1
							                    FROM	sys_link_count
							                    WHERE	module_id		= @kmodule
								                    AND	submodule_id	= @ksubmodule
								                    AND	enterprise_id	= @enterprise_id
								                    AND	entity_id		= @entity_id
								                    AND	[path]			= @path)
								
                    UPDATE	sys_link_count
	                    SET	[count] = [count] + 1
                    FROM	sys_link_count
                    WHERE	module_id		= @kmodule
	                    AND	submodule_id	= @ksubmodule
	                    AND	enterprise_id	= @enterprise_id
	                    AND	entity_id		= @entity_id
	                    AND	[path]			= @path
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_resource')
                    BEGIN
                        DROP PROCEDURE [sp_new_resource];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_resource]
	                    @enterprise_id bigint,
	                    @context_id bigint,
	                    @proxy_id bigint,
	                    @resource_name varchar(50)
                    as

                    DECLARE @DATE DATETIME;
                    SET @DATE = GETDATE()

                    INSERT INTO resource (context_id, proxy_id, [name], [enabled], [create_date])
                    VALUES (@context_id, @proxy_id, @resource_name, 1, @DATE)

                    SELECT	r.*, c.enterprise_id, 0 AS resource_plugin_qty
                    FROM		resource r
	                    inner	join context c
		                    on	c.id = r.context_id
                    WHERE	r.context_id	= @context_id
	                    AND	r.proxy_id		= @proxy_id
	                    AND	r.name			= @resource_name
	                    AND	r.create_date	= @DATE
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_proxy')
                    BEGIN
                        DROP PROCEDURE [sp_new_proxy];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_proxy]
	                    @enterprise_id bigint,
	                    @proxy_name varchar(255)
                    as

                    DECLARE @DATE DATETIME;
                    SET @DATE = GETDATE()

                    INSERT INTO proxy (enterprise_id, [name], [create_date])
                    VALUES (@enterprise_id, @proxy_name, @DATE)

                    select	p.*, 
		                    resource_qty = (
						                    select		COUNT(distinct r1.proxy_id) 
							                    from	resource r1 
							                    where	r1.proxy_id = p.id) 
                    FROM		proxy p
                    WHERE	p.enterprise_id	= @enterprise_id
	                    AND	p.name			= @proxy_name
	                    AND	p.create_date	= @DATE
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_identity')
                    BEGIN
                        DROP PROCEDURE [sp_new_identity];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_identity]
	                    @entityId bigint,
	                    @resourcePluginId bigint
                    as

                    declare @rpId bigint
                    declare @new bit = 1

                    /* Resgata o resource_plugin_id */
                    select		@rpId = id
	                    from	resource_plugin  with(nolock)
	                    where	id = @resourcePluginId 

                    select		@new = 0
	                    from	[identity] with(nolock)
	                    where 	resource_plugin_id = @rpId
		                    and	entity_id = @entityId

                    /* Insere o identity, caso não exista */
                    insert into [identity] (resource_plugin_id, entity_id)
                    select		@rpId, @entityId
	                    where	not exists (select		1 from [identity] with(nolock)
							                    where 	resource_plugin_id = @rpId
								                    and	entity_id = @entityId)
		                    and	exists (select	1	from entity e with(nolock)
						                    where	e.id = @entityId
							                    and	e.deleted = 0)

                    /* Retorna a tabela do entity e identity */
                    select top	1	e.*, 
				                    i.id identity_id, 
				                    @new new_identity,
				                    block_inheritance = case when exists (select 1 from identity_block_inheritance bi with(nolock) where bi.identity_id = i.id) then cast(1 as bit) else cast(0 as bit) end
	                    from	entity e with(nolock)
                    left join	[identity] i with(nolock)
		                    on	e.id = i.entity_id
		                    and	i.resource_plugin_id = @rpId
	                    where 	e.id = @entityId
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_entity_and_identity')
                    BEGIN
                        DROP PROCEDURE [sp_new_entity_and_identity];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_entity_and_identity]
	                    @resourcePluginId bigint,
	                    @alias varchar(30),
	                    @full_name varchar(300)
                    as

                    declare @contextId bigint
                    declare @entityId bigint
                    declare @rpId bigint
                    declare @date datetime

                    /* Define a data de criação para localizar o usuário */
                    set @date = GETDATE()

                    /* Procura o contexto */
                    set @contextId = (select top 1 context_id from resource r with(nolock) inner join resource_plugin rp with(nolock) on rp.resource_id = r.id where rp.id = @resourcePluginId)

                    /* Insere o Entity */
                    insert into entity (context_id, alias, full_name, must_change_password, create_date)
			                    values (@contextId, @alias, @full_name, 1, @date)

                    /* Resgata o entity Id */
                    select		@entityId = id 
	                    from	entity  with(nolock)
	                    where	context_id = @contextId
		                    and alias = @alias
		                    and full_name = @full_name
		                    and create_date = @date

                    /* Resgata o resource_plugin_id */
                    select		@rpId = id
	                    from	resource_plugin  with(nolock)
	                    where	id = @resourcePluginId 

                    /* Insere o identity, caso não exista */
                    insert into [identity] (resource_plugin_id, entity_id)
                    select		@rpId, @entityId
	                    where	not exists (select		1 from [identity] with(nolock)
							                    where 	resource_plugin_id = @rpId
								                    and	entity_id = @entityId)

                    /* Retorna a tabela do entity e identity */

                    select top	1 e.*, i.id identity_id
	                    from	entity e with(nolock)
                    inner join	[identity] i with(nolock)
		                    on	e.id = i.entity_id
	                    where 	e.id = @entityId
		                    and	i.resource_plugin_id = @rpId
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_check_login_exists')
                    BEGIN
                        DROP PROCEDURE [sp_check_login_exists];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_check_login_exists]
	                    @context_id bigint,
	                    @entity_id bigint,
	                    @login varchar(200)
                    as

	                    DECLARE @COUNT BIT

	                    select		@COUNT = COUNT(e.id) 
		                    from	entity e with(nolock) 
		                    inner	join [identity] i with(nolock) on e.id = i.entity_id 
		                    inner	join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id 
		                    left	join identity_field ife with(nolock) on ife.field_id = rp.login_field_id and ife.identity_id = i.id 
		                    where	e.id <> @entity_id and e.context_id = @context_id and (e.login = @login or ife.[value] = @login)
	
	                    select case when @COUNT > 0 then cast(1 as bit) else cast(0 as bit) end
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_resource_plugin')
                    BEGIN
                        DROP PROCEDURE [sp_new_resource_plugin];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_resource_plugin]
	                    @enterprise_id bigint,
	                    @resource_id bigint,
	                    @plugin_id bigint,
	                    @mail_domain varchar(200)
                    as

                    DECLARE @DATE DATETIME;
                    SET @DATE = GETDATE()

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
                               ,[create_date])
                         VALUES
                               (@resource_id
                               ,@plugin_id
                               ,0
                               ,0
                               ,@mail_domain
                               ,0
                               ,0
                               ,0
                               ,0
                               ,0
                               ,0
                               ,0
                               ,0
                               ,1
                               ,1
                               ,1
                               ,1
                               ,''none''
                               ,@DATE)

                    SELECT	rp.*, (r.name + '' x '' + p.name) as name, r.name resource_name, p.name plugin_name
                    FROM		resource_plugin rp
	                    INNER	JOIN resource r ON r.id = rp.resource_id
	                    INNER	JOIN plugin p ON p.id = rp.plugin_id
                    WHERE	rp.resource_id	= @resource_id
	                    AND	rp.plugin_id		= @plugin_id
	                    AND	rp.mail_domain		= @mail_domain
	                    AND	rp.create_date		= @DATE
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_audit_identity')
                    BEGIN
                        DROP PROCEDURE [sp_new_audit_identity];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_new_audit_identity]
	                    @pluginId bigint,
	                    @resourceId bigint,
	                    @id varchar(100),
	                    @full_name varchar(500),
	                    @event varchar(100),
	                    @fields varchar(max)
                    as

                    declare @rpId bigint
                    declare @new bit = 1

                    /* Resgata o resource_plugin_id */
                    select		@rpId = id
	                    from	resource_plugin  with(nolock)
	                    where	plugin_id = @pluginId 
		                    and resource_id = @resourceId

                    select		@new = 0
	                    from	[audit_identity] with(nolock)
	                    where 	resource_plugin_id = @rpId
		                    and	[id] = @id
		                    and	[event] = @event

                    /* Insere o identity, caso não exista */
                    IF (@new = 1)
                    BEGIN
	                    INSERT INTO [audit_identity]
			                       ([resource_plugin_id]
			                       ,[id]
			                       ,[full_name]
			                       ,[event]
			                       ,[create_date]
			                       ,[update_date]
			                       ,[fields])
	                    select		@rpId, @id, @full_name, @event, GETDATE(), GETDATE(), @fields
		                    where	@new = 1
                    END
                    ELSE
                    BEGIN
	                    UPDATE	[audit_identity]
		                    SET [full_name]		= @full_name,
			                    [update_date]	= GETDATE(),
			                    [fields]		= @fields
		                    WHERE 	resource_plugin_id = @rpId
			                    and	[id] = @id
			                    and	[event] = @event
                    END
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_clone_resourceplugin')
                    BEGIN
                        DROP PROCEDURE [sp_clone_resourceplugin];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_clone_resourceplugin]
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

                                        END
                    ');


                    INSERT INTO [db_install] ([version]) VALUES (39);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when count(*) = 0 then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 39.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
