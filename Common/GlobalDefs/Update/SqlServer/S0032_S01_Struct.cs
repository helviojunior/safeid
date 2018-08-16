using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0032_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    ALTER TABLE dbo.service_status ADD started_at datetime NULL

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_service_start')
                    BEGIN
                        DROP PROCEDURE [sp_service_start];
                    END

                    EXEC('CREATE PROCEDURE [dbo].[sp_service_start]
	                    @name varchar(200)
                    as

                    /* Insere o identity, caso não exista */
                    insert into service_status (service_name)
                    select		@name
	                    where	not exists (select		1 from service_status with(nolock)
							                    where 	service_name = @name)

                    /* Atualiza o registro */
                    update		service_status
		                    set	started_at = GETDATE()
	                    where	service_name = @name
                    ');


                    INSERT INTO [db_ver] ([version]) VALUES (32);
                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 32; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
