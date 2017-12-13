using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SafeTrend.Data;
using SafeTrend.Data.Update;

namespace IAM.GlobalDefs.Update
{
    public class IAMDbUpdate : IDisposable
    {
        private IAMDatabase db;
        private Int64 serial = 0;
        private Int64 updateSerial = 0;

        private IEnumerable<IUpdateScript> scripts = null;

        public IAMDbUpdate(String server, String dbName, String username, String password)
        {
            scripts = UpdateScriptRepository.GetScriptsBySqlProviderName("System.Data.SqlClient");

            if ((scripts == null) || (scripts.Count<IUpdateScript>() == 0))
                return;

            foreach (IUpdateScript s in scripts)
                if ((Int64)s.Serial > updateSerial)
                    updateSerial = (Int64)s.Serial;

            this.db = new IAMDatabase(server, dbName, username, password);

            //Verifica se a base de dados está atualizada
            try
            {
                this.serial = db.ExecuteScalar<Int64>("select isnull(max([version]),0) from [db_ver]");
            }
            catch
            {
                this.serial = 0;
            }
        }

        public Boolean IsUpToDate
        {
            get
            {
                return (this.serial == updateSerial);
            }
        }

        public void Update()
        {
            if (IsUpToDate)
                return;

            new AutomaticUpdater().Run(db, this.scripts, this.serial + 1);
        }

        public void Dispose()
        {
            if (this.db != null)
                this.db.Dispose();
            this.db = null;
        }
    }
}
