using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace CAS.PluginInterface
{
    [Serializable()]
    public class CASChangePasswordResult: CASJsonBase
    {
        public Boolean Success;
        public String UserName;
        public String ErrorText;

        public CASChangePasswordResult()
        {
            this.Success = false; //Definir como falso por padrão pois é usado em outras áreas do sistema
        }

        public CASChangePasswordResult(String errorText)
            :base()
        {
            this.ErrorText = errorText;
        }

        public CASChangePasswordResult(Boolean success, String userName)
            : base()
        {
            this.Success = success;
            this.UserName = userName;
        }

    }
}
