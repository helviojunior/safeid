﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqliteServer
{
    public class InsertDefaultData : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    INSERT INTO [CAS_Role] ([Name], [Description]) VALUES ('Administrator','System administrator');
                    INSERT INTO [CAS_User] ([Name], [Surname], [Username], [Password], [Email]) VALUES ('admin', '', 'admin', '0CC52C6751CC92916C138D8D714F003486BF8516933815DFC11D6C3E36894BFA044F97651E1F3EEBA26CDA928FB32DE0869F6ACFB787D5A33DACBA76D34473A3', '');
                    INSERT INTO [CAS_UserRole_InRole] ([User_Username], [Role_Name]) VALUES ('admin', 'Administrator');

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT Count(*) = 0 FROM [CAS_User]"; }
        }


        public double Serial { get { return 0; } }
        public string Provider { get { return "System.Data.SQLite"; } }
    }
}
