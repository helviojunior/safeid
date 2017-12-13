using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.CodeManager
{
    public delegate void CodeLogEvent(Object sender, CodePluginLogType type, String text);

    public enum CodePluginLogType
    {
        Debug = 0,
        Trace = 100,
        Information = 200,
        Warning = 300,
        Error = 400,
        Fatal = 500
    }


    public enum CodePluginConfigTypes
    {
        String = 1,
        Int32 = 2,
        Int64 = 3,
        DateTime = 4,
        Base64FileData = 5,
        Boolean = 6,
        Uri = 7,
        Password = 8
    }

}
