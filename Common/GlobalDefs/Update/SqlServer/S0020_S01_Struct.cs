using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0020_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    	
                    IF (EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'login_rule' AND  COLUMN_NAME = 'separator'))
                    BEGIN
    
                        EXEC('
                        UPDATE login_rule 
		                    SET [rule] = REPLACE(CASE WHEN separator = ''.'' THEN REPLACE([rule], '','', '',dot,'') 
			                    WHEN separator = ''-'' THEN REPLACE([rule], '','', '',hyphen,'') 
			                    ELSE [rule]
			                    END, ''dot,index'',''index'')')
    
                        EXEC('ALTER TABLE dbo.login_rule DROP COLUMN separator');
    
                    END


                    IF (EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'login_rule' AND  COLUMN_NAME = 'name' AND CHARACTER_MAXIMUM_LENGTH = 50))
                    BEGIN
	                    ALTER TABLE dbo.login_rule ALTER COLUMN name VARCHAR (300) NOT NULL
                    END

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 20.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
