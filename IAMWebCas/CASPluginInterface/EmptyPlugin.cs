using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CAS.PluginInterface
{

    public class EmptyPlugin : CASConnectorBase
    {
        public override event LogEvent Log;
        public override String GetPluginName() { return "CAS Empty Plugin"; }
        public override Uri GetPluginId() { return new Uri("connector://CAS/plugins/empty"); }

        public override PluginConfigFields[] GetConfigFields()
        {

            List<PluginConfigFields> conf = new List<PluginConfigFields>();
            return conf.ToArray();
        }

        public EmptyPlugin()
            : base(null, null, null, null) { }

        protected override CASTicketResult iGrant(CASTicketResult oldToken, String username, String password)
        {
            CASTicketResult ret = new CASTicketResult();
            return ret;
        }

        public override CASChangePasswordResult ChangePassword(CASTicketResult ticket, String password)
        {
            return new CASChangePasswordResult("Not implemented");
        }

        public override CASChangePasswordResult ChangePassword(CASUserInfo users, String password)
        {
            return new CASChangePasswordResult("Not implemented");
        }

        public override CASUserInfo FindUser(String username)
        {
            return new CASUserInfo("Not implemented");
        }

    }
}
