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
using System.Security.Cryptography;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail;

using IAM.Config;
using IAM.Log;
//using IAM.SQLDB;
using IAM.Scheduler;
using IAM.LocalConfig;
using IAM.GlobalDefs;
using SafeTrend.Json;
using SafeTrend.Report;

namespace IAM.Report
{
    public partial class IAMReport : ServiceBase
    {

        ServerLocalConfig localConfig;
        String basePath = "";

        Timer reportTimer;
        Timer statusTimer;
        Boolean executing = false;
        Boolean executing2 = false;

        public IAMReport()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            /*************
             * Carrega configurações
             */

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);

            localConfig = new ServerLocalConfig();
            localConfig.LoadConfig();

            if ((localConfig.SqlServer == null) || (localConfig.SqlServer.Trim() == ""))
                StopOnError("Parâmetro 'sqlserver' não localizado no arquivo de configuração 'server.conf'", null);
            
            if ((localConfig.SqlDb == null) || (localConfig.SqlDb.Trim() == ""))
                StopOnError("Parâmetro 'sqldb' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlUsername == null) || (localConfig.SqlUsername.Trim() == ""))
                StopOnError("Parâmetro 'sqlusername' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlPassword == null) || (localConfig.SqlPassword.Trim() == ""))
                StopOnError("Parâmetro 'sqlpassword' não localizado no arquivo de configuração 'server.conf'", null);


            Int32 cnt = 0;
            Int32 stepWait = 15000;
            while (cnt <= 10)
            {
                try
                {
                    IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                    db.openDB();

                    db.closeDB();

                    break;
                }
                catch (Exception ex)
                {
                    if (cnt < 10)
                    {
                        TextLog.Log("Report", "Falha ao acessar o banco de dados: " + ex.Message);
                        Thread.Sleep(stepWait);
                        stepWait = stepWait * 2;
                        cnt++;
                    }
                    else
                    {
                        StopOnError("Falha ao acessar o banco de dados", ex);
                    }
                }
            }

            
            /*************
             * Inicia timer que processa os arquivos
             */

            reportTimer = new Timer(new TimerCallback(ReportTimer), null, 1000, 60000);
            statusTimer = new Timer(new TimerCallback(TmrServiceStatusCallback), null, 100, 10000);
        }


        private void TmrServiceStatusCallback(Object o)
        {
            IAMDatabase db = null;
            try
            {
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 600;

                db.ServiceStatus("Report", JSON.Serialize2(new { host = Environment.MachineName, executing = executing }), null);

                db.closeDB();
            }
            catch { }
            finally
            {
                if (db != null)
                    db.Dispose();

                db = null;
            }
        }


        private void BuildReport(Int64 reportId)
        {
            IAMDatabase db = null;
            try
            {

                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();

                DataTable dtS = db.Select("select * from report where id = " + reportId);

                if ((dtS == null) || (dtS.Rows.Count == 0))
                    return;

                //Chega as propriedades básicas do report
                List<MailAddress> recipents = new List<MailAddress>();

                if ((dtS.Rows[0]["recipient"] != DBNull.Value) && (!String.IsNullOrWhiteSpace((String)dtS.Rows[0]["recipient"])))
                {
                    String[] tTo = dtS.Rows[0]["recipient"].ToString().Split(",;".ToCharArray());
                    foreach (String s in tTo)
                        try
                        {
                            if (!String.IsNullOrWhiteSpace(s))
                                recipents.Add(new MailAddress(s));
                        }
                        catch { }
                }

                if (recipents.Count == 0)
                    throw new Exception("No valid email informed in recipient");


                switch (dtS.Rows[0]["type"].ToString().ToLower())
                {
                    case "audit":
                        auditReport(db, dtS, recipents);
                        break;

                    case "integrity":
                        integrityTextReport(db, dtS, recipents);
                        break;

                    default:
                        usersTextReport(db, dtS, recipents);
                        break;
                }

            }
            catch (Exception ex)
            {
                TextLog.Log("Report", "\tError building report: " + ex.Message);
                try
                {
                    db.AddUserLog(LogKey.Report, DateTime.Now, "Report", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Erro building report", ex.Message);
                }
                catch { }
            }
            finally
            {
                if (db != null)
                    db.Dispose();
            }
                        
        }

        [Serializable()]
        class FieldItem
        {
            [OptionalField]
            public String data_name;

            [OptionalField]
            public String field_id;

            [OptionalField]
            public String data_type;

            [OptionalField]
            public String value;
        }

        /*[{"data_name":"id","field_id":"16","data_type":"string","value":"110059940913696826169"},{"data_name":"lastLoginTime","field_id":"14","data_type":"datetime","value":"1969- 12-31T22:00:00.0000000- 02:00"},{"data_name":"creationTime","field_id":"12","data_type":"datetime","value":"2013-12- 05T06:01:54.0000000- 02:00"},{"data_name":"primaryEmail","field_id":"4","data_type":"string","value":"adriana.tenorio@fael.edu.br"},{"data_name":"fullname","field_id":"1","data_type":"string","value":"Adriana Aparecida Goll Tenorio"}] [{"data_name":"id","field_id":"16","data_type":"string","value":"110059940913696826169"},{"data_name":"lastLoginTime","field_id":"14","data_type":"datetime","value":"1969- 12-31T22:00:00.0000000- 02:00"},{"data_name":"creationTime","field_id":"12","data_type":"datetime","value":"2013-12- 05T06:01:54.0000000- 02:00"},{"data_name":"primaryEmail","field_id":"4","data_type":"string","value":"adriana.tenorio@fael.edu.br"},{"data_name":"fullname","field_id":"1","data_type":"string","value":"Adriana Aparecida Goll Tenorio"}]*/

        static public void auditReport(IAMDatabase db, DataTable dtS, List<MailAddress> recipents)
        {

            Int64 enterpriseId = (Int64)dtS.Rows[0]["enterprise_id"];

            List<FileInfo> files = new List<FileInfo>();
            StringBuilder body = new StringBuilder();

            DataTable dtContext = db.Select("select distinct c.* from context c with(nolock) where c.enterprise_id = " + enterpriseId + " order by name");
            if ((dtContext != null) && (dtContext.Rows.Count > 0))
                foreach (DataRow drC in dtContext.Rows)
                {

                    PDFReport report = new PDFReport(dtS.Rows[0]["title"].ToString() + " - " + drC["name"], "SafeTrend - SafeID v1.0");
                    body.AppendLine(dtS.Rows[0]["title"].ToString() + " - " + drC["name"]);
                    

                    FileInfo tmpFile = new FileInfo(Path.Combine(Path.GetTempPath(), "audit-" + DateTime.Now.ToString("yyyyMMdd") + "-" + drC["id"] + "-" + DateTime.Now.ToString("hhmmssfffff") + ".pdf"));
                    if (tmpFile.Exists)
                        tmpFile.Delete();

                    body.AppendLine("    Arquivo: " + tmpFile.Name);
                    Int64 erroCount = 0;

                    DataTable dtResource = db.Select("select distinct r.* from resource r with(nolock) inner join resource_plugin rp  with(nolock) on rp.resource_id = r.id inner join context c with(nolock) on c.id = r.context_id where c.id = " + drC["id"] + " order by name");
                    if ((dtResource != null) && (dtResource.Rows.Count > 0))
                        foreach (DataRow drR in dtResource.Rows)
                        {

                            DataTable dtRP = db.Select("select distinct rp.*, p.name plugin_name, p.scheme, p.id plugin_id from resource r with(nolock) inner join resource_plugin rp with(nolock) on rp.resource_id = r.id inner join plugin p with(nolock) on rp.plugin_id = p.id where r.id = " + drR["id"] + " order by p.name");
                            if ((dtRP != null) && (dtRP.Rows.Count > 0))
                            {
                                report.AddH1("Recurso " + drR["name"]);

                                foreach (DataRow drRP in dtRP.Rows)
                                {
                                    report.AddH2("Plugin " + drRP["plugin_name"]);

                                    PluginConfig pluginConfig = new PluginConfig(db.Connection, drRP["scheme"].ToString(), (Int64)drRP["plugin_id"], (Int64)drRP["id"]);

                                    DataTable dtAudit = db.Select("select * from audit_identity a where resource_plugin_id = " + drRP["id"] + " and update_date >= DATEADD(day,-15,getdate()) order by full_name");
                                    if ((dtAudit != null) && (dtAudit.Rows.Count > 0))
                                    {
                                        Int64 count = 1;

                                        foreach (DataRow drAudit in dtAudit.Rows)
                                        {
                                            erroCount++;

                                            try
                                            {

                                                report.AddParagraph(String.Format("{0:0000}. {1}", count, drAudit["full_name"].ToString()), 1, 3, true);

                                                switch (drAudit["event"].ToString().ToLower())
                                                {
                                                    case "not_exists":
                                                        report.AddParagraph("Problema encontrado: Usuário inexistente no SafeID", 2, 3, false);
                                                        break;

                                                    case "locked":
                                                        report.AddParagraph("Problema encontrado: Usuário inexistente no SafeID e não pode ser inserido pois está com status de bloqueado.", 2, 3, false);
                                                        break;

                                                    case "input_filter_empty":
                                                        report.AddParagraph("Problema encontrado: Informação para identificação não encontrado.", 2, 3, false);
                                                        break;

                                                    default:
                                                        report.AddParagraph("Problema encontrado: desconhecido", 2, 3, false);
                                                        break;
                                                }


                                                report.AddParagraph("Registrio criado em " + MessageResource.FormatDate((DateTime)drAudit["create_date"], false) + " e atualizado em " + MessageResource.FormatDate((DateTime)drAudit["update_date"], false), 2, 3, false);


                                                List<FieldItem> fields = JSON.Deserialize<List<FieldItem>>(drAudit["fields"].ToString());

                                                List<String> keys = new List<string>();
                                                List<String> others = new List<string>();

                                                foreach (FieldItem fi in fields)
                                                {

                                                    foreach (PluginConfigMapping m in pluginConfig.mapping)
                                                        if ((m.data_name.ToLower() == fi.data_name.ToLower()))
                                                            if (m.is_id || m.is_unique_property)
                                                            {
                                                                if (!keys.Contains(m.field_name + " = " + fi.value))
                                                                    keys.Add(m.field_name + " = " + fi.value);
                                                            }
                                                            else
                                                            {
                                                                if (!others.Contains(m.field_name + " = " + fi.value))
                                                                    others.Add(m.field_name + " = " + fi.value);
                                                            }

                                                }


                                                report.AddParagraph("Identificadores: ", 2, 3, false);
                                                for (Int32 c = 0; c < keys.Count; c++)
                                                    report.AddParagraph(keys[c], 3, (c == keys.Count - 1 ? 3 : 0), false);


                                                report.AddParagraph("Outros dados: ", 2, 3, false);
                                                for (Int32 c = 0; c < others.Count; c++)
                                                    report.AddParagraph(others[c], 3, (c == others.Count - 1 ? 6 : 0), false);

                                            }
                                            catch (Exception ex)
                                            {
                                                report.AddParagraph("Erro processando informação: " + ex.Message, 1, 0, false);
                                            }

                                            count++;
                                        }
                                    }
                                    else
                                    {
                                        report.AddParagraph("Nenhuma inconsistência encontrada", 1, 0, false);
                                    }

                                }
                            }
                            else
                            {
                                report.AddH1("Recurso " + drR["name"], false);
                                report.AddParagraph("Nenhum plugin vinculado a este recurso.");
                            }

                            //select distinct rp.* from resource r with(nolock) inner join resource_plugin rp with(nolock) on rp.resource_id = r.id where r.id = 1


                        }

                    body.AppendLine("    Inconsistências reportadas: " + erroCount);

                    //Salva e envia o relatório
                    report.SaveToFile(tmpFile.FullName);

                    files.Add(new FileInfo( tmpFile.FullName));

                    body.AppendLine("");
                }

            List<Attachment> atts = new List<Attachment>();
            foreach(FileInfo f in files)
                atts.Add(new Attachment(f.FullName));

            try
            {

                sendEmail(db, dtS.Rows[0]["title"].ToString(), recipents, body.ToString(), false, atts);
            }
            catch (Exception ex)
            {
                db.AddUserLog(LogKey.Report, DateTime.Now, "Report", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Erro sending report", ex.Message);
            }

            //Exclui os arquivos temporários
            foreach(FileInfo f in files)
                try
                {
                    f.Delete();
                }
                catch { }
        }

        static public void usersTextReport(IAMDatabase db, DataTable dtS, List<MailAddress> recipents)
        {

            StringBuilder errors = new StringBuilder();

            DataTable dtU = db.Select("select e.*, c.name context_name from entity e inner join context c on c.id = e.context_id where e.deleted = 0 and c.enterprise_id = " + dtS.Rows[0]["enterprise_id"] + " order by c.name, e.full_name");
            if ((dtU == null) || (dtU.Rows.Count == 0))
                return;

            DataTable dtUsers = new DataTable();
            dtUsers.Columns.Add("context_name", typeof(String));
            dtUsers.Columns.Add("full_name", typeof(String));
            dtUsers.Columns.Add("login", typeof(String));
            dtUsers.Columns.Add("create_date", typeof(DateTime));
            dtUsers.Columns.Add("last_login", typeof(DateTime));
            dtUsers.Columns.Add("locked", typeof(String));

            Dictionary<String, String> title = new Dictionary<string, string>();
            title.Add("context_name", "Contexto");
            title.Add("full_name", "Nome completo");
            title.Add("login", "Login");
            title.Add("create_date", "Data de criação");
            title.Add("last_login", "Ultimo login");
            title.Add("locked", "Bloqueado");

            List<Int64> fields = new List<Int64>();

            DataTable dtF = db.Select("select distinct f.id, f.name, rp.[order] from report_mapping rp inner join field f on rp.field_id = f.id  order by rp.[order], f.name");
            if ((dtF != null) && (dtF.Rows.Count > 0))
            {
                foreach (DataRow dr in dtF.Rows)
                {
                    fields.Add((Int64)dr["id"]);
                    dtUsers.Columns.Add("f_" + dr["id"], typeof(String));
                    title.Add("f_" + dr["id"], dr["name"].ToString());
                }
            }

            DataTable dtUsers2 = dtUsers.Clone();

            String fieldFilter = String.Join(",", fields);

            DateTime dateRef = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-1);

            foreach (DataRow dU in dtU.Rows)
            {
                try
                {


                    DataRow newItem = dtUsers.NewRow();
                    newItem["context_name"] = dU["context_name"];
                    newItem["full_name"] = dU["full_name"];
                    newItem["login"] = dU["login"];
                    newItem["create_date"] = dU["create_date"];
                    newItem["last_login"] = (dU["last_login"] == DBNull.Value ? DBNull.Value : dU["last_login"]);
                    newItem["locked"] = (((Boolean)dU["locked"]) ? "Y" : "N");

                    if (fields.Count > 0)
                    {
                        //Primeiro realiza a busca e preenchimento dos dados da entidade
                        try
                        {
                            DataTable dtUserData = db.Select("select efe.field_id, efe.value from [entity] e inner join entity_field efe on efe.entity_id = e.id where e.id = " + dU["id"] + " group by efe.field_id, efe.value");
                            foreach (DataRow dUD in dtUserData.Rows)
                            {
                                if (newItem["f_" + dUD["field_id"]] == DBNull.Value)
                                    newItem["f_" + dUD["field_id"]] = dUD["value"];
                            }
                        }
                        catch { }


                        //Primeiro realiza a busca e preenchimento dos dados com as informações dos plugins de entrada
                        try
                        {
                            DataTable dtUserData = db.Select("select ife.field_id, ife.value from [identity] i inner join identity_field ife on ife.identity_id = i.id inner join resource_plugin rp on i.resource_plugin_id = rp.id where rp.enable_import = 1 and rp.permit_add_entity = 1 and i.entity_id = " + dU["id"] + " and ife.field_id in (" + fieldFilter + ")  and not exists (select 1 from identity_block_inheritance bi where bi.identity_id = i.id) group by ife.field_id, ife.value");
                            foreach (DataRow dUD in dtUserData.Rows)
                            {
                                if (newItem["f_" + dUD["field_id"]] == DBNull.Value)
                                    newItem["f_" + dUD["field_id"]] = dUD["value"];
                            }
                        }
                        catch { }

                        //Depois com os outros plugins
                        try
                        {
                            DataTable dtUserData = db.Select("select ife.field_id, ife.value from [identity] i inner join identity_field ife on ife.identity_id = i.id where i.entity_id = " + dU["id"] + " and ife.field_id in (" + fieldFilter + ")  and not exists (select 1 from identity_block_inheritance bi where bi.identity_id = i.id) group by ife.field_id, ife.value");
                            foreach (DataRow dUD in dtUserData.Rows)
                            {
                                if (newItem["f_" + dUD["field_id"]] == DBNull.Value)
                                    newItem["f_" + dUD["field_id"]] = dUD["value"];
                            }
                        }
                        catch { }
                    }

                    dtUsers.Rows.Add(newItem.ItemArray);

                    //Caso a criação seja do dia anterior ou deste dia inclui na segunda tabela tb.
                    if (((DateTime)dU["create_date"]).CompareTo(dateRef) == 1)
                        dtUsers2.Rows.Add(newItem.ItemArray);
                
                }
                catch (Exception ex)
                {
                    errors.AppendLine("Error processing registry: " + ex.Message);
                }

            }

            if (errors.ToString() != "")
                db.AddUserLog(LogKey.Report, null, "Report", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Report error", errors.ToString());

            ReportBase rep1 = new ReportBase(dtUsers, title);
            ReportBase rep2 = new ReportBase(dtUsers2, title);

            List<Attachment> atts = new List<Attachment>();

            try
            {
                using (MemoryStream ms1 = new MemoryStream(Encoding.UTF8.GetBytes(rep1.GetTXT())))
                using (MemoryStream ms2 = new MemoryStream(Encoding.UTF8.GetBytes(rep1.GetXML("Usuários", ""))))
                using (MemoryStream ms3 = new MemoryStream(Encoding.UTF8.GetBytes(rep2.GetTXT())))
                using (MemoryStream ms4 = new MemoryStream(Encoding.UTF8.GetBytes(rep2.GetXML("Usuários", ""))))
                {
                    atts.Add(new Attachment(ms1, "all.txt"));
                    //atts.Add(new Attachment(ms2, "all.xls"));
                    atts.Add(new Attachment(ms3, "created.txt"));
                    //atts.Add(new Attachment(ms4, "created.xls"));

                    sendEmail(db, dtS.Rows[0]["title"].ToString(), recipents, dtUsers2.Rows.Count + " criados deste " + dateRef.ToString("yyyy-MM-dd HH:mm:ss"), false, atts);
                }
            }
            catch (Exception ex)
            {
                db.AddUserLog(LogKey.Report, DateTime.Now, "Report", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Erro sending report", ex.Message);
            }

            /*
            DataTable created = db.Select("select * from vw_entity_mails where create_date between CONVERT(datetime, convert(varchar(10),DATEADD(DAY, -1, GETDATE()),120) + ' 00:00:00', 120) and CONVERT(datetime, convert(varchar(10),getdate(),120) + ' 23:59:59', 120) order by context_name, full_name");
            DataTable all = db.Select("select * from vw_entity_mails order by context_name, full_name");
            Dictionary<String, String> title = new Dictionary<string, string>();
            title.Add("context_name", "Contexto");
            title.Add("full_name", "Nome completo");
            title.Add("login", "Login");
            title.Add("create_date", "Data de criação");
            title.Add("last_login", "Ultimo login");
            title.Add("mail", "E-mail");
            title.Add("locked", "Bloqueado");

            ReportBase rep1 = new ReportBase(created, title);
            ReportBase rep2 = new ReportBase(all, title);

            List<Attachment> atts = new List<Attachment>();

            using (MemoryStream ms1 = new MemoryStream(Encoding.UTF8.GetBytes(rep1.GetTXT())))
            using (MemoryStream ms2 = new MemoryStream(Encoding.UTF8.GetBytes(rep1.GetXML("Usuários", ""))))
            using (MemoryStream ms3 = new MemoryStream(Encoding.UTF8.GetBytes(rep2.GetTXT())))
            using (MemoryStream ms4 = new MemoryStream(Encoding.UTF8.GetBytes(rep2.GetXML("Usuários", ""))))
            {
                atts.Add(new Attachment(ms1, "created.txt"));
                atts.Add(new Attachment(ms2, "created.xls"));
                atts.Add(new Attachment(ms3, "all.txt"));
                atts.Add(new Attachment(ms4, "all.xls"));

                sendEmail(db, "Listagem de usuários em " + DateTime.Now.ToString("dd/MM/yyyy"), recipents, created.Rows.Count + " usuários criados de " + DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy") + " até " + DateTime.Now.ToString("dd/MM/yyyy"), false, atts);
            }*/

        }

        static public void integrityTextReport(IAMDatabase db, DataTable dtS, List<MailAddress> recipents)
        {

            StringBuilder errors = new StringBuilder();

            DataTable dtL = db.Select("select l.text from logs l where text like 'Integrity check error: Multiplus entities%' and l.date >= DATEADD(day,-1,getdate()) and l.enterprise_id = " + dtS.Rows[0]["enterprise_id"] + " group by l.text");
            if (dtL == null)
                return;

            DataTable dtErrors = new DataTable();
            dtErrors.Columns.Add("text", typeof(String));

            Dictionary<String, String> title = new Dictionary<string, string>();
            title.Add("text", "Texto");
            
            List<String> duplicatedEntities = new List<String>();

            foreach (DataRow dU in dtL.Rows)
            {
                try
                {


                    DataRow newItem = dtErrors.NewRow();
                    newItem["text"] = dU["text"];

                    dtErrors.Rows.Add(newItem.ItemArray);


                    //Captura somente os IDs das entidades
                    Regex rex = new Regex(@"\((.*?)\)");
                    Match m = rex.Match(dU["text"].ToString());
                    if (m.Success)
                    {
                        String[] entities = m.Groups[1].Value.Replace(" ", "").Split(",".ToCharArray());
                        duplicatedEntities.AddRange(entities);
                    }


                }
                catch (Exception ex)
                {
                    errors.AppendLine("Error processing registry: " + ex.Message);
                }

            }



            Dictionary<String, String> title2 = new Dictionary<string, string>();
            title2.Add("id", "Entity ID");
            title2.Add("login", "Login");
            title2.Add("full_name", "Nome Completo");
            title2.Add("change_password", "Ultima troca de senha");
            title2.Add("last_login", "Ultimo Login ");


            DataTable dtUsr = new DataTable();
            dtUsr.Columns.Add("id", typeof(Int64));
            dtUsr.Columns.Add("login", typeof(String));
            dtUsr.Columns.Add("full_name", typeof(String));
            dtUsr.Columns.Add("change_password", typeof(DateTime));
            dtUsr.Columns.Add("last_login", typeof(DateTime));

            //select e.id, e.login, e.full_name, e.change_password, e.last_login from entity e where id in (10583, 13065) order by e.full_name

            DataTable dtU = db.Select("select e.id, e.login, e.full_name, e.change_password, e.last_login from entity e where id in (" + String.Join(",", duplicatedEntities) + ") order by e.full_name");
            
            if (errors.ToString() != "")
                db.AddUserLog(LogKey.Report, null, "Report", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Report error", errors.ToString());

            ReportBase rep1 = new ReportBase(dtErrors, title);

            List<Attachment> atts = new List<Attachment>();

            try
            {
                using (MemoryStream ms1 = new MemoryStream(Encoding.UTF8.GetBytes(rep1.GetTXT())))
                {
                    atts.Add(new Attachment(ms1, "integrity-check.txt"));

                    if (dtU != null)
                    {
                        ReportBase rep2 = new ReportBase(dtU, title2);
                        using (MemoryStream ms2 = new MemoryStream(Encoding.UTF8.GetBytes(rep2.GetTXT())))
                        {
                            atts.Add(new Attachment(ms2, "integrity-users.txt"));

                            sendEmail(db, dtS.Rows[0]["title"].ToString(), recipents, dtL.Rows.Count + " erros de integridade", false, atts);
                        }
                    }
                    else
                    {

                        sendEmail(db, dtS.Rows[0]["title"].ToString(), recipents, dtL.Rows.Count + " erros de integridade", false, atts);
                    }
                }
            }
            catch (Exception ex)
            {
                db.AddUserLog(LogKey.Report, DateTime.Now, "Report", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Erro sending report", ex.Message);
            }


        }

        static public void sendEmail(IAMDatabase db, String Subject, List<MailAddress> to, String body, Boolean isHTML, List<Attachment> atts)
        {
            sendEmail(db, Subject, to, null, body, isHTML, atts);
        }

        static public void sendEmail(IAMDatabase db, String Subject, List<MailAddress> to, String replyTo, String body, Boolean isHTML, List<Attachment> atts)
        {

            using (ServerDBConfig conf = new ServerDBConfig(db.Connection))
            {

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(conf.GetItem("mailFrom"));
                foreach (MailAddress t in to)
                    mail.To.Add(t);

                mail.Subject = Subject;

                mail.IsBodyHtml = isHTML;
                mail.Body = body;

                if (replyTo != null)
                {
                    try
                    {
                        mail.ReplyTo = new MailAddress(replyTo);
                    }
                    catch { }
                }

                if ((atts != null) && (atts.Count > 0))
                    foreach (Attachment a in atts)
                        mail.Attachments.Add(a);

                /*Non-Encrypted	AUTH		25 (or 587)
                Secure (TLS)	StartTLS	587
                Secure (SSL)	SSL			465*/

                SmtpClient client = new SmtpClient();
                client.Host = conf.GetItem("smtpServer");
                client.Port = 25;
                client.EnableSsl = false;

                try
                {
                    Int32 port = Int32.Parse( conf.GetItem("smtpPort"));
                    switch (port)
                    {
                        case 587:
                            client.EnableSsl = true;
                            break;

                        case 465:
                            client.EnableSsl = true;
                            break;
                    }
                }
                catch { }

                if (!String.IsNullOrEmpty(conf.GetItem("username")) && !String.IsNullOrEmpty(conf.GetItem("password")))
                    client.Credentials = new System.Net.NetworkCredential(conf.GetItem("username"), conf.GetItem("password"));

                System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(RemoteCertificateValidationCallback);

                client.Send(mail);

                client = null;
                mail = null;
            }
        }


        static private bool RemoteCertificateValidationCallback(Object sender,
                                                               X509Certificate certificate,
                                                               X509Chain chain,
                                                               SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }


        private void ReportTimer(Object state)
        {

            if (executing)
                return;

            executing = true;

            //TextLog.Log("Report", "Starting report timer");
            try
            {
                //IAMDeploy deploy = new IAMDeploy("report", localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                //deploy.DeployAll();

                IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();

                DataTable dtS = db.Select("select * from report_schedule");

                try
                {

                    //Processa um a um dos agendamentos
                    foreach (DataRow dr in dtS.Rows)
                        CheckSchedule(db, (Int64)dr["id"], (Int64)dr["report_id"], dr["schedule"].ToString(), (DateTime)dr["next"]);

                }
                catch (Exception ex)
                {
                    TextLog.Log("Report", "\tError on report timer schedule: " + ex.Message);
                    db.AddUserLog(LogKey.Report, null, "Report", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Error on report scheduler", ex.Message);
                }

                db.closeDB();
            }
            catch (Exception ex1)
            {
                TextLog.Log("Report", "\tError on report timer: " + ex1.Message);
                
            }
            finally
            {
                //TextLog.Log("Report", "\tScheduled for new report process in 60 seconds");
                //TextLog.Log("Report", "Finishing report timer");
                executing = false;
            }
        }

        private void CheckSchedule(IAMDatabase db, Int64 scheduleId, Int64 reportId, String jSonSchedule, DateTime next)
        {

            DateTime date = DateTime.Now;
            TimeSpan ts = date - new DateTime(1970, 01, 01);

            Schedule schedule = new Schedule();
            try
            {
                schedule.FromJsonString(jSonSchedule);
            }
            catch
            {
                schedule = null;
            }

            if (schedule == null)
                return;

            //Check Start date

            TimeSpan stDateTs = next - new DateTime(1970, 01, 01);
            if (ts.TotalSeconds >= stDateTs.TotalSeconds) //Data e hora atual maior ou igual a data que se deve iniciar
            {
                TextLog.Log("Report", "[" + reportId + "] Starting execution");

                try
                {
                    BuildReport(reportId);
                }
                catch (Exception ex)
                {
                    TextLog.Log("Report", "[" + reportId + "] Error on execution " + ex.Message);
                }
                finally
                {
                    TextLog.Log("Report", "[" + reportId + "] Execution completed");

                    //Agenda a próxima execução
                    DateTime calcNext = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, schedule.TriggerTime.Hour, schedule.TriggerTime.Minute, 0);
                    DateTime nextExecute = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
                    switch (schedule.Trigger)
                    {
                        case ScheduleTtiggers.Dialy:
                            calcNext = calcNext.AddDays(1);
                            break;

                        case ScheduleTtiggers.Monthly:
                            calcNext = calcNext.AddMonths(1);
                            break;

                        case ScheduleTtiggers.Annually:
                            calcNext = calcNext.AddYears(1);
                            break;
                    }

                    //TextLog.Log("PluginStarter", "Calc 1 " + calcNext.ToString("yyyy-MM-dd HH:mm:ss"));

                    if (schedule.Repeat > 0)
                    {
                        if (nextExecute.AddMinutes(schedule.Repeat).CompareTo(calcNext) < 0)
                        {
                            nextExecute = nextExecute.AddMinutes(schedule.Repeat);
                            //TextLog.Log("PluginStarter", "Calc 2 " + nextExecute.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        else
                        {
                            nextExecute = calcNext;
                        }
                    }
                    else
                        nextExecute = calcNext;


                    db.ExecuteNonQuery("update report_schedule set [next] = '" + nextExecute.ToString("yyyy-MM-dd HH:mm:ss") + "' where id = " + scheduleId, CommandType.Text, null);
                }
            }
        }



        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("Report", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("Report", text);
            }

            Process.GetCurrentProcess().Kill();
        }

        protected override void OnStop()
        {
            
        }

        public void Start(string[] args)
        {
            OnStart(args);
        }

    }
}
