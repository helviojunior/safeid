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

using IAM.Config;
using IAM.PluginManager;
using IAM.PluginInterface;
using IAM.Log;
using IAM.CA;
using SafeTrend.Json;
using IAM.GlobalDefs;

namespace IAM.Proxy
{
    public partial class IAMProxy : ServiceBase
    {
        
        Proxy proxy;

        public IAMProxy()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            /*************
             * Carrega configurações
             */

            LocalConfig localConfig = new LocalConfig();
            try
            {
                localConfig.LoadConfig();
            }
            catch(Exception ex) {
                StopOnError("Falha ao carregar o arquivo de confguração 'proxy.conf'", ex);
            }

            if ((localConfig.Server == null) || (localConfig.Server.Trim() == ""))
                StopOnError("Parâmetro 'Server' não localizado no arquivo de configuração 'proxy.conf'", null);

            if ((localConfig.Hostname == null) || (localConfig.Hostname.Trim() == ""))
                StopOnError("Parâmetro 'Hostname' não localizado no arquivo de configuração 'proxy.conf'", null);

            /*************
             * Inicia timer que busca as configurações no servidor
             */

            try
            {
                proxy = new Proxy(localConfig);
                Log.TextLog.Log("Proxy", "Proxy started");
            }
            catch(Exception ex) {
                StopOnError("Erro starting proxy", ex);
            }
            
        }

        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                Log.TextLog.Log("Proxy", "FATAL ERROR -> " + text + " " + ex.Message);
            }
            else
            {
                Log.TextLog.Log("Proxy", "FATAL ERROR -> " + text);
            }
            
            Process.GetCurrentProcess().Kill();
        }

        protected override void OnStop()
        {
            if (proxy != null)
                proxy.End();
        }

        public void Start(string[] args)
        {
            OnStart(args);
        }

    }
}
