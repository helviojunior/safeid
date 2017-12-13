using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace CPanelV2
{
    //{"status":1,"redirect":"/cpsess5542387976/frontend/x3/index.html?post_login=26386455459312","security_token":"/cpsess5542387976"}

    [Serializable()]
    public class cPanelLogin
    {
        [OptionalField()]
        public Int16 status;
        [OptionalField()]
        public String redirect;
        [OptionalField()]
        public String security_token;
        [OptionalField()]
        public String message;
    }

    //{"cpanelresult":{"apiversion":"2","error":"Access denied","data":{"reason":"Access denied","result":"0"},"type":"text"}}
    [Serializable()]
    public class cPanelResultBase
    {
        [OptionalField()]
        public cPanelResult cpanelresult;
    }

    [Serializable()]
    public class cPanelResult
    {
        [OptionalField()]
        public String apiversion;
        [OptionalField()]
        public String error;
        [OptionalField()]
        public String type;
        [OptionalField()]
        public List<cPanelResultData> data;
        
    }

    [Serializable()]
    public class cPanelResultData
    {
        
        [OptionalField()]
        public String reason;
        [OptionalField()]
        public String result;

        [OptionalField()]
        public String _diskquota;
        [OptionalField()]
        public String _diskused;
        [OptionalField()]
        public String diskquota;
        [OptionalField()]
        public Double diskused;
        [OptionalField()]
        public Int32 diskusedpercent;
        [OptionalField()]
        public Int32 diskusedpercent20;
        [OptionalField()]
        public String domain;
        [OptionalField()]
        public String email;
        [OptionalField()]
        public String humandiskquota;
        [OptionalField()]
        public String humandiskused;
        [OptionalField()]
        public String login;
        [OptionalField()]
        public String mtime;
        [OptionalField()]
        public String txtdiskquota;
        [OptionalField()]
        public String user;
    }

}
