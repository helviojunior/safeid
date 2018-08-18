using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.WebAPI;
using System.Data;
using System.Data.SqlClient;
using SafeTrend.Json;
using IAM.GlobalDefs.WebApi;
using IAM.GlobalDefs;
using System.Globalization;
using System.Threading;
using System.Runtime.Serialization;

namespace IAMWebServer._admin.content
{
    public partial class service_status : System.Web.UI.Page
    {
        [Serializable()]
        class ServiceStatusBase
        {
            [OptionalField]
            public String start_time;

            [OptionalField]
            public String host;

            [OptionalField]
            public Boolean executing;

            [OptionalField]
            public String last_status;
        }

        [Serializable()]
        class ServiceStatusEngine : ServiceStatusBase
        {
            
            [OptionalField]
            public Int64 total_registers;

            [OptionalField]
            public Int32 percent;
            
            [OptionalField]
            public Int64 atual_register;

            [OptionalField]
            public Int32 errors;

            [OptionalField]
            public Int32 new_users;

            [OptionalField]
            public Int32 ignored;

            [OptionalField]
            public Int32 thread_count;

            [OptionalField]
            public Int32 queue_count;

        }

        [Serializable()]
        class ServiceStatusInbound : ServiceStatusBase
        {

            [OptionalField]
            public Int64 total_files;

            [OptionalField]
            public Double percent;

            [OptionalField]
            public Int64 processed_files;

        }

        [Serializable()]
        class ServiceStatusDispatcher : ServiceStatusBase
        {

            [OptionalField]
            public Boolean executing_deploy_now;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod != "POST")
                return;

            String area = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["area"]))
                area = (String)RouteData.Values["area"];

            Int64 enterpriseId = 0;
            if ((Session["enterprise_data"]) != null && (Session["enterprise_data"] is EnterpriseData))
                enterpriseId = ((EnterpriseData)Session["enterprise_data"]).Id;


            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();

            LMenu menu1 = null;
            LMenu menu2 = null;
            LMenu menu3 = null;

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";


            switch (area)
            {
                case "":
                case "content":
                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {
                        DataTable dtServices = db.ExecuteDataTable("select * from service_status order by service_name", CommandType.Text, null);
                        if ((dtServices != null) && (dtServices.Rows.Count > 0))
                        {

                            html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                            html += "    <tr>";
                            html += "        <th class=\"pointer header headerSortDown\" data-column=\"name\">Serviço <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer tHide header\" data-column=\"login\">Status do serviço <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Data de inicio <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Status da execução <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Data do processamento <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer w150 tHide mHide header\" data-column=\"login\">% <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Informações adicionais <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer w80 tHide mHide header\" data-column=\"login\">Ações <div class=\"icomoon\"></div></th>";
                            html += "    </tr>";
                            html += "</thead>";

                            html += "<tbody>";

                            String trTemplate = "    <tr class=\"user\">";
                            trTemplate += "            <td class=\"pointer ident10\">{0}</td>";
                            trTemplate += "            <td class=\"pointer tHide\">{1}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\">{2}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\">{3}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\">{4}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\">{5}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\" style=\"line-height: 17px;\">{6}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\"><button href=\"/admin/service_status/{0}/action/restart/\" class=\"a-btn btn-service confirm-action\" confirm-title=\"Restart\" confirm-text=\"Deseja reiniciar o serviço '{0}'?\" ok=\"Sim\" cancel=\"Não\"><div class=\"ico icon-loop\"></div></button></td>";
                            trTemplate += "    </tr>";

                            foreach (DataRow drS in dtServices.Rows)
                            {
                                String eStatus = "";
                                String eDate = "";
                                String sDate = "";
                                String ePercent = "";
                                String eInfo = "";

                                try
                                {
                                    sDate = MessageResource.FormatDate((DateTime)drS["started_date"], false);
                                }
                                catch { }

                                String sStatus = "";
                                DateTime lastSync = (DateTime)drS["last_status"];
                                TimeSpan ts = DateTime.Now - lastSync;
                                if (ts.TotalSeconds > 60)
                                {
                                    sStatus += "<div class=\"red-text level-icon level-300\" style=\"margin: 0;\"></div>";
                                    sStatus += "<span class=\"red-text\">Último status a " + MessageResource.FormatTs(ts) + "</span>";
                                }
                                else
                                {
                                    sStatus += "<div class=\"green-text level-icon level-200\" style=\"margin: 0;\"></div>";
                                    sStatus += "<span class=\"green-text\">Ativo</span>";

                                    try
                                    {
                                        switch (drS["service_name"].ToString().ToLower())
                                        {
                                            case "engine":

                                                ServiceStatusEngine stE = JSON.Deserialize<ServiceStatusEngine>(drS["additional_data"].ToString());
                                                if (stE.executing)
                                                {
                                                    eStatus = "Processando registros";

                                                    if (!String.IsNullOrEmpty(stE.last_status))
                                                        eStatus = stE.last_status;

                                                    //ePercent = stE.percent + "%";

                                                    ePercent += "<div class=\"center\"><canvas id=\"usrLockChart\" width=\"40\" height=\"40\"></canvas></div>";
                                                    js += "iamadmin.buildPercentChart('#usrLockChart'," + stE.percent + ",{strokeColor:'#e5e5e5',textColor:'#333',color:'#76c558',showText:true});";

                                                    eInfo = "Total de registros: " + stE.total_registers;
                                                    eInfo += "<br />Processado: " + stE.atual_register;
                                                    eInfo += "<br />Erros: " + stE.errors;
                                                    eInfo += "<br />Ignorados: " + stE.ignored;
                                                    eInfo += "<br />Novas entidades: " + stE.new_users;
                                                    eInfo += "<br />Atualizados: " + (stE.atual_register - stE.errors - stE.ignored - stE.new_users);
                                                    eInfo += "<br />Threads: " + stE.thread_count;
                                                    eInfo += "<br />Fila nas threads: " + stE.queue_count;

                                                    try
                                                    {
                                                        DateTime dt1 = DateTime.Parse(stE.start_time);
                                                        eDate = MessageResource.FormatDate(dt1, false);

                                                        //Tempo estimado para conclusão
                                                        TimeSpan ts2 = DateTime.Now - dt1;
                                                        Double calc = (ts2.TotalSeconds / (Double)stE.atual_register) * (Double)(stE.total_registers - stE.atual_register);

                                                        eInfo += "<br />Conclusão estimada: " + MessageResource.FormatTime(DateTime.Now.AddSeconds(calc));
                                                    }
                                                    catch { }

                                                }

                                                break;

                                            case "report":
                                                ServiceStatusBase stR = JSON.Deserialize<ServiceStatusBase>(drS["additional_data"].ToString());
                                                break;

                                            case "inbound":
                                                ServiceStatusInbound stI = JSON.Deserialize<ServiceStatusInbound>(drS["additional_data"].ToString());
                                                if (stI.executing)
                                                {
                                                    eStatus = "Processando importação";

                                                    if (!String.IsNullOrEmpty(stI.last_status))
                                                        eStatus = stI.last_status;


                                                    //ePercent = stE.percent + "%";

                                                    ePercent += "<div class=\"center\"><canvas id=\"inboundLockChart\" width=\"40\" height=\"40\"></canvas></div>";
                                                    js += "iamadmin.buildPercentChart('#inboundLockChart'," + stI.percent + ",{strokeColor:'#e5e5e5',textColor:'#333',color:'#76c558',showText:true});";

                                                    eInfo = "Total de arquivos: " + stI.total_files;
                                                    eInfo += "<br />Processado: " + stI.processed_files;

                                                    try
                                                    {
                                                        DateTime dt1 = DateTime.Parse(stI.start_time);
                                                        eDate = MessageResource.FormatDate(dt1, false);

                                                        //Tempo estimado para conclusão
                                                        TimeSpan ts2 = DateTime.Now - dt1;
                                                        Double calc = (ts2.TotalSeconds / (Double)stI.processed_files) * (Double)(stI.total_files - stI.processed_files);

                                                        eInfo += "<br />Conclusão estimada: " + MessageResource.FormatTime(DateTime.Now.AddSeconds(calc));
                                                    }
                                                    catch { }

                                                }
                                                break;

                                            case "dispatcher":
                                                ServiceStatusDispatcher stD = JSON.Deserialize<ServiceStatusDispatcher>(drS["additional_data"].ToString());
                                                if (stD.executing)
                                                    eStatus += "Executando publicação";

                                                if (stD.executing_deploy_now)
                                                {
                                                    if (eStatus != "")
                                                        eStatus += "/";

                                                    eStatus += "Executando publicação sob demanda";
                                                }

                                                break;
                                        }


                                    }
                                    catch { }
                                }

                                html += String.Format(trTemplate, drS["service_name"], sStatus, sDate, eStatus, eDate, ePercent, eInfo);
                            }

                            html += "</tbody></table>";

                            html += "<span class=\"empty-results content-loading user-list-loader hide\"></span>";

                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                            contentRet.js = "iamadmin.doReload(10000);" + js;
                        }
                        else
                        {
                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        }
                    }
                    break;

                case "sidebar":
                    break;

                case "mobilebar":
                    break;


                case "buttonbox":
                    break;
            }

            if (contentRet != null)
            {
                if (!String.IsNullOrWhiteSpace((String)Request["cid"]))
                    contentRet.callId = (String)Request["cid"];

                Retorno.Controls.Add(new LiteralControl(contentRet.ToJSON()));
            }
        }
    }
}