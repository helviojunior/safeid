using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IAM.PluginInterface;

namespace Plugin.csv
{
    public class CSVPlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "IAM CSV Plugin"; }
        public override String GetPluginDescription() { return "Plugin para integragir com base de dados em arquivo texto separado por virgula"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/csvplugin");
        }

        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("Dir. Importação", "import_folder", "Diretório de importação", PluginConfigTypes.String, true, @"c:\IAMProxy\csvimport"));
            conf.Add(new PluginConfigFields("Delimitador", "delimiter", "Delimitador de coluna no CSV", PluginConfigTypes.String, true, ","));

            return conf.ToArray();
        }


        public override PluginConnectorConfigActions[] GetConfigActions()
        {

            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();

            return conf.ToArray();
        }

        public override PluginConnectorBaseFetchResult FetchFields(Dictionary<String, Object> config)
        {
            PluginConnectorBaseFetchResult ret = new PluginConnectorBaseFetchResult();

            LogEvent iLog = new LogEvent(delegate(Object sender, PluginLogType type, string text)
            {
                if (Log != null)
                    Log(sender, type, text);
            });


            if (!CheckInputConfig(config, true, iLog, true, true))
            {
                ret.success = false;
                return ret;
            }

            List<PluginConfigFields> cfg = new List<PluginConfigFields>();
            PluginConfigFields[] tmpF = this.GetConfigFields();
            foreach (PluginConfigFields cf in tmpF)
            {
                try
                {
                    iLog(this, PluginLogType.Information, "Field " + cf.Name + " (" + cf.Key + "): " + (config.ContainsKey(cf.Key) ? config[cf.Key].ToString() : "empty"));
                }
                catch (Exception ex)
                {
                    iLog(this, PluginLogType.Error, "Field " + cf.Name + " (" + cf.Key + "): error on get data -> " + ex.Message);
                }
            }

            try
            {


                DirectoryInfo importDir = null; ;
                try
                {
                    importDir = new DirectoryInfo(config["import_folder"].ToString());
                    if (!importDir.Exists)
                        throw new DirectoryNotFoundException();
                }
                catch (Exception ex)
                {
                    iLog(this, PluginLogType.Error, "Erro ao localizar o diretório de importação (" + config["import_folder"].ToString() + "): " + ex.Message);
                    return ret;
                }

                foreach (FileInfo f in importDir.GetFiles("*.csv"))
                {

                    iLog(this, PluginLogType.Information, "Iniciando mapeamento do arquivo '" + f.Name + "'");

                    try
                    {
                        String[] firstLineData = new String[0];
                        Boolean firstLine = true;
                        Int32 count = 0;
                        Int32 cLine = 0;

                        using (FileStream fs = f.OpenRead())
                        using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            while (!reader.EndOfStream)
                            {
                                cLine++;

                                String line = reader.ReadLine();

                                try
                                {
                                    String[] parts = line.Split(config["delimiter"].ToString().ToCharArray());

                                    if (firstLine)
                                    {
                                        firstLineData = parts;
                                        firstLine = false;

                                        for (Int32 c = 0; c < firstLineData.Length; c++)
                                            if (!String.IsNullOrWhiteSpace(firstLineData[c]) && !ret.fields.ContainsKey(firstLineData[c]))
                                                ret.fields.Add(firstLineData[c], new List<string>());


                                        if (!ret.fields.ContainsKey("csv_file"))
                                            ret.fields.Add("csv_file", new List<string>());

                                        if (!ret.fields.ContainsKey("csv_file_line"))
                                            ret.fields.Add("csv_file_line", new List<string>());


                                    }
                                    else
                                    {
                                        if (firstLineData.Length != parts.Length)
                                            throw new Exception("Linha com número diferente de colunas");

                                        count++;

                                        if (count >= 10)
                                            break;

                                        ret.fields["csv_file"].Add(f.Name);
                                        ret.fields["csv_file_line"].Add(cLine.ToString());

                                        for (Int32 c = 0; c < firstLineData.Length; c++)
                                            ret.fields[firstLineData[c]].Add(parts[c]);

                                    }

                                }
                                catch (Exception ex)
                                {
                                    iLog(this, PluginLogType.Error, "Falha ao importar a linha '" + line + "' do arquivo '" + f.Name + "': " + ex.Message);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        iLog(this, PluginLogType.Error, "Falha ao mapear os dados do arquivo '" + f.Name + "': " + ex.Message);
                    }
                }
                

                ret.success = true;
            }
            catch (Exception ex)
            {
                iLog(this, PluginLogType.Error, ex.Message);
            }

            return ret;
        }

        public override Boolean TestPlugin(Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            return true;
        }

        public override Boolean ValidateConfigFields(Dictionary<String, Object> config, Boolean checkDirectoryExists, LogEvent Log, Boolean checkImport, Boolean checkDeploy)
        {

            LogEvent iLog = new LogEvent(delegate(Object sender, PluginLogType type, string text)
            {
                if (Log != null)
                    Log(sender, type, text);
            });

            if (!CheckInputConfig(config, checkDirectoryExists, iLog, checkImport, checkDeploy))
                return false;

            //Verifica as informações próprias deste plugin
            return true;
        }


        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            //Não implementado
        }

        public override void ProcessImport(String cacheId, String importId, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            if (!CheckInputConfig(config, true, Log))
                return;

            DirectoryInfo importDir = null; ;
            try
            {
                importDir = new DirectoryInfo(config["import_folder"].ToString());
                if (!importDir.Exists)
                    throw new DirectoryNotFoundException();
            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, "Erro ao localizar o diretório de importação (" + config["import_folder"].ToString() + "): " + ex.Message);
                return;
            }

            foreach (FileInfo f in importDir.GetFiles("*.csv"))
            {

                Log(this, PluginLogType.Information, "Iniciando importação do arquivo '"+ f.Name +"'");
                
                try
                {
                    String[] firstLineData = new String[0];
                    Boolean firstLine = true;

                    using (FileStream fs = f.OpenRead())
                    using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        while (!reader.EndOfStream)
                        {
                            String line = reader.ReadLine();

                            PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);
                            try
                            {

                                String[] parts = line.Split(config["delimiter"].ToString().ToCharArray());

                                if (firstLine)
                                {
                                    firstLineData = parts;
                                    firstLine = false;
                                }
                                else
                                {
                                    if (firstLineData.Length != parts.Length)
                                        throw new Exception("Linha com número diferente de colunas");


                                    for (Int32 c = 0; c < firstLineData.Length; c++)
                                        package.AddProperty(firstLineData[c], parts[c], "string");

                                }

                                ImportPackageUser(package);
                            }
                            catch (Exception ex)
                            {
                                Log(this, PluginLogType.Error, "Falha ao importar a linha '" + line + "' do arquivo '" + f.Name + "': " + ex.Message);
                            }
                            finally
                            {
                                package.Dispose();
                                package = null;
                            }
                        }
                    }

                    f.MoveTo(f.FullName + ".imported");

                    Log(this, PluginLogType.Information, "Importação do arquivo '" + f.Name + "' concluida");



                }
                catch (Exception ex)
                {
                    Log(this, PluginLogType.Error, "Falha ao importar os dados do arquivo '"+ f.Name +"': " + ex.Message);
                }
            }

            //realiza o processo de importação do txt
            
        }

        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            return;

            if (!CheckInputConfig(config, true, Log))
                return;


            DirectoryInfo importDir = null; ;
            try
            {
                importDir = new DirectoryInfo(config["import_folder"].ToString());
                if (!importDir.Exists)
                    throw new DirectoryNotFoundException();
            }
            catch (Exception ex)
            {
                Log(this, PluginLogType.Error, "Erro ao localizar o diretório de importação (" + config["import_folder"].ToString() + "): " + ex.Message);
                return;
            }

            FileInfo f = new FileInfo(Path.Combine(importDir.FullName, "export"+ DateTime.Now.ToString("yyyyMMddHHmmss-ffffff") +".txt"));

            if (!f.Directory.Exists)
                f.Directory.Create();

            using (FileStream fs = f.Open(FileMode.Create))
            using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
            {
                foreach (PluginConnectorBasePackageData dt in package.pluginData)
                    w.WriteLine(dt.dataName + "," + dt.dataType + "," + dt.dataValue);

                w.Flush();
                w.Close();
                fs.Close();
            }

        }

        public override void ProcessDelete(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            //Nda
        }

        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }
}
