using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.WebAPI
{
    public enum ErrorType
    {
        /*NO_METHOD = 100,
		PARAMETERS = 200,
		NO_AUTH = 300,
		PERMISSIONS = 400,
		INTERNAL = 500*/

        ParseError = 100,
        InvalidRequest = 200,
        MethodNotFound = 300,
        InvalidParameters = 400,
        InternalError = 500,
        ApplicationError = 600,
        SystemError = 700,
        TransportError = 800,
        JSPNRPCVersion = 900

    }
}
