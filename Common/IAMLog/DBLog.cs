using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.Log
{
    public class DBLog
    {
        public static void Log(String Source, String text)
        {
            Log(Source, null, text);
        }

        public static void Log(String Source, String submodule, String text)
        {
            /* Date
             * Source
             * Prioridade (Error, warning, etc...)
             * Proxy
             * Enterprise
             * Resource
             * Plugin
             * Entity
             * Identity
             * Text
             * Additional JSON Data
             */

        }
    }
}
