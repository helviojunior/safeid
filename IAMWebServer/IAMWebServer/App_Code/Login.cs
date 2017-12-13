using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using SafeTrend.Json;
using System.Globalization;
using System.Resources;
using System.Threading;
using IAM.Config;
using IAM.CA;
using System.Data.SqlClient;
using IAM.GlobalDefs;
using SafeTrend.Data;



public static class LoginUser
{

    static public LoginData LogedUser(Page page)
    {
        if (page.Session["login"] == null)
            return null;

        if (!(page.Session["login"] is LoginData))
            return null;

        //Verifica se a autenticação é da mesma empresa
        if ((page.Session["enterprise_data"]) == null || !(page.Session["enterprise_data"] is EnterpriseData) || (((EnterpriseData)page.Session["enterprise_data"]).Id != ((LoginData)page.Session["login"]).EnterpriseId))
            return null;

        return (LoginData)page.Session["login"];
    }

    /*
    static public LoginResult AuthUser(Page page, String username, String password)
    {
        return AuthUser(page, username, password, false);
    }

    static public LoginResult AuthUser(Page page, String username, String password, Boolean byPassPasswordCheck)
    {

        try
        {
            if ((username == null) || (username.Trim() == "") || (username == password) || (username.Trim() == ""))
                return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));

            Int64 enterpriseId = 0;
            if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

            DbParameterCollection par = new DbParameterCollection();;
            par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
            par.Add("@login", typeof(String), username.Length).Value = username;

            DataTable tmp = null;

            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {
                tmp = db.ExecuteDataTable("select distinct id, alias, full_name, login, enterprise_id, password, must_change_password from vw_entity_logins with(nolock) where deleted = 0 and enterprise_id = @enterprise_id and locked = 0 and (login = @login or value = @login)", CommandType.Text, par);

                if ((tmp != null) && (tmp.Rows.Count > 0))
                {
                    foreach (DataRow dr in tmp.Rows)
                    {

                        using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.Connection, enterpriseId))
                        using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(dr["password"].ToString())))
                            if (byPassPasswordCheck || Encoding.UTF8.GetString(cApi.clearData) == password)
                            {
                                //Realiza o login
                                try
                                {
                                    //Adiciona o ciookie do usuário
                                    HttpCookie cookie = new HttpCookie("uid");
                                    //Define o valor do cookie
                                    cookie.Value = tmp.Rows[0]["id"].ToString();
                                    //Time para expiração (1 min)
                                    DateTime dtNow = DateTime.Now;
                                    TimeSpan tsMinute = new TimeSpan(365, 0, 0, 0);
                                    cookie.Expires = dtNow + tsMinute;
                                    //Adiciona o cookie
                                    page.Response.Cookies.Add(cookie);
                                }
                                catch { }

                                LoginData l = new LoginData();
                                l.Alias = tmp.Rows[0]["alias"].ToString();
                                l.FullName = tmp.Rows[0]["full_name"].ToString();
                                l.Login = tmp.Rows[0]["login"].ToString();
                                l.Id = (Int64)tmp.Rows[0]["id"];
                                l.EnterpriseId = (Int64)tmp.Rows[0]["enterprise_id"];

                                page.Session["login"] = l;

                                db.ExecuteNonQuery("update entity set last_login = getdate() where id = " + l.Id, CommandType.Text, null);

                                db.AddUserLog(LogKey.User_Logged, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, l.Id, 0, MessageResource.GetMessage("user_logged") + " " + Tools.Tool.GetIPAddress(), "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");

                                return new LoginResult(true, "User OK", (Boolean)tmp.Rows[0]["must_change_password"]);
                                break;
                            }
                            else
                            {
                                db.AddUserLog(LogKey.User_WrongPassword, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, MessageResource.GetMessage("user_wrong_password") + " " + Tools.Tool.GetIPAddress(), "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");
                            }
                    }

                    return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));
                }
                else
                {
                    db.AddUserLog(LogKey.User_WrongUserAndPassword, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, MessageResource.GetMessage("user_wrong_password") + " " + Tools.Tool.GetIPAddress(), "{ \"username\":\"" + username.Replace("'", "").Replace("\"", "") + "\", \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");
                    return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));
                }
            }
        }
        catch (Exception ex)
        {
            Tools.Tool.notifyException(ex, page);
            return new LoginResult(false, "Internal error", ex.Message);
        }
        finally
        {

        }

    }


    static public LoginResult AuthUserByTicket(Page page, String ticket)
    {

        try
        {
            if ((ticket == null) || (ticket.Trim() == ""))
                return new LoginResult(false, MessageResource.GetMessage("invalid_ticket"));

            Int64 enterpriseId = 0;
            if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

            DbParameterCollection par = new DbParameterCollection();;
            par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
            par.Add("@tgc", typeof(String), ticket.Length).Value = ticket;

            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {

                DataTable tmp = db.ExecuteDataTable("select distinct l.id, l.alias, l.full_name, l.login, l.enterprise_id, l.password, l.must_change_password, s.id as service_id, s.service_uri, et.grant_ticket, et.long_ticket from vw_entity_logins l with(nolock)  inner join cas_entity_ticket et with(nolock) on et.entity_id = l.id inner join cas_service s with(nolock) on l.enterprise_id = s.enterprise_id and et.service_id = s.id where et.grant_ticket = @tgc and s.enterprise_id = @enterprise_id", CommandType.Text, par);

                if ((tmp != null) && (tmp.Rows.Count > 0))
                {
                    foreach (DataRow dr in tmp.Rows)
                    {

                        //Realiza o login
                        try
                        {
                            //Adiciona o ciookie do usuário
                            HttpCookie cookie = new HttpCookie("uid");
                            //Define o valor do cookie
                            cookie.Value = tmp.Rows[0]["id"].ToString();
                            //Time para expiração (1 min)
                            DateTime dtNow = DateTime.Now;
                            TimeSpan tsMinute = new TimeSpan(365, 0, 0, 0);
                            cookie.Expires = dtNow + tsMinute;
                            //Adiciona o cookie
                            page.Response.Cookies.Add(cookie);
                        }
                        catch { }

                        LoginData l = new LoginData();
                        l.Alias = tmp.Rows[0]["alias"].ToString();
                        l.FullName = tmp.Rows[0]["full_name"].ToString();
                        l.Login = tmp.Rows[0]["login"].ToString();
                        l.Id = (Int64)tmp.Rows[0]["id"];
                        l.EnterpriseId = (Int64)tmp.Rows[0]["enterprise_id"];

                        page.Session["login"] = l;

                        db.ExecuteNonQuery("update entity set last_login = getdate() where id = " + l.Id, CommandType.Text, null);

                        db.AddUserLog(LogKey.User_Logged, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, l.Id, 0, MessageResource.GetMessage("user_logged") + " " + Tools.Tool.GetIPAddress(), "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");

                        return new LoginResult(true, "User OK", (Boolean)tmp.Rows[0]["must_change_password"]);
                        break;
                    }

                    return new LoginResult(false, MessageResource.GetMessage("invalid_ticket"));
                }
                else
                {
                    db.AddUserLog(LogKey.User_WrongTicket, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, MessageResource.GetMessage("user_wrong_password") + " " + Tools.Tool.GetIPAddress(), "{ \"ticket\":\"" + ticket.Replace("'", "").Replace("\"", "") + "\", \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");
                    return new LoginResult(false, MessageResource.GetMessage("invalid_ticket"));
                }
            }
        }
        catch (Exception ex)
        {
            Tools.Tool.notifyException(ex, page);
            return new LoginResult(false, "Internal error");
        }
        finally
        {

        }


    }*/

    static public Int64 FindUser(Page page, String username, out String error)
    {

        try
        {
            if ((username == null) || (username.Trim() == ""))
            {
                error = MessageResource.GetMessage("valid_username");
                return 0;
            }
            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {
                DataTable tmp = db.Select(String.Format("select id, locked from vw_entity_logins with(nolock) where (login = '{0}' or value = '{0}') group by id, locked", Tools.Tool.TrataInjection(username)));
                if ((tmp == null) || (tmp.Rows.Count == 0))
                {
                    error = MessageResource.GetMessage("valid_username");
                    return 0;

                }
                else if (tmp.Rows.Count > 1)
                {
                    error = MessageResource.GetMessage("ambiguous_id");
                    return 0;
                }
                else if ((Boolean)tmp.Rows[0]["locked"])
                {
                    error = MessageResource.GetMessage("user_locked");
                    return 0;
                }
                else
                {
                    error = "";
                    return (Int64)tmp.Rows[0]["id"];
                }
            }
        }
        catch (Exception ex)
        {
            error = MessageResource.GetMessage("internal_error");
            Tools.Tool.notifyException(ex, page);
            return 0;
        }
        finally
        {

        }


    }
    
    static public void NewCode(Page page, Int64 entityId, out String error)
    {


        error = "";
        try
        {
            if (entityId == 0)
                return;

            String code = GenerateCode(6);
            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            using(DbParameterCollection par = new DbParameterCollection())
            {
                par.Add("@code", typeof(String)).Value = code;
                par.Add("@entity_id", typeof(Int64)).Value = entityId;

                db.ExecuteNonQuery("update entity set recovery_code = @code where deleted = 0 and id = @entity_id and (recovery_code is null or ltrim(rtrim(recovery_code)) = '')", CommandType.Text, par);

                db.AddUserLog(LogKey.User_NewRecoveryCode, null, "AutoService", UserLogLevel.Info, 0, 0, 0, 0, 0, entityId, 0, MessageResource.GetMessage("new_recovery_code") + " (" + code + ")", "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");
            }
        }
        catch (Exception ex)
        {
            error = MessageResource.GetMessage("internal_error");
            Tools.Tool.notifyException(ex, page);
            return;
        }
        finally
        {

        }


    }

    public static Boolean SendCode(Int64 entityId, String sendTo, Boolean isMail, Boolean isSMS, out String error)
    {
        error = "";



        try
        {
            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {
                DataTable tmp = db.Select(String.Format("select id, recovery_code from entity with(nolock) where deleted = 0 and id = {0}", entityId));
                if ((tmp == null) || (tmp.Rows.Count == 0))
                {
                    error = MessageResource.GetMessage("entity_not_found");
                    return false;
                }


                if (isMail)
                    Tools.Tool.sendEmail("Password recover code", sendTo, "Code: " + tmp.Rows[0]["recovery_code"].ToString(), false);
            }
            return true;
        }
        catch(Exception ex) {
            error = ex.Message;
            return false;
        }

    }

    public static String MaskData(String data, Boolean isMail, Boolean isSMS)
    {
        String tData = "";
        if (isMail)
        {
            
            String[] parts = data.Trim().ToLower().Split("@".ToCharArray(), 2);
            if (parts.Length != 2)
                return "";

            Int32 start = 3;
            if (parts[0].Length < 3)
                start = 1;

            for (Int32 p = 0; p < parts[0].Length; p++)
                if (p >= start)
                    tData += "*";        
                else
            tData += parts[0][p].ToString();        
            

            tData += "@" + parts[1];
        }

        return tData;
    }

    private static String GenerateCode(Int32 Length)
    {

        String tmp = "";
        //65 a 90
        //97 a 122

        for (Int32 i = 65; i <= 90; i++)
        {
            tmp += Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) });
        }

        /*
        for (Int32 i = 97; i <= 122; i++)
        {
            tmp += Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) });
        }

        for (Int32 i = 0; i <= 9; i++)
        {
            tmp += i.ToString();
        }*/

        String passwd = "";
        Random rnd = new Random();

        while (passwd.Length < Length)
        {
            Int32 next = rnd.Next(0, tmp.Length - 1);
            passwd += tmp[next];
            Thread.Sleep(50);
        }

        return passwd;
    }
    
    
}