using System;
using System.Collections.Generic;
using System.Text;

namespace AkAuthAgent
{
    public enum AuthStatus
    {
        OK = 0,
        BadPassword = 1,
        NoUser = 2,
        None = 999
    }

    public class AuthTestStatus
    {
        public AuthStatus Status { get; internal set; }
        public String[] Groups { get; internal set; }

        public AuthTestStatus(ushort status)
            : this(status, null) { }

        public AuthTestStatus(ushort status, String[] groups)
        {
            switch (status)
            {
                case AuthBase.FWAUT_ST_BAD_PWD:
                    this.Status = AuthStatus.BadPassword;
                    break;

                case AuthBase.FWAUT_ST_NO_USER:
                    this.Status = AuthStatus.NoUser;
                    break;

                case AuthBase.FWAUT_ST_OK:
                    this.Status = AuthStatus.OK;
                    break;

                default:
                    this.Status = AuthStatus.None;
                    break;
            }
            this.Groups = (groups != null ? groups : new String[0]);
        }
    }

}
