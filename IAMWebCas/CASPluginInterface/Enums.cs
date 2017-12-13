using System;
using System.Collections.Generic;
using System.Text;

namespace CAS.PluginInterface
{
    public enum PluginLogType
    {
        Debug = 0,
        Trace = 100,
        Information = 200,
        Warning = 300,
        Error = 400,
        Fatal = 500
    }


    public enum PluginConfigTypes
    {
        String = 1,
        Int32 = 2,
        Int64 = 3,
        DateTime = 4,
        Directory = 5,
        StringFixedList = 6,
        Base64FileData = 7,
        Boolean = 8,
        Uri = 9,
        StringList = 10
    }
}
