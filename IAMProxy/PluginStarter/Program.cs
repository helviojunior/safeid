using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Principal;

using IAM.PluginInterface;
using IAM.PluginManager;
using IAM.Log;

namespace IAM.PluginStarter
{

    class Program
    {
        static String configHash = "";
        static Timer restartTimer;
        static Timer checkConfigTimer;
        static String basePath;

        static Int32 Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            if (args.Length == 0)
                return 1;
            
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(Program));
            basePath = Path.GetDirectoryName(asm.Location);
            String fileName = Path.GetFileName(asm.Location).Replace(".exe","");

            if (!File.Exists(Path.Combine(basePath, "config.json")))
            {
                TextLog.Log("PluginStarter", "Json config file not found");
                return 2;
            }

            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            TextLog.Log("PluginStarter", "Run as administrative right? " + hasAdministrativeRight);

            String ConfigJson =  Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(basePath, "config.json")));

            using (SHA1 hAlg = SHA1.Create())
                configHash = ComputeHash(hAlg, ConfigJson);

            PluginBase plugin = null;
            try
            {
                Uri pluginUri = new Uri(args[0].ToLower());

                
            //Checa Starter de plugin duplicado
                foreach (Process p in Process.GetProcessesByName(fileName))
                {
                    if ((p.StartInfo != null) && (!String.IsNullOrEmpty(p.StartInfo.Arguments)) && (p.StartInfo.Arguments.ToLower() == pluginUri.AbsoluteUri))
                    {
                        TextLog.Log("PluginStarter", "Duplicated plugin starter found for " + pluginUri.AbsoluteUri);
                        return 7;
                    }
                }


                switch (pluginUri.Scheme)
                {

                    case "connector":

                        List<PluginConnectorBase> cp = Plugins.GetPlugins<PluginConnectorBase>(Path.Combine(basePath, "plugins"));

                        foreach (PluginConnectorBase p in cp)
                            if (p.GetPluginId().AbsoluteUri.ToLower() == args[0].ToLower())
                                plugin = p;

                        break;

                    case "agent":

                        List<PluginAgentBase> ap = Plugins.GetPlugins<PluginAgentBase>(Path.Combine(basePath, "plugins"));

                        foreach (PluginAgentBase p in ap)
                            if (p.GetPluginId().AbsoluteUri.ToLower() == args[0].ToLower())
                                plugin = p;

                        break;
                }
            }
            catch (Exception ex)
            {
                TextLog.Log("PluginStarter", "Error loading plugins");
                return 3;
            }


            if (plugin == null)
            {
                TextLog.Log("PluginStarter", "Error startin with plugin " + args[0]);
                return 4;
            }

            try
            {
                switch (plugin.GetPluginId().Scheme.ToLower())
                {
                    case "connector":
                        ConnectorStarter st = new ConnectorStarter(ConfigJson, (PluginConnectorBase)plugin);
                        restartTimer = new Timer(new TimerCallback(RestartApp), st, 7200000, 7200000);
                        checkConfigTimer = new Timer(new TimerCallback(CheckConfig), st, 100, 60000);

                        break;

                    case "agent":
                        AgentStarter aSt = new AgentStarter(ConfigJson, (PluginAgentBase)plugin);
                        restartTimer = new Timer(new TimerCallback(RestartApp), aSt, 7200000, 7200000);
                        checkConfigTimer = new Timer(new TimerCallback(CheckConfig), aSt, 100, 60000);
                        break;

                    default:
                        TextLog.Log("PluginStarter", "Not implemented scheme '" + plugin.GetPluginId().Scheme + "': " + plugin.GetPluginId().AbsoluteUri);
                        return 5;
                        break;
                }
            }
            catch(Exception ex) {
                TextLog.Log("PluginStarter", "Error startin with plugin " + plugin.GetPluginId().AbsoluteUri + ": " + ex.Message);
                return 6;
            }

            TextLog.Log("PluginStarter", "Successfully started with plugin " + plugin.GetPluginId().AbsoluteUri);
            Console.WriteLine("Successfully started with plugin " + plugin.GetPluginId().AbsoluteUri);

            while(true)
                Console.ReadLine();

            return 0;
        }

        static void CheckConfig(Object starter)
        {
            try
            {

                if (!File.Exists(Path.Combine(basePath, "config.json")))
                    throw new Exception("Json config file not found");

                String ConfigJson = Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(basePath, "config.json")));

                String thisHash = "";
                using (SHA1 hAlg = SHA1.Create())
                    thisHash = ComputeHash(hAlg, ConfigJson);

                if (thisHash != configHash)
                {
                    TextLog.Log("PluginStarter", "Config file changed, updating...");

                    if (starter is ConnectorStarter)
                    {
                        ConnectorStarter st = (ConnectorStarter)starter;
                        st.NewConfig(ConfigJson);
                    }
                    else
                    {
                        AgentStarter st = (AgentStarter)starter;
                        st.NewConfig(ConfigJson);
                    }

                    configHash = thisHash;
                }
            }
            catch (Exception ex)
            {
                TextLog.Log("PluginStarter", "Erro on check config file update: " + ex.Message);
            }
        }

        static void RestartApp(Object starter)
        {
            TextLog.Log("PluginStarter", "Restart agendado");

            if (starter is ConnectorStarter)
            {
                ConnectorStarter st = (ConnectorStarter)starter;
                while (st.Executing)
                    Thread.Sleep(1000);
            }
            

            Process.GetCurrentProcess().Kill();
        }


        static private String ComputeHash(HashAlgorithm alg, String text)
        {


            // Convert the input string to a byte array and compute the hash.
            byte[] data = alg.ComputeHash(Encoding.UTF8.GetBytes(text));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();

        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }
    }
}
