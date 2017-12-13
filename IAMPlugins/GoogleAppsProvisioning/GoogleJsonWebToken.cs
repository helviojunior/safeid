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

namespace GoogleAdmin
{
    public class GoogleJsonWebToken
    {

        public static GoogleAccessToken GetAccessToken(String base64CertData, String clientIdEMail, String scope, String adminDelegatedEmail)
        {
            return GetAccessToken(base64CertData, clientIdEMail, scope, adminDelegatedEmail, null);
        }

        public static GoogleAccessToken GetAccessToken(String base64CertData, String clientIdEMail, String scope, String adminDelegatedEmail, JSON.DebugMessage dbg)
        {
            try
            {

                if ((base64CertData == null) || (base64CertData == ""))
                    throw new Exception("Certificate data is empty");

                // certificate
                var certificate = new X509Certificate2(Convert.FromBase64String(base64CertData), "notasecret");

                // header
                var header = new { typ = "JWT", alg = "RS256" };

                // claimset
                var times = GetExpiryAndIssueDate(dbg);
                var claimset = new
                {
                    iss = clientIdEMail,
                    prn = adminDelegatedEmail, //Ver comentário abaixo
                    scope = scope,
                    aud = "https://accounts.google.com/o/oauth2/token",
                    iat = times[0],
                    exp = times[1],
                };


                if (dbg != null) try { dbg("iat", times[0].ToString() + " ==> " + new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(times[0]).ToString("yyyy-MM-dd HH:mm:ss")); }
                    catch { };

                if (dbg != null) try { dbg("exp", times[1].ToString() + " ==> " + new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(times[1]).ToString("yyyy-MM-dd HH:mm:ss")); }
                    catch { };

                /* The email address of the user for which the application is requesting delegated access.
                 * Sem colocar este parâmetro o Token é gerado, porém ao listar os usuário apresenta o erro:
                 * (403) - Not Authorized to access this resource/api
                 */

                JavaScriptSerializer ser = new JavaScriptSerializer();

                // encoded header
                var headerSerialized = ser.Serialize(header);
                var headerBytes = Encoding.UTF8.GetBytes(headerSerialized);
                var headerEncoded = Convert.ToBase64String(headerBytes);

                // encoded claimset
                var claimsetSerialized = ser.Serialize(claimset);
                var claimsetBytes = Encoding.UTF8.GetBytes(claimsetSerialized);
                var claimsetEncoded = Convert.ToBase64String(claimsetBytes);

                // input
                var input = headerEncoded + "." + claimsetEncoded;
                var inputBytes = Encoding.UTF8.GetBytes(input);

                // signiture
                var rsa = certificate.PrivateKey as RSACryptoServiceProvider;
                var cspParam = new CspParameters
                {
                    KeyContainerName = rsa.CspKeyContainerInfo.KeyContainerName,
                    KeyNumber = rsa.CspKeyContainerInfo.KeyNumber == KeyNumber.Exchange ? 1 : 2
                };
                var aescsp = new RSACryptoServiceProvider(cspParam) { PersistKeyInCsp = false };
                var signatureBytes = aescsp.SignData(inputBytes, "SHA256");
                var signatureEncoded = Convert.ToBase64String(signatureBytes);

                // jwt
                var jwt = headerEncoded + "." + claimsetEncoded + "." + signatureEncoded;

                var client = new WebClient();
                client.Encoding = Encoding.UTF8;
                var uri = "https://accounts.google.com/o/oauth2/token";
                var content = new NameValueCollection();

                content["assertion"] = jwt;
                content["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";

                string jData = "";

                if (dbg != null) try { dbg("JWT", jwt); }
                    catch { };

                try
                {
                    jData = Encoding.UTF8.GetString(client.UploadValues(uri, "POST", content));

                    if (dbg != null) try{ dbg("Return", jData); } catch{};
                }
                catch (Exception ex)
                {
                    if (dbg != null) try { dbg("Error: " + ex.Message, ""); }
                        catch { };

                    try
                    {
                        if (ex is WebException)
                            using (WebResponse response = ((WebException)ex).Response)
                            {
                                HttpWebResponse httpResponse = (HttpWebResponse)response;
                                using (Stream data = response.GetResponseStream())
                                using (var reader = new StreamReader(data))
                                {
                                    jData = reader.ReadToEnd();
                                }
                            }
                    }
                    catch {
                        GoogleAccessToken err = new GoogleAccessToken();
                        err.error = ex.Message;
                        jData = JSON.Serialize<GoogleAccessToken>(err);
                    }
                }

                if (dbg != null) try { dbg("Return", jData); }
                    catch { };

                return JSON.Deserialize<GoogleAccessToken>(jData);
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
            var issueTime = GetNetworkUTCTime(dbg);

            var iat = (int)issueTime.Subtract(utc0).TotalSeconds;
            var exp = (int)issueTime.AddMinutes(55).Subtract(utc0).TotalSeconds;

            return new[] { iat, exp };
        }

        private static DateTime GetNetworkUTCTime(JSON.DebugMessage dbg)
        {
            //default Windows time server
            String[] ntpServerLst = new String[] { "a.st1.ntp.br", "b.st1.ntp.br", "c.st1.ntp.br", "d.st1.ntp.br", "a.ntp.br", "b.ntp.br", "c.ntp.br", "gps.ntp.br", "time.windows.com" };

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            Boolean find = false;

            foreach (String ntpServer in ntpServerLst)
            {
                try
                {
                    if (dbg != null) try { dbg("NTP - Get UTC date from NTP server " + ntpServer, ""); }
                        catch { };

                    IPAddress[] addresses = Dns.GetHostEntry(ntpServer).AddressList;
                    
                    //The UDP port number assigned to NTP is 123
                    var ipEndPoint = new IPEndPoint(addresses[0], 123);
                    //NTP uses UDP
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    socket.Connect(ipEndPoint);

                    //Stops code hang if NTP is blocked
                    socket.ReceiveTimeout = 3000;

                    socket.Send(ntpData);
                    socket.Receive(ntpData);
                    socket.Close();

                    find = true;
                    break;
                }
                catch { }
            }

            DateTime networkDateTime = DateTime.UtcNow;
            if (find)
            {
                //Offset to get to the "Transmit Timestamp" field (time at which the reply 
                //departed the server for the client, in 64-bit timestamp format."
                const byte serverReplyTime = 40;

                //Get the seconds part
                ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

                //Get the seconds fraction
                ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

                //Convert From big-endian to little-endian
                intPart = SwapEndianness(intPart);
                fractPart = SwapEndianness(fractPart);

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

                //**UTC** time
                networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

                if (dbg != null) try { dbg("NTP - UTC Date " + networkDateTime.ToString("yyyy-MM-dd HH:mm:ss"), ""); }
                    catch { };
            }
            else
            {
                if (dbg != null) try { dbg("NTP - Erro on get UTC date from NTP server, using system UTC date " + networkDateTime.ToString("yyyy-MM-dd HH:mm:ss"), ""); }
                    catch { };    
            }

            return networkDateTime;
        }

        // stackoverflow.com/a/3294698/162671
        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
    }
}