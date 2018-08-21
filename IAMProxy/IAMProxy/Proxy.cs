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
using System.Security.Principal;

using IAM.Config;
using IAM.PluginManager;
using IAM.PluginInterface;
using IAM.Log;
using IAM.CA;
using SafeTrend.Json;
using IAM.GlobalDefs;
using SafeTrend.Json;

namespace IAM.Proxy
{
    [Serializable()]
    internal class DownloadPlugin
    {
        public string name;
        public string status;

        [OptionalField]
        public string content;

        [OptionalField]
        public string date;

    }

    [Serializable()]
    internal class SyncStatus : GlobalDefs.GlobalJson
    {
        [OptionalField]
        public Int32 config;

        [OptionalField]
        public Int32 files;

        [OptionalField]
        public Int32 fetch;
    }

    internal class Proxy
    {
        LocalConfig localConfig;
        ProxyConfig config;

        Timer configTimer;
        String basePath = "";
        List<Uri> onlinePlugins;

        Boolean downloadConfigExecuting = false;
        Boolean receiveFilesExecuting = false;
        Boolean sendFilesExecuting = false;
        Boolean fetchExecuting = false;


        //Timers
        Timer sendTimer;
        //Timer receiveTimer;
        Timer deleteTimer;
        Timer syncTimer;

        //Values used to Sync
        Boolean syncExecuting = false;
        
        SyncStatus lastSync = new SyncStatus();

        LogProxy logProxy;
        Version ver;

        public Proxy(LocalConfig localConfig)
        {

            this.onlinePlugins = new List<Uri>();

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            this.ver = asm.GetName().Version;
            this.basePath = Path.GetDirectoryName(asm.Location);

            this.localConfig = localConfig;

            
            //Realizando a checagem dos certificados
            Log.TextLog.Log("Proxy", "Starting up...");
            Log.TextLog.Log("Proxy", "\tHostname..: " + this.localConfig.Hostname);
            Log.TextLog.Log("Proxy", "\tServer....: " + this.localConfig.Server);
            Log.TextLog.Log("Proxy", "\tUseHttps...: " + (this.localConfig.UseHttps ? "Yes": "No"));
            
            Log.TextLog.Log("Proxy", "Checking certificates...");

            if ((this.localConfig.ServerCertificate == null) || (this.localConfig.ServerCertificate == ""))
            {
                Log.TextLog.Log("Proxy", "Server certificate is empty");
                return;
            }

            if ((this.localConfig.ClientCertificate == null) || (this.localConfig.ClientCertificate == ""))
            {
                Log.TextLog.Log("Proxy", "Client certificate is empty");
                return;
            }

            try
            {
                byte[] tmp = Convert.FromBase64String(this.localConfig.ServerCertificate);
                String tmp2 = Encoding.UTF8.GetString(tmp);
                tmp = null;
            }
            catch (Exception ex)
            {
                Log.TextLog.Log("Proxy", "Server Base64 parse error: " + ex.Message);
                Log.TextLog.Log("Proxy", this.localConfig.ServerCertificate);
                return;
            }

            try
            {
                byte[] tmp = Convert.FromBase64String(this.localConfig.ClientCertificate);
                String tmp2 = Encoding.UTF8.GetString(tmp);
                tmp = null;
            }
            catch (Exception ex)
            {
                Log.TextLog.Log("Proxy", "Client Base64 parse error: " + ex.Message);
                Log.TextLog.Log("Proxy", this.localConfig.ClientCertificate);
                return;
            }

            try
            {

                String checkError = "";
                if (!CATools.CheckSignedCertificate(CATools.LoadCert(Resource1.IAMServerCertificateRoot), CATools.LoadCert(Convert.FromBase64String(this.localConfig.ServerCertificate)), out checkError))
                {
                    File.WriteAllBytes(Path.Combine(basePath, "logs\\ca.cer"), Resource1.IAMServerCertificateRoot);
                    File.WriteAllBytes(Path.Combine(basePath, "logs\\serverCertificate.cer"), Convert.FromBase64String(this.localConfig.ServerCertificate));

                    Log.TextLog.Log("Proxy", "Server certificate check error: " + checkError);
                    return;
                }
                else
                {
                    Log.TextLog.Log("Proxy", "Server certificate OK");
                }

                Uri srv = new Uri("//" + this.localConfig.Server.ToLower());

                String key = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(srv.Host));

                checkError = "";
                if (!CATools.CheckSignedCertificate(CATools.LoadCert(Resource1.IAMServerCertificateRoot), CATools.LoadCert(Convert.FromBase64String(this.localConfig.ClientCertificate), key), out checkError))
                {
                    File.WriteAllBytes(Path.Combine(basePath, "logs\\ca.cer"), Resource1.IAMServerCertificateRoot);
                    File.WriteAllBytes(Path.Combine(basePath, "logs\\clientCertificate.pfx"), Convert.FromBase64String(this.localConfig.ClientCertificate));
                    try
                    {
                        File.WriteAllBytes(Path.Combine(basePath, "logs\\clientCertificate.pfx"), CATools.X509ToByte(CATools.LoadCert(Convert.FromBase64String(this.localConfig.ClientCertificate))));
                    }
                    catch { }

                    Log.TextLog.Log("Proxy", "Client certificate check error: " + checkError);
                    return;
                }
                else
                {
                    Log.TextLog.Log("Proxy", "Client certificate OK");
                }

                Log.TextLog.Log("Proxy", "Checking permissions...");

                WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

                TextLog.Log("Proxy", "Run as administrative right? " + hasAdministrativeRight);

                Log.TextLog.Log("Proxy", "Starting thread of download configuration");

                Thread nt = new Thread(new ThreadStart(StartDownloadAndSync));
                nt.Start();
            }
            catch (Exception ex)
            {
                Log.TextLog.Log("Proxy", "Error: " + ex.Message);
                return;
            }
        }

        public void End()
        {
            //Finaliza todos os processos filhos
            //Finaliza todos os processos filhos
            List<Process> procs = ProcessUtilities.GetChieldProcess();
            for (Int32 i = 0; i < procs.Count; i++)
                try
                {
                    procs[i].Kill();
                }
                catch { }

            //Killall("IAMPluginStarter");

            if (logProxy != null)
                logProxy.SaveToSend("log-proxy");
        }

        /* Timers Callback
        ========================================*/
        private void SyncTimer(Object state)
        {
            if (syncExecuting)
                return;

            syncExecuting = true;

            GetSync();

            if ((lastSync.files > 0) && (lastSync.config > 0) && (lastSync.fetch > 0))
            {
                Log.TextLog.Log("Proxy", "Starting thread of download config, files and fetch (Real time sync)");
                Thread nt = new Thread(new ThreadStart(ConfigAndReceiveAndFetch));
                nt.Start();
            }
            else if ((lastSync.files > 0) && (lastSync.config > 0))
            {
                Log.TextLog.Log("Proxy", "Starting thread of download config and files (Real time sync)");
                Thread nt = new Thread(new ThreadStart(ConfigAndReceive));
                nt.Start();
            }
            else if ((lastSync.fetch > 0) && (lastSync.config > 0))
            {
                Log.TextLog.Log("Proxy", "Starting thread of download config and fetch (Real time sync)");
                Thread nt = new Thread(new ThreadStart(ConfigAndFetch));
                nt.Start();
            }
            else if (lastSync.fetch > 0)
            {
                Log.TextLog.Log("Proxy", "Starting thread of download fetch (Real time sync)");
                Thread nt = new Thread(new ThreadStart(Fetch));
                nt.Start();
            }
            else if (lastSync.config > 0)
            {
                Log.TextLog.Log("Proxy", "Starting thread of download config (Real time sync)");
                Thread nt = new Thread(new ThreadStart(Config));
                nt.Start();
            }
            else if (lastSync.files > 0)
            {
                Log.TextLog.Log("Proxy", "Starting thread of download files (Real time sync)");
                Thread nt = new Thread(new ThreadStart(ReceiveFiles));
                nt.Start();
            }

            syncExecuting = false;
        }

        private void StartDownloadAndSync()
        {
            //Realiza o download da configuração atual e arquivos se houver
            Config();

            //configTimer = new Timer(new TimerCallback(ConfigTimer), null, 50, 300000);
            sendTimer = new Timer(new TimerCallback(SendTimer), null, 1000, 15000);
            //receiveTimer = new Timer(new TimerCallback(ReceiveTimer), null, 1000, 120000);
            deleteTimer = new Timer(new TimerCallback(DeleteTimer), null, 5000, 120000);

            //Realiza get de sincronização a cada 10 segundos
            syncTimer = new Timer(new TimerCallback(SyncTimer), null, 1000, 10000);

        }

        private void ConfigAndReceiveAndFetch()
        {
            Config();
            ReceiveFiles();
            Fetch();
        }

        private void ConfigAndFetch()
        {
            Config();
            Fetch();
        }

        private void ConfigAndReceive()
        {
            Config();
            ReceiveFiles();
        }

        private void DeleteTimer(Object state)
        {
            DeleteOldFiles();
        }

        /*
        private void ReceiveTimer(Object state)
        {
            ReceiveFiles();
        }*/

        private void SendTimer(Object state)
        {
            SendFiles();
        }

        private void ConfigTimer(Object state)
        {
            Config();
        }

        private void GetSync()
        {
            WebClient client = new WebClient();
            try
            {
                Uri uri = new Uri((localConfig.UseHttps ? "https://" : "http://") + localConfig.Server + "/proxy/sync/");
                client.Headers.Add("X-SAFEID-PROXY", localConfig.Hostname);
                client.Headers.Add("X-SAFEID-VERSION", ver.ToString());
                
                String jData = client.DownloadString(uri);
                lastSync = SyncStatus.Deserialize<SyncStatus>(jData);
            }
            catch { }
            finally
            {
                client.Dispose();
            }
        }

        public void DeleteOldFiles()
        {

            //Realiza exclusões dos arquivos de log e move para não encher o disco do proxy

            //Exclui todos os arquivos do diretório Move que tenham sido alterados a mais de 15 dias
            DateTime cutDate = DateTime.Now.AddDays(-15);

            try
            {
                DirectoryInfo mvDir = new DirectoryInfo(Path.Combine(Path.Combine(basePath, "Out"), "move"));
                if (mvDir.Exists)
                {
                    FileInfo[] files = mvDir.GetFiles("*.iamdat");

                    foreach (FileInfo f in files)
                    {
                        if (f.LastWriteTimeUtc.CompareTo(cutDate) == -1)
                            try
                            {
                                Log.TextLog.Log("Proxy", "Deleting file '" + f.Name + "' from move folder. LastWriteTimeUtc = " + f.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                                f.Delete();
                            }
                            catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.TextLog.Log("Proxy", "Erro ao excluir os arquivos enviados com mais de 15 dias: " + ex.Message);
            }

            //Exclui todos os logs com mais de 30 dias
            try
            {
                cutDate = DateTime.Now.AddDays(-30);
                DirectoryInfo logsDir = new DirectoryInfo(Path.Combine(basePath, "logs"));
                if (logsDir.Exists)
                {
                    FileInfo[] files = logsDir.GetFiles("*.log");

                    foreach (FileInfo f in files)
                    {
                        if (f.LastWriteTimeUtc.CompareTo(cutDate) == -1)
                            try
                            {
                                Log.TextLog.Log("Proxy", "Deleting file '" + f.Name + "' from log folder. LastWriteTimeUtc = " + f.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                                f.Delete();
                            }
                            catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.TextLog.Log("Proxy", "Erro ao excluir os arquivos de log com mais de 30 dias: " + ex.Message);
            }

        }

        private void Fetch()
        {
            if (fetchExecuting)
                return;

            if (config == null)
                return;

            Console.WriteLine("Fetch");

            fetchExecuting = true;

            Uri uri = new Uri((localConfig.UseHttps ? "https://" : "http://") + localConfig.Server + "/proxy/api.aspx");

            List<PluginConnectorBaseFetchPackage> list = null;
            try
            {
                WebClient client = new WebClient();
                Byte[] data = client.UploadData(uri, Encoding.UTF8.GetBytes(JSON.GetRequest("fetch", localConfig.Hostname, "")));

                String Json = Encoding.UTF8.GetString(data);

                JSONResponse resp = JSON.GetResponse(Json);

                if (resp.response == "success")
                {
                    if ((resp.data != null) && (resp.data.Length > 0))
                    {

                        Byte[] bData = Convert.FromBase64String(resp.data);

                        String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
                        using (CryptApi cApi = CryptApi.ParsePackage(CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass), bData))
                            list = JSON.Deserialize<List<PluginConnectorBaseFetchPackage>>(Encoding.UTF8.GetString(cApi.clearData));

                    }
                }
                else
                {
                    Log.TextLog.Log("Proxy", "Erro on fetch download process: " + resp.error);
                }

            }
            catch(Exception ex)
            {
                AddProxyLog(UserLogLevel.Error, "Erro on process fetch package", "");
                Log.TextLog.Log("Proxy", "Erro on fetch download process: " + ex.Message);
            }


            try
            {
                //Realiza o fetch de cada um dos pacotes e ja retorna a informação
                if (list != null)
                {
                    List<Dictionary<String, Object>> sendData = new List<Dictionary<string, object>>();

                    foreach (PluginConnectorBaseFetchPackage pkg in list)
                    {
                        Boolean result = false;
                        String resultData = "";
                        StringBuilder fetchLog = new StringBuilder();
                        fetchLog.AppendLine("Starting fields fetch");

                        Dictionary<String, Object> newItem = new Dictionary<string, object>();
                        newItem.Add("pkg_id", pkg.pkgId);
                        newItem.Add("resource_plugin_id", pkg.resourcePluginId);
                        newItem.Add("fetch_id", pkg.fetchId);

                        try
                        {
                            List<PluginConnectorBase> p1 = Plugins.GetPlugins<PluginConnectorBase>(pkg.pluginRawData);
                            PluginConnectorBase selectedPlugin = null;

                            foreach (PluginConnectorBase p in p1)
                                if (p.GetPluginId().AbsoluteUri.ToLower() == pkg.pluginUri.AbsoluteUri.ToString().ToLower())
                                    selectedPlugin = p;

                            if (selectedPlugin == null)
                            {
                                fetchLog.AppendLine("Plugin uri '" + pkg.pluginUri.AbsoluteUri + "' not found in assembly");
                                continue;
                            }


                            LogEvent log = new LogEvent(delegate(Object sender, PluginLogType type, String text)
                            {
                                fetchLog.AppendLine("[" + type.ToString() + "] " + text);
                            });


                            selectedPlugin.Log += log;
                            PluginConnectorBaseFetchResult status = selectedPlugin.FetchFields(pkg.config);
                            selectedPlugin.Log -= log;


                            if (status == null)
                            {
                                fetchLog.AppendLine("Plugin process im empty");
                            }
                            else
                            {
                                status.pkgId = pkg.pkgId;
                                status.resourcePluginId = pkg.resourcePluginId;
                                status.pluginUri = pkg.pluginUri;
                                status.fetchId = pkg.fetchId;

                                resultData = JSON.Serialize<PluginConnectorBaseFetchResult>(status);
                                result = status.success;
                            }
                        }
                        catch (Exception ex)
                        {
                            fetchLog.AppendLine("Erro on fields fetch: " + ex.Message);
                        }

                        fetchLog.AppendLine("Fields fetch ended");
                        newItem.Add("result", result);
                        newItem.Add("logs", fetchLog.ToString());
                        newItem.Add("result_data", Convert.ToBase64String(Encoding.UTF8.GetBytes(resultData)));

                        sendData.Add(newItem);
                    }

                    //Send data to proxy API

                    String cSendData = "";
                    String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
                    using (CryptApi cApi = new CryptApi(
                        CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass),
                        Encoding.UTF8.GetBytes(JSON.Serialize<List<Dictionary<String, Object>>>(sendData)))
                        )
                    {
                        cSendData = Convert.ToBase64String(cApi.ToBytes());
                    }

                    WebClient client = new WebClient();
                    client.UploadData(uri, Encoding.UTF8.GetBytes(JSON.GetRequest("fetch_result", localConfig.Hostname, cSendData)));

                }
            }
            catch(Exception ex)
            {
                Log.TextLog.Log("Proxy", "Erro on process and sens data fetch: " + ex.Message);
                Thread.Sleep(30000);
            }
            finally
            {
                list = null;
                fetchExecuting = false;
            }
        }

        private void ReceiveFiles()
        {
            if (receiveFilesExecuting)
                return;

            receiveFilesExecuting = true;

            //Aguarda a finalização do processamento da configuração por 1 minuto
            Int32 whaitCount = 0;
            while (downloadConfigExecuting)
            {
                if (whaitCount > 60)
                    return;

                whaitCount++;
                Thread.Sleep(1000);
            }

            //Log.TextLog.Log("Proxy", "Starting transfer thread");
            try
            {

                if ((localConfig.Server == null) || (localConfig.Server.Trim() == ""))
                    return;

                if ((localConfig.Hostname == null) || (localConfig.Hostname.Trim() == ""))
                    return;

                if (config == null)
                    return;

                if ((config.server_cert == null) || (config.server_cert == ""))
                    return;

                if ((config.client_cert == null) || (config.client_cert == ""))
                    return;

                DownloadTransfer();

                lastSync.files = 0;
            }
            finally
            {
                //Log.TextLog.Log("Proxy", "Finishing transfer thread");
            }

            receiveFilesExecuting = false;
        }

        private void SendFiles()
        {
            if (sendFilesExecuting)
                return;

            sendFilesExecuting = true;

            //Aguarda a finalização do processamento da configuração por 1 minuto
            Int32 whaitCount = 0;
            while (downloadConfigExecuting)
            {
                if (whaitCount > 60)
                    return;

                whaitCount++;
                Thread.Sleep(1000);
            }



            //Log.TextLog.Log("Proxy", "Starting transfer thread");
            try
            {

                if ((localConfig.Server == null) || (localConfig.Server.Trim() == ""))
                    return;

                if ((localConfig.Hostname == null) || (localConfig.Hostname.Trim() == ""))
                    return;

                if (config == null)
                    return;

                if ((config.server_cert == null) || (config.server_cert == ""))
                    return;

                if ((config.client_cert == null) || (config.client_cert == ""))
                    return;

                UploadTransfer();

                //UploadLogs();

            }
            finally
            {
                //Log.TextLog.Log("Proxy", "Finishing transfer thread");
            }

            sendFilesExecuting = false;

        }

        private void DownloadTransfer()
        {

            DirectoryInfo inDir = new DirectoryInfo(Path.Combine(basePath, "In"));
            if (!inDir.Exists)
                inDir.Create();

            Uri uri = new Uri((localConfig.UseHttps ? "https://" : "http://") + localConfig.Server + "/proxy/api.aspx");
            StringBuilder dwnLog = new StringBuilder();
            try
            {

                WebClient client = new WebClient();
                Byte[] data = client.UploadData(uri, Encoding.UTF8.GetBytes(JSON.GetRequest("transfer_receive", localConfig.Hostname, "")));

                String Json = Encoding.UTF8.GetString(data);

#if DEBUG
                Log.TextLog.Log("Proxy", "\tReceived data from server. Datalen " + Json.Length);
                Log.TextLog.Log("Proxy", "\t" + Json);
#endif

                JSONResponse resp = JSON.GetResponse(Json);

                if (resp.response == "success")
                {
                    //

                    if ((resp.data != null) && (resp.data.Length > 0))
                    {

                        JsonGeneric fileList = new JsonGeneric();
                        try
                        {
                            fileList.FromJsonString(resp.data);
                        }
                        catch (Exception ex)
                        {
                            Log.TextLog.Log("Proxy", "\tError parsing file list " + ex.Message);
                        }

                        if ((fileList.fields != null) && (fileList.fields.Length > 0))
                        {

                            Int32 nameCol = fileList.GetKeyIndex("name");

                            if (fileList.data.Count == 0)
                                return;

                            Log.TextLog.Log("Proxy", "Starting receive transfer thread");

                            try
                            {

                                Log.TextLog.Log("Proxy", "\tListed " + fileList.data.Count + " file(s) to download");

                                foreach (String[] dr in fileList.data)
                                {
                                    try
                                    {
                                        dwnLog.AppendLine("File " + dr[nameCol]);

                                        data = client.UploadData(uri, Encoding.UTF8.GetBytes(JSON.GetRequest("transfer_receive", localConfig.Hostname, dr[nameCol])));

                                        Json = Encoding.UTF8.GetString(data);
                                        JSONResponse resp2 = JSON.GetResponse(Json);

                                        if (resp2.response == "success")
                                        {
                                            FileInfo fName = new FileInfo(Path.Combine(inDir.FullName, dr[nameCol]));
                                            if (!fName.Directory.Exists)
                                                fName.Directory.Create();

                                            File.WriteAllBytes(fName.FullName, Convert.FromBase64String(resp2.data));
                                            Log.TextLog.Log("Proxy", "\tFile path " + fName.FullName);
                                            Log.TextLog.Log("Proxy", "\tFile writed " + dr[nameCol]);
                                            dwnLog.AppendLine("File writed " + dr[nameCol]);

                                            //Notifica o recebimento
                                            data = client.UploadData(uri, Encoding.UTF8.GetBytes(JSON.GetRequest("transfer_receive_ok", localConfig.Hostname, dr[nameCol])));
                                            Json = Encoding.UTF8.GetString(data);
                                            JSONResponse resp3 = JSON.GetResponse(Json);

                                            if (resp3.response == "success")
                                            {
                                                Log.TextLog.Log("Proxy", "\tNotifying server: " + (resp3.data != null ? resp3.data : ""));
                                                dwnLog.AppendLine("Notifying server: " + (resp3.data != null ? resp3.data : ""));
                                            }
                                            else
                                            {
                                                Log.TextLog.Log("Proxy", "\tError notifying server " + (resp3.error != null ? resp3.error : ""));
                                                dwnLog.AppendLine("Error notifying server " + (resp3.error != null ? resp3.error : ""));
                                            }
                                        }
                                        else
                                        {
                                            Log.TextLog.Log("Proxy", "\tError downloading file from server. " + (resp2.error != null ? resp2.error : ""));
                                            dwnLog.AppendLine("Error downloading file from server. " + (resp2.error != null ? resp2.error : ""));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.TextLog.Log("Proxy", "\tError downloading file from server. " + ex.Message);
                                        dwnLog.AppendLine("Error downloading file from server. " + ex.Message);
                                    }

                                    dwnLog.AppendLine("");

                                    
                                }
                            }
                            finally
                            {
                                Log.TextLog.Log("Proxy", "Finishing receive transfer thread");
                            }
                        }

                        /*
                        System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(plugin.GetType());
                        DirectoryInfo dirFrom = new DirectoryInfo(Path.Combine(inDir.FullName, Path.GetFileNameWithoutExtension(asm.Location) + "\\" + context));
                        */
                    }
                }
                else
                {
                    Log.TextLog.Log("Proxy", "Error downloading file list from server. " + (resp.error != null ? resp.error : ""));
                    dwnLog.AppendLine("Error downloading file list from server. " + (resp.error != null ? resp.error : ""));
                }
            }
            catch (Exception ex)
            {
                Log.TextLog.Log("Proxy", "Error downloading file list from server " + localConfig.Hostname + "(" + ex.Message + ")");
                dwnLog.AppendLine("Error downloading file list from server " + localConfig.Hostname + "(" + ex.Message + ")");

            }
            finally
            {
#if DEBUG
                AddProxyLog(UserLogLevel.Info, "File(s) downloaded by proxy.", dwnLog.ToString());
#endif
                if (logProxy != null)
                    logProxy.SaveToSend("log-proxy");
                dwnLog.Clear();
                dwnLog = null;
            }

        }

        private void UploadLogs()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();

            DirectoryInfo logDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "logs"));
            if (!logDir.Exists)
            {
                //Log.TextLog.Log("Proxy", "\tNo file to upload");
                return;
            }

            FileInfo[] files = logDir.GetFiles("*.log");

            if (files.Length == 0)
                return;

            List<FileInfo> filesToSend = new List<FileInfo>();

            Int32 lDate = Int32.Parse(DateTime.Now.ToString("yyyyMMddHH"));

            foreach (FileInfo f in files)
            {
                try
                {
                    String tDate = f.Name.Split("-".ToCharArray())[0];
                    Int32 date = Int32.Parse(tDate);

                    if (date < lDate)
                    {
                        filesToSend.Add(f);
                    }
                }
                catch { }
            }


            if (filesToSend.Count == 0)
                return;

            Uri uri = new Uri((localConfig.UseHttps ? "https://" : "http://") + localConfig.Server + "/proxy/api.aspx");

            foreach (FileInfo f in filesToSend)
            {
                try
                {
                    Byte[] fData = File.ReadAllBytes(f.FullName);

                    WebClient client = new WebClient();
                    Byte[] data = client.UploadData(uri, Encoding.UTF8.GetBytes("{\"request\":\"send_logs\", \"host\":\"" + localConfig.Hostname + "\", \"data\":\"" + Convert.ToBase64String(fData) + "\"}"));

                    String Json = Encoding.UTF8.GetString(data);
                    JSONResponse resp = JSON.GetResponse(Json);

                    if (resp.response == "success")
                    {
                        Log.TextLog.Log("Proxy", "\tSended file " + f.Name + " to server. Datalen " + fData.Length);
#if debug
                            AddProxyLog(UserLogLevel.Info, "Sended file " + f.Name + " to server. " + resp.data, "");
#endif
                        DirectoryInfo mvDir = new DirectoryInfo(Path.Combine(f.DirectoryName, "move"));
                        if (!mvDir.Exists)
                            mvDir.Create();

                        Log.TextLog.Log("Proxy", "\tMoving file " + f.Name + " to 'move' folder");
                        File.Move(f.FullName, Path.Combine(mvDir.FullName, f.Name));

                    }
                    else
                    {
                        Log.TextLog.Log("Proxy", "\tError on send file " + f.Name + " to server. " + (resp.error != null ? resp.error : ""));
                    }

                }
                catch (Exception ex)
                {
                    Log.TextLog.Log("Proxy", "\tError sending file to server " + localConfig.Hostname + "(" + ex.Message + ")");
                }
            }
        }

        private void UploadTransfer()
        {
            DirectoryInfo outDir = new DirectoryInfo(Path.Combine(basePath, "Out"));
            if (!outDir.Exists)
            {
                //Log.TextLog.Log("Proxy", "\tNo file to upload");
                return;
            }

            Uri uri = new Uri((localConfig.UseHttps ? "https://" : "http://") + localConfig.Server + "/proxy/api.aspx");

            FileInfo[] files = outDir.GetFiles("*.iamdat");

            //if (files.Length == 0)
            //Log.TextLog.Log("Proxy", "\tNo file to upload");

            if (files.Length == 0)
                return;

            Log.TextLog.Log("Proxy", "Starting send transfer thread");

            try
            {

                foreach (FileInfo f in files)
                {

                    try
                    {
                        Byte[] fData = File.ReadAllBytes(f.FullName);

                        WebClient client = new WebClient();
                        Byte[] data = client.UploadData(uri, Encoding.UTF8.GetBytes("{\"request\":\"transfer_send\", \"host\":\"" + localConfig.Hostname + "\", \"data\":\"" + Convert.ToBase64String(fData) + "\"}"));

                        String Json = Encoding.UTF8.GetString(data);
                        JSONResponse resp = JSON.GetResponse(Json);

                        if (resp.response == "success")
                        {
                            Log.TextLog.Log("Proxy", "\tSended file " + f.Name + " to server. Datalen " + fData.Length);
#if DEBUG
                            AddProxyLog(UserLogLevel.Info, "Sended file " + f.Name + " to server. ", resp.data);
#endif
                            DirectoryInfo mvDir = new DirectoryInfo(Path.Combine(f.DirectoryName, "move"));
                            if (!mvDir.Exists)
                                mvDir.Create();

                            Log.TextLog.Log("Proxy", "\tMoving file " + f.Name + " to 'move' folder");
                            File.Move(f.FullName, Path.Combine(mvDir.FullName, f.Name));

                        }
                        else
                        {
                            Log.TextLog.Log("Proxy", "\tError on send file " + f.Name + " to server from " + localConfig.Hostname + (resp.error != null ? resp.error : ""));

#if DEBUG
                            AddProxyLog(UserLogLevel.Info, "Error on send file " + f.Name + " to server from " + localConfig.Hostname, (resp.error != null ? resp.error : ""));
#endif
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.TextLog.Log("Proxy", "\tError sending file to server from " + localConfig.Hostname + "(" + ex.Message + ")");

#if DEBUG
                        AddProxyLog(UserLogLevel.Info, "Error sending file to server from " + localConfig.Hostname, ex.Message);
#endif
                    }
                }
            }
            finally
            {
                Log.TextLog.Log("Proxy", "Finishing send transfer thread");
            }
        }



        private void Config()
        {
            if (downloadConfigExecuting)
                return;

            Log.TextLog.Log("Proxy", "Starting config download thread");
            downloadConfigExecuting = true;
            try
            {

                if (logProxy != null)
                    logProxy.SaveToSend("log-proxy");

                String Json = "";
                Log.TextLog.Log("Proxy", "\tConnecting on " + localConfig.Server + " with " + (localConfig.UseHttps ? "https" : "http"));
                try
                {
                    Uri uri = new Uri((localConfig.UseHttps ? "https://" : "http://") + localConfig.Server + "/proxy/api.aspx");

                    WebClient client = new WebClient();
                    Byte[] data = client.UploadData(uri, Encoding.UTF8.GetBytes("{\"request\":\"proxy_config\", \"host\":\"" + localConfig.Hostname + "\"}"));

                    Console.WriteLine("Received configuration data from server. Datalen " + data.Length);
                    Log.TextLog.Log("Proxy", "\tReceived configuration data from server. Datalen " + data.Length);

                    Json = Encoding.UTF8.GetString(data);
                }
                catch (Exception ex)
                {
                    Log.TextLog.Log("Proxy", "\tError retrieving the configuration from server " + localConfig.Hostname + "(" + ex.Message + ")");
                }

                if (Json.Length == 0)
                    return;

                ProxyConfig tmpConfig = new ProxyConfig();
                try
                {
                    tmpConfig = new ProxyConfig();
                    tmpConfig.FromJsonString(Json);

                    if (configTimer != null)
                        configTimer.Dispose();
                    configTimer = null;

                }
                catch (Exception ex)
                {
                    Log.TextLog.Log("Proxy", "\tError parsing the configuration (" + ex.Message + ")");
                    Log.TextLog.Log("Proxy", "\t" + Json);
                    return;
                }

                try
                {
                    //Inclui os certificados locais na configuração para checagem
                    //Pois o servidor web não enviará os certificados, eles precisarão existir na configuração local
                    tmpConfig.server_cert = localConfig.ServerCertificate;
                    tmpConfig.client_cert = localConfig.ClientCertificate;


                    //Realizando a checagem dos certificados
                    Log.TextLog.Log("Proxy", "\tChecking received certificates...");

                    if ((tmpConfig.server_cert == null) || (tmpConfig.server_cert == ""))
                    {
                        Log.TextLog.Log("Proxy", "\tReceived server certificate is empty");
                        return;
                    }

                    if ((tmpConfig.client_cert == null) || (tmpConfig.client_cert == ""))
                    {
                        Log.TextLog.Log("Proxy", "\tReceived client certificate is empty");
                        return;
                    }

                    try
                    {
                        byte[] tmp = Convert.FromBase64String(tmpConfig.server_cert);
                        tmp = null;
                    }
                    catch (Exception ex)
                    {
                        Log.TextLog.Log("Proxy", "\tReceived server Base64 parse error: " + ex.Message);
                        Log.TextLog.Log("Proxy", "\t" + tmpConfig.server_cert);
                        return;
                    }

                    try
                    {
                        byte[] tmp = Convert.FromBase64String(tmpConfig.client_cert);
                        tmp = null;
                    }
                    catch (Exception ex)
                    {
                        Log.TextLog.Log("Proxy", "\tReceived client Base64 parse error: " + ex.Message);
                        Log.TextLog.Log("Proxy", "\t" + tmpConfig.client_cert);
                        return;
                    }

                    String checkError = "";
                    if (!CATools.CheckSignedCertificate(CATools.LoadCert(Resource1.IAMServerCertificateRoot), CATools.LoadCert(Convert.FromBase64String(tmpConfig.server_cert)), out checkError))
                    {
                        Log.TextLog.Log("Proxy", "\tReceived server certificate check error: " + checkError);
                        return;
                    }
                    else
                    {
                        Log.TextLog.Log("Proxy", "\tReceived server certificate OK");
                    }

                    String key = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(tmpConfig.fqdn));

                    checkError = "";
                    if (!CATools.CheckSignedCertificate(CATools.LoadCert(Resource1.IAMServerCertificateRoot), CATools.LoadCert(Convert.FromBase64String(tmpConfig.client_cert), key), out checkError))
                    {
                        Log.TextLog.Log("Proxy", "\tReceived client certificate check error: " + checkError);
                        return;
                    }
                    else
                    {
                        Log.TextLog.Log("Proxy", "\tReceived client certificate OK");
                    }

                    //Depois de todas as checagem seta a config atual
                    config = tmpConfig;

                    if (logProxy != null)
                        logProxy = null;

                    logProxy = new LogProxy(basePath, config.server_cert);

                    key = null;

                    //Inicia download dos plugins
                    if ((config.plugins != null))
                    {

                        foreach (PluginConfig p in config.plugins)
                            UpdatePlugin(new Uri(p.uri), p.assembly);


                    }

                    //Salva o arquivo de configuração localmente
                    try
                    {
                        Log.TextLog.Log("Proxy", "\tUpdating local config file 'config.json'");
                        File.WriteAllBytes(Path.Combine(basePath, "config.json"), Encoding.UTF8.GetBytes(config.ToJsonString()));
                    }
                    catch(Exception ex)
                    {
                        Log.TextLog.Log("Proxy", "\tError saving local config file 'config.json'");
#if DEBUG
                        Log.TextLog.Log("Proxy", "\tError: " + ex.Message);
#endif
                        return;
                    }

                    //Lista a configuração de cada plugin
                    try
                    {
                        if (!Directory.Exists(Path.Combine(basePath, "plugins")))
                            Directory.CreateDirectory(Path.Combine(basePath, "plugins"));

                        List<PluginConnectorBase> plugins = Plugins.GetPlugins<PluginConnectorBase>(Path.Combine(basePath, "plugins"));

                        if ((config.plugins == null))
                            throw new Exception("Plugin list is null");

#if DEBUG
                        if (plugins.Count == 0)
                            Log.TextLog.Log("Proxy", "\tLocal plugin list is empty");
#endif

                        foreach (PluginConnectorBase p in plugins)
                        {

#if DEBUG
                            Log.TextLog.Log("Proxy", "\tStarting config loader to " + p.GetPluginId().AbsoluteUri);
#endif

                            foreach (PluginConfig pConf in config.plugins)
                            {
#if DEBUG
                                Log.TextLog.Log("Proxy", "\tChecking local math to received plugin config " + pConf.uri.ToLower());
#endif
                                if (pConf.uri.ToLower() == p.GetPluginId().AbsoluteUri.ToLower())
                                {
                                    TextLog.Log("Proxy", "\tResource x plugin " + pConf.resource_plugin + " for " + p.GetPluginId().AbsoluteUri);
                                }

                            }

                        }


                    }
                    catch (Exception ex)
                    {
                        Log.TextLog.Log("Proxy", "\tError loading plugins " + ex.Message);
                    }


                    //Inicia os coletores (1 para cada plugin)
                    //Somente dos plugins ativos
                    try
                    {
                        Log.TextLog.Log("Proxy", "List plugins...");
                        List<PluginBase> plugins = Plugins.GetPlugins<PluginBase>(Path.Combine(basePath, "plugins"));

                        Log.TextLog.Log("Proxy", (plugins != null ? (plugins.Count > 0 ? plugins.Count + " plugins" : "plugins is empty") : "plugins is null"));

                        if ((config.plugins != null))
                        {
                            foreach (PluginBase p in plugins)
                                if (config.plugins.Exists(p1 => (p1.uri.ToLower() == p.GetPluginId().AbsoluteUri.ToLower())))
                                    StartPlugin(p.GetPluginId(), "\t", false);
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.TextLog.Log("Proxy", "\tError loading plugins " + ex.Message);
                    }

                    //configTimer = new Timer(new TimerCallback(ConfigTimer), null, 900000, 900000);
                    //Log.TextLog.Log("Proxy", "\tScheduled for new configuration download in 900 seconds");


                }
                catch (Exception ex)
                {
                    Log.TextLog.Log("Proxy", "\tError parsing the configuration (" + ex.Message + ")");
                }
            }
            finally
            {
                Log.TextLog.Log("Proxy", "Finishing config download thread");
                downloadConfigExecuting = false;

                lastSync.config = 0;

            }

        }


        private void StartPlugin(Uri plugin, String logPrefix, Boolean forceStart)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Verb = "runas";

            Log.TextLog.Log("Proxy", logPrefix + " StartPlugin: " + plugin.AbsoluteUri);

            if (!forceStart && onlinePlugins.Exists(o => (o.AbsoluteUri.ToLower() == plugin.AbsoluteUri.ToLower())))
            {
                Log.TextLog.Log("Proxy", logPrefix + " Plugin already started ");
                return;
            }

            switch (plugin.Scheme.ToLower())
            {
                case "connector":
                case "agent":
                    psi.Arguments = plugin.AbsoluteUri.ToLower();
                    psi.FileName = Path.Combine(basePath, "IAMPluginStarter.exe");
                    break;

            }

            Process p = new Process();
            p.StartInfo = psi;
            p.EnableRaisingEvents = true;
            p.Exited += new EventHandler(p_Exited);
            p.Start();

            Log.TextLog.Log("Proxy", logPrefix + "Started " + plugin.AbsoluteUri + " on pid " + p.Id);

            //Adiciona na tabela para previnir a inicialização de mais de um executável por plugin
            onlinePlugins.Add(plugin);

        }

        void p_Exited(object sender, EventArgs e)
        {
            Process p = (Process)sender;

            //prevenção contra finalização instanTanea do plugin e não remoção da tabela caso isso ocorra
            Thread.Sleep(1000);

            onlinePlugins.Remove(new Uri(p.StartInfo.Arguments));

            Log.TextLog.Log("Proxy", "Collector for " + p.StartInfo.Arguments + " with pid " + p.Id + " closed");

            Thread.Sleep(5000);
            StartPlugin(new Uri(p.StartInfo.Arguments), "", true);
        }

        private void UpdatePlugin(Uri pluginUri, String assembly)
        {



            Log.TextLog.Log("Proxy", "\tStarting plugin update " + pluginUri.AbsoluteUri);

            try
            {

                DirectoryInfo pluginsDir = new DirectoryInfo(Path.Combine(basePath, "plugins"));

                if (!pluginsDir.Exists)
                {
                    pluginsDir.Create();
                    Log.TextLog.Log("Proxy", "\tPlugin directory created on " + pluginsDir.FullName);
                }

                DateTime updatedDate = new DateTime(1970, 01, 01);


                FileInfo pluginFile = new FileInfo(Path.Combine(pluginsDir.FullName, assembly));
                if (pluginFile.Exists)
                    updatedDate = pluginFile.LastWriteTimeUtc;

                Uri uri = new Uri((localConfig.UseHttps ? "https://" : "http://") + localConfig.Server + "/proxy/api.aspx");

                Byte[] fData = new Byte[0];
                if (pluginFile.Exists)
                    fData = File.ReadAllBytes(pluginFile.FullName);

                String hash = CATools.SHA1Checksum(fData);

                JsonGeneric req = new JsonGeneric();
                req.fields = new String[] { "updated", "uri", "checksum" };
                req.data.Add(new String[] { updatedDate.ToString("o"), pluginUri.AbsoluteUri.ToLower(), hash });

                WebClient client = new WebClient();

                String tst = JSON.GetRequest("plugin_download", localConfig.Hostname, req.ToJsonString());

                Byte[] data = client.UploadData(uri, Encoding.UTF8.GetBytes(JSON.GetRequest("plugin_download", localConfig.Hostname, req.ToJsonString())));

                Console.WriteLine("Received plugin data from server. Datalen " + data.Length);
                Log.TextLog.Log("Proxy", "\tReceived plugin data from server. Datalen " + data.Length);

                String Json = Encoding.UTF8.GetString(data);

                if (Json.Length > 0)
                {

                    DownloadPlugin resp = JSON.Deserialize<DownloadPlugin>(Json);
                    if (resp.status == "updated")
                    {
                        Log.TextLog.Log("Proxy", "\tCurrent version of the plugin is the most updated");
                    }
                    else if (resp.status == "outdated")
                    {
                        //Realiza a decriptografia dos dados recebidos
                        fData = new Byte[0];
                        try
                        {
                            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
                            using (CryptApi cApi = CryptApi.ParsePackage(CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass), Convert.FromBase64String(resp.content)))
                                fData = cApi.clearData;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Decrypt error " + ex.Message);
                        }

                        DateTime serverUpdated = updatedDate;
                        
                        DateTime.TryParseExact(resp.date, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out serverUpdated);

                        if (updatedDate > serverUpdated)
                        {
                            Log.TextLog.Log("Proxy", "\tLocal version of this plugin is more updated than server version, ignoring server version...");
                            Log.TextLog.Log("Proxy", "\tIf you want to update local version, just delete local plugin dll");
                        }else{
                            try
                            {
                                if (pluginFile.Exists)
                                    File.Delete(pluginFile.FullName + ".old");

                                if (pluginFile.Exists)
                                    File.Move(pluginFile.FullName, pluginFile.FullName + ".old");
                            }
                            catch { }

                            File.WriteAllBytes(pluginFile.FullName, fData);

                            pluginFile.LastAccessTimeUtc = serverUpdated;
                            pluginFile.CreationTimeUtc = serverUpdated;
                            pluginFile.LastWriteTimeUtc = serverUpdated;

                            Log.TextLog.Log("Proxy", "\tPlugin file writed");
                            //throw new Exception("Server error " + resp.error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.TextLog.Log("Proxy", "\tErro on plugin update " + pluginUri.AbsoluteUri + ": " + ex.Message);
            }

            Log.TextLog.Log("Proxy", "\tFinishing plugin update " + pluginUri.AbsoluteUri);
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

        public void SaveLogToSend()
        {

            if (logProxy != null)
                logProxy.SaveToSend("log-proxy");

        }


        private void AddProxyLog(UserLogLevel level, String text, String additionalInfo)
        {
            if (logProxy != null)
                logProxy.AddLog(LogKey.Proxy_Event, "Proxy", 0, "0", "", level, 0, 0, text, additionalInfo);
        }

        private void AddPackageTrack(String source, String resource, String filename, String packageId, String flow, String text)
        {
            if (logProxy != null)
                logProxy.AddPackageTrack(source, resource, filename, packageId, flow, text);
        }


    }
}
