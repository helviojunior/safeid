using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.Config
{
    public class ServerDBConfig: IAMDatabase, IDisposable
    {
        Boolean noClose = false;
        public ServerDBConfig(SqlConnection conn) :
            this(conn, false) { }

        public ServerDBConfig(SqlConnection conn, Boolean noClose)
            :base(conn)
        {
            this.noClose = noClose;
        }

        public String GetItem(String key)
        {
            DataTable dt = ExecuteDataTable("select * from server_config with(nolock) where data_name = '" + key + "'");
            if ((dt == null) || (dt.Rows.Count == 0))
                return "";

            return dt.Rows[0]["data_value"].ToString();
        }

        public void Dispose()
        {
            if (noClose)
                return;

            base.closeDB();
        }
    }
}
