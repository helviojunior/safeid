using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;

namespace Zabbix
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


    /* API Ver
    =============================*/
    [Serializable()]
    public class APIVerResult : ResponseBase
    {
        [OptionalField()]
        public String result;
    }


    /* Auth
    =============================*/
    [Serializable()]
    public class AuthResult : ResponseBase
    {
        [OptionalField()]
        public String result;
    }

    /* User
    =============================*/
    [Serializable()]
    public class UserListResult : ResponseBase
    {
        [OptionalField()]
        public List<UserData> result;
    }

    [Serializable()]
    public class UserData
    {
        [OptionalField()]
        public String userid;

        [OptionalField()]
        public String alias;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public String surname;

        [OptionalField()]
        public String url;

        [OptionalField()]
        public String type;

        [OptionalField()]
        public String lang;
        
        [OptionalField()]
        public List<MediaData> medias;

    }

    [Serializable()]
    public class IdsData
    {
        [OptionalField()]
        public List<String> userids;
    }

    [Serializable()]
    public class UserCreateResult : ResponseBase
    {
        [OptionalField()]
        public IdsData result;
    }

    /* Medias
    =============================*/
    [Serializable()]
    public class MediaData
    {
        [OptionalField()]
        public String mediaid;

        [OptionalField()]
        public Int32 active;

        [OptionalField()]
        public String mediatypeid;

        [OptionalField()]
        public String period;

        [OptionalField()]
        public String sendto;

        [OptionalField()]
        public String severity;

        [OptionalField()]
        public String userid;

    }

    /* Groups
    =============================*/
    [Serializable()]
    public class GroupIdsData
    {
        [OptionalField()]
        public List<String> usrgrpids;
    }

    [Serializable()]
    public class GroupCreateResult : ResponseBase
    {
        [OptionalField()]
        public GroupIdsData result;
    }

    [Serializable()]
    public class GroupData
    {
        [OptionalField()]
        public String usrgrpid;

        [OptionalField()]
        public String gui_access;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public String users_status;

        [OptionalField()]
        public String debug_mode;

    }

    [Serializable()]
    public class GroupListResult : ResponseBase
    {
        [OptionalField()]
        public List<GroupData> result;
    }


    /* Depracated ***/

    [Serializable()]
    public class ZabbixDirectoryResponseBase
    {
        [OptionalField()]
        public ZabbixDirectoryResponseError error;
        [OptionalField()]
        public String kind;
        [OptionalField()]
        public String nextPageToken;
    }

    [Serializable()]
    public class ZabbixResponse : ZabbixDirectoryResponseBase
    {
        [OptionalField()]
        public List<ZabbixDirectoryUserInfo> users;
    }

    [Serializable()]
    public class ZabbixDirectoryUserName
    {
        [OptionalField()]
        public String givenName;
        [OptionalField()]
        public String familyName;

        public String fullName;
    }

    [Serializable()]
    public class ZabbixDirectoryUserEmail
    {
        public String address;
        
        [OptionalField()]
        public Boolean primary;
    }
    
    
    [Serializable()]
    public class ZabbixDirectoryExternalIds
    {
        [OptionalField()]
        public String value;
        
        [OptionalField()]
        public String type;

        [OptionalField()]
        public String customType;
    }
    

    [Serializable()]
    public class ZabbixDirectoryUserInfo : ZabbixDirectoryResponseBase
    {
        [OptionalField()]
        public String primaryEmail;
        [OptionalField()]
        public ZabbixDirectoryUserName name;
        [OptionalField()]
        public String orgUnitPath;

        [OptionalField()]
        public String id;
        [OptionalField()]
        public Boolean isAdmin;
        [OptionalField()]
        public String lastLoginTime;
        [OptionalField()]
        public String creationTime;
        [OptionalField()]
        public Boolean suspended;
        [OptionalField()]
        public Boolean changePasswordAtNextLogin;
        [OptionalField()]
        public List<ZabbixDirectoryUserEmail> emails;
        [OptionalField()]
        public String customerId;
        [OptionalField()]
        public List<ZabbixDirectoryExternalIds> externalIds;
    }

    [Serializable()]
    public class ZabbixDirectoryResponseError
    {
        [OptionalField()]
        public List<ZabbixDirectoryResponseError> errors;
        [OptionalField()]
        public String code;
        [OptionalField()]
        public String message;
        [OptionalField()]
        public String domain;
        [OptionalField()]
        public String reason;
    }
}
