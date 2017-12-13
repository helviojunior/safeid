using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APIChangePwd
{
    class Program
    {
        static void Main(string[] args)
        {

            IAM.IAMUserClass uc = new IAM.IAMUserClass();
            uc.ChangeUserPasswd("intranet.integracao", "LuXXF052mNTCFoH9Lll1", "teste001", "123456");


        }
    }
}
