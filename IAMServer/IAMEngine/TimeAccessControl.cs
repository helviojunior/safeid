using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

using IAM.Config;
using IAM.PluginManager;
using IAM.PluginInterface;
using IAM.Log;
using IAM.CA;
//using IAM.SQLDB;
using IAM.LocalConfig;
using IAM.License;
using IAM.GlobalDefs;
using IAM.GlobalDefs;
using SafeTrend.Json;

namespace IAM.Engine
{
    class TimeAccessControl
    {
        private Timer procTimer;
        private ServerLocalConfig localConfig;
        private String basePath;
        private Boolean executing;

        public TimeAccessControl(ServerLocalConfig config)
        {
            this.localConfig = config;
            
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);
        }

        public void Start()
        {
            procTimer = new Timer(new TimerCallback(TmrCallback), this, 1000, 60000);
        }

        private void TmrCallback(Object sender)
        {
            if (executing)
                return;

            executing = true;

            TextLog.Log("Engine", "Time access control", "Starting processor timer");
            IAMDatabase db = null;

            try
            {
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 600;

                //Seleciona as entidades/identidades vinculadas a um resource x plugin que tenha controle de acesso por horário
                DataTable dtRegs = db.Select("select i.id, i.temp_locked, e.id entity_id, r.name resource_name from entity e with(nolock) inner join [identity] i with(nolock) on e.id = i.entity_id  inner join resource_plugin rp with(nolock) on i.resource_plugin_id = rp.id and i.resource_plugin_id = rp.id inner join resource r with(nolock) on rp.resource_id = r.id  inner join resource_plugin_role_time_acl acl with(nolock) on acl.resource_plugin_id = rp.id  inner join role r1 with(nolock) on r1.id = acl.role_id inner join identity_role ir with(nolock) on ir.identity_id = i.id and ir.role_id = r1.id where r.enabled = 1 and rp.enabled = 1 group by i.id, i.temp_locked, e.id, r.name");

                if ((dtRegs == null) || (dtRegs.Rows.Count == 0))
                {
                    TextLog.Log("Engine", "Time access control", "\t0 registers to process");
                    return;
                }

                foreach (DataRow dr in dtRegs.Rows)
                    try
                    {
                        using (EntityTimeControl eAcl = new EntityTimeControl(db, (Int64)dr["id"]))
                        {
                            StringBuilder tLog = new StringBuilder();
                            EntityTimeControl.ProccessLog log = new EntityTimeControl.ProccessLog(delegate(String text)
                            {
                                tLog.AppendLine(text);

#if DEBUG
                                TextLog.Log("Engine", "Time access control", text);
#endif

                            });

                            eAcl.OnLog += log;
                            eAcl.Process((Boolean)dr["temp_locked"]);
                            eAcl.OnLog -= log;

                            if ((Boolean)dr["temp_locked"] != eAcl.Locked)
                                db.AddUserLog((eAcl.Locked ? LogKey.User_TempLocked : LogKey.User_TempUnlocked), null, "Engine", UserLogLevel.Info, 0, 0, 0, 0, 0, Int64.Parse(dr["entity_id"].ToString()), Int64.Parse(dr["id"].ToString()), "Identity of resource " + dr["resource_name"] + (eAcl.Locked ? " locked by the time profile" : " unlocked by the time profile"), tLog.ToString());
                            
                            tLog.Clear();
                            tLog = null;
                        }
                    }
                    catch(Exception ex) {
                        TextLog.Log("Engine", "Time access control", "\tError on time control processor " + ex.Message);
                    }

                Console.WriteLine("");
                
            }
            catch (Exception ex)
            {
                db.AddUserLog(LogKey.Import, null, "Engine", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Error on time control processor", ex.Message);
                TextLog.Log("Engine", "Time access control", "\tError on time control processor timer " + ex.Message);
            }
            finally
            {
                TextLog.Log("Engine", "Time access control", "Finishing processor timer");

                if (db != null)
                    db.closeDB();

                executing = false;
            }
        }

    }
}
