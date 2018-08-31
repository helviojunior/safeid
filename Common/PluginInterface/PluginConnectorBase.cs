using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;

namespace IAM.PluginInterface
{

    public abstract class PluginConnectorBase : PluginBase, IDisposable
    {
        public abstract PluginConnectorConfigActions[] GetConfigActions();

        public abstract void ProcessImport(String cacheId, String importId, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping);
        public abstract void ProcessImportAfterDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping);
        public abstract void ProcessDeploy(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping);
        public abstract void ProcessDelete(String cacheId, PluginConnectorBaseDeployPackage package, Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping);
        public abstract Boolean TestPlugin(Dictionary<String, Object> config, List<PluginConnectorBaseDeployPackageMapping> fieldMapping);
        public abstract PluginConnectorBaseFetchResult FetchFields(Dictionary<String, Object> config);

        public abstract event NotityChangeUserEvent NotityChangeUser;
        public abstract event NotityChangeUserEvent NotityDeletedUser;
        public abstract event ImportPackageUserEvent ImportPackageUser;
        public abstract event ImportPackageStructEvent ImportPackageStruct;

        public static PasswordStrength CheckPasswordStrength(String password, String fullName)
        {
            IPasswordStrength pc = new IPasswordStrength(fullName, password);
            return pc.Result;
        }

        public static Boolean CheckPasswordComplexity(String password, Boolean uppercase, Boolean lowercase, Boolean numeric, Boolean special)
        {
            if (password.Length < 8)
                return false;

            Boolean contain = false;
            if (uppercase)
            {
                for (Int32 i = 65; i <= 90; i++)
                {
                    String tmp = Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) });
                    if (password.IndexOf(tmp) > -1)
                    {
                        contain = true;
                        break;
                    }
                }

                if (!contain)
                    return false;
            }

            if (lowercase)
            {
                contain = false;
                for (Int32 i = 97; i <= 122; i++)
                {
                    String tmp = Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) });
                    if (password.IndexOf(tmp) > -1)
                    {
                        contain = true;
                        break;
                    }
                }

                if (!contain)
                    return false;
            }

            if (numeric)
            {
                contain = false;
                for (Int32 i = 0; i <= 9; i++)
                {
                    String tmp = i.ToString();
                    if (password.IndexOf(tmp) > -1)
                    {
                        contain = true;
                        break;
                    }
                }

                if (!contain)
                    return false;
            }

            if (special)
            {
                String tmp2 = "\"'!@#$%¨&*()-=_+<>;:{}[]";
                contain = false;
                foreach (Char c in tmp2)
                    if (password.IndexOf(c.ToString()) > -1)
                    {
                        contain = true;
                        break;
                    }

                if (!contain)
                    return false;
            }

            return true;
        }

        public void Dispose()
        {

        }

    }
}
