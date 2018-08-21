using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace IAM.PluginManager
{
    public static class Plugins
    {
        public static List<T> GetPlugins<T>()
        {

            //string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            string folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return GetPlugins<T>(folder);

        }

        public static List<T> GetPlugins<T>(Byte[] rawAssembly)
        {
            List<T> tList = new List<T>();

            try
            {
                Assembly assembly = Assembly.Load(rawAssembly);

                Type[] classes = assembly.GetTypes();

                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsNotPublic) continue;


                    if (type.BaseType.Equals(typeof(T))) //Primeiro nível
                    {
                        object obj = Activator.CreateInstance(type);
                        T t = (T)obj;
                        tList.Add(t);
                    }
                    else if ((type.BaseType.BaseType != null) && type.BaseType.BaseType.Equals(typeof(T))) //Segundo nível
                    {
                        object obj = Activator.CreateInstance(type);
                        T t = (T)obj;
                        tList.Add(t);
                    }

                }
            }
            catch (Exception ex)
            {
#if DEBUG

                Console.WriteLine("PluginManager error: " + ex.Message);

#endif
            }

            return tList;
        }

        public static List<T> GetPlugins<T>(string folder)
        {
            string[] files = Directory.GetFiles(folder, "*.dll");
            List<T> tList = new List<T>();

            foreach (string file in files)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(file);

                    Type[] classes = assembly.GetTypes();

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (!type.IsClass || type.IsNotPublic) continue;

                        
                        if (type.BaseType.Equals(typeof(T))) //Primeiro nível
                        {
                            object obj = Activator.CreateInstance(type);
                            T t = (T)obj;
                            tList.Add(t);
                        }
                        else if ((type.BaseType.BaseType != null) && type.BaseType.BaseType.Equals(typeof(T))) //Segundo nível
                        {
                            object obj = Activator.CreateInstance(type);
                            T t = (T)obj;
                            tList.Add(t);
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("PluginManager error: " + ex.Message);
                }
            }

            return tList;
        }
    }
}
