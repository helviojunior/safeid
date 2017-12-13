using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CAS.PluginInterface;

namespace CAS.PluginManager
{
    public static class CASPlugins
    {
        public static List<CASPluginService> GetPlugins2(String configPath, String assemblyPath)
        {
            String tmp = "";
            try
            {
                return GetPlugins2(configPath, assemblyPath, out tmp);
            }
            finally
            {
                tmp = null;
            }
        }

        public static List<CASPluginService> GetPlugins2(String configPath, String assemblyPath, out String outLog)
        {
            List<CASPluginService> ret = new List<CASPluginService>();
            StringBuilder log = new StringBuilder();
            outLog = "";
            try
            {
                log.AppendLine("Starting GetPlugins2");

                log.AppendLine("configPath exists? " + Directory.Exists(configPath));
                log.AppendLine("assemblyPath exists? " + Directory.Exists(assemblyPath));

                if (!Directory.Exists(configPath) || !Directory.Exists(assemblyPath))
                    return ret;

                string[] confFiles = Directory.GetFiles(configPath, "*.conf");

                log.AppendLine("confFiles.Length = " + confFiles.Length);

                foreach (string file in confFiles)
                {
                    
                    try
                    {
                        log.AppendLine("1");
                        CASPluginConfig cfg = new CASPluginConfig();
                        try
                        {
                            cfg.LoadFromXML(new FileInfo(file));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Error parsing config file '" + file + "'", ex);
                        }
                        

                        log.AppendLine("2");
                        if (!String.IsNullOrEmpty(cfg.PluginAssembly))
                        {
                            log.AppendLine("3");
                            FileInfo asmFile = new FileInfo(Path.Combine(assemblyPath, cfg.PluginAssembly));
                            if (asmFile.Exists)
                            {
                                log.AppendLine("4");
                                Assembly assembly = Assembly.LoadFile(asmFile.FullName);

                                log.AppendLine("5");
                                CASPluginService newItem = new CASPluginService();
                                newItem.Config = cfg;

                                log.AppendLine("6");
                                Type[] classes = assembly.GetTypes();

                                log.AppendLine("7");
                                foreach (Type type in assembly.GetTypes())
                                {
                                    log.AppendLine("8");
                                    if (!type.IsClass || type.IsNotPublic) continue;

                                    log.AppendLine("9");
                                    if (type.BaseType.Equals(typeof(CASConnectorBase))) //Primeiro nível
                                    {
                                        newItem.Plugin = type;

                                        /*object obj = Activator.CreateInstance(type);
                                        CASConnectorBase t = (CASConnectorBase)obj;
                                        newItem.Plugin = t;*/
                                    }
                                    else if ((type.BaseType.BaseType != null) && type.BaseType.BaseType.Equals(typeof(CASConnectorBase))) //Segundo nível
                                    {
                                        newItem.Plugin = type;
                                        /*
                                        object obj = Activator.CreateInstance(type);
                                        CASConnectorBase t = (CASConnectorBase)obj;
                                        newItem.Plugin = t;*/
                                    }

                                }

                                log.AppendLine("10");
                                if (newItem.Plugin != null)
                                    ret.Add(newItem);

                                log.AppendLine("11");
                                log.AppendLine("Config file '" + file + "' loaded as " + newItem.Config.Service);
                            }
                            else
                            {
                                log.AppendLine("Erro on load config file '" + file + "': Assembly file not exists (" + asmFile.FullName + ")");
                                cfg = null;
                            }
                        }
                        else
                        {
                            log.AppendLine("Erro on load config file '" + file + "': Parameter PluginAssembly is empty");
                            cfg = null;
                        }
                    }
                    catch(Exception ex) {
                        log.AppendLine("Erro on load config file '" + file + "': " + ex.Message);
                    }
                    finally
                    {
                    }

                    log.AppendLine("");
                }
            }
            finally
            {
                outLog = log.ToString();
                log.Clear();
                log = null;
            }

            return ret;
        }



        public static List<T> GetPlugins<T>()
        {

            //string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            string folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return GetPlugins<T>(folder);

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


        private static void Log(String text)
        {
            try
            {
                Int32 pid = 0;
                try
                {
                    pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                }
                catch { }

                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(CASPlugins));

                String path = Path.Combine(Path.GetDirectoryName(asm.Location), "logs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                BinaryWriter writer = new BinaryWriter(File.Open(Path.Combine(path, DateTime.Now.ToString("yyyyMMdd") + pid + ".log"), FileMode.Append));
                writer.Write(Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ==> [" + (pid > 0 ? " -> " + pid : "") + "] \r\n" + text + Environment.NewLine));
                writer.Flush();
                writer.Close();

                asm = null;
            }
            catch { }
        }
    }
}
