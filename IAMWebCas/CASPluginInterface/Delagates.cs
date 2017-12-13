using System;
using System.Collections.Generic;
using System.Text;

namespace CAS.PluginInterface
{

    public delegate void LogEvent(Object sender, PluginLogType type, String text);
    public delegate void LogEvent2(Object sender, PluginLogType type, String text, String additionalData);

}
