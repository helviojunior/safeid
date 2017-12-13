using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.UserProcess
{

    public class LoginCache
    {
        private static BaseCache instance { get; set; }

        public static void AddItem(Int64 context_id, Int64 entity_id, String value)
        {
            if (instance == null)
                instance = new BaseCache();

            instance.AddItem(context_id, entity_id, value);
        }

        public static Boolean Exists(Int64 context_id, Int64 entity_id, String value)
        {
            if (instance == null)
                instance = new BaseCache();

            return instance.Exists(context_id, entity_id, value);
        }

        public static Boolean HasOther(Int64 context_id, Int64 entity_id, String value)
        {
            if (instance == null)
                instance = new BaseCache();

            return instance.HasOther(context_id, entity_id, value);
        }

        public static void Clear()
        {
            if (instance != null)
                instance.Clear();
        }
    }
}
