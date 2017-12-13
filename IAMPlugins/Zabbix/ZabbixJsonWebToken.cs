using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using SafeTrend.Json;
using System.IO;
using System.Net.Sockets;

namespace Zabbix
{
    public class ZabbixJsonWebToken
    {

        public static ZabbixAccessToken GetAccessToken(Uri zabbixApi, String username, String password)
        {
            return GetAccessToken(zabbixApi, username, password, null);
        }

        public static ZabbixAccessToken GetAccessToken(Uri zabbixApi, String username, String password, JSON.DebugMessage dbg)
        {
            try
            {

                JSON.DebugMessage dbg2 = new JSON.DebugMessage(delegate(String data, String debug)
                {
                    if (dbg != null)
                        dbg(data, debug);
                });


                WebClient client = new WebClient();
                String jData = "";
                Byte[] content = new Byte[0];

                //Get Zabbix API Version

                String sData = JSON.Serialize2(new
                    {
                        jsonrpc = "2.0",
                        method = "apiinfo.version",
                        id = 1
                    });

                APIVerResult ver = null;
                try
                {
                    ver = JSON.JsonWebRequest<APIVerResult>(zabbixApi, sData, "application/json", null, "POST", null, dbg2);

                }
                catch (Exception ex)
                {
                    if (dbg != null) try { dbg("Error: " + ex.Message, ""); }
                        catch { };

                    ZabbixAccessToken err = new ZabbixAccessToken();
                    err.error = ex.Message;
                    return err;
                }


                sData = JSON.Serialize2(new
                {
                    jsonrpc = "2.0",
                    method = "user.login",
                    _params = new
                    {
                        user = username,
                        password = password
                    },
                    id = 1
                });
                sData = sData.Replace("_params", "params");


                AuthResult auth = null;
                try
                {
                    auth = JSON.JsonWebRequest<AuthResult>(zabbixApi, sData, "application/json", null, "POST", null, dbg2);

                    if (auth.error == null)
                    {

                        ZabbixAccessToken ok = new ZabbixAccessToken();
                        ok.api_ver = new Version(ver.result);
                        ok.access_token = auth.result;
                        ok.expires_in = 180;

                        if (dbg != null) try { dbg(JSON.Serialize2(ok), ""); }
                            catch { };
                        
                        return ok;
                    }
                    else
                    {
                        ZabbixAccessToken err = new ZabbixAccessToken();
                        err.error = auth.error.message;
                        return err;
                    }
                                        
                }
                catch (Exception ex)
                {
                    if (dbg != null) try { dbg("Error: " + ex.Message, ""); }
                        catch { };

                    ZabbixAccessToken err = new ZabbixAccessToken();
                    err.error = ex.Message;
                    return err;
                }
                
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro on GetAccessToken: " + ex.Message);
                throw ex;
            }
        }

        private static int[] GetExpiryAndIssueDate(JSON.DebugMessage dbg)
        {
            var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var issueTime = DateTime.UtcNow;

            var iat = (int)issueTime.Subtract(utc0).TotalSeconds;
            var exp = (int)issueTime.AddMinutes(55).Subtract(utc0).TotalSeconds;

            return new[] { iat, exp };
        }

    }
}