using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorBaseDeployPackageMapping : IDisposable
    {
        public String dataName;
        public String dataType;
        public Boolean isId;
        public Boolean isUnique;
        public Boolean isPassword;
        public Boolean isLogin;
        public Boolean isName;

        public PluginConnectorBaseDeployPackageMapping(String dataName, String dataType, Boolean isId, Boolean isUnique, Boolean isPassword)
            : this(dataName, dataType, isId, isUnique, isPassword, false, false) { }

        public PluginConnectorBaseDeployPackageMapping(String dataName, String dataType, Boolean isId, Boolean isUnique, Boolean isPassword, Boolean isLogin, Boolean isName)
        {
            this.dataName = dataName;
            this.dataType = dataType;
            this.isId = isId;
            this.isUnique = isUnique;
            this.isPassword = isPassword;
            this.isLogin = isLogin;
            this.isName = isName;
        }

        public void Dispose()
        {
            this.dataName = null;
            this.dataType = null;
        }
    }
}
