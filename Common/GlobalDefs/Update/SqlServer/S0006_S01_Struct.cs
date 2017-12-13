using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0006_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_new_container')
                    BEGIN
                        DROP PROCEDURE sp_new_container;
                    END

                    EXEC ('
                    CREATE procedure [dbo].[sp_new_container]
	                    @enterprise_id bigint,
	                    @context_id bigint,
	                    @parent_id bigint,
	                    @container_name varchar(50)
                    as
                    BEGIN

                    DECLARE @DATE DATETIME;
                    SET @DATE = GETDATE()

                    INSERT INTO [container] (context_id, parent_id, [name], [create_date])
                    VALUES (@context_id, @parent_id, @container_name, @DATE)

                    SELECT	c1.*, c.name context_name, c.enterprise_id, 0 AS entity_qty
                    FROM		[container] c1
	                    inner	join context c
		                    on	c.id = c1.context_id
                    WHERE	c1.context_id	= @context_id
	                    AND	c1.parent_id	= @parent_id
	                    AND	c1.name			= @container_name
	                    AND	c1.create_date	= @DATE

                    END
                    ');

                    INSERT INTO [db_ver] ([version]) VALUES (6);


                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 6.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
