using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsSQLServer
{
    internal class MSSQLDB : BaseDB
    {
        private Int32 i_pid;

        public MSSQLDB()
            : base()
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public MSSQLDB(String server, String dbName, String username, String password)
            : base(server, dbName, username, password)
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }


        public MSSQLDB(String connectionString)
            : base(connectionString)
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }

    }
}
