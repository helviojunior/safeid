using System;
using System.Collections.Generic;
using System.Text;
using ActiveDirectory;
using GoogleAdmin;
using IAM.PluginInterface;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;


namespace Test
{
    class Program
    {


        static void Main(string[] args)
        {

            /*
            ###########################################################
            ##  This is an Sample test of Plugins
            ##  This methods are implemented of Proxy Architheture
            ###########################################################
            */

            //Deploy

            //Mapping of fields
            List<PluginConnectorBaseDeployPackageMapping> fieldMapping = new List<PluginConnectorBaseDeployPackageMapping>();
            fieldMapping.Add(new PluginConnectorBaseDeployPackageMapping("Email", "string", false, false, false));
            fieldMapping.Add(new PluginConnectorBaseDeployPackageMapping("Nome_Compl", "string", false, false, false));
            fieldMapping.Add(new PluginConnectorBaseDeployPackageMapping("CPF", "string", false, false, false));
            fieldMapping.Add(new PluginConnectorBaseDeployPackageMapping("Setor", "string", false, false, false));


            PluginConnectorBaseDeployPackage pkg = new PluginConnectorBaseDeployPackage();
            pkg.container = "BaseContainer";
            pkg.entityId = 10;
            pkg.fullName = new FullName("Helvio Junior");
            pkg.identityId = 11;
            pkg.locked = false;
            pkg.login = "teste001";
            pkg.password = "@aa123456!";
            pkg.pluginData = new List<PluginConnectorBasePackageData>();
            pkg.pluginData.Add(new PluginConnectorBasePackageData("Email", "helvio_junior@hotmail.com", "string"));
            pkg.properties.Add(new PluginConnectorBasePackageData("Nome_Compl", "Helvio Junior", "string"));
            pkg.pluginAction.Add(new PluginConnectorBaseDeployPackageAction(PluginActionType.Add, "Test Role", "group", "Teste_direct"));



            //Config definition
            //This config depends on Plugin Config requirements
            Dictionary<String, Object> config = new Dictionary<String, Object>();
            config.Add("sample1", "sample_value");
            config.Add("sample2", "sample_value");
            config.Add("sample3", "sample_value");

            //Sample of config to use at LDAP
            config.Add("ldap_server", "ldap_ip_address");
            config.Add("username", "ldap_user");
            config.Add("password", "ldap_password");


            ActiveDirectory.ActiveDirectoryPlugin pg = new ActiveDirectory.ActiveDirectoryPlugin();
            pg.Log += new IAM.PluginInterface.LogEvent(pg_Log);
            pg.Log2 += new LogEvent2(pg_Log2);
            pg.NotityChangeUser += new NotityChangeUserEvent(pg_NotityChangeUser);
            pg.ImportPackageUser += new ImportPackageUserEvent(pg_ImportPackageUser);

            //Process Import of all users from Plugin
            pg.ProcessImport("CacheID", "ImporID", config, fieldMapping);

            //Process Deploy of Package
            pg.ProcessDeploy("CacheID", pkg, config, fieldMapping);

            //Process Import from the same user of an deployed User
            pg.ProcessImportAfterDeploy("CacheID", pkg, config, fieldMapping);

        }

        static void pg_ImportPackageUser(PluginConnectorBaseImportPackageUser package)
        {

        }

        static void pg_NotityDeletedUser(object sender, long entityId, long identityId = 0)
        {

        }

        static void pg_ImportPackage(PluginConnectorBaseImportPackageUser pckage)
        {

        }

        static void pg_NotityChangeUser(Object sender, long entityId, long identityId)
        {

        }

        static void pg_Log2(Object sender, PluginLogType type, long entityId, long identityId, string text, string additionalData)
        {
            Console.WriteLine(text);
        }

        static void pg_Registry(string importId, string registryId, string dataName, string dataValue, string dataType)
        {
            Console.WriteLine(registryId + "\t" + dataName + "\t" + dataValue);
        }

        static void pg_Log(Object sender, IAM.PluginInterface.PluginLogType type, string text)
        {
            Console.WriteLine(text);
        }
    }
}
