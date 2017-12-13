using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IAM.GlobalDefs.WebApi
{
    /* General
    =============================*/
    [Serializable()]
    public class ResponseError
    {
        [OptionalField()]
        public Int32 code;

        [OptionalField()]
        public string data;

        [OptionalField()]
        public string message;
        
        [OptionalField()]
        public string debug;

    }

    [Serializable()]
    public class ResponseBase
    {
        public string jsonrpc;
        public string id;

        [OptionalField()]
        public ResponseError error;

    }


    [Serializable()]
    public class BooleanResult : ResponseBase
    {
        [OptionalField()]
        public Boolean result;
    }


    /* Auth
    =============================*/
    [Serializable()]
    public class AuthKey
    {
        [OptionalField()]
        public string sessionid;

        [OptionalField()]
        public Int32 expires;

        [OptionalField()]
        public bool success;
    }

    [Serializable()]
    public class AuthResult : ResponseBase
    {
        [OptionalField()]
        public AuthKey result;
    }


    /* User
    =============================*/
    [Serializable()]
    public class UserData
    {
        [OptionalField()]
        public Int32 userid;

        [OptionalField()]
        public string alias;

        [OptionalField()]
        public string full_name;
        
        [OptionalField()]
        public string login;
        
        [OptionalField()]
        public bool must_change_password;
        
        [OptionalField()]
        public Int32 change_password;

        [OptionalField()]
        public Int32 create_date;

        [OptionalField()]
        public bool locked;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public String context_name;

        [OptionalField()]
        public Int64 last_login;
        
        [OptionalField()]
        public Int32 identity_qty;

        [OptionalField()]
        public Int64 container_id;
        
    }

    [Serializable()]
    public class UserDataProperty
    {
        [OptionalField()]
        public string resource_name;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public Int64 field_id;

        [OptionalField()]
        public string value;
    }


    [Serializable()]
    public class UserDataGeneral
    {
        [OptionalField()]
        public string enterprise_name;

        [OptionalField()]
        public string context_name;

        [OptionalField()]
        public string container_path;
    }

    [Serializable()]
    public class UserDataRole
    {
        [OptionalField()]
        public string resource_name;

        [OptionalField()]
        public string name;
    }

    [Serializable()]
    public class UserDataIdentity
    {
        [OptionalField()]
        public Int64 identity_id;

        [OptionalField()]
        public string resource_name;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public Boolean temp_locked;
    }


    [Serializable()]
    public class GetResult2
    {
        [OptionalField()]
        public UserData info;

        [OptionalField()]
        public UserDataGeneral general;
        
        [OptionalField()]
        public List<UserDataProperty> properties;

        [OptionalField()]
        public List<UserDataRole> roles;

        [OptionalField()]
        public List<UserDataIdentity> identities;
        
    }

    [Serializable()]
    public class GetResult : ResponseBase
    {
        [OptionalField()]
        public GetResult2 result;
    }

    [Serializable()]
    public class SearchResult : ResponseBase
    {
        [OptionalField()]
        public List<UserData> result;
    }


    /* Logs
    =============================*/
    [Serializable()]
    public class LogItem
    {

        [OptionalField()]
        public String log_id;

        [OptionalField()]
        public Int64 date;

        [OptionalField()]
        public string source;

        [OptionalField()]
        public Int32 level;

        [OptionalField()]
        public Int64 identity_id;

        [OptionalField()]
        public string resource_name;

        [OptionalField()]
        public string plugin_name;

        [OptionalField()]
        public string text;

        [OptionalField()]
        public string additional_data;


        [OptionalField()]
        public Int64 executed_by_entity_id;

        [OptionalField()]
        public string executed_by_name;

    }

    [Serializable()]
    public class Logs2
    {
        [OptionalField()]
        public UserData info;

        [OptionalField()]
        public List<LogItem> logs;

    }


    [Serializable()]
    public class LogData : ResponseBase
    {
        [OptionalField()]
        public LogItem result;
    }


    [Serializable()]
    public class SystemLogs : ResponseBase
    {
        [OptionalField()]
        public List<LogItem> result;
    }


    [Serializable()]
    public class Logs : ResponseBase
    {
        [OptionalField()]
        public Logs2 result;
    }


    /* License
    =============================*/
    [Serializable()]
    public class LicenseInfo
    {
        [OptionalField()]
        public Boolean hasLicense;

        [OptionalField()]
        public Int32 used;

        [OptionalField()]
        public Int32 available;

    }


    [Serializable()]
    public class License : ResponseBase
    {
        [OptionalField()]
        public LicenseInfo result;
    }

    /* Context
    =============================*/
    [Serializable()]
    public class ContextData
    {

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public Int32 entity_qty;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public String password_rule;

        [OptionalField()]
        public Int32 auth_key_time;

        [OptionalField()]
        public Int32 password_length;

        [OptionalField()]
        public Boolean password_upper_case;

        [OptionalField()]
        public Boolean password_lower_case;

        [OptionalField()]
        public Boolean password_digit;

        [OptionalField()]
        public Boolean password_symbol;

        [OptionalField()]
        public Boolean password_no_name;

    }


    [Serializable()]
    public class ContextGetResult : ResponseBase
    {
        [OptionalField()]
        public ContextFullData result;
    }


    [Serializable()]
    public class ContextListResult : ResponseBase
    {
        [OptionalField()]
        public List<ContextData> result;
    }

    [Serializable()]
    public class ContextLoginMailRulesResult : ResponseBase
    {
        [OptionalField()]
        public List<ContextLoginMailRulesData> result;
    }


    [Serializable()]
    public class ContextFullData
    {
        [OptionalField()]
        public ContextData info;

    }

    [Serializable()]
    public class ContextLoginMailRulesData
    {
        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public Int64 rule_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public String rule;

        [OptionalField()]
        public Int32 order;

    }

    /* Container
    =============================*/
    [Serializable()]
    public class ContainerData
    {

        [OptionalField()]
        public Int64 container_id;

        [OptionalField()]
        public Int64 parent_id;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public String context_name;

        [OptionalField()]
        public String path;

        [OptionalField()]
        public Int32 entity_qty;

        [OptionalField()]
        public Int64 create_date;
        
        [OptionalField()]
        public List<ContainerData> chields;
        
    }


    [Serializable()]
    public class ContainerDeleteResult : ResponseBase
    {
        [OptionalField()]
        public Boolean result;
    }


    [Serializable()]
    public class ContainerGetResult : ResponseBase
    {
        [OptionalField()]
        public ContainerFullData result;
    }


    [Serializable()]
    public class ContainerListResult : ResponseBase
    {
        [OptionalField()]
        public List<ContainerData> result;
    }


    [Serializable()]
    public class ContainerFullData
    {
        [OptionalField()]
        public ContainerData info;
        
    }

    /* Role
    =============================*/
    [Serializable()]
    public class RoleFullData
    {
        [OptionalField()]
        public RoleData info;

        [OptionalField()]
        public UserDataGeneral general;

        [OptionalField()]
        public List<UserDataProperty> properties;

        [OptionalField()]
        public List<UserDataRole> roles;

    }


    [Serializable()]
    public class RoleData
    {
        [OptionalField()]
        public Int64 role_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public Int64 parent_id;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public Int32 entity_qty;

        [OptionalField()]
        public Int64 create_date;

    }


    [Serializable()]
    public class RoleListResult : ResponseBase
    {
        [OptionalField()]
        public List<RoleData> result;
    }


    [Serializable()]
    public class RoleGetResult : ResponseBase
    {
        [OptionalField()]
        public RoleFullData result;
    }

    [Serializable()]
    public class RoleDeleteResult : ResponseBase
    {
        [OptionalField()]
        public Boolean result;
    }


    /* Enterprise
    =============================*/
    [Serializable()]
    public class EnterpriseGetResult : ResponseBase
    {
        [OptionalField()]
        public EnterpriseFullData result;
    }


    [Serializable()]
    public class EnterpriseFullData
    {
        [OptionalField()]
        public EnterpriseData2 info;

        [OptionalField()]
        public List<String> fqdn_alias;

        [OptionalField()]
        public List<EnterpriseAuthPars> auth_parameters;

    }

    [Serializable()]
    public class EnterpriseData2
    {

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public String fqdn;

        [OptionalField()]
        public String server_cert;

        [OptionalField()]
        public String language;

        [OptionalField()]
        public String auth_plugin;

        [OptionalField()]
        public Int64 create_date;

    }
    
    [Serializable()]
    public class EnterpriseAuthPars
    {
        [OptionalField()]
        public String key;

        [OptionalField()]
        public String value;

    }


    /* Plugin
    =============================*/
    [Serializable()]
    public class PluginFullData
    {
        [OptionalField()]
        public PluginData info;

        [OptionalField()]
        public List<PluginParamterData> parameters;

        [OptionalField()]
        public List<PluginActionData> actions;
        
    }

    [Serializable()]
    public class PluginData
    {
        [OptionalField()]
        public Int64 plugin_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public string scheme;

        [OptionalField()]
        public string uri;

        [OptionalField()]
        public string assembly;

        [OptionalField()]
        public Int32 resource_plugin_qty;

        [OptionalField()]
        public Int64 create_date;

    }

    [Serializable()]
    public class PluginActionData
    {
        [OptionalField()]
        public string name;

        [OptionalField()]
        public string key;

        [OptionalField()]
        public string description;

        [OptionalField()]
        public string field_name;

        [OptionalField()]
        public string field_key;

        [OptionalField()]
        public string field_description;

        [OptionalField()]
        public List<String> macros;

    }

    [Serializable()]
    public class PluginParamterData
    {
        [OptionalField()]
        public string name;

        [OptionalField()]
        public string key;

        [OptionalField()]
        public string description;

        [OptionalField()]
        public string default_value;

        [OptionalField()]
        public string type;

        [OptionalField()]
        public Boolean import_required;

        [OptionalField()]
        public Boolean deploy_required;

        [OptionalField()]
        public List<String> list_value;

    }

    [Serializable()]
    public class PluginListResult : ResponseBase
    {
        [OptionalField()]
        public List<PluginData> result;
    }


    [Serializable()]
    public class PluginGetResult : ResponseBase
    {
        [OptionalField()]
        public PluginFullData result;
    }

    [Serializable()]
    public class PluginDeleteResult : ResponseBase
    {
        [OptionalField()]
        public Boolean result;
    }


    /* Proxy
    =============================*/
    [Serializable()]
    public class ProxyGetResult : ResponseBase
    {
        [OptionalField()]
        public ProxyFullData result;
    }

    [Serializable()]
    public class ProxyFullData
    {
        [OptionalField()]
        public ProxyData info;

    }


    [Serializable()]
    public class ProxyListResult : ResponseBase
    {
        [OptionalField()]
        public List<ProxyData> result;
    }


    [Serializable()]
    public class ProxyData
    {
        [OptionalField()]
        public Int64 proxy_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public Int64 last_sync;

        [OptionalField()]
        public string last_sync_address;

        [OptionalField()]
        public string last_sync_version;

        [OptionalField()]
        public Int32 resource_qty;

    }


    /* Resource
    =============================*/
    [Serializable()]
    public class ResourceGetResult : ResponseBase
    {
        [OptionalField()]
        public ResourceFullData result;
    }

    [Serializable()]
    public class ResourceFullData
    {
        [OptionalField()]
        public ResourceData info;

    }


    [Serializable()]
    public class ResourceListResult : ResponseBase
    {
        [OptionalField()]
        public List<ResourceData> result;
    }


    [Serializable()]
    public class ResourceData
    {
        [OptionalField()]
        public Int64 resource_id;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public Int64 proxy_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public Boolean enabled;

        [OptionalField()]
        public Int32 resource_plugin_qty;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public String proxy_name;

        [OptionalField()]
        public String context_name;

    }


    [Serializable()]
    public class ResourceDeleteResult : ResponseBase
    {
        [OptionalField()]
        public Boolean result;
    }

    /* Resource x Plugin
    =============================*/
    [Serializable()]
    public class ResourcePluginGetResult : ResponseBase
    {
        [OptionalField()]
        public ResourcePluginFullData result;
    }

    [Serializable()]
    public class ResourcePluginFullData
    {
        [OptionalField()]
        public ResourcePluginData info;

        [OptionalField()]
        public ResourcePluginRelatedNames related_names;

        [OptionalField()]
        public ResourcePluginCheckConfig check_config;
    }


    [Serializable()]
    public class ResourcePluginListResult : ResponseBase
    {
        [OptionalField()]
        public List<ResourcePluginFullData> result;
    }

    [Serializable()]
    public class ResourcePluginParamterList : ResponseBase
    {
        [OptionalField()]
        public List<ResourcePluginParameter> result;
    }

    [Serializable()]
    public class ResourcePluginMappingList : ResponseBase
    {
        [OptionalField()]
        public List<ResourcePluginMapping> result;
    }

    [Serializable()]
    public class ResourcePluginFilterList : ResponseBase
    {
        [OptionalField()]
        public List<ResourcePluginFilter> result;
    }

    [Serializable()]
    public class ResourcePluginScheduleList : ResponseBase
    {
        [OptionalField()]
        public List<Scheduler.Schedule> result;
    }

    [Serializable()]
    public class ResourcePluginRoleList : ResponseBase
    {
        [OptionalField()]
        public List<ResourcePluginRole> result;
    }

    [Serializable()]
    public class ResourcePluginFetchList : ResponseBase
    {
        [OptionalField()]
        public List<ResourcePluginFetchData> result;
    }


    [Serializable()]
    public class ResourcePluginData
    {
        [OptionalField()]
        public Int64 resource_plugin_id;

        [OptionalField()]
        public Int64 resource_id;

        [OptionalField()]
        public Int64 plugin_id;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public String context_name;

        [OptionalField()]
        public String name;
        
        [OptionalField()]
        public String mail_domain;

        [OptionalField()]
        public Int32 order;

        [OptionalField()]
        public Boolean permit_add_entity;

        [OptionalField()]
        public Boolean enabled;

        [OptionalField()]
        public Boolean resource_enabled;

        [OptionalField()]
        public Boolean build_login;

        [OptionalField()]
        public Boolean build_mail;

        [OptionalField()]
        public Boolean enable_import;

        [OptionalField()]
        public Boolean enable_deploy;

        [OptionalField()]
        public Boolean deploy_after_login;

        [OptionalField()]
        public Boolean password_after_login;

        [OptionalField()]
        public Boolean deploy_process;

        [OptionalField()]
        public Boolean deploy_all;

        [OptionalField()]
        public Boolean import_groups;

        [OptionalField()]
        public Boolean import_containers;

        [OptionalField()]
        public Int64 name_field_id;

        [OptionalField()]
        public Int64 mail_field_id;

        [OptionalField()]
        public Int64 login_field_id;

        [OptionalField()]
        public String deploy_password_hash;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public Int64 proxy_last_sync;

        [OptionalField()]
        public Int32 identity_qty;

        [OptionalField()]
        public String plugin_uri;

        [OptionalField()]
        public String plugin_scheme;

        [OptionalField()]
        public String password_salt;

        [OptionalField()]
        public Boolean use_password_salt;

        [OptionalField()]
        public Boolean password_salt_end;
        
    }

    [Serializable()]
    public class ResourcePluginFetchData
    {

        [OptionalField()]
        public Int64 fetch_id;

        [OptionalField()]
        public Int64 request_date;

        [OptionalField()]
        public Int64 response_date;

        [OptionalField()]
        public Boolean success;

        [OptionalField()]
        public String logs;

        [OptionalField()]
        public List<ResourcePluginFetchField> fetch_fields;

    }

    [Serializable()]
    public class ResourcePluginFetchField
    {

        [OptionalField()]
        public String key;

        [OptionalField()]
        public List<string> sample_data;

    }


    [Serializable()]
    public class ResourcePluginCheckConfig
    {

        [OptionalField()]
        public Boolean general;

        [OptionalField()]
        public Boolean plugin_par;

        [OptionalField()]
        public Boolean mapping;

        [OptionalField()]
        public List<String> error_messages;

    }

    [Serializable()]
    public class ResourcePluginRelatedNames
    {
        [OptionalField()]
        public String resource_name;

        [OptionalField()]
        public String plugin_name;

        [OptionalField()]
        public String name_field_name;

        [OptionalField()]
        public String mail_field_name;

        [OptionalField()]
        public String login_field_name;

    }

    [Serializable()]
    public class ResourcePluginParameter
    {
        [OptionalField()]
        public String key;

        [OptionalField()]
        public String value;
    }


    [Serializable()]
    public class ResourcePluginFilter
    {
        [OptionalField()]
        public String filter_name;

        [OptionalField()]
        public Int64 filter_id;

        [OptionalField()]
        public String conditions_description;

    }


    [Serializable()]
    public class ResourcePluginMapping
    {
        [OptionalField()]
        public String data_name;

        [OptionalField()]
        public Int64 field_id;

        [OptionalField()]
        public String field_name;

        [OptionalField()]
        public String field_data_type;

        [OptionalField()]
        public Boolean is_id;

        [OptionalField()]
        public Boolean is_password;

        [OptionalField()]
        public Boolean is_property;

        [OptionalField()]
        public Boolean is_unique_property;

    }


    [Serializable()]
    public class ResourcePluginRole
    {
        [OptionalField()]
        public Int64 role_id;

        [OptionalField()]
        public String role_name;

        [OptionalField()]
        public List<ResourcePluginRoleAction> actions;

        [OptionalField()]
        public List<TimeACL.TimeAccess> time_acl;

        [OptionalField()]
        public List<ResourcePluginFilter> filters;
    }

    [Serializable()]
    public class ResourcePluginRoleAction
    {
        [OptionalField()]
        public String action_key;

        [OptionalField()]
        public String action_add_value;

        [OptionalField()]
        public String action_del_value;

        [OptionalField()]
        public String additional_data;

    }


    [Serializable()]
    public class ResourcePluginTFResult : ResponseBase
    {
        [OptionalField()]
        public Boolean result;
    }

    /* Field
    =============================*/
    [Serializable()]
    public class FieldGetResult : ResponseBase
    {
        [OptionalField()]
        public FieldFullData result;
    }

    [Serializable()]
    public class FieldFullData
    {
        [OptionalField()]
        public FieldData info;

    }


    [Serializable()]
    public class FieldListResult : ResponseBase
    {
        [OptionalField()]
        public List<FieldData> result;
    }


    [Serializable()]
    public class FieldData
    {
        [OptionalField()]
        public Int64 field_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public String data_type;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public Boolean public_field;

        [OptionalField()]
        public Boolean user_field;
        
    }

    [Serializable()]
    public class FieldDeleteResult : ResponseBase
    {
        [OptionalField()]
        public Boolean result;
    }


    /* System SystemRole
    =============================*/
    [Serializable()]
    public class SystemRoleFullData
    {
        [OptionalField()]
        public SystemRoleData info;
    }


    [Serializable()]
    public class SystemRoleData
    {
        [OptionalField()]
        public Int64 role_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public Int64 parent_id;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public Int32 entity_qty;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public Boolean enterprise_admin;

        [OptionalField()]
        public List<SystemRolePermission> permissions;

    }

    [Serializable()]
    public class SystemRolePermission
    {
        [OptionalField()]
        public Int64 permission_id;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public string key;

        [OptionalField()]
        public String module_name;

        [OptionalField()]
        public String sub_module_name;

    }


    [Serializable()]
    public class SystemRoleListResult : ResponseBase
    {
        [OptionalField()]
        public List<SystemRoleData> result;
    }


    [Serializable()]
    public class SystemRoleGetResult : ResponseBase
    {
        [OptionalField()]
        public SystemRoleFullData result;
    }

    [Serializable()]
    public class SystemRoleDeleteResult : ResponseBase
    {
        [OptionalField()]
        public Boolean result;
    }

    [Serializable()]
    public class SystemRolePermissionsTree : ResponseBase
    {
        [OptionalField()]
        public List<SystemRolePermissionModule> result;
    }


    [Serializable()]
    public class SystemRolePermissionModule
    {
        [OptionalField()]
        public Int64 module_id;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public string key;

        [OptionalField()]
        public List<SystemRolePermissionSubModule> submodules;
    }

    [Serializable()]
    public class SystemRolePermissionSubModule
    {
        [OptionalField()]
        public Int64 submodule_id;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public string key;

        [OptionalField()]
        public String api_module;

        [OptionalField()]
        public List<SystemRolePermissionItem> permissions;
    }

    [Serializable()]
    public class SystemRolePermissionItem
    {
        [OptionalField()]
        public Int64 permission_id;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public string key;

    }


    /* Filters
    =============================*/
    [Serializable()]
    public class FilterGetResult : ResponseBase
    {
        [OptionalField()]
        public FilterFullData result;
    }

    [Serializable()]
    public class FilterFullData
    {
        [OptionalField()]
        public FilterData info;

    }


    [Serializable()]
    public class FilterListResult : ResponseBase
    {
        [OptionalField()]
        public List<FilterData> result;
    }

    [Serializable()]
    public class FilterUseResult : ResponseBase
    {
        [OptionalField()]
        public List<FilterUseData> result;
    }


    [Serializable()]
    public class FilterData
    {
        [OptionalField()]
        public Int64 filter_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public Int32 ignore_qty;

        [OptionalField()]
        public Int32 lock_qty;

        [OptionalField()]
        public Int32 role_qty;

        [OptionalField()]
        public String conditions_description;

        [OptionalField()]
        public List<FilterCondition> conditions;
    }


    [Serializable()]
    public class FilterUseData
    {
        [OptionalField()]
        public Int64 filter_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public String resource_plugin_name;

        [OptionalField()]
        public Int64 resource_plugin_id;

        [OptionalField()]
        public Int32 ignore_qty;

        [OptionalField()]
        public Int32 lock_qty;

        [OptionalField()]
        public Int32 role_qty;

    }


    [Serializable()]
    public class FilterCondition
    {
        [OptionalField()]
        public Int64 group_id;

        [OptionalField()]
        public String group_selector;

        [OptionalField()]
        public Int64 field_id;

        [OptionalField()]
        public String field_name;

        [OptionalField()]
        public String data_type;

        [OptionalField()]
        public String condition;
        
        [OptionalField()]
        public String selector;

        [OptionalField()]
        public String text;

    }



    /* Workflow
    =============================*/
    [Serializable()]
    public class WorkflowGetResult : ResponseBase
    {
        [OptionalField()]
        public WorkflowFullData result;
    }

    [Serializable()]
    public class WorkflowFullData
    {
        [OptionalField()]
        public WorkflowData info;

    }


    [Serializable()]
    public class WorkflowListResult : ResponseBase
    {
        [OptionalField()]
        public List<WorkflowData> result;
    }


    [Serializable()]
    public class WorkflowData
    {
        [OptionalField()]
        public Int64 workflow_id;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public String description;

        [OptionalField()]
        public Int64 owner_id;

        [OptionalField()]
        public Boolean enabled;

        [OptionalField()]
        public Boolean deleted;

        [OptionalField()]
        public Boolean deprecated;

        [OptionalField()]
        public Int64 original_id;

        [OptionalField()]
        public Int64 version;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public Int32 request_qty;

        [OptionalField()]
        public WorkflowDataAccess access;

        [OptionalField()]
        public List<WorkflowDataActivity> activities;
    }


    [Serializable()]
    public class WorkflowDataAccess
    {
        [OptionalField()]
        public Int64 entity_id;

        [OptionalField()]
        public List<Int64> role_id;

    }

    [Serializable()]
    public class WorkflowDataActivity
    {
        [OptionalField()]
        public Int64 activity_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public Int32 escalation_days;

        [OptionalField()]
        public Int32 expiration_days;
        
        [OptionalField()]
        public Int64 auto_deny;

        [OptionalField()]
        public Int64 auto_approval;

        [OptionalField()]
        public WorkflowDataActivityManualApproval manual_approval;

    }

    [Serializable()]
    public class WorkflowDataActivityManualApproval
    {
        [OptionalField()]
        public Int64 role_approver;

        [OptionalField()]
        public Int64 entity_approver;

    }

    /*
                a.Add("activity_id", activity.ActivityId);
                a.Add("name", activity.Name);
                a.Add("escalation_days", activity.EscalationDays);
                a.Add("expiration_days", activity.ExpirationDays);
                a.Add("auto_deny", activity.AutoDeny);
                a.Add("auto_approval", activity.AutoApproval);

                if (activity.ManualApproval != null && (activity.ManualApproval.EntityApprover != 0 || activity.ManualApproval.RoleApprover != 0))
                {
                    Dictionary<string, object> manual_approval = new Dictionary<string, object>();

                    manual_approval.Add("entity_approver", activity.ManualApproval.EntityApprover);
                    manual_approval.Add("role_approver", activity.ManualApproval.RoleApprover);

                    a.Add("manual_approval", manual_approval);
                }
*/

    /* Workflow request
    =============================*/
    [Serializable()]
    public class WorkflowRequestGetResult : ResponseBase
    {
        [OptionalField()]
        public WorkflowRequestFullData result;
    }

    [Serializable()]
    public class WorkflowRequestFullData
    {
        [OptionalField()]
        public WorkflowRequestData info;

    }


    [Serializable()]
    public class WorkflowRequestListResult : ResponseBase
    {
        [OptionalField()]
        public List<WorkflowRequestData> result;
    }


    [Serializable()]
    public class WorkflowRequestData
    {
        [OptionalField()]
        public Int64 access_request_id;

        [OptionalField()]
        public Int64 entity_id;

        [OptionalField()]
        public Int64 context_id;

        [OptionalField()]
        public Int64 enterprise_id;

        [OptionalField()]
        public Int64 workflow_id;

        [OptionalField()]
        public Int32 status;

        [OptionalField()]
        public String description;

        [OptionalField()]
        public String entity_full_name;

        [OptionalField()]
        public String entity_login;

        [OptionalField()]
        public Int64 start_date;

        [OptionalField()]
        public Int64 end_date;

        [OptionalField()]
        public Int64 create_date;

        [OptionalField()]
        public WorkflowData workflow;

        [OptionalField()]
        public Boolean deployed;

    }

}
