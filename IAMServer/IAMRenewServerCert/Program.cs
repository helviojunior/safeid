using System;
using System.Collections.Generic;
using System.Text;


using IAM.Config;
using IAM.Log;
using IAM.CA;
//using IAM.SQLDB;
using IAM.LocalConfig;
using IAM.GlobalDefs;


namespace IAMRenewServerCert
{
    class Program
    {

        static void Main(string[] args)
        {

            ServerLocalConfig localConfig;

            /*************
             * Carrega configurações
             */

            localConfig = new ServerLocalConfig();
            localConfig.LoadConfig();

            if ((localConfig.SqlServer == null) || (localConfig.SqlServer.Trim() == ""))
                StopOnError("Parâmetro 'sqlserver' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlDb == null) || (localConfig.SqlDb.Trim() == ""))
                StopOnError("Parâmetro 'sqldb' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlUsername == null) || (localConfig.SqlUsername.Trim() == ""))
                StopOnError("Parâmetro 'sqlusername' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlPassword == null) || (localConfig.SqlPassword.Trim() == ""))
                StopOnError("Parâmetro 'sqlpassword' não localizado no arquivo de configuração 'server.conf'", null);


            using (IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
            {
                try
                {
                    db.openDB();
                }
                catch (Exception ex)
                {
                    StopOnError("Erro on acess database ", ex);
                }

                try
                {
                    

                    ServerKey2 sk = new ServerKey2(db.Connection);
                    sk.RenewCert(db.Connection);

                    Console.WriteLine("Renewed certificate successfully");
                    TextLog.Log("RenewServerCert", "Renewed certificate successfully");
                }
                catch (Exception ex)
                {
                    UnhandledException.WriteEvent(null, ex, false);
                    StopOnError("Error on renew certificate ", ex);
                }

            }

            Console.WriteLine("Pressione ENTER para finalizar");
            Console.ReadLine();

        }


        private static void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
                TextLog.Log("RenewServerCert", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("RenewServerCert", text);
            }

        }
    }
}
