using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CASPluginIAM
{

    [Serializable()]
    public class APIAuthResult : APIResponseBase
    {
        [OptionalField()]
        public APIAuthKey result;
    }


    [Serializable()]
    public class APIAuthKey
    {
        [OptionalField()]
        public string sessionid;

        [OptionalField()]
        public Int64 expires;

        [OptionalField()]
        public Int64 create_time;

        [OptionalField()]
        public bool success;

        [OptionalField()]
        public Int64 userid;
    }

    [Serializable()]
    public class APIResponseBase
    {
        public string jsonrpc;
        public string id;

        [OptionalField()]
        public APIResponseError error;

    }

    [Serializable()]
    public class APIResponseError
    {
        [OptionalField()]
        public Int32 code;

        [OptionalField()]
        public string data;

        [OptionalField()]
        public string message;

        [OptionalField()]
        public string debug;

        [OptionalField()]
        public Boolean lowercase;
        
        [OptionalField()]
        public Boolean number_char;

        [OptionalField()]
        public Boolean numbers;

        [OptionalField()]
        public Boolean symbols;

        [OptionalField()]
        public Boolean uppercase;

        [OptionalField()]
        public Boolean name_part;
    }

    [Serializable()]
    public class APISearchResult : APIResponseBase
    {
        [OptionalField()]
        public List<APIUserData> result;
    }

    [Serializable()]
    public class APIUserGetResult : APIResponseBase
    {
        [OptionalField()]
        public APIUserGetData result;
    }

    [Serializable()]
    public class APIUserGetData
    {
        [OptionalField()]
        public APIUserData info;

        [OptionalField()]
        public List<APIUserDataProperty> properties;
    }


    [Serializable()]
    public class APIUserAuthResult : APIResponseBase
    {
        [OptionalField()]
        public APIUserAuthData result;
    }


    [Serializable()]
    public class APIUserChangePasswordResult : APIResponseBase
    {
        [OptionalField()]
        public APIUserChangePasswordData result;
    }


    [Serializable()]
    public class APIUserChangePasswordData
    {
        [OptionalField()]
        public bool success;

    }


    [Serializable()]
    public class APIUserAuthData
    {
        [OptionalField()]
        public Int32 userid;

        [OptionalField()]
        public string full_name;

        [OptionalField()]
        public string login;

        [OptionalField()]
        public bool success;

        [OptionalField()]
        public bool must_change;

        [OptionalField()]
        public List<APIRoleData> roles;
    }


    [Serializable()]
    public class APIRoleListResult : APIResponseBase
    {
        [OptionalField()]
        public List<APIRoleData> result;
    }

    [Serializable()]
    public class APIRoleData
    {
        [OptionalField()]
        public string resource_name;

        [OptionalField()]
        public string name;
    }

    [Serializable()]
    public class APIUserData
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
        public Int64 last_login;

    }


    [Serializable()]
    public class APIUserDataProperty
    {
        [OptionalField()]
        public string resource_name;

        [OptionalField()]
        public string name;

        [OptionalField()]
        public string value;
    }

}
