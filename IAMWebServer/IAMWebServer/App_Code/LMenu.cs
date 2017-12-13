using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IAMWebServer
{
    public class LMenu
    {
        public String Name;
        public String HRef;

        public LMenu(String Name, String HRef)
        {
            this.Name = Name;
            this.HRef = HRef;

        }
    }
}