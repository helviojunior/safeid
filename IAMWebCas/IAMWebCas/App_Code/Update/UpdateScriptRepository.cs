using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace SafeTrend.Data.Update
{
    public static class UpdateScriptRepository
    {
        /// <summary>
        /// Creates the list of scripts that should be executed on app start. Ordering matters!
        /// </summary>
        public static IEnumerable<IUpdateScript> GetScriptsBySqlProviderName(ConnectionStringSettings connectionString)
        {
            switch (connectionString.ProviderName)
            {
                case "SQLite":
                case "System.Data.SQLite":
                    return new List<IUpdateScript>
                    {
                        new SqliteServer.InitialCreateScript(),
                        new SqliteServer.InsertDefaultData(),
                    };

                case "SqlClient":
                case "System.Data.SqlClient":
                    return new List<IUpdateScript>
                    {
                        new SqlServer.InitialCreateScript(),
                        new SqlServer.InsertDefaultData(),
                    };

                default:
                    throw new NotImplementedException(string.Format("The provider '{0}' is not supported yet", connectionString.ProviderName));
            }
        }
    }
}