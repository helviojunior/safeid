using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ODBC
{
    internal class OdbcDB : BaseDB
    {
        private Int32 i_pid;

        public OdbcDB()
            : base()
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public OdbcDB(String systemDSN, String username, String password)
            : base(systemDSN, username, password)
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }


        public OdbcDB(String systemDSN)
            : base(systemDSN)
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }

    }
}
