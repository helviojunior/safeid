using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

using IAM.Log;


namespace IAM.MultiProxy
{
    public partial class IAMMultiProxy : ServiceBase
    {

        String basePath = "";
        List<String> onlineProxy;

        public IAMMultiProxy()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            onlineProxy = new List<String>();

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);

            DirectoryInfo dir = new DirectoryInfo(Path.Combine(basePath, "proxies"));
            if (dir.Exists)
            {
                foreach (DirectoryInfo d in dir.GetDirectories())
                    if (d.Name != "_base")
                        foreach (FileInfo f in d.GetFiles("*.exe"))
                        {
                            if (f.Name.ToLower() == "iamproxy.exe")
                                StartProxy(f, "");
                        }
            }
        }


        private void StartProxy(FileInfo proxyName, String logPrefix)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Verb = "runas"; //To run as administrator
            psi.FileName = proxyName.FullName;
            psi.WorkingDirectory = proxyName.Directory.FullName;

            Process p = new Process();
            p.StartInfo = psi;
            p.EnableRaisingEvents = true;
            p.Exited += new EventHandler(p_Exited);
            p.Start();

            Log.TextLog.Log("MultiProxy", logPrefix + "Started Proxy " + proxyName.FullName  + " on pid " + p.Id);

            //Adiciona na tabela para previnir a inicialização de mais de um executável por proxy
            onlineProxy.Add(proxyName.FullName);

        }

        void p_Exited(object sender, EventArgs e)
        {
            Process p = (Process)sender;

            //prevenção contra finalização instanTanea do plugin e não remoção da tabela caso isso ocorra
            Thread.Sleep(1000);

            onlineProxy.Remove(p.StartInfo.FileName);

            Log.TextLog.Log("MultiProxy", "Proxy " + p.StartInfo.Arguments + " with pid " + p.Id + " closed");

            Thread.Sleep(60000);
            StartProxy(new FileInfo( p.StartInfo.FileName), "");
        }

        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
            }

            //EventLog.WriteEntry("IAM.Collector", text + ex.Message, EventLogEntryType.Error);
            
            //if (!System.Environment.UserInteractive)
                //EventLog.WriteEntry("IAM.Collector", "Serviço finalizado por erro", EventLogEntryType.Information);

            Process.GetCurrentProcess().Kill();
        }

        protected override void OnStop()
        {
            //if (!System.Environment.UserInteractive)
                //EventLog.WriteEntry("IAM.Collector", "Serviço finalizado", EventLogEntryType.Information);
            
            //Finaliza todos os processos filhos
            List<Process> procs = ProcessUtilities.GetChieldProcess();
            for (Int32 i = 0; i < procs.Count; i++)
            {
                //Resgata a listagem de filhos do processo que será finalizado
                List<Process> procChields = ProcessUtilities.GetChieldProcess(procs[i].Id);
                
                //Finaliza o processo
                try
                {
                    procs[i].Kill();
                }
                catch { }

                //Finaliza os filhos do processo
                try
                {
                    for (Int32 c = 0; c < procChields.Count; c++)
                        try
                        {
                            procChields[c].Kill();
                        }
                        catch { }
                }
                catch { }

            }

            //Killall("IAMProxy");
            //Killall("IAMPluginStarter");
        }

        public void Killall(String name)
        {
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.ProcessName.ToLower() == name.ToLower())
                        p.Kill();
                }
                catch { }
            }
        }

        public void Start(string[] args)
        {
            OnStart(args);
        }

    }
}
