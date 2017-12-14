/// 
/// @file Sample.cs
/// <summary>
/// Sample plugin, to be used as Sample in order to create new integration Plugins 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 14/12/2017
/// $Id: Sample.cs, v1.0 2017/11/14 Helvio Junior $

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IAM.PluginInterface;


namespace SamplePlugin
{
    /// <summary>
    /// SamplePlugin class. This Class should be an instance of PluginConnectorBase Class
    /// </summary>
    public class SamplePlugin : PluginConnectorBase
    {

        public override String GetPluginName() { return "Sample Plugin"; }
        public override String GetPluginDescription() { return "Sample Plugin"; }

        public override Uri GetPluginId()
        {
            return new Uri("connector://iam/plugins/sample");
        }

        /// <summary>
        /// Get Config Fields, this Method is used to provide wich configurations is needed by this plugin. 
        /// This configuration will be filled by user at "Web Console > Resource x Plugin > Plugin Parameters"
        /// This configuration needs be all configuration needed to this plugin do his job.
        /// </summary>
        public override PluginConfigFields[] GetConfigFields()
        {
            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            conf.Add(new PluginConfigFields("URL do servidor", "server_uri", "URL do servidor", PluginConfigTypes.Uri, true, @"http://localhost/"));
            conf.Add(new PluginConfigFields("Usuário", "username", "Usuário", PluginConfigTypes.String, true, ""));
            conf.Add(new PluginConfigFields("Senha", "password", "Senha", PluginConfigTypes.Password, true, ""));
            conf.Add(new PluginConfigFields("Número da Empresa no Senior (NumEmp)", "numemp", "Campo NumEmp no Senior", PluginConfigTypes.String, true, ""));
            
            return conf.ToArray();
        }

        /// <summary>
        /// Get Config Actions, this Method is used to provide wich actions are available when an User is attached at one IM Profile. 
        /// This configuration will be filled by user at "Web Console > Resource x Plugin > Roles"
        /// </summary>
        public override PluginConnectorConfigActions[] GetConfigActions()
        {

            List<PluginConnectorConfigActions> conf = new List<PluginConnectorConfigActions>();
            conf.Add(new PluginConnectorConfigActions("Add at Group", "group", "Add/Delete an user in a Group", "Group Name", "group_name", "Group Name at integrated system to add the User"));

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
                    iLog(this, PluginLogType.Information, "Field " + cf.Name + " (" + cf.Key + "): error on get data -> " + ex.Message);
                }
            }

            try
            {
                

                ret.success = true;
            }
            catch (Exception ex)
            {
                iLog(this, PluginLogType.Error, ex.Message);
                ret.success = false;
            }

            return ret;
        }

        public override Boolean TestPlugin(Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            return true;
        }

        /// <summary>
        /// Validate Config Fields, this Method is used to Platform check if all required cofiguration was provided by Admin. 
        /// This configuration will be filled by user at "Web Console > Resource x Plugin > Plugin Parameters"
        /// </summary>
        /// <param name="config">Dictionary with all configuration filled by Admin</param>
        /// <param name="checkDirectoryExists">Check or not an directory, if there is an physicaly directory to be checked</param>
        /// <param name="Log">Event Handler to log an information</param>
        /// <param name="checkImport">Check or not Import Config</param>
        /// <param name="checkDeploy">Check or not Deploy Config</param>
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

        /// <summary>
        /// Process Import, this Method is called when the system is Import all users from replicated system 
        /// </summary>
        /// <param name="cacheId">Unique ID to cache</param>
        /// <param name="importId">Unique ID to import package</param>
        /// <param name="config">Dictionary with all configuration filled by Admin</param>
        /// <param name="fieldMapping">Fields mapping filled by Admin at "Web Console > Resource x Plugin > Fields Mapping"</param>
        public override void ProcessImport(String cacheId, String importId, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

            String lastStep = "CheckInputConfig";

            if (!CheckInputConfig(config, true, Log))
                return;

            StringBuilder processLog = new StringBuilder();
            StringBuilder debugLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;

            try
            {
                
                lastStep = "Get user List";

                for (Int32 user = 0; user <= 10; user++)
                {
                    //One package by user
                    PluginConnectorBaseImportPackageUser package = new PluginConnectorBaseImportPackageUser(importId);

                    package.AddProperty("username", "test-" + user, "string");// User Login from replicated system
                    package.AddProperty("full_name", "Test Name to Sample User " + user, "string");// User Full Name from replicated system

                    ImportPackageUser(package);

                }

            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process import (" + lastStep + "): " + ex.Message);

                Log2(this, PluginLogType.Error, 0, 0, "Error on process import: " + ex.Message, "Last step: " + lastStep);
            }
            finally
            {

                if (logType != PluginLogType.Information)
                    processLog.AppendLine(debugLog.ToString());

                Log2(this, logType, 0, 0, "Import executed", processLog.ToString());
                processLog.Clear();
                processLog = null;

                debugLog.Clear();
                debugLog = null;
            }


        }

        /// <summary>
        /// Process Import After User Deploy, this Method is called when the system is Import only deployed user from replicated system 
        /// </summary>
        /// <param name="cacheId">Unique ID to cache</param>
        /// <param name="package">Deployed package</param>
        /// <param name="config">Dictionary with all configuration filled by Admin</param>
        /// <param name="fieldMapping">Fields mapping filled by Admin at "Web Console > Resource x Plugin > Fields Mapping"</param>
        public override void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            String lastStep = "CheckInputConfig";

            if (!CheckInputConfig(config, true, Log))
                return;


            StringBuilder processLog = new StringBuilder();
            StringBuilder debugLog = new StringBuilder();
            PluginLogType logType = PluginLogType.Information;
            String importId = Guid.NewGuid().ToString();

            try
            {

                lastStep = "Get User Data";

                PluginConnectorBaseImportPackageUser packageImport = new PluginConnectorBaseImportPackageUser(importId);
                //package.AddProperty(key, u[key], "string");
                ImportPackageUser(packageImport);



            }
            catch (Exception ex)
            {
                logType = PluginLogType.Error;
                processLog.AppendLine("Error on process import (" + lastStep + "): " + ex.Message);

                Log2(this, PluginLogType.Error, 0, 0, "Error on process import: " + ex.Message, "Last step: " + lastStep);
            }
            finally
            {

                if (logType != PluginLogType.Information)
                    processLog.AppendLine(debugLog.ToString());

                Log2(this, logType, 0, 0, "Import executed", processLog.ToString());
                processLog.Clear();
                processLog = null;

                debugLog.Clear();
                debugLog = null;
            }


        }

        /// <summary>
        /// Process Deploy, this Method is called when the system is deploing an user to replicated system 
        /// </summary>
        /// <param name="cacheId">Unique ID to cache</param>
        /// <param name="package">Deployed package</param>
        /// <param name="config">Dictionary with all configuration filled by Admin</param>
        /// <param name="fieldMapping">Fields mapping filled by Admin at "Web Console > Resource x Plugin > Fields Mapping"</param>
        public override void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {

        }

        /// <summary>
        /// Process Delete, this Method is called when the system is deleting an user to replicated system 
        /// </summary>
        /// <param name="cacheId">Unique ID to cache</param>
        /// <param name="package">Deployed package</param>
        /// <param name="config">Dictionary with all configuration filled by Admin</param>
        /// <param name="fieldMapping">Fields mapping filled by Admin at "Web Console > Resource x Plugin > Fields Mapping"</param>
        public override void ProcessDelete(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping)
        {
            
        }


        public override event LogEvent Log;
        public override event ImportPackageUserEvent ImportPackageUser;
        public override event ImportPackageStructEvent ImportPackageStruct;
        public override event LogEvent2 Log2;
        public override event NotityChangeUserEvent NotityChangeUser;
        public override event NotityChangeUserEvent NotityDeletedUser;
    }
}
