using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;

namespace CAS.PluginInterface
{

    [Serializable()]
    public class CASJsonBase
    {

        public static String Serialize<T>(T obj)
        {
            String ret = "";

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));

            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, obj);
                ms.Flush();
                ret = Encoding.UTF8.GetString(ms.ToArray());
            }

            return ret;
        }


        public static T Deserialize<T>(String jsonText)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonText)))
                return (T)ser.ReadObject(ms);
        }


    }
}
