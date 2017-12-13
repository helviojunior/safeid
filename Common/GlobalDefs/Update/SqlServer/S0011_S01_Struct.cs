using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0011_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (select count(*) from sys_sub_module where id = 14) = 0
                    BEGIN
                        SET IDENTITY_INSERT sys_sub_module ON;
                        insert into sys_sub_module (id, [module_id], [key], [api_module], name) values(14, 2, 'license', 'license', 'Licença');
                        SET IDENTITY_INSERT sys_sub_module OFF;

                        SET IDENTITY_INSERT [sys_permission] ON;
                        INSERT INTO [sys_permission]([id],[submodule_id],[key],[name]) VALUES (33, 14, 'info','Informações de licenciamento');
                        SET IDENTITY_INSERT [sys_permission] OFF;
                    END

                    INSERT INTO [db_ver] ([version]) VALUES (11);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 11.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
