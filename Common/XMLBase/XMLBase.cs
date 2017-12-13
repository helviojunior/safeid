using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO;
using System.Net;
using System.Reflection;

namespace SafeTrend.Xml
{
    public class DeserializeException : Exception
    {

        public String XML { get; private set; }
        public Exception Exception { get; private set; }

        public DeserializeException(String XML, Exception Exception)
        {
            this.XML = XML;
            this.Exception = Exception;
        }
    }

    public static class XML
    {
        public delegate void DebugMessage(String data, String debug);

        public static T Deserialize<T>(String xmlText)
        {
            using (MemoryStream s = new MemoryStream(Encoding.UTF8.GetBytes(xmlText)))
            {
                XmlSerializer ser = new XmlSerializer(typeof(T));
                return (T)ser.Deserialize(s);
            }

        }

        public static String Serialize<T>(T obj)
        {
            String ret = "";

            using (MemoryStream s = new MemoryStream())
            {
                XmlSerializer ser = new XmlSerializer(typeof(T));
                ser.Serialize(s, obj);
                s.Flush();

                ret = Encoding.UTF8.GetString(s.ToArray());
            }

            return ret;
        }

        public static T XmlWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers)
        {
            return XmlWebRequest<T>(uri, postData, ContentType, headers, null, null, null);
        }

        public static T XmlWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method)
        {
            return XmlWebRequest<T>(uri, postData, ContentType, headers, method, null, null);
        }

        public static T XmlWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method, CookieContainer cookie)
        {
            return XmlWebRequest<T>(uri, postData, ContentType, headers, method, cookie, null);
        }

        public static T XmlWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method, CookieContainer cookie, DebugMessage debugCallback)
        {

            String xml = TextWebRequest(uri, postData, ContentType, headers, method, cookie, debugCallback);

            //Trata Envelope SOAP fora do padrão
            xml = xml.Replace("<S:", "<soapenv:");
            xml = xml.Replace("</S:", "</soapenv:");
            xml = xml.Replace("xmlns:S=", "xmlns:soapenv=");
            

            try
            {

                if (xml == "")
                    return (T)((Object)null);
                else
                    return Deserialize<T>(xml);

            }
            catch (Exception ex)
            {
                throw new DeserializeException(xml, ex);
            }
        }


        public static String TextWebRequest(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method, CookieContainer cookie, DebugMessage debugCallback)
        {

            if (debugCallback != null) debugCallback("Request URI: ", uri.AbsoluteUri);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "Mozilla/5.0 (compatible; SafeID/1.0; +http://www.safeid.com.br)";

            if (cookie != null)
                request.CookieContainer = cookie;

            if (headers != null)
                foreach (String k in headers.Keys)
                    switch (k.ToLower())
                    {

                        default:
                            request.Headers.Add(k, headers[k]);
                            break;
                    }

            //request.ServicePoint.Expect100Continue = false;
            //ServicePointManager.MaxServicePointIdleTime = 2000;

            if (!String.IsNullOrWhiteSpace(method))
            {
                switch (method.ToUpper())
                {
                    case "GET":
                    case "POST":
                    case "PUT":
                    case "DELETE":
                        request.Method = method.ToUpper();
                        break;

                    default:
                        request.Method = "GET";
                        break;
                }
            }
            else
            {
                request.Method = "GET";
            }

            try
            {
                if (debugCallback != null) debugCallback("POST Data", postData);
                if (!String.IsNullOrWhiteSpace(postData))
                {
                    request.ContentType = ContentType.Split(";".ToCharArray(), 2)[0].Trim() + "; charset=UTF-8";

                    // Create POST data and convert it to a byte array.
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                    request.ContentLength = byteArray.Length;
                    using (Stream dataStream = request.GetRequestStream())
                    {
                        dataStream.Write(byteArray, 0, byteArray.Length);
                    }
                }

                //request.Headers.Add("Content-Type", "application/json; charset=UTF-8");
            }
            catch (Exception ex)
            {
                if (debugCallback != null) debugCallback("POST Data Error", ex.Message);
            }

            String jData = "";
            try
            {
                // Get the response.
                if (debugCallback != null) debugCallback("GetResponse", "");
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Encoding enc = Encoding.UTF8;
                    try
                    {
                        enc = Encoding.GetEncoding(response.ContentEncoding);
                    }
                    catch { }

                    Stream dataStream = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(dataStream, enc))
                        jData = reader.ReadToEnd();
                }

            }
            catch (Exception ex)
            {
                if (debugCallback != null) debugCallback("GetResponse Error", ex.Message);
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
                catch { }
            }

            if (debugCallback != null) debugCallback("Return Text", jData);
            if (jData == "")
                return "";
            else
                return jData;
        }


    }

}
