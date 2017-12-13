using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using IAM.EnterpriseCreator;
using IAM.LocalConfig;
//using IAM.SQLDB;
using IAM.GlobalDefs;
using System.Data;
using System.IO;

namespace InstallConfig
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length != 2)
            {
                Console.WriteLine("Parâmetros: nome fqdn ");
                return;
            }

            ServerLocalConfig localConfig = new ServerLocalConfig();
            localConfig.LoadConfig();

            if ((localConfig.SqlServer == null) || (localConfig.SqlServer.Trim() == ""))
                StopOnError("Parâmetro 'sqlserver' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlDb == null) || (localConfig.SqlDb.Trim() == ""))
                StopOnError("Parâmetro 'sqldb' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlUsername == null) || (localConfig.SqlUsername.Trim() == ""))
                StopOnError("Parâmetro 'sqlusername' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlPassword == null) || (localConfig.SqlPassword.Trim() == ""))
                StopOnError("Parâmetro 'sqlpassword' não localizado no arquivo de configuração 'server.conf'", null);

            IAMDatabase db = null;
            try
            {
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                
            }
            catch (Exception ex)
            {
                StopOnError("Erro conectando na base de dados: " + ex.Message, null);
            }

            //Verifica se existe um certificado de servidor para usar
            DataTable dt = db.ExecuteDataTable("select server_cert, server_pkcs12_cert from server_cert with(nolock)");

            if ((dt != null) && (dt.Rows.Count > 0)) //Existe certificado, então lê
            {
                //this.ServerCertString = dt.Rows[0]["server_cert"].ToString();
                //this.ServerPKCS12String = dt.Rows[0]["server_pkcs12_cert"].ToString();


                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(Program));
                    FileInfo certFile = new FileInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "IAMServerCertificateRoot.cer"));
                    if (certFile.Exists)
                        certFile.Delete();

                    File.WriteAllBytes(certFile.FullName, Convert.FromBase64String(dt.Rows[0]["server_cert"].ToString()));
                }
                catch { }

                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(Program));
                    FileInfo certFile = new FileInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "IAMServerCertificateRoot.pfx"));
                    if (certFile.Exists)
                        certFile.Delete();

                    File.WriteAllBytes(certFile.FullName, Convert.FromBase64String(dt.Rows[0]["server_pkcs12_cert"].ToString()));
                }
                catch { }

            }

            //Creator creator = new Creator(db, "SafeID - Start enterprise", "demo.safeid.com.br", "pt-BR", "//login.safeid.com.br/cas/");
            Creator creator = new Creator(db, args[0], args[1], "pt-BR");
            creator.BuildCertificates();
            creator.Commit();
        }

        static private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
            }

            Process.GetCurrentProcess().Kill();
        }

    }
}
