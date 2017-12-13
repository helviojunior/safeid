using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.AuthPlugins
{

    public enum AuthEventType
    {
        Debug = 0,
        Trace = 100,
        Information = 200,
        Warning = 300,
        Error = 400,
        Fatal = 500
    }


    public enum AuthConfigTypes
    {
        String = 1,
        Int32,
        Int64,
        DateTime,
        Boolean,
        Uri,
        Password
    }


}
