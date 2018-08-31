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
using SafeTrend.Data;
using System.Globalization;
using System.Threading;

namespace IAMWebServer._admin.content
{
    public partial class dashboard : System.Web.UI.Page
    {
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
                    html += "<h3>" + MessageResource.GetMessage("password_change") + "</h3>";
                    html += "<div id=\"chartLastPwdChange\" class=\"chart-container\"></div>";
                    html += "<h3>" + MessageResource.GetMessage("login") + "</h3>";
                    html += "<div id=\"chartLogin\" class=\"chart-container\"></div>";
                    /*html += "<h3>" + MessageResource.GetMessage("users") + "</h3>";
                    html += "<div id=\"chartUserFlow\" class=\"flow-chart\"></div>";
                    html += "<h3>" + MessageResource.GetMessage("config") + "</h3>";
                    html += "<div id=\"chartConfigFlow\" class=\"flow-chart\"></div>";*/

                    DbParameterCollection par2 = new DbParameterCollection();;
                    par2.Add("@enterpriseId", typeof(Int64)).Value = enterpriseId;
                    par2.Add("@dStart", typeof(DateTime)).Value = DateTime.Now.AddDays(-15);
                    par2.Add("@dEnd", typeof(DateTime)).Value = DateTime.Now;
                    par2.Add("@key", typeof(Int32)).Value = (Int32)LogKey.User_PasswordChanged;

                    DataTable dtPwd = null;

                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {

                        dtPwd = db.ExecuteDataTable("sp_get_chart_data", CommandType.StoredProcedure, par2);

                        if ((dtPwd != null) && (dtPwd.Rows.Count > 0))
                        {
                            List<String> labels = new List<string>();
                            List<String> data = new List<string>();

                            CultureInfo culture = Thread.CurrentThread.CurrentCulture;
                            DateTimeFormatInfo dtfi = culture.DateTimeFormat;
                            string mes = culture.TextInfo.ToTitleCase(dtfi.GetMonthName(DateTime.Now.Month));

                            foreach (DataRow dr in dtPwd.Rows)
                            {
                                DateTime date = (DateTime)dr["date"];
                                Int32 value = (Int32)dr["qty"];

                                //date.ToString("mmmm/dd");

                                //labels.Add(culture.TextInfo.ToTitleCase(dtfi.GetMonthName(date.Month)));
                                labels.Add(date.ToString("MMM/dd"));
                                data.Add(value.ToString());
                            }

                            js += "var dataPwd = {";
                            js += "	labels : ['" + String.Join("','", labels) + "'],";
                            js += "	datasets : [";
                            js += "		{";
                            js += "			fillColor : \"rgba(220,220,220,0.5)\",";
                            js += "			strokeColor : \"rgba(220,220,220,1)\",";
                            js += "			pointColor : \"rgba(220,220,220,1)\",";
                            js += "			pointStrokeColor : \"#fff\",";
                            js += "			data : [" + String.Join(",", data) + "]";
                            js += "		}";
                            js += "	]";
                            js += "};";

                        }

                        par2 = new DbParameterCollection(); ;
                        par2.Add("@enterpriseId", typeof(Int64)).Value = enterpriseId;
                        par2.Add("@dStart", typeof(DateTime)).Value = DateTime.Now.AddDays(-15);
                        par2.Add("@dEnd", typeof(DateTime)).Value = DateTime.Now;
                        par2.Add("@key", typeof(Int32)).Value = (Int32)LogKey.User_Logged;

                        DataTable dtLog = db.ExecuteDataTable("sp_get_chart_data", CommandType.StoredProcedure, par2);

                        if ((dtLog != null) && (dtLog.Rows.Count > 0))
                        {
                            List<String> labels = new List<string>();
                            List<String> data = new List<string>();

                            CultureInfo culture = Thread.CurrentThread.CurrentCulture;
                            DateTimeFormatInfo dtfi = culture.DateTimeFormat;
                            string mes = culture.TextInfo.ToTitleCase(dtfi.GetMonthName(DateTime.Now.Month));

                            foreach (DataRow dr in dtLog.Rows)
                            {
                                DateTime date = (DateTime)dr["date"];
                                Int32 value = (Int32)dr["qty"];

                                //date.ToString("mmmm/dd");

                                //labels.Add(culture.TextInfo.ToTitleCase(dtfi.GetMonthName(date.Month)));
                                labels.Add(date.ToString("MMM/dd"));
                                data.Add(value.ToString());
                            }

                            js += "var dataLogin = {";
                            js += "	labels : ['" + String.Join("','", labels) + "'],";
                            js += "	datasets : [";
                            js += "		{";
                            js += "			fillColor : \"rgba(220,220,220,0.5)\",";
                            js += "			strokeColor : \"rgba(220,220,220,1)\",";
                            js += "			pointColor : \"rgba(220,220,220,1)\",";
                            js += "			pointStrokeColor : \"#fff\",";
                            js += "			data : [" + String.Join(",", data) + "]";
                            js += "		}";
                            js += "	]";
                            js += "};";

                        }

                        js += "iamadmin.buildLineChart('#chartLastPwdChange', dataPwd);";
                        js += "iamadmin.buildLineChart('#chartLogin', dataLogin);";
                        //js += "iamadmin.buildFlowChart('#chartUserFlow', '" + ApplicationVirtualPath + "admin/chartdata/flow/user/');";
                        //js += "iamadmin.buildFlowChart('#chartConfigFlow', '" + ApplicationVirtualPath + "admin/chartdata/flow/config/');";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        contentRet.js = js;
                    }
                    break;

                case "sidebar":
                    if (menu1 != null)
                    {
                        html += "<div class=\"section-nav-header\">";
                        html += "    <div class=\"crumbs\">";
                        html += "        <div class=\"subject subject-color\">";
                        html += "            <a href=\"" + menu1.HRef + "\">" + menu1.Name + "</a>";
                        html += "        </div>";
                        if (menu2 != null)
                        {
                            html += "        <div class=\"topic topic-color\">";
                            html += "            <a href=\"" + menu2.HRef + "\">" + menu2.Name + "</a>";
                            html += "        </div>";
                        }
                        html += "    </div>";
                        if (menu3 != null)
                        {
                            html += "    <div class=\"crumbs tutorial-title\">";
                            html += "        <h2 class=\"title tutorial-color\">" + menu3.Name + "</h2>";
                            html += "    </div>";
                        }
                        html += "</div>";
                    }


                    DbParameterCollection par = new DbParameterCollection();;
                    par.Add("@enterpriseId", typeof(Int64)).Value = enterpriseId;

                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {

                        DataTable dtUsers = db.ExecuteDataTable("sp_user_statistics", System.Data.CommandType.StoredProcedure, par);
                        if ((dtUsers != null) && (dtUsers.Rows.Count > 0))
                        {
                            Int64 total = (Int64)dtUsers.Rows[0]["total"];
                            Int64 locked = (Int64)dtUsers.Rows[0]["locked"];
                            Int64 logged = (Int64)dtUsers.Rows[0]["logged"];

                            Int32 pLocked = 0;
                            Int32 pLogged = 0;

                            try
                            {
                                pLocked = (Int32)(((Double)locked / (Double)total) * 100F);
                            }
                            catch { }

                            try
                            {
                                pLogged = (Int32)(((Double)logged / (Double)total) * 100F);
                            }
                            catch { }

                            html += "<div class=\"ds1\">";
                            html += "<div class=\"center\">" + MessageResource.GetMessage("entity") + "</div>";
                            html += "<div class=\"center\"><span class=\"big\">" + total + "</span><span class=\"small\"> " + MessageResource.GetMessage("total") + "</span></div>";

                            html += "<div class=\"center\"><canvas id=\"usrLockChart\" width=\"30\" height=\"30\"></canvas><span class=\"big txt1\">" + locked + "<span> <span class=\"small\">" + MessageResource.GetMessage("locked") + "<span></div>";
                            html += "<div class=\"center\"><canvas id=\"usrLoggedChart\" width=\"30\" height=\"30\"></canvas><span class=\"big txt1\">" + logged + "<span> <span class=\"small\">" + MessageResource.GetMessage("logged") + "<span></div>";
                            //html += "<div class=\"center\"><canvas id=\"usrTotalChart\" width=\"30\" height=\"30\"></canvas><span class=\"big txt1\">" + total + "<span> <span class=\"small\">Total<span></div>";

                            html += "</div>";


                            //js += "iamadmin.buildPercentChart('#usrTotalChart',100,{color:'#2d88b4',showText:false});";
                            js += "iamadmin.buildPercentChart('#usrLockChart'," + pLocked + ",{color:'#f5663a',showText:false});";
                            js += "iamadmin.buildPercentChart('#usrLoggedChart'," + pLogged + ",{color:'#76c558',showText:false});";
                        }

                        html += "<div class=\"ds2\">";
                        html += "<div class=\"center\">" + MessageResource.GetMessage("licensing") + "</div>";


                        try
                        {

                            String rData = "";
                            String query = "";

                            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                query = (String)RouteData.Values["query"];

                            var tmpReq = new
                            {
                                jsonrpc = "1.0",
                                method = "license.info",
                                parameters = new String[0],
                                id = 1
                            };

                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                            String jData = "";
                            try
                            {
                                jData = WebPageAPI.ExecuteLocal(db, this, rData);
                            }
                            finally
                            {

                            }

                            if (String.IsNullOrWhiteSpace(jData))
                                throw new Exception("");

                            License ret2 = JSON.Deserialize<License>(jData);
                            if (ret2 == null)
                            {
                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            }
                            else if (ret2.error != null)
                            {
                                eHtml += String.Format(errorTemplate, ret2.error.data);
                            }
                            else if (ret2.result == null)
                            {
                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            }
                            else
                            {

                                Int32 percent = 0;
                                if (ret2.result.hasLicense)
                                {
                                    if (ret2.result.available == 0)//Licença ilimitada
                                    {
                                        percent = 0;
                                    }
                                    else if (ret2.result.used > ret2.result.available)
                                    {
                                        percent = 100;
                                    }
                                    else
                                    {
                                        percent = (ret2.result.used / ret2.result.available) * 100;
                                    }
                                }
                                else
                                {
                                    percent = 100;
                                }


                                String color = "#76c558";
                                if (percent < 70)
                                {
                                    color = "#76c558";
                                }
                                else if (percent < 85)
                                {
                                    color = "#f5663a";
                                }
                                else
                                {
                                    color = "rgb(202, 52, 56)";
                                }

                                js += "iamadmin.buildPercentChart('#licChart'," + percent + ",{color:'" + color + "',showText:true});";

                                html += "<canvas id=\"licChart\" width=\"100\" height=\"100\" class=\"big-center\"></canvas>";
                                if (ret2.result.hasLicense)
                                {
                                    html += "<div class=\"center\">" + MessageResource.GetMessage("licensing_total") + "</div>";
                                    html += "<div class=\"center\"><span class=\"big\">" + ret2.result.used + "</span><span class=\"small\"> " + MessageResource.GetMessage("of") + " " + (ret2.result.available == 0 ? MessageResource.GetMessage("unlimited") : ret2.result.available.ToString()) + "</span></div>";
                                }
                                else
                                    html += "<div class=\"center\"><span class=\"big\">" + MessageResource.GetMessage("no_licecse") + "</span></div>";

                            }


                        }
                        catch (Exception ex)
                        {
                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                        }


                        html += "</div>";

                        
                        try
                        {
                            html += "<div class=\"ds1\" style=\"min-height: 10px;\"><div class=\"center\"><span class=\"small\">v. " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "</span></div></div>";
                        }
                        catch { }

                        

                    }

                    contentRet = new WebJsonResponse("#main aside", (eHtml != "" ? eHtml : html));
                    contentRet.js = js;

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