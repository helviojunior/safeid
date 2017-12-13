using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Excel
{
    internal class OdbcDB : BaseDB
    {
        private Int32 i_pid;

        public OdbcDB()
            : base()
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public OdbcDB(FileInfo fileName)
            : base(fileName)
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }


    }
}
