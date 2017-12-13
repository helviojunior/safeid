using System;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Data.SqlClient;
using IAM.Config;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.UserProcess
{
    class TestTimer : IAMDatabase
    {
        private Stopwatch stopWatch;
        public String text;
        private RegistryProcess.ProccessLog procLog;
        private Boolean executing = false;

        public TestTimer(String text, RegistryProcess.ProccessLog procLog)
        {
#if !DEBUG
            return;
#endif

            this.text = text;
            this.procLog = procLog;
            this.stopWatch = new Stopwatch();
            this.Start();
        }

        public void Start()
        {
#if !DEBUG
            return;
#endif
            stopWatch.Start();
            executing = true;
        }

        public void Stop(SqlConnection conn, SqlTransaction trans)
        {
#if !DEBUG
            return;
#endif
            Stop(conn, trans, null);
        }

        public void Stop(SqlConnection conn, SqlTransaction trans, String postText)
        {

            if (!executing)
                return;

#if !DEBUG
            return;
#endif

            executing = false;

            stopWatch.Stop();

            if (postText != null)
                text += ": " + postText;

            TimeSpan ts = stopWatch.Elapsed;

            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:0000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);

            if (procLog != null)
                procLog("[" + elapsedTime + "] " + text);

            //So grava os itens com tempo maior que 200ms
            if (ts.TotalMilliseconds > 200)
            {
                try
                {
                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@total_ms", typeof(Int64)).Value = ts.TotalMilliseconds;
                    par.Add("@text_total", typeof(String), elapsedTime.Length).Value = elapsedTime;
                    par.Add("@text", typeof(String), text.Length).Value = text;

                    ExecuteNonQuery(conn, "insert into import_profiler (total_ms, text_total, text) values (@total_ms, @text_total, @text)", System.Data.CommandType.Text, par, trans);
                }
                catch { }
            }
        }
    }
}
