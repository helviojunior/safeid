using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    public enum PluginActionType
    {
        Add = 100,
        Remove = 200
    }

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
        StringList = 10,
        Password = 11
    }


    public enum HashAlg
    {
        None = 0,
        MD5 = 2,
        SHA1 = 3,
        SHA256 = 4,
        SHA512 = 5,
    }
}
