using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Diagnostics;

namespace IAM.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(IAM.Server.Program));

            if (System.Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        Console.WriteLine("Installing service...");
                        ManagedInstallerClass.InstallHelper(new string[] { asm.Location });
                        try
                        {
                            //StartService();
                        }
                        catch (Exception ex)
                        {
                            //EventLog.WriteEntry("IAM.Collector", "Falha ao tentar iniciar o serviço: " + ex.Message, EventLogEntryType.Error);
                        }
                        break;

                    case "--uninstall":
                        Console.WriteLine("Unistalling service......");
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", asm.Location });
                        break;

                    default:
                        Console.WriteLine("Starting in console mode...");

                        IAMServer col = new IAMServer();
                        col.Start(args);

                        while (true)
                            Console.ReadLine();

                        break;
                }
            }
            else
            {
                Console.Write("Starting in service mode...");

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new IAMServer() };

                ServiceBase.Run(ServicesToRun);
            }
        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }
    }
}
