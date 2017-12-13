using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.GlobalDefs
{
    public class LoginResult
    {
        public Boolean Success { get; internal set; }
        public String Text { get; internal set; }
        public String Debug { get; internal set; }
        public Boolean ChangePassword { get; set; }
        public LoginData LoginData { get; set; }


        public LoginResult(Boolean Success, String Text) :
            this(Success, Text, false, null) { }

        public LoginResult(Boolean Success, String Text, String debug) :
            this(Success, Text, false, null) {
                this.Debug = debug;
        }

        public LoginResult(Boolean Success, String Text, Boolean ChangePassword) :
            this(Success, Text, ChangePassword, null) { }


        public LoginResult(Boolean Success, String Text, Boolean ChangePassword, LoginData LoginData)
        {
            this.Success = Success;
            this.Text = Text;
            this.ChangePassword = ChangePassword;
            this.LoginData = LoginData;
        }

    }
}
