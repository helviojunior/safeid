using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Globalization;

//using IAM.SQLDB;
using IAM.GlobalDefs;
using IAM.TimeACL;
using SafeTrend.Json;

namespace IAM.Engine
{
    public class EntityTimeControl : IDisposable
    {
        private Int64 identityId;

        public Boolean Locked { get; internal set; }

        public delegate void ProccessLog(String text);
        public event ProccessLog OnLog;

        private IAMDatabase db;

        public EntityTimeControl(IAMDatabase db, Int64 identityId)
        {
            this.db = db;
            this.identityId = identityId;

        }

        public void Log(String text)
        {
            if (OnLog != null)
                OnLog(text);
        }

        public void Process(Boolean atualState) 
        {
            
            //Define o status atual como bloqueado
            Log("Starting processor on identity " + this.identityId);
            Log("Set user default value with locked");
            this.Locked = true;

            try
            {
                //Verifica se o usuário atual é um administrador
                DataTable dtSysRoles = db.Select("select distinct r.* from sys_entity_role ser inner join sys_role r on ser.role_id = r.id inner join [identity] i on ser.entity_id = i.entity_id where i.id = " + this.identityId);
                if ((dtSysRoles != null) && (dtSysRoles.Rows.Count > 0))
                    foreach (DataRow dr in dtSysRoles.Rows)
                        if ((Boolean)dr["sa"] || (Boolean)dr["ea"])
                        {
                            this.Locked = false;
                            Log("User is " + ((Boolean)dr["sa"] ? "system admin" : "enterprise admin") + " and can not be locked");
                            break;
                        }

                if (!this.Locked)
                    return;

                Log("Is not admin, continue");

                //Verifica se está na lista de liberação
                DataTable dtIgnore = db.Select("select * from identity_acl_ignore a where identity_id = " + this.identityId + " and GETDATE() between a.start_date and a.end_date");
                if ((dtIgnore != null) && (dtIgnore.Rows.Count > 0))
                {
                    
                    Log("User has in ignore list between " + IAM.GlobalDefs.MessageResource.FormatDate((DateTime)dtIgnore.Rows[0]["start_date"], false) + " and " + IAM.GlobalDefs.MessageResource.FormatDate((DateTime)dtIgnore.Rows[0]["end_date"], false));
                    this.Locked = false;
                    return;
                }

                Log("Not in ignore/white list");

                //Verifica as roles que tenham controle de horário e que esta identity está vinculada
                DataTable dtAcl = db.Select("select distinct acl.*, r.name resource_name, r1.name role_name from entity e with(nolock) inner join [identity] i with(nolock) on e.id = i.entity_id  inner join resource_plugin rp with(nolock) on i.resource_plugin_id = rp.id and i.resource_plugin_id = rp.id inner join resource r with(nolock) on rp.resource_id = r.id  inner join resource_plugin_role_time_acl acl with(nolock) on acl.resource_plugin_id = rp.id  inner join role r1 with(nolock) on r1.id = acl.role_id inner join identity_role ir with(nolock) on ir.identity_id = i.id and ir.role_id = r1.id where r.enabled = 1 and rp.enabled = 1 and i.id = " + this.identityId);
                if ((dtAcl != null) && (dtAcl.Rows.Count > 0))
                    foreach (DataRow dr in dtAcl.Rows)
                    {
                        if (!this.Locked)
                            return;

                        TimeAccess time = new TimeAccess();
                        time.FromJsonString(dr["time_acl"].ToString());

                        Log("--------------");
                        Log("Resource " + dr["resource_name"] + " and role " + dr["role_name"]);
                        Log("\tTime rule " + time.ToString());

                        switch (time.Type)
                        {
                            case TimeAccessType.NotDefined:
                                //indiferente
                                break;

                            case TimeAccessType.Never:
                                Log("\tLocked by resource " + dr["resource_name"] + " and role " + dr["role_name"]);
                                break;

                            case TimeAccessType.Always:
                                Log("\tUnlocked by " + dr["resource_name"] + " and role " + dr["role_name"]);
                                this.Locked = false;
                                return; //Retorna a liberação
                                break;

                            case TimeAccessType.SpecificTime:
                                //Verifica o horário
                                if (time.BetweenTimes(DateTime.Now))
                                {
                                    Log("\tBetween time window");

                                    Log("\tUnlocked by " + dr["resource_name"] + " and role " + dr["role_name"]);
                                    this.Locked = false;
                                    return; //Retorna a liberação

                                }

                                break;
                        }

                        
                    }
            }
            finally
            {
                Log("--------------");
                Log("Locked? " + this.Locked);

                if (atualState != this.Locked)
                    db.ExecuteNonQuery("update [identity] set temp_locked = " + (this.Locked ? "1" : "0") + " where id = " + this.identityId, CommandType.Text, null);
            }
                       
        }

        public void Dispose()
        {

        }
    }
}
