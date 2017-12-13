using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Resources;
using System.Threading;
using System.Globalization;

using System.Reflection;
using System.Collections;


namespace IAM.GlobalDefs
{
    public static class MessageResource
    {
        private static ResourceManager rm = null;
        private static CultureInfo ci = null;

        static public String FormatDate(DateTime date, Boolean isShort)
        {
            return date.ToShortDateString() + " " + (!isShort ? date.ToString("HH:mm:ss") : "");
        }

        static public String FormatTime(DateTime time)
        {
            return time.ToString("HH:mm:ss");
        }

        static public String FormatTs(TimeSpan ts)
        {
            String text = "";

            if ((Int32)ts.TotalDays > 0)
            {
                text += (Int32)ts.TotalDays + "d ";

                if ((Int32)ts.Hours > 0)
                    text += (Int32)ts.Hours + "h ";

                if ((Int32)ts.Minutes > 0)
                    text += (Int32)ts.Minutes + "m ";
            }
            else if ((Int32)ts.TotalHours > 0)
            {
                text += (Int32)ts.TotalHours + "h ";

                if ((Int32)ts.Minutes > 0)
                    text += (Int32)ts.Minutes + "m ";
            }
            else
            {
                text += (Int32)ts.TotalMinutes + "m ";
                text += (Int32)ts.Seconds + "s ";
            }

            return text;
        }

        static public String GetMessage(String key)
        {
            return GetMessage(key, "");
        }

        static public String GetMessage(String key, String alternate)
        {
            if (rm == null)
                rm = new ResourceManager("IAM.GlobalDefs.Languages.Strings", System.Reflection.Assembly.GetExecutingAssembly());

            if (ci == null)
                ci = Thread.CurrentThread.CurrentCulture;

            String ret = "";
            try
            {
                ret = rm.GetString(key, ci);
            }
            catch { }

            if (String.IsNullOrEmpty(ret))
                ret = alternate; 

            return ret;
        }
    }
}
