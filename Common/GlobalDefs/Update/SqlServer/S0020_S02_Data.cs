using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0020_S02_Data : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    

                    SET IDENTITY_INSERT sys_permission ON;

                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (3007, 3, 'getloginrules','Visualizar as regras de criação de login');
                    INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (3008, 3, 'changeloginrules','Alterar as regras de criação de login');

                    SET IDENTITY_INSERT sys_permission OFF;


                    INSERT INTO [db_ver] ([version]) VALUES (20);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 20.2; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
