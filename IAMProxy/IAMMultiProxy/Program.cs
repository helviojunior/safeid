using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Diagnostics;
using System.Security.Principal;

namespace IAM.MultiProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(IAM.MultiProxy.Program));

            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!RunningAsService())
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":

                        if (!hasAdministrativeRight)
                        {
                            RunElevated(asm.Location, parameter);
                            Process.GetCurrentProcess().Kill();
                        }

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

                        if (!hasAdministrativeRight)
                        {
                            RunElevated(asm.Location, parameter);
                            Process.GetCurrentProcess().Kill();
                        }

                        Console.WriteLine("Unistalling service......");
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", asm.Location });
                        break;

                    default:
                        Console.WriteLine("Iniciando em modo console...");

                        IAMMultiProxy col = new IAMMultiProxy();
                        col.Start(args);

                        while (true)
                            Console.ReadLine();

                        break;
                }
            }
            else
            {
                Console.Write("Iniciando em modo serviço...");

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new IAMMultiProxy() };

                ServiceBase.Run(ServicesToRun);
            }
        }


        static bool RunningAsService()
        {
            if (System.Environment.UserInteractive)
            {
                return false;
            }
            else
            {
                Process p = ProcessUtilities.GetParentProcess();
                return (p != null && p.ProcessName == "services");
            }
        }


        private static bool RunElevated(string fileName, String arguments)
        {
            //MessageBox.Show("Run: " + fileName);
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.Verb = "runas";
            processInfo.FileName = fileName;
            processInfo.Arguments = arguments;
            try
            {
                Process p = Process.Start(processInfo);
                p.WaitForExit();
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                //Do nothing. Probably the user canceled the UAC window
            }
            return false;
        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }
    }
}
