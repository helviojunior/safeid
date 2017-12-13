using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SafeTrend.Data;
using SafeTrend.Data.Update;

namespace InstallWizard
{
    public class IAMDbInstall : IDisposable
    {
        private IAM.GlobalDefs.IAMDatabase db;

        private IEnumerable<ICreateScript> scripts = null;

        public IAMDbInstall( IAM.GlobalDefs.IAMDatabase database )
        {
            scripts = CreateScriptRepository.GetScriptsBySqlProviderName("System.Data.SqlClient");

            if ((scripts == null) || (scripts.Count<ICreateScript>() == 0))
                return;

            this.db = database;

        }

        public void Create(Object transaction)
        {
            new ScriptExecutor().Run(db, transaction, this.scripts);
        }

        public void Dispose()
        {

        }
    }
}
