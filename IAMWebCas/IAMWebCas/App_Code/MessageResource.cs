using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Resources;
using System.Threading;
using System.Globalization;

using System.Reflection;
using System.Collections;


namespace CAS.Web
{
    public static class MessageResource
    {
        private static ResourceManager rm = null;
        private static CultureInfo ci = null;

        static public String GetMessage(String key)
        {
            if (rm == null)
                rm = new ResourceManager("IAMWebServer.Languages.Strings", System.Reflection.Assembly.GetExecutingAssembly());

            if (ci == null)
                ci = Thread.CurrentThread.CurrentCulture;

            String ret = "";
            try
            {
                ret = rm.GetString(key, ci);
            }
            catch { }

            return ret;
        }
    }
}
