using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
//using IAM.Config;
//using IAM.SQLDB;
using SafeTrend.Json;
using System.Threading;
using System.Globalization;
using System.Resources;
using IAM.GlobalDefs;


    public static class EnterpriseIdentify
    {

        static public Boolean Identify(Page Page)
        {
            return Identify(Page, false);
        }

         static public Boolean Identify(Page Page, Boolean JsonReturn)
        {
            String tmp = "";
            return Identify(Page, JsonReturn, out tmp);
        }

         static public Boolean Identify(Page Page, Boolean JsonReturn, out String errorText)
         {
             return Identify(Page, JsonReturn, false, out errorText);
         }

        static public Boolean Identify(Page Page, Boolean JsonReturn, Boolean supressReturn)
        {
            String tmp = "";
            return Identify(Page, JsonReturn, supressReturn, out tmp);
        }

        static public Boolean Identify(Page Page, Boolean JsonReturn, Boolean supressReturn, out String errorText)
        {
            
            try
            {

                Boolean busca = false;

                if ((Page.Session["enterprise_data"] == null) || !(Page.Session["enterprise_data"] is EnterpriseData))
                    busca = true;

                if ((!busca) && ((EnterpriseData)Page.Session["enterprise_data"]).Host.ToLower() != Page.Request.Url.Host.ToLower())
                    busca = true;

                if (busca)
                {
                    Page.Session["enterprise_data"] = null;

                    EnterpriseData data = new EnterpriseData();
                    data.Host = Page.Request.Url.Host.ToLower();

                    if ((Page.Request.Url.Port != 80) && (Page.Request.Url.Port != 443))
                        data.Host += ":" + Page.Request.Url.Port;

                    try
                    {

                        DataTable dt = null;

                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            dt = db.Select("select id, e.fqdn, name, ef.fqdn alias, language, auth_plugin from [enterprise] e left join enterprise_fqdn_alias ef on ef.enterprise_id = e.id where e.fqdn = '" + data.Host + "' or ef.fqdn = '" + data.Host + "'");

                        if ((dt != null) && (dt.Rows.Count > 0))
                        {
                            data.Host = dt.Rows[0]["fqdn"].ToString().ToLower();
                            data.Name = dt.Rows[0]["name"].ToString();
                            data.Language = dt.Rows[0]["language"].ToString();
                            data.Id = (Int64)dt.Rows[0]["id"];
                            data.AuthPlugin = dt.Rows[0]["auth_plugin"].ToString();

                            Page.Session["enterprise_data"] = data;

                            errorText = "";

                            return true;
                        }
                        else
                        {
                            errorText = "Nenhuma empresa encontrada com o host '" + data.Host + "'";
                            throw new Exception("Nenhuma empresa encontrada com o host '" + data.Host + "'");
                        }

                    }
                    catch (Exception ex)
                    {
                        errorText = "Falha ao identificar a empresa: " + ex.Message;
                        throw new Exception("Falha ao identificar a empresa", ex);
                    }

                }
                else
                {
                    errorText = "";

                }

                if ((Page.Session["enterprise_data"] != null) && (Page.Session["enterprise_data"] is EnterpriseData))
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(((EnterpriseData)Page.Session["enterprise_data"]).Language);
                else
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                
                return true;
            }
            catch (Exception ex)
            {
                Tools.Tool.notifyException(ex, Page);

                errorText = "Falha na identificação da empresa e/ou empresa não cadastrada";

                if (!supressReturn)
                {
                    Byte[] erro = new Byte[0];

                    
                    if (JsonReturn)
                    {
                        erro = Encoding.UTF8.GetBytes(JSON.GetResponse(false, "Falha na identificação da empresa e/ou empresa não cadastrada", ""));
                    }
                    else
                    {

                        erro = Encoding.UTF8.GetBytes("Falha na identificação da empresa e/ou empresa não cadastrada");
                        Page.Response.Status = "500 Internal error";
                        Page.Response.StatusCode = 500;

                    }

                    Page.Response.ContentType = "text/json;charset=UTF-8";
                    Page.Response.ContentEncoding = Encoding.UTF8;
                    Page.Response.OutputStream.Write(erro, 0, erro.Length);
                    Page.Response.End();
                }

                return false;
            }

        }
    }

