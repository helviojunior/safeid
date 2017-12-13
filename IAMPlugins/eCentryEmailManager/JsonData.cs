using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace eCentry
{
    //[{"code":3001,"message":"O subdom\u00ednio informado \u00e9 inv\u00e1lido ou inexistente."}]

    [Serializable()]
    public class emLogin : emBase
    {
        [OptionalField()]
        public String apikey;
    }

    [Serializable()]
    public class emBase
    {
        [OptionalField()]
        public Int32 code;
        [OptionalField()]
        public String message;
    }

    [Serializable()]
    public class emUserCreate : emBase
    {
        [OptionalField()]
        public String cid;
    }

    [Serializable()]
    public class emGroupCreate : emBase
    {
        [OptionalField()]
        public String id;
    }

    [Serializable()]
    public class emGroup : emBase
    {
        [OptionalField()]
        public String id;

        [OptionalField()]
        public String parent_id;

        [OptionalField()]
        public String folder_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public String description;

        public override string ToString()
        {
            return id + "-> " + name;
        }

    }


    [Serializable()]
    public class emUserGroup : emBase
    {
        [OptionalField()]
        public String group_id;

        public override string ToString()
        {
            return group_id;
        }

    }


    [Serializable()]
    public class emUser : emBase
    {
        [OptionalField()]
        public String id;

        [OptionalField()]
        public String parent_id;

        [OptionalField()]
        public String folder_id;

        [OptionalField()]
        public String name;

        [OptionalField()]
        public String description;

        [OptionalField()]
        public emUserStatusEmail statusemail;

        [OptionalField()]
        public String email;

        [OptionalField()]
        public String date_creation;

        [OptionalField()]
        public String date_modified;
        
        [OptionalField()]
        public Int32 rating;

        [OptionalField()]
        public String gender;

        [OptionalField()]
        public String date_birth;
    }

    [Serializable()]
    public class emContactCount
    {
        [OptionalField()]
        public Int64 total;
    }

    [Serializable()]
    public class emInviteResponse : emBase
    {
        [OptionalField()]
        public Boolean success;
    }


    public enum emUserStatusEmail
    {
        OK = 0,
        TempError = 1,
        HardError = 2,
        UserExclusionRequested = 5,
        UserAbuseRequested = 6,
        NotExists = 7
    }

    //[{"id":"2","name":"Administrativo Curitiba","description":null,"parent_id":"0","folder_id":"0","amount":"3","rating":"0"},{"id":"3","name":"Colaboradores Fael","description":null,"parent_id":"0","folder_id":"0","amount":"292","rating":"0"},{"id":"5","name":"Financeiro","description":null,"parent_id":"0","folder_id":"0","amount":"7","rating":"0"},{"id":"6","name":"Marketing","description":null,"parent_id":"0","folder_id":"0","amount":"15","rating":"0"},{"id":"7","name":"RH","description":null,"parent_id":"0","folder_id":"0","amount":"3","rating":"0"},{"id":"8","name":"TI","description":null,"parent_id":"0","folder_id":"0","amount":"12","rating":"0"},{"id":"9","name":"Qualidade e Desenvolvimento Acad\u00eamico","description":null,"parent_id":"0","folder_id":"0","amount":"2","rating":"0"},{"id":"10","name":"Produtora","description":null,"parent_id":"0","folder_id":"0","amount":"17","rating":"0"},{"id":"11","name":"Gestores","description":null,"parent_id":"0","folder_id":"0","amount":"10","rating":"0"},{"id":"12","name":"Gest\u00e3o de Rede","description":null,"parent_id":"0","folder_id":"0","amount":"2","rating":"0"},{"id":"13","name":"Faeltec","description":null,"parent_id":"0","folder_id":"0","amount":"4","rating":"0"},{"id":"14","name":"Diretores FAEL","description":null,"parent_id":"0","folder_id":"0","amount":"2","rating":"0"},{"id":"15","name":"Editora","description":null,"parent_id":"0","folder_id":"0","amount":"6","rating":"0"},{"id":"16","name":"inscritos","description":null,"parent_id":"0","folder_id":"0","amount":"0","rating":"0"},{"id":"17","name":"testeTI","description":null,"parent_id":"0","folder_id":"0","amount":"14","rating":"0"},{"id":"18","name":"TI Infraestrutura","description":null,"parent_id":"0","folder_id":"0","amount":"4","rating":"0"}]


}
