using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;

namespace IAM.GlobalDefs.Tools
{
    static public class Tool
    {
        public static DateTime ParseDate(String date)
        {
            
            Exception ex = null;

            try
            {
                System.Globalization.CultureInfo cultureinfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                return DateTime.Parse(date, cultureinfo);
            }
            catch(Exception iEx) { ex = iEx; }

            try
            {
                System.Globalization.CultureInfo cultureinfo = new System.Globalization.CultureInfo("pt-BR");
                return DateTime.Parse(date, cultureinfo);
            }
            catch(Exception iEx) { ex = iEx; }

            try
            {
                System.Globalization.CultureInfo cultureinfo = new System.Globalization.CultureInfo("en-US");
                return DateTime.Parse(date, cultureinfo);
            }
            catch(Exception iEx) { ex = iEx; }


            throw new Exception("Erro converting '" + date + "' to DateTime", ex);
        }
    }
}
