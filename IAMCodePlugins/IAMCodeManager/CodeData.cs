using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace IAM.CodeManager
{
    public class CodeData
    {
        public String DataId { get; internal set; }
        public String MaskedData { get; internal set; }
        public String ClearData { get; internal set; }
        public String Prefix { get; internal set; }

        public CodeData(String Prefix, String ClearData, String MaskedData)
        {
            this.MaskedData = (!String.IsNullOrEmpty(Prefix) ? Prefix + ": " : "") + MaskedData;
            this.ClearData = ClearData;
            this.Prefix = Prefix;

            this.CalcHash();
        }
        
        private void CalcHash()
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            this.DataId = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(this.Prefix.ToLower() + this.ClearData.ToLower()))).Replace("-", "");
        }
    }
}
