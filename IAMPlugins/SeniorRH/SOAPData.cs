using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;

namespace SeniorRH
{

    
    public class ConsultarColaboradorPorCPFBody
    {
        [XmlElement(ElementName = "ConsultarColaboradorPorCPFResponse", Namespace = "http://services.senior.com.br")]
        public ColaboradorResponse ColaboradoresResponse { get; set; }
    }

    public class ColaboradoresAdmitidosBody
    {
        [XmlElement(ElementName = "ColaboradoresAdmitidosResponse", Namespace = "http://services.senior.com.br")]
        public ColaboradorResponse ColaboradoresResponse { get; set; }
    }

    public class ComplementaresBody
    {
        [XmlElement(ElementName = "ComplementaresResponse", Namespace = "http://services.senior.com.br")]
        public ComplementaresResponse ComplementaresResponse { get; set; }
    }


    [XmlType(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class ColaboradoresAdmitidosSOAP : SOAPEnvelope
    {

        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public ColaboradoresAdmitidosBody Body { get; set; }

    }

    [XmlType(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class ComplementaresSOAP : SOAPEnvelope
    {

        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public ComplementaresBody Body { get; set; }

    }


    [XmlType(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class ConsultarColaboradorPorCPFSOAP : SOAPEnvelope
    {

        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public ConsultarColaboradorPorCPFBody Body { get; set; }

    }

    /* #####################
    ## Gerais
    */


    public abstract class SOAPEnvelope
    {
        [XmlAttribute(AttributeName = "soapenv", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public string soapenva { get; set; }

        [XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2001/XMLSchema")]
        public string xsd { get; set; }

        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string xsi { get; set; }

        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns = new XmlSerializerNamespaces();
        public SOAPEnvelope()
        {
            xmlns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
        }
    }


    public class ColaboradoresResult : GenericResult
    {

        [XmlElement("TMCSColaboradores")]
        public List<Object> colaboradores { get; set; }

        public List<Dictionary<String, String>> getDict()
        {
            List<Dictionary<String, String>> tmp = new List<Dictionary<String, String>>();

            foreach (Object o in colaboradores)
            {
                if (o is XmlNode[])
                {
                    tmp.Add(((XmlNode[])o).ToDictionary(element => element.Name, element => element.InnerText));
                }
            }

            return tmp;
        }

    }


    public class ComplementaresResult : GenericResult
    {

        [XmlElement("TMCSColaboradores")]
        public List<Object> colaboradores { get; set; }

        public List<Dictionary<String, String>> getDict()
        {
            List<Dictionary<String, String>> tmp = new List<Dictionary<String, String>>();

            foreach (Object o in colaboradores)
            {
                if (o is XmlNode[])
                {
                    tmp.Add(((XmlNode[])o).ToDictionary(element => element.Name, element => element.InnerText));
                }
            }

            return tmp;
        }

    }

    public class TMCSColaboradores
    {
        [XmlElement("numCpf")]
        public String numCpf { get; set; }

    }

    public abstract class GenericResult
    {
        [OptionalField, XmlElement(ElementName = "erroExecucao")]
        public String ErroExecucao;
    }

    public class ColaboradorResponse
    {
        [XmlElement("result", Namespace = "")]
        public ColaboradoresResult Result { get; set; }
    }

    public class ComplementaresResponse
    {
        [XmlElement("result", Namespace = "")]
        public ComplementaresResult Result { get; set; }
    }

}
