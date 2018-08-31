using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace SafeTrend.Xml
{

    public class ResultEmptyException : Exception
    {
        public ResultEmptyException()
            : base() { }

        public ResultEmptyException(string message)
            : base(message) { }

        public ResultEmptyException(string message, Exception innerException)
            : base(message, innerException) { }

    }

}