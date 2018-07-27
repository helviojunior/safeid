using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SafeTrend.Json;

namespace IAMWebServer
{
    [Serializable()]
    public class CSharpShellResponse
    {
        public String session;
        public String data;

        public CSharpShellResponse()
        {
        }

        public CSharpShellResponse(String session, String data)
        {
            this.session = session;
            this.data = data;
        }

        public String ToJSON()
        {
            return JSON.Serialize<CSharpShellResponse>(this);
        }

    }

}