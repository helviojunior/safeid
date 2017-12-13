using System;
using System.Collections.Generic;
using System.Text;

namespace AkAuthAgent
{

    public enum AuthResult
    {
        OK = 1,
        BadPassword = 2,
        NoUser = 3,
        Error = 4
    }

    public class AuthUserResult
    {
        public String Username;
        public AuthResult Result;
        public String[] Groups;

        public AuthUserResult(String username)
        {
            this.Username = username;
            this.Result = AuthResult.NoUser;
        }
    }
}
