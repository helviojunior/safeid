using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SafeTrend.WebAPI
{
    [Serializable()]
    public class ErrorData
    {
        public Int32 code;
        public String message;
        public String data;
        public String debug;
    }
}
