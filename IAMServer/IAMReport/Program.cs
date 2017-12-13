using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Diagnostics;
using System.Security.Principal;

namespace IAM.Report
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(IAM.Report.Program));

            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            if (System.Environment.UserInteractive)
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
                        Console.WriteLine("Starting in console mode...");

                        IAMReport col = new IAMReport();
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
                ServicesToRun = new ServiceBase[] { new IAMReport() };

                ServiceBase.Run(ServicesToRun);
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
