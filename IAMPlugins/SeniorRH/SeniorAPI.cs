using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SafeTrend.Xml;
using System.Net;
using System.Security.Cryptography;

namespace SeniorRH
{
    internal class SeniorAPI
    {
        private String username;
        private String password;
        private String numEmp;
        private CookieContainer cookie;
        //private Uri serverUri = new Uri("https://example.com/g5-senior-services/rubi_Synccom_senior_g5_rh_fp_consultarColaboradorPorCPF?wsdl");
        private Uri baseUri;


        public SeniorAPI(String username, String password, String numEmp, Uri baseUri)
        {
            this.username = username;
            this.password = password;
            this.cookie = new CookieContainer();
            this.baseUri = baseUri;
            this.numEmp = numEmp;

        }

        public List<Dictionary<String, String>> GetUsers(XML.DebugMessage debugCallback = null)
        {
            try
            {
                List<Dictionary<String, String>> tmp = new List<Dictionary<string, string>>();

                Uri callUri = new Uri(this.baseUri.Scheme + "://" + this.baseUri.Host + ":" + this.baseUri.Port + "/g5-senior-services/rubi_Synccom_senior_g5_rh_fp_colaboradoresAdmitidos?wsdl");

                for (Int32 abrTipCol = 1; abrTipCol <= 3; abrTipCol++)
                {

                    StringBuilder post = new StringBuilder();

                    post.AppendLine("<?xml version='1.0' encoding='UTF-8'?><soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ser=\"http://services.senior.com.br\">");
                    post.AppendLine("  <soapenv:Body>");
                    post.AppendLine("    <ser:ColaboradoresAdmitidos>");
                    post.AppendLine("      <user>" + this.username + "</user>");
                    post.AppendLine("      <password>" + this.password + "</password>");
                    post.AppendLine("      <encryption>0</encryption>");
                    post.AppendLine("      <parameters>");
                    post.AppendLine("        <numEmp>" + this.numEmp + "</numEmp>");
                    post.AppendLine("        <abrTipCol>" + abrTipCol + "</abrTipCol>");
                    post.AppendLine("        <iniPer>" + DateTime.Now.AddDays(-365).ToString("dd/MM/yyyy") + "</iniPer>");
                    //post.AppendLine("        <iniPer>18/10/2017</iniPer>");
                    //post.AppendLine("        <fimPer>" + DateTime.Now.ToString("dd/MM/yyyy") + "</fimPer>");
                    post.AppendLine("      </parameters>");
                    post.AppendLine("    </ser:ColaboradoresAdmitidos>");
                    post.AppendLine("  </soapenv:Body>");
                    post.AppendLine("</soapenv:Envelope>");

                    ColaboradoresAdmitidosSOAP result = XML.XmlWebRequest<ColaboradoresAdmitidosSOAP>(callUri, post.ToString(), "text/xml", null, "POST", this.cookie, debugCallback);

                    if (result == null || result.Body == null || result.Body.ColaboradoresResponse == null || result.Body.ColaboradoresResponse.Result == null)
                        throw new Exception("ResultSet is empty");

                    if (!String.IsNullOrEmpty(result.Body.ColaboradoresResponse.Result.ErroExecucao))
                        throw new Exception(result.Body.ColaboradoresResponse.Result.ErroExecucao);

                    tmp.AddRange(result.Body.ColaboradoresResponse.Result.getDict());
                }

                return tmp;

            }
            catch (DeserializeException ex)
            {
                throw ex.Exception;
            }
        }


        public List<Dictionary<String, String>> GetUserData(String cpf, XML.DebugMessage debugCallback = null)
        {
            try
            {

                Uri callUri = new Uri(this.baseUri.Scheme + "://" + this.baseUri.Host + ":" + this.baseUri.Port + "/g5-senior-services/rubi_Synccom_senior_g5_rh_fp_consultarColaboradorPorCPF?wsdl");

                StringBuilder post = new StringBuilder();

                post.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ser=\"http://services.senior.com.br\">");
                post.Append("  <soapenv:Body>");
                post.Append("    <ser:ConsultarColaboradorPorCPF>");
                post.Append("      <user>" + this.username + "</user>");
                post.Append("      <password>" + this.password + "</password>");
                post.Append("      <encryption>0</encryption>");
                post.Append("      <parameters>");
                post.Append("        <numCpf>" + cpf + "</numCpf>");
                post.Append("      </parameters>");
                post.Append("    </ser:ConsultarColaboradorPorCPF>");
                post.Append("  </soapenv:Body>");
                post.Append("</soapenv:Envelope>");

                ConsultarColaboradorPorCPFSOAP result = XML.XmlWebRequest<ConsultarColaboradorPorCPFSOAP>(callUri, post.ToString(), "text/xml", null, "POST", this.cookie, debugCallback);

                if (result == null || result.Body == null || result.Body.ColaboradoresResponse == null || result.Body.ColaboradoresResponse.Result == null)
                    throw new Exception("ResutSet is empty");

                if (!String.IsNullOrEmpty(result.Body.ColaboradoresResponse.Result.ErroExecucao))
                    throw new Exception(result.Body.ColaboradoresResponse.Result.ErroExecucao);

                return result.Body.ColaboradoresResponse.Result.getDict();

            }
            catch (DeserializeException ex)
            {
                throw ex.Exception;
            }
        }


    }
}
