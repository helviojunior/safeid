using System;

namespace InstallWizard
{
    public class S0037_S01_views : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_eligible_identity')
                    BEGIN
                        DROP VIEW [vw_eligible_identity];
                    END

                    EXEC('CREATE VIEW [dbo].[vw_eligible_identity]
                    as
                    select	e.id, 
		                    identity_id =	case when not min(i1.identity_id) is null then min(i1.identity_id)
						                    else min(i2.identity_id) end
                    from	entity e
                    left join (select i.entity_id, i.id identity_id from [identity] i
                    inner join resource_plugin rp on i.resource_plugin_id = rp.id and rp.permit_add_entity = 1) i1
                    on i1.entity_id = e.id
                    left join (select i.entity_id, i.id identity_id from [identity] i
                    inner join resource_plugin rp on i.resource_plugin_id = rp.id) i2
                    on i2.entity_id = e.id
                    group by e.id
                    --where  e.id = 1
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_logs')
                    BEGIN
                        DROP VIEW [vw_logs];
                    END

                    EXEC('CREATE view [dbo].[vw_logs]
                    as
                    select 
	                    l.date,
	                    e.id entity_id,
	                    e.full_name,
	                    l.source,
	                    p1.name,
	                    p.uri,
	                    l.level,
	                    l.text
                    from logs l
                    inner join entity e on l.entity_id = e.id
                    left join plugin p on l.plugin_id = p.id
                    left join proxy p1 on l.proxy_id = p1.id
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_entity_confirmations')
                    BEGIN
                        DROP VIEW [vw_entity_confirmations];
                    END

                    EXEC('CREATE view [dbo].[vw_entity_confirmations]
                    as
                    select ent.id enterprise_id, e.id entity_id, ife.value, c.field_id, c.is_mail, c.is_sms
                    from identity_field ife with(nolock) 
                    inner join [identity] i on ife.identity_id = i.id 
                    inner join confirmation_rules c on c.field_id = ife.field_id
                    inner join entity e with(nolock) on i.entity_id = e.id
                    inner join context co on e.context_id = co.id
                    inner join enterprise ent on ent.id = co.enterprise_id
                    where e.deleted = 0
                    group by ent.id, e.id, ife.value,  c.field_id, c.is_mail, c.is_sms
                    UNION
                    select ent.id enterprise_id, e.id entity_id, efe.value, c.field_id, c.is_mail, c.is_sms
                    from entity_field efe with(nolock) 
                    inner join confirmation_rules c on c.field_id = efe.field_id
                    inner join entity e with(nolock) on efe.entity_id = e.id
                    inner join context co on e.context_id = co.id
                    inner join enterprise ent on ent.id = co.enterprise_id
                    where e.deleted = 0
                    group by ent.id, e.id, efe.value,  c.field_id, c.is_mail, c.is_sms
                    ');

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_entity_roles')
                    BEGIN
                        DROP VIEW [vw_entity_roles];
                    END

                    EXEC('CREATE view [dbo].[vw_entity_roles]
                    as
                    select distinct e.*, res.name resource_name, i.id identity_id, r.name from entity e 
                    inner join [identity] i with(nolock)
	                    on i.entity_id = e.id
                    inner join identity_role ir with(nolock)
	                    on ir.identity_id = i.id
                    inner join role r with(nolock)
	                    on r.id = ir.role_id
                    inner join resource_plugin rp with(nolock)
	                    on i.resource_plugin_id = rp.id
                    inner join resource res with(nolock)
	                    on res.id = rp.resource_id
                    where e.deleted = 0
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_entity_logins2')
                    BEGIN
                        DROP VIEW [vw_entity_logins2];
                    END

                    EXEC('CREATE view [dbo].[vw_entity_logins2]
                    as
	                    select e.id, e.context_id, e.login from entity e with(nolock) 

	                    union 

	                    select e.id, e.context_id, ife.value from entity e with(nolock) 
	                    inner join [identity] i with(nolock) on e.id = i.entity_id 
	                    inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id 
	                    inner join identity_field ife with(nolock) on ife.field_id = rp.login_field_id and ife.identity_id = i.id
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_entity_all_data')
                    BEGIN
                        DROP VIEW [vw_entity_all_data];
                    END

                    EXEC('CREATE view [dbo].[vw_entity_all_data]
                    as
                    select e.*, c.enterprise_id, i.id identity_id, f.name, r.name resource_name, ife.field_id, ife.value, rp.id resource_plugin_id, rp.resource_id, rp.plugin_id
                    from entity e  with(nolock)
                    inner join context c with(nolock) on e.context_id = c.id
                    inner join [identity] i with(nolock) on i.entity_id = e.id
                    inner join identity_field ife with(nolock) on ife.identity_id = i.id
                    inner join field f with(nolock) on ife.field_id = f.id
                    inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id
                    inner join resource r with(nolock) on r.id = rp.resource_id

                    UNION

                    select e.*, c.enterprise_id, 0, f.name, ''Entity Data'', efe.field_id, efe.value, 0, 0, 0
                    from entity e  with(nolock)
                    inner join context c with(nolock) on e.context_id = c.id
                    inner join entity_field efe with(nolock) on efe.entity_id = e.id
                    inner join field f with(nolock) on efe.field_id = f.id
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_collector_imports_regs')
                    BEGIN
                        DROP VIEW [vw_collector_imports_regs];
                    END

                    EXEC('CREATE view [dbo].[vw_collector_imports_regs]
                    as

                    /****** Sempre usar  with(nolock) para evitar deadlock no engine ******/

                    select 
	                    ci.*,
	                    c.enterprise_id,
	                    c.id as context_id,
	                    rp.resource_id,
	                    rp.plugin_id,
	                    p.uri plugin_uri,
	                    rp.build_login,
	                    rp.build_mail
                    from collector_imports ci with(nolock)
	                    inner	join resource_plugin rp with(nolock)
		                    on	ci.resource_plugin_id = rp.id
	                    inner	join resource r with(nolock)
		                    on	r.id = rp.resource_id
	                    inner	join plugin p with(nolock)
		                    on	p.id = rp.plugin_id
	                    inner	join context c with(nolock)
		                    on	c.id = r.context_id
                    where ci.[status] = ''F''
                    ');

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_proxy_plugin')
                    BEGIN
                        DROP VIEW [vw_proxy_plugin];
                    END

                    EXEC('create view [dbo].[vw_proxy_plugin]
                    as
                    select		p.scheme,
			                    p.id plugin_id,
			                    r.proxy_id,
			                    rp.id resource_plugin_id,
			                    c.enterprise_id
	                    from	plugin p with(nolock) 
	                    inner	join resource_plugin rp with(nolock) 
		                    on	rp.plugin_id = p.id 
	                    inner	join [resource] r with(nolock) 
		                    on	r.id = rp.resource_id 
	                    inner	join context c with(nolock) 
		                    on	c.id = r.context_id 
	                    where	r.enabled = 1 
		                    and rp.enabled = 1 
	                    group	by 
			                    p.scheme,
			                    p.id,
			                    r.proxy_id,
			                    rp.id,
			                    c.enterprise_id
                    UNION

                    select		p.scheme,
			                    p.id, 
			                    p1.id,
			                    0,
			                    p1.enterprise_id
	                    from	plugin p with(nolock) 
	                    inner	join proxy_plugin pp with(nolock) 
		                    on	pp.plugin_id = p.id 
	                    inner	join proxy p1 
		                    on	p1.id = pp.proxy_id 
	                    where	pp.enabled = 1 
	                    group	by 
			                    p.scheme,
			                    p.id,
			                    p1.id,
			                    p1.enterprise_id
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_entity_mails')
                    BEGIN
                        DROP VIEW [vw_entity_mails];
                    END

                    EXEC('CREATE view [dbo].[vw_entity_mails]
                    as
                    select	c.name context_name, 
		                    c.id context_id,
		                    e.full_name, 
		                    e.login, 
		                    e.create_date, 
		                    e.last_login, 
		                    v.value mail, 
		                    e.locked
                    from entity e with(nolock) 
                    inner join	context c
	                    on	c.id = e.context_id
                    left join (
	                    select i.entity_id, m.data_name, ife.value, f.data_type, r.name, f.name field_name
	                    from identity_field ife with(nolock) 
	                    inner join [identity] i on ife.identity_id = i.id 
	                    inner join entity e with(nolock) on i.entity_id = e.id 
	                    inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id 
	                    inner join resource r with(nolock) on r.id = rp.resource_id and r.context_id = e.context_id 
	                    inner join resource_plugin_mapping m with(nolock) on m.resource_plugin_id = rp.id and m.field_id = ife.field_id 
	                    inner join field f with(nolock) on ife.field_id = f.id
	                    where m.field_id = rp.mail_field_id
                    ) v on v.entity_id = e.id
                    where e.deleted = 0
                    group by c.name, c.id, e.full_name, e.login, e.create_date, e.last_login, v.value, e.locked

                    UNION

                    select		c.name context_name, c.id context_id, e.full_name, e.login, e.create_date, e.last_login, efe.value mail, e.locked
	                    from	entity e
	                    inner	join context c
		                    on	c.id = e.context_id
	                    inner	join entity_field efe
		                    on	efe.entity_id = e.id
                    where e.deleted = 0
                    group by c.name, c.id, e.full_name, e.login, e.create_date, e.last_login, efe.value, e.locked
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_entity_logins')
                    BEGIN
                        DROP VIEW [vw_entity_logins];
                    END

                    EXEC('CREATE view [dbo].[vw_entity_logins]
                    as
                    select	e.*,
		                    c.enterprise_id,
		                    v.data_name, 
		                    v.value, 
		                    v.data_type, 
		                    v.name, 
		                    v.field_name
                    from entity e with(nolock) 
                    inner join	context c with(nolock)
	                    on	c.id = e.context_id
                    left join (
	                    select i.entity_id, m.data_name, ife.value, f.data_type, r.name, f.name field_name
	                    from identity_field ife with(nolock) 
	                    inner join [identity] i with(nolock) on ife.identity_id = i.id 
	                    inner join entity e with(nolock) on i.entity_id = e.id 
	                    inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id 
	                    inner join resource r with(nolock) on r.id = rp.resource_id and r.context_id = e.context_id 
	                    inner join resource_plugin_mapping m with(nolock) on m.resource_plugin_id = rp.id and m.field_id = ife.field_id 
	                    inner join field f with(nolock) on ife.field_id = f.id
	                    where m.field_id = rp.login_field_id or m.field_id = rp.mail_field_id
                    ) v on v.entity_id = e.id
                    where e.deleted = 0
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_entity_ids')
                    BEGIN
                        DROP VIEW [vw_entity_ids];
                    END

                    EXEC('CREATE view [dbo].[vw_entity_ids]
                    as

                    select e.id, c.enterprise_id, e.context_id, e.login value, cast(1 as bit) is_login, cast(0 as bit) is_mail
                    from entity e inner join context c on e.context_id = c.id
                    union
                    select e.id, c.enterprise_id, e.context_id, ife.value, case when m.field_id = rp.login_field_id then cast(1 as bit) else cast(0 as bit) end, case when m.field_id = rp.mail_field_id then cast(1 as bit) else cast(0 as bit) end
                    from identity_field ife with(nolock) 
                    inner join [identity] i on ife.identity_id = i.id 
                    inner join entity e with(nolock) on i.entity_id = e.id 
                    inner join context c on e.context_id = c.id
                    inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id 
                    inner join resource r with(nolock) on r.id = rp.resource_id and r.context_id = e.context_id 
                    inner join resource_plugin_mapping m with(nolock) on m.resource_plugin_id = rp.id and m.field_id = ife.field_id 
                    where m.field_id = rp.login_field_id or m.field_id = rp.mail_field_id
                    or m.is_id = 1 or m.is_unique_property = 1
                    group by e.id, c.enterprise_id, e.login, ife.value, rp.login_field_id, rp.mail_field_id, m.field_id, e.context_id
                    ');


                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_schedules')
                    BEGIN
                        DROP VIEW [vw_schedules];
                    END

                    EXEC('CREATE view [dbo].[vw_schedules]
                    as
                    select rp.id resource_plugin_id, rs.id schedule_id, r.context_id, r.id resource_id, r.proxy_id, rp.plugin_id, rs.schedule, p.[assembly], rs.[next], rp.[order]
                    from resource_plugin_schedule rs 
                    inner join resource_plugin rp on rs.resource_plugin_id = rp.id
                    inner join resource r on rp.resource_id = r.id
                    inner join plugin p on rp.plugin_id = p.id
                    where r.enabled = 1 and rp.enable_deploy = 1 and rp.enabled = 1
                    ');



                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_filters_use')
                    BEGIN
                        DROP VIEW [vw_filters_use];
                    END

                    EXEC('CREATE view [dbo].[vw_filters_use]
                    as
                    select 
	                    rp.id resource_plugin_id, 
	                    c.id context_id,
	                    c.enterprise_id,
	                    r.name + '' x '' + p.name resource_plugin_name, 
	                    f.id filter_id,
	                    count(distinct l.filter_id) lock_qty,
	                    count(distinct i.filter_id) ignore_qty,
	                    count(distinct rf.role_id) role_qty
                    from resource_plugin rp with(nolock)
                    inner join resource r with(nolock) on rp.resource_id = r.id
                    inner join context c with(nolock) on r.context_id = c.id
                    inner join plugin p with(nolock) on rp.plugin_id = p.id
                    inner join filters f with(nolock) on f.enterprise_id = c.enterprise_id
                    left join resource_plugin_lock_filter l with(nolock) on l.resource_plugin_id = rp.id and l.filter_id = f.id
                    left join resource_plugin_ignore_filter i with(nolock) on i.resource_plugin_id = rp.id and i.filter_id = f.id
                    left join resource_plugin_role_filter rf with(nolock) on rf.resource_plugin_id = rp.id and rf.filter_id = f.id
                    group by rp.id, 
	                    c.id,
	                    c.enterprise_id,
	                    r.name + '' x '' + p.name, 
	                    f.id
                    having count(distinct l.filter_id) > 0 or count(distinct i.filter_id) > 0 or count(distinct rf.role_id) > 0
                    ');				

                    INSERT INTO [db_install] ([version]) VALUES (37);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when count(*) = 0 then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 37.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
