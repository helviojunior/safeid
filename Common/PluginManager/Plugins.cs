using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace IAM.PluginManager
{
    public static class Plugins
    {
        public delegate void DebugMessage(String message, Exception exception);

        public static List<T> GetPlugins<T>(DebugMessage debugCallback = null)
        {

            //string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            string folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return GetPlugins<T>(folder, debugCallback);

        }

        public static List<T> GetPlugins<T>(Byte[] rawAssembly, DebugMessage debugCallback = null)
        {
            List<T> tList = new List<T>();

            try
            {
                Assembly assembly = Assembly.Load(rawAssembly);

                Type[] classes = assembly.GetTypes();

                foreach (Type type in assembly.GetTypes())
                {
                    try
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
                    catch(Exception ex) {
                        if (debugCallback != null)
                            debugCallback("Error loading plugin " + type.FullName, ex);
                    }
                }
            }
            catch (Exception ex)
            {

                if (debugCallback != null)
                    debugCallback("Error loading plugins", ex);

            }

            return tList;
        }

        public static List<T> GetPlugins<T>(string folder, DebugMessage debugCallback = null)
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
                        try
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
                        catch (Exception ex)
                        {
                            if (debugCallback != null)
                                debugCallback("Error loading plugin " + type.FullName, ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (debugCallback != null)
                        debugCallback("Error loading plugins", ex);
                }
            }

            return tList;
        }
    }
}
