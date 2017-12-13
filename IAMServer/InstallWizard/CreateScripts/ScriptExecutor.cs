using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SafeTrend.Data;

namespace InstallWizard
{
    class ScriptExecutor
    {
        public void Run(DbBase ctx, Object transaction, IEnumerable<ICreateScript> scripts)
        {
            RunInDatabase(ctx, transaction, scripts, -1);
        }

        public void Run(DbBase ctx, Object transaction, IEnumerable<ICreateScript> scripts, double minSerial)
        {
            RunInDatabase(ctx, transaction, scripts, minSerial);
        }

        private void RunInDatabase(DbBase ctx, Object transaction, IEnumerable<ICreateScript> scripts, double minSerial)
        {

            foreach (ICreateScript item in scripts)
            {
                if (item.Serial < minSerial)
                    continue;

                try
                {
                    if (!string.IsNullOrEmpty(item.Precondition))
                    {
                        var preConditionResult = ctx.ExecuteScalar<Int64>(item.Precondition, transaction);
                        if (preConditionResult == 0)
                            continue;
                    }
                    ctx.ExecuteNonQuery(item.Command, transaction);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message + " on execute script " + item.GetType().Name + " (" + item.Serial + ")", ex);
                }

            }
        }
    }
}
