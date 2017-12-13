using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0019_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    
                    
                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'F' AND name = 'FK_sys_role_permission_sys_role')
                    BEGIN
                        ALTER TABLE dbo.sys_role_permission DROP CONSTRAINT FK_sys_role_permission_sys_role;
                    END

                    ALTER TABLE dbo.sys_role_permission ADD CONSTRAINT
	                    FK_sys_role_permission_sys_role FOREIGN KEY
	                    (
	                    role_id
	                    ) REFERENCES dbo.sys_role
	                    (
	                    id
	                    ) ON UPDATE  NO ACTION 
	                     ON DELETE  NO ACTION 
	

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'F' AND name = 'FK_sys_role_permission_sys_permission')
                    BEGIN
                        ALTER TABLE dbo.sys_role_permission DROP CONSTRAINT FK_sys_role_permission_sys_permission;
                    END

                    ALTER TABLE dbo.sys_role_permission ADD CONSTRAINT
	                    FK_sys_role_permission_sys_permission FOREIGN KEY
	                    (
	                    permission_id
	                    ) REFERENCES dbo.sys_permission
	                    (
	                    id
	                    ) ON UPDATE  CASCADE
	                     ON DELETE  NO ACTION 


                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 19.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
