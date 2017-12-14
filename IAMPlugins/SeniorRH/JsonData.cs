using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SeniorRH
{
    //[{"code":3001,"message":"O subdom\u00ednio informado \u00e9 inv\u00e1lido ou inexistente."}]

    //{ "FUNC": {"KEY": "0bb001e4cd7b517c3bcf1acf50a6", "RETURN": {"ID": "01", "value": "Usuário e\/ou senha inválidos" }, "TRANS": "" }}




    [Serializable()]
    [DataContract]
    public class akna1105Result
    {
        /*
        [OptionalField()]
        [DataMember(Name = "EMKT")]
        public aknaLista EMKT;

        [Serializable()]
        [DataContract]
        public class aknaFunc
        {
            [OptionalField()]
            [DataMember] //Ver como colocar lista tb
            public aknaReturnList _return;

            [OptionalField()]
            [DataMember(Name = "KEY")]
            public String key;


            // public class ItemList : List<string> { }

            [CollectionDataContract(ItemName = "RETURN")]
            public class aknaReturnList : List<aknaReturn> { }


        }
        */
    }


    [DataContract, XmlRoot("MAIN"), XmlType("MAIN")]
    public class SeniorUserData : aknaBase
    {
        [OptionalField, DataMember, XmlElement("EMKT")]
        public aknaReturn EMKT;

    }

    [DataContract, XmlRoot("MAIN"), XmlType("MAIN")]
    public class AknaCommandResponse : aknaBase
    {
        [OptionalField, DataMember, XmlElement("EMKT")]
        public aknaReturn EMKT;

    }

    [DataContract, XmlRoot("MAIN"), XmlType("MAIN")]
    public class AknaListResponse : aknaBase
    {
        [OptionalField, DataMember, XmlElement("EMKT")]
        public aknaLista EMKT;

        [Serializable()]
        [DataContract]
        public class aknaLista
        {
            [OptionalField, DataMember, XmlElement("LISTA")]
            public List<aknaListaItem> Listas;

            [OptionalField, DataMember, XmlAttribute("TRANS")]
            public String trans;

            [OptionalField, DataMember, XmlAttribute("KEY")]
            public String key;


            [Serializable()]
            [DataContract]
            public class aknaListaItem
            {
                [OptionalField, DataMember, XmlAttribute("ID")]
                public String id;

                [OptionalField, DataMember, XmlAttribute("INDICE")]
                public String indice;

                [OptionalField, DataMember, XmlAttribute("CONTATOS_VALIDOS")]
                public String contatos_validos;

                [OptionalField, DataMember, XmlAttribute("ARQUIVADA")]
                public String arquivada;

                [OptionalField, DataMember, XmlText]
                public String name;

                public override string ToString()
                {
                    return id + "-> " + name;
                }


            }
        }
    }
    
    
    public class aknaBase
    {
        [OptionalField, DataMember, XmlElement("FUNC")]
        public aknaReturn FUNC;


    }

    [Serializable()]
    [DataContract]
    public class aknaReturn
    {
        [OptionalField, DataMember, XmlElement("RETURN")]
        public List<aknaReturnItem> _return;

        [OptionalField, DataMember, XmlAttribute("TRANS")]
        public String trans;

        [OptionalField, DataMember, XmlAttribute("KEY")]
        public String key;

        [Serializable()]
        [DataContract]
        public class aknaReturnItem
        {
            [OptionalField, DataMember, XmlAttribute("ID")]
            public String id;

            [OptionalField, DataMember, XmlText]
            public String value;

            public override string ToString()
            {
                return id + " -> " + value;
            }

        }
    }




}
