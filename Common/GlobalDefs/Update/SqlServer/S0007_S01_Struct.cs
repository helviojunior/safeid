using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0007_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_insert_entity_to_container')
                    BEGIN
                        DROP PROCEDURE sp_insert_entity_to_container;
                    END

                    EXEC ('
                    CREATE procedure [dbo].[sp_insert_entity_to_container]
	                    @enterprise_id bigint,
	                    @container_id bigint,
	                    @entity_id bigint
                    AS
                    BEGIN

	                    DELETE FROM entity_container WHERE entity_id = @entity_id

	                    INSERT INTO entity_container (entity_id, container_id, [auto]) VALUES ( @entity_id, @container_id, 0)

	                    SELECT TOP	1 c.*, e.entity_id, e.auto
		                    FROM	entity_container e
		                    INNER JOIN container c on e.container_id = c.id
		                    WHERE	e.entity_id = @entity_id
	
                    END
                    ');

                    INSERT INTO [db_ver] ([version]) VALUES (7);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 7.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
