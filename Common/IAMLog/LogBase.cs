using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SafeTrend.Json;

namespace IAM.Log
{
    [Serializable()]
    public abstract class LogBase
    {
        public DateTime Date { get; internal set;}
        public String Source { get; internal set;}
        public Int32 Level { get; internal set;}
        public Int64 Proxy { get; internal set;}
        public String ProxyName { get; internal set; }
        public Int64 Enterprise { get; internal set;}
        public Int64 Context { get; internal set; }
        public Int64 Resource { get; internal set; }
        public Int64 Plugin { get; internal set; }
        public String PluginUri { get; internal set; }
        public Int64 Entity { get; internal set; }
        public Int64 Identity { get; internal set; }
        public String Text { get; internal set;}
        public String Additional { get; internal set;}

        public LogBase()
        {

        }

        public LogBase(String jsonData)
        {
            LogBase item = JSON.Deserialize<LogBase>(jsonData);
            this.Date = item.Date;
            this.Source = item.Source;
            this.Level = item.Level;
            this.Proxy = item.Proxy;
            this.ProxyName = item.ProxyName;
            this.Enterprise = item.Enterprise;
            this.Context = item.Context;
            this.Resource = item.Resource;
            this.Plugin = item.Plugin;
            this.PluginUri = item.PluginUri;
            this.Entity = item.Entity;
            this.Identity = item.Identity;
            this.Text = item.Text;
            this.Additional = item.Additional;
        }

        public String ToJson()
        {
            return JSON.Serialize<LogBase>(this);
        }

    }
}
