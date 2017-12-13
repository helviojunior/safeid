using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;

namespace IAM.PluginInterface
{

    public abstract class PluginAgentBase : PluginBase
    {

        public abstract void Start(Dictionary<String, Object> config);
        public abstract void Stop();

    }
}
