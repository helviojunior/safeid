using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Text;
using System.Security.Cryptography;


using IAM.GlobalDefs;
using SafeTrend.Data;
using IAM.Config;
using IAM.CA;

namespace IAMWebServer
{
    public class CASUtils
    {
        public static EnterpriseData EnterpriseByService(Page page, String service)
        {
            if (String.IsNullOrEmpty(service))
                return null;

            DbParameterCollection par = null;
            try
            {
                par = new DbParameterCollection();;
                par.Add("@svc", typeof(String), service.Length).Value = service.TrimEnd("/".ToCharArray()).Replace("https://", "//").Replace("http://", "//").Trim();

                using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                {
                    DataTable dt = db.ExecuteDataTable("select * from [cas_service] s inner join enterprise e on s.enterprise_id = e.id where s.service_uri = @svc", CommandType.Text, par);

                    if ((dt != null) && (dt.Rows.Count > 0))
                    {
                        EnterpriseData data = new EnterpriseData();
                        data.Host = page.Request.Url.Host.ToLower();

                        data.Host = dt.Rows[0]["fqdn"].ToString().ToLower();
                        data.Name = dt.Rows[0]["name"].ToString();
                        data.Language = dt.Rows[0]["language"].ToString();
                        data.Id = (Int64)dt.Rows[0]["id"];

                        return data;
                    }
                    else
                    {
                        return null;
                    }
                }

            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                par = null;
            }

        }

        public static Boolean ServiceExists(String service)
        {
            if (String.IsNullOrEmpty(service))
                return false;

            DbParameterCollection par = null;
            try
            {
                par = new DbParameterCollection();;
                par.Add("@svc", typeof(String), service.Length).Value = service.TrimEnd("/".ToCharArray()).Replace("https://", "//").Replace("http://", "//").Trim();

                DataTable dt = null;

                using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    dt = db.ExecuteDataTable("select * from [cas_service] where service_uri = @svc", CommandType.Text, par);

                if ((dt != null) && (dt.Rows.Count > 0))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                par = null;
            }

        }

        public static LoginResult Grant(String service, HttpCookie cookie)
        {
            if ((cookie == null) || (String.IsNullOrEmpty(cookie.Value)))
                return new LoginResult(false, MessageResource.GetMessage("invalid_ticket"));

            return Grant(service, cookie.Value, false);
        }

        public static LoginResult Grant(String service, String ticket, Boolean renew)
        {
            if (String.IsNullOrEmpty(ticket))
                return new LoginResult(false, MessageResource.GetMessage("invalid_ticket"));

            DbParameterCollection par = null;
            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {
                try
                {
                    par = new DbParameterCollection(); ;
                    par.Add("@tgc", typeof(String), ticket.Length).Value = ticket.Trim();
                    par.Add("@svc", typeof(String), service.Length).Value = service.TrimEnd("/".ToCharArray()).Replace("https://", "//").Replace("http://", "//").Trim();

                    Int64 userId = 0;

                    //Realiza a busca pelo ticket no mesmo serviço
                    DataTable dt = db.ExecuteDataTable("select * from [cas_entity_ticket] t inner join [cas_service] s on t.service_id = s.id where t.expire_date > getdate() " + (renew ? " and create_by_credentials = 1 " : "") + " and s.service_uri = @svc and t.grant_ticket = @tgc", CommandType.Text, par);
                    if ((dt != null) && (dt.Rows.Count > 0))
                    {
                        //Atualiza a expiração
                        //DB.ExecuteSQL("update cas_entity_ticket set expire_date = dateadd(day,1,getdate()) where entity_id = " + l.Id + " and service_id = " + tmp.Rows[0]["service_id"].ToString(), null, CommandType.Text);
                        userId = (Int64)dt.Rows[0]["entity_id"];
                    }
                    else
                    {
                        //Realiza a busca do ticket em outro serviço
                        //Se existir copia o ticket para o serviço atual
                        dt = db.ExecuteDataTable("select * from [cas_entity_ticket] t inner join [cas_service] s on t.service_id = s.id where t.expire_date > getdate() " + (renew ? " and create_by_credentials = 1 " : "") + " and t.grant_ticket = @tgc", CommandType.Text, par);
                        if ((dt != null) && (dt.Rows.Count > 0))
                        {
                            par.Add("@entity_id", typeof(Int64)).Value = (Int64)dt.Rows[0]["entity_id"];
                            par.Add("@grant_ticket", typeof(String), dt.Rows[0]["grant_ticket"].ToString().Length).Value = dt.Rows[0]["grant_ticket"].ToString().Trim();
                            par.Add("@long_ticket", typeof(String), dt.Rows[0]["long_ticket"].ToString().Length).Value = dt.Rows[0]["long_ticket"].ToString().Trim();

                            //Cria o ticket
                            db.ExecuteNonQuery("insert into cas_entity_ticket ([entity_id],[service_id],[grant_ticket],[long_ticket],[create_by_credentials]) select @entity_id, s.id, @grant_ticket, @long_ticket, 0 from cas_service s where s.service_uri = @svc", CommandType.Text, par);
                            userId = (Int64)dt.Rows[0]["entity_id"];
                        }
                        else
                        {
                            return new LoginResult(false, MessageResource.GetMessage("invalid_ticket"));
                        }
                    }

                    if (userId > 0)
                    {
                        LoginData l = new LoginData();

                        DataTable dtEntity = db.ExecuteDataTable("select distinct l.id, l.alias, l.full_name, l.login, l.enterprise_id, l.password, l.must_change_password, s.id as service_id, s.service_uri, et.grant_ticket, et.long_ticket from vw_entity_logins l  inner join dbo.cas_entity_ticket et on et.entity_id = l.id inner join cas_service s on l.enterprise_id = s.enterprise_id and et.service_id = s.id where et.grant_ticket = @tgc and s.service_uri = @svc", CommandType.Text, par);

                        if ((dtEntity != null) && (dtEntity.Rows.Count > 0))
                        {

                            l.Alias = dtEntity.Rows[0]["alias"].ToString();
                            l.FullName = dtEntity.Rows[0]["full_name"].ToString();
                            l.Login = dtEntity.Rows[0]["login"].ToString();
                            l.Id = (Int64)dtEntity.Rows[0]["id"];
                            l.EnterpriseId = (Int64)dtEntity.Rows[0]["enterprise_id"];
                            l.CASGrantTicket = dtEntity.Rows[0]["grant_ticket"].ToString();
                            l.CASLongTicket = dtEntity.Rows[0]["long_ticket"].ToString();

                            return new LoginResult(true, "User OK", (Boolean)dtEntity.Rows[0]["must_change_password"], l);
                        }

                    }

                    return new LoginResult(false, MessageResource.GetMessage("invalid_ticket"));
                }
                catch (Exception ex)
                {
                    return new LoginResult(false, "Internal error");
                }
                finally
                {
                    par = null;
                }
            }

            return new LoginResult(false, MessageResource.GetMessage("invalid_ticket"));
        }


        static public LoginResult Grant(Page page, String username, String password)
        {

            try
            {
                if ((username == null) || (username.Trim() == "") || (username == password) || (username.Trim() == ""))
                    return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));

                Int64 enterpriseId = 0;
                if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                    enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

                String svc = page.Request.QueryString["service"].TrimEnd("/".ToCharArray()).Replace("https://", "//").Replace("http://", "//").Trim();

                DbParameterCollection par = new DbParameterCollection();;
                par.Add("@login", typeof(String), username.Length).Value = username;
                par.Add("@svc", typeof(String), svc.Length).Value = svc;

                using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                {

                    DataTable tmp = db.ExecuteDataTable("select distinct l.id, l.alias, l.full_name, l.login, l.enterprise_id, l.password, l.must_change_password, s.id as service_id, c.service_uri, c.grant_ticket, c.long_ticket from vw_entity_logins l inner join cas_service s on l.enterprise_id = s.enterprise_id left join (select * from cas_entity_ticket c1 inner join cas_service s on s.id = c1.service_id) c on l.id = c.entity_id and c.service_uri = @svc where l.deleted = 0 and l.locked = 0 and (l.login = @login or l.value = @login) and s.service_uri = @svc", CommandType.Text, par);

                    if ((tmp != null) && (tmp.Rows.Count > 0))
                    {
                        foreach (DataRow dr in tmp.Rows)
                        {

                            using (SqlConnection conn = IAMDatabase.GetWebConnection())
                            using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(conn, enterpriseId))
                            using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(dr["password"].ToString())))
                                if (Encoding.UTF8.GetString(cApi.clearData) == password)
                                {
                                    //Realiza o login

                                    LoginData l = new LoginData();
                                    l.Alias = tmp.Rows[0]["alias"].ToString();
                                    l.FullName = tmp.Rows[0]["full_name"].ToString();
                                    l.Login = tmp.Rows[0]["login"].ToString();
                                    l.Id = (Int64)tmp.Rows[0]["id"];
                                    l.EnterpriseId = (Int64)tmp.Rows[0]["enterprise_id"];
                                    l.CASGrantTicket = CASTicket.Generate();
                                    l.CASLongTicket = CASTicket.Generate();

                                    if (tmp.Rows[0]["grant_ticket"] != DBNull.Value)
                                        l.CASGrantTicket = tmp.Rows[0]["grant_ticket"].ToString();

                                    if (tmp.Rows[0]["long_ticket"] != DBNull.Value)
                                        l.CASLongTicket = tmp.Rows[0]["long_ticket"].ToString();

                                    try
                                    {
                                        page.Response.Cookies.Remove("TGC-SafeID");
                                        page.Response.Cookies.Remove("TGT-SafeID");
                                    }
                                    catch { }

                                    try
                                    {
                                        //Adiciona o cookie do TGC
                                        HttpCookie cookie = new HttpCookie("TGC-SafeID");
                                        //cookie.Domain = page.Request.Url.Host;
                                        cookie.Path = "/cas";
                                        cookie.Value = l.CASGrantTicket;

                                        DateTime dtNow = DateTime.Now;
                                        TimeSpan tsMinute = new TimeSpan(30, 0, 0, 0);
                                        cookie.Expires = dtNow + tsMinute;

                                        //Adiciona o cookie
                                        page.Response.Cookies.Add(cookie);
                                    }
                                    catch { }

                                    try
                                    {
                                        //Adiciona o cookie do TGC
                                        HttpCookie cookie = new HttpCookie("TGT-SafeID");
                                        //cookie.Domain = page.Request.Url.Host;
                                        cookie.Path = "/cas";
                                        cookie.Value = l.CASLongTicket;

                                        DateTime dtNow = DateTime.Now;
                                        TimeSpan tsMinute = new TimeSpan(30, 0, 0, 0);
                                        cookie.Expires = dtNow + tsMinute;

                                        //Adiciona o cookie
                                        page.Response.Cookies.Add(cookie);
                                    }
                                    catch { }

                                    db.ExecuteNonQuery("update entity set last_login = getdate() where id = " + l.Id, CommandType.Text, null);

                                    if (tmp.Rows[0]["service_uri"] == DBNull.Value)
                                        db.ExecuteNonQuery("insert into cas_entity_ticket ([entity_id],[service_id],[grant_ticket],[long_ticket],[create_by_credentials]) VALUES (" + l.Id + ", " + tmp.Rows[0]["service_id"].ToString() + ", '" + l.CASGrantTicket + "', '" + l.CASLongTicket + "',1)", CommandType.Text, null);
                                    else
                                        db.ExecuteNonQuery("update cas_entity_ticket set grant_ticket = '" + l.CASGrantTicket + "', long_ticket = '" + l.CASLongTicket + "', expire_date = dateadd(day,1,getdate()), create_by_credentials = 1 where entity_id = " + l.Id + " and service_id = " + tmp.Rows[0]["service_id"].ToString(), CommandType.Text, null);

                                    db.AddUserLog(LogKey.User_Logged, null, "CAS", UserLogLevel.Info, 0, 0, 0, 0, 0, l.Id, 0, MessageResource.GetMessage("user_logged") + " " + Tools.Tool.GetIPAddress(), "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");

                                    return new LoginResult(true, "User OK", (Boolean)tmp.Rows[0]["must_change_password"], l);
                                    break;
                                }
                                else
                                {
                                    db.AddUserLog(LogKey.User_WrongPassword, null, "CAS", UserLogLevel.Info, 0, 0, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, MessageResource.GetMessage("user_wrong_password") + " " + Tools.Tool.GetIPAddress(), "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");
                                }
                        }

                        return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));
                    }
                    else
                    {
                        db.AddUserLog(LogKey.User_WrongUserAndPassword, null, "CAS", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, MessageResource.GetMessage("user_wrong_password") + " " + Tools.Tool.GetIPAddress(), "{ \"username\":\"" + username.Replace("'", "").Replace("\"", "") + "\", \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");
                        return new LoginResult(false, MessageResource.GetMessage("valid_username_pwd"));
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


        }



    }


    /// <summary>
    /// This class can generate random passwords, which do not include ambiguous 
    /// characters, such as I, l, and 1. The generated password will be made of
    /// 7-bit ASCII symbols. Every four characters will include one lower case
    /// character, one upper case character, one number, and one special symbol
    /// (such as '%') in a random order. The password will always start with an
    /// alpha-numeric character; it will not start with a special symbol (we do
    /// this because some back-end systems do not like certain special
    /// characters in the first position).
    /// </summary>
    public class CASTicket
    {
        // Define supported password characters divided into groups.
        // You can add (or remove) characters to (from) these groups.
        private static string PASSWORD_CHARS_LCASE = "abcdefgijkmnopqrstwxyz";
        private static string PASSWORD_CHARS_UCASE = "ABCDEFGHJKLMNPQRSTWXYZ";
        private static string PASSWORD_CHARS_NUMERIC = "23456789";

        public static string Generate()
        {

            // Create a local array containing supported password characters
            // grouped by types. You can remove character groups from this
            // array, but doing so will weaken the password strength.
            char[][] charGroups = new char[][] 
        {
            PASSWORD_CHARS_LCASE.ToCharArray(),
            PASSWORD_CHARS_UCASE.ToCharArray(),
            PASSWORD_CHARS_NUMERIC.ToCharArray()
        };

            // Use this array to track the number of unused characters in each
            // character group.
            int[] charsLeftInGroup = new int[charGroups.Length];

            // Initially, all characters in each group are not used.
            for (int i = 0; i < charsLeftInGroup.Length; i++)
                charsLeftInGroup[i] = charGroups[i].Length;

            // Use this array to track (iterate through) unused character groups.
            int[] leftGroupsOrder = new int[charGroups.Length];

            // Initially, all character groups are not used.
            for (int i = 0; i < leftGroupsOrder.Length; i++)
                leftGroupsOrder[i] = i;

            // Because we cannot use the default randomizer, which is based on the
            // current time (it will produce the same "random" number within a
            // second), we will use a random number generator to seed the
            // randomizer.

            // Use a 4-byte array to fill it with random bytes and convert it then
            // to an integer value.
            byte[] randomBytes = new byte[4];

            // Generate 4 random bytes.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(randomBytes);

            // Convert 4 bytes into a 32-bit integer value.
            int seed = (randomBytes[0] & 0x7f) << 24 |
                        randomBytes[1] << 16 |
                        randomBytes[2] << 8 |
                        randomBytes[3];

            // Now, this is real randomization.
            Random random = new Random(seed);

            // This array will hold password characters.
            char[] ticket = null;

            // Allocate appropriate memory for the password.
            ticket = new char[20];

            // Index of the next character to be added to password.
            int nextCharIdx;

            // Index of the next character group to be processed.
            int nextGroupIdx;

            // Index which will be used to track not processed character groups.
            int nextLeftGroupsOrderIdx;

            // Index of the last non-processed character in a group.
            int lastCharIdx;

            // Index of the last non-processed group.
            int lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;

            // Generate password characters one at a time.
            for (int i = 0; i < ticket.Length; i++)
            {
                // If only one character group remained unprocessed, process it;
                // otherwise, pick a random character group from the unprocessed
                // group list. To allow a special character to appear in the
                // first position, increment the second parameter of the Next
                // function call by one, i.e. lastLeftGroupsOrderIdx + 1.
                if (lastLeftGroupsOrderIdx == 0)
                    nextLeftGroupsOrderIdx = 0;
                else
                    nextLeftGroupsOrderIdx = random.Next(0,
                                                         lastLeftGroupsOrderIdx);

                // Get the actual index of the character group, from which we will
                // pick the next character.
                nextGroupIdx = leftGroupsOrder[nextLeftGroupsOrderIdx];

                // Get the index of the last unprocessed characters in this group.
                lastCharIdx = charsLeftInGroup[nextGroupIdx] - 1;

                // If only one unprocessed character is left, pick it; otherwise,
                // get a random character from the unused character list.
                if (lastCharIdx == 0)
                    nextCharIdx = 0;
                else
                    nextCharIdx = random.Next(0, lastCharIdx + 1);

                // Add this character to the password.
                ticket[i] = charGroups[nextGroupIdx][nextCharIdx];

                // If we processed the last character in this group, start over.
                if (lastCharIdx == 0)
                    charsLeftInGroup[nextGroupIdx] =
                                              charGroups[nextGroupIdx].Length;
                // There are more unprocessed characters left.
                else
                {
                    // Swap processed character with the last unprocessed character
                    // so that we don't pick it until we process all characters in
                    // this group.
                    if (lastCharIdx != nextCharIdx)
                    {
                        char temp = charGroups[nextGroupIdx][lastCharIdx];
                        charGroups[nextGroupIdx][lastCharIdx] =
                                    charGroups[nextGroupIdx][nextCharIdx];
                        charGroups[nextGroupIdx][nextCharIdx] = temp;
                    }
                    // Decrement the number of unprocessed characters in
                    // this group.
                    charsLeftInGroup[nextGroupIdx]--;
                }

                // If we processed the last group, start all over.
                if (lastLeftGroupsOrderIdx == 0)
                    lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;
                // There are more unprocessed groups left.
                else
                {
                    // Swap processed group with the last unprocessed group
                    // so that we don't pick it until we process all groups.
                    if (lastLeftGroupsOrderIdx != nextLeftGroupsOrderIdx)
                    {
                        int temp = leftGroupsOrder[lastLeftGroupsOrderIdx];
                        leftGroupsOrder[lastLeftGroupsOrderIdx] =
                                    leftGroupsOrder[nextLeftGroupsOrderIdx];
                        leftGroupsOrder[nextLeftGroupsOrderIdx] = temp;
                    }
                    // Decrement the number of unprocessed groups.
                    lastLeftGroupsOrderIdx--;
                }
            }

            TimeSpan ts = DateTime.Now - new DateTime(1970,1,1);

            // Convert ticket characters into a string and return the result.
            return "LT-SafeID-" + ((Int64)ts.TotalSeconds).ToString() + "-" + new string(ticket);
        }
    }



}
