using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;

namespace GoogleAdmin
{
    [Serializable()]
    public class GoogleDirectoryResponseBase
    {
        [OptionalField()]
        public GoogleDirectoryResponseError error;
        [OptionalField()]
        public String kind;
        [OptionalField()]
        public String nextPageToken;
    }

    [Serializable()]
    public class GoogleDirectoryResponse : GoogleDirectoryResponseBase
    {
        [OptionalField()]
        public List<GoogleDirectoryUserInfo> users;
    }

    [Serializable()]
    public class GoogleDirectoryUserName
    {
        [OptionalField()]
        public String givenName;
        [OptionalField()]
        public String familyName;

        public String fullName;
    }

    [Serializable()]
    public class GoogleDirectoryUserEmail
    {
        public String address;
        
        [OptionalField()]
        public Boolean primary;
    }
    
    
    [Serializable()]
    public class GoogleDirectoryExternalIds
    {
        [OptionalField()]
        public String value;
        
        [OptionalField()]
        public String type;

        [OptionalField()]
        public String customType;
    }
    

    [Serializable()]
    public class GoogleDirectoryUserInfo : GoogleDirectoryResponseBase
    {
        [OptionalField()]
        public String primaryEmail;
        [OptionalField()]
        public GoogleDirectoryUserName name;
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
        public List<GoogleDirectoryUserEmail> emails;
        [OptionalField()]
        public String customerId;
        [OptionalField()]
        public List<GoogleDirectoryExternalIds> externalIds;
    }

    [Serializable()]
    public class GoogleDirectoryResponseError
    {
        [OptionalField()]
        public List<GoogleDirectoryResponseError> errors;
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
