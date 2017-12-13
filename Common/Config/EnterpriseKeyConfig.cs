using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSSL.X509;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;


namespace IAM.Config
{
    public class EnterpriseKeyConfig : IAMDatabase, IDisposable, ICloneable
    {

        private Int64 enterpriseId;

        public X509Certificate ServerPKCS12Cert { get; private set; }
        public X509Certificate ClientPKCS12Cert { get; private set; }
        public X509Certificate ServerCert { get; private set; }
        public String ServerPKCS12String { get; private set; }
        public String ClientPKCS12String { get; private set; }
        public String ServerCertString { get; private set; }
        public Uri ServerInstallationKey { get; private set; }

        protected EnterpriseKeyConfig(){}

        public EnterpriseKeyConfig(SqlConnection conn, Int64 enterpriseId) :
            this(conn, enterpriseId, null) { }

        public EnterpriseKeyConfig(SqlConnection conn, Int64 enterpriseId, SqlTransaction transaction)
        {
            this.enterpriseId = enterpriseId;
            base.Connection = conn;

            DataTable dt = ExecuteDataTable("select fqdn, server_cert, server_pkcs12_cert from enterprise with(nolock) where id = " + this.enterpriseId, transaction);

            if ((dt == null) || (dt.Rows.Count == 0)) //Não encontrou a empresa
                throw new Exception("Enterprise '" + enterpriseId + "' not found");

            System.Security.Cryptography.SHA1Managed sha = new System.Security.Cryptography.SHA1Managed();
            Byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(dt.Rows[0]["fqdn"].ToString()));
            String key = BitConverter.ToString(hash).Replace("-", "");

            this.ServerCertString = dt.Rows[0]["server_cert"].ToString();
            this.ServerPKCS12String = dt.Rows[0]["server_pkcs12_cert"].ToString();

            this.ServerCert = CATools.LoadCert(Convert.FromBase64String(this.ServerCertString));
            this.ServerPKCS12Cert = CATools.LoadCert(Convert.FromBase64String(this.ServerPKCS12String), key);

            //Atualiza o certificado em arquivo (apenas para visualização do usuário)
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(ServerKey2));
                FileInfo certFile = new FileInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "eCerts\\" + dt.Rows[0]["fqdn"].ToString() + ".cer"));
                if (certFile.Exists)
                    certFile.Delete();

                if (!certFile.Directory.Exists)
                    certFile.Directory.Create();

                File.WriteAllBytes(certFile.FullName, Convert.FromBase64String(this.ServerCertString));
            }
            catch { }
            /*
            }
            else
            {
                //Cria

                this.BuildCert();

                DbParameterCollection par = new DbParameterCollection();
                par.Add("@server_cert", typeof(String), this.ServerCertString.Length).Value = this.ServerCertString;
                par.Add("@server_pkcs12_cert", typeof(String), this.ServerPKCS12String.Length).Value = this.ServerPKCS12String;

                ExecuteSQL(conn, "insert into server_cert (server_cert, server_pkcs12_cert) values (@server_cert, @server_pkcs12_cert)", par, CommandType.Text);

            }
            */

        }

        public Object Clone()
        {

            EnterpriseKeyConfig clone = new EnterpriseKeyConfig();

            clone.enterpriseId = this.enterpriseId;

            clone.ServerPKCS12Cert = this.ServerPKCS12Cert;
            clone.ClientPKCS12Cert = this.ClientPKCS12Cert;
            clone.ServerCert = this.ServerCert;
            clone.ServerPKCS12String = this.ServerPKCS12String;
            clone.ClientPKCS12String = this.ClientPKCS12String;
            clone.ServerCertString = this.ServerCertString;
            clone.ServerInstallationKey = this.ServerInstallationKey;

            return clone;
        }

        public void RenewCert(SqlConnection conn)
        {
            RenewCert(conn, null);
        }

        public void RenewCert(SqlConnection conn, SqlTransaction transaction)
        {
            SqlTransaction trans = transaction;

            base.Connection = conn;

            if (trans == null)
                trans = conn.BeginTransaction();
            
            DataTable dt = ExecuteDataTable("select fqdn, server_cert, server_pkcs12_cert, client_pkcs12_cert from enterprise with(nolock) where id = " + this.enterpriseId, trans);

            if ((dt == null) || (dt.Rows.Count == 0)) //Não encontrou a empresa
                throw new Exception("Enterprise '" + enterpriseId + "' not found");

            System.Security.Cryptography.SHA1Managed sha = new System.Security.Cryptography.SHA1Managed();
            Byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(dt.Rows[0]["fqdn"].ToString()));
            String key = BitConverter.ToString(hash).Replace("-", "");

            //Resgata o certificado do banco
            X509Certificate atualServerPKCS12Cert = CATools.LoadCert(Convert.FromBase64String(dt.Rows[0]["server_pkcs12_cert"].ToString()), key);
            X509Certificate atualClientPKCS12Cert = CATools.LoadCert(Convert.FromBase64String(dt.Rows[0]["client_pkcs12_cert"].ToString()), key);

            //Se tudo OK, inicia o processo
            
            try
            {
                //Cria o novo certificado, e a chave se não existir ainda
                this.BuildCert(conn, trans);

                //Exclui o certificado atual do banco
                //ExecuteSQL(conn, "delete from server_cert", null, CommandType.Text, trans);

                //Salva o novo certificado
                DbParameterCollection par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = this.enterpriseId;
                par.Add("@server_cert", typeof(String)).Value = this.ServerCertString;
                par.Add("@server_pkcs12_cert", typeof(String)).Value = this.ServerPKCS12String;
                par.Add("@client_pkcs12_cert", typeof(String)).Value = this.ClientPKCS12String;

                ExecuteNonQuery("update enterprise set server_cert = @server_cert, server_pkcs12_cert = @server_pkcs12_cert, client_pkcs12_cert = @client_pkcs12_cert where id = @enterprise_id", CommandType.Text, par, trans);

                //Criptografa a senha de todas as entidades
                DataTable dtEnt = ExecuteDataTable("select e.id, e.login, e.password from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id inner join enterprise e1 with(nolock) on e1.id = c.enterprise_id where e1.id = " + this.enterpriseId, trans);
                if (dtEnt == null)
                    throw new Exception("Erro on SQL");

                foreach (DataRow dr in dtEnt.Rows)
                {
                    Console.Write("[EK] Entity " + dr["id"] + ": ");

                    try
                    {

                        using (CryptApi decryptApi = CryptApi.ParsePackage(atualServerPKCS12Cert, Convert.FromBase64String(dr["password"].ToString())))
                        using (CryptApi ecryptApi = new CryptApi(this.ServerCert, decryptApi.clearData))
                        {
                            DbParameterCollection pPar = new DbParameterCollection();
                            String b64 = Convert.ToBase64String(ecryptApi.ToBytes());
                            pPar.Add("@password", typeof(String), b64.Length).Value = b64;

                            Exception ex1 = null;
                            for (Int32 count = 1; count <= 3; count++)
                            {
                                try
                                {
                                    ExecuteNonQuery("update entity set password = @password where id = " + dr["id"], CommandType.Text, pPar, trans);
                                    ex1 = null;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    ex1 = ex;
                                    if (ex.Message.ToLower().IndexOf("timeout") != -1)
                                    {
                                        System.Threading.Thread.Sleep(1000 * count);
                                    }
                                }
                            }

                            if (ex1 != null)
                                throw ex1;

                            Log(this.enterpriseId.ToString(), dr["id"].ToString(), dr["login"].ToString(), Encoding.UTF8.GetString(decryptApi.clearData));
                            Console.WriteLine("OK");
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Err");
                        throw ex;
                    }

                }

                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(ServerKey2));
                    FileInfo certFile = new FileInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "eCerts\\" + dt.Rows[0]["fqdn"].ToString() + ".cer"));
                    if (certFile.Exists)
                        certFile.Delete();


                    if (!certFile.Directory.Exists)
                        certFile.Directory.Create();

                    File.WriteAllBytes(certFile.FullName, Convert.FromBase64String(this.ServerCertString));
                }
                catch { }

                //Se tudo estiver OK, realiza o commit dos dados
                Console.WriteLine("Commit");
                
                if (transaction == null) trans.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Rollback");
                if (transaction == null) trans.Rollback();
                throw ex;
            }

        }

        private static void Log(String enterprise, String id, String username, String password)
        {
            try
            {
                Int32 pid = 0;
                try
                {
                    pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                }
                catch { }

                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();

                String path = Path.Combine(Path.GetDirectoryName(asm.Location), "logs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                FileInfo file = new FileInfo(Path.Combine(path, DateTime.Now.ToString("yyyyMMdd") + "-RenewCert.log"));

                if (!file.Exists)
                {
                    BinaryWriter writer1 = new BinaryWriter(File.Open(file.FullName, FileMode.Append));
                    writer1.Write(Encoding.UTF8.GetBytes(DateTime.Now.ToString("o") + " ==> [" + "ServerKey" + (pid > 0 ? " -> " + pid : "") + "] " + Environment.NewLine + Environment.NewLine));
                    writer1.Write(Encoding.UTF8.GetBytes("enterprise_id,user_id,login,password" + Environment.NewLine));
                    writer1.Flush();
                    writer1.Close();
                }

                BinaryWriter writer = new BinaryWriter(File.Open(file.FullName, FileMode.Append));
                writer.Write(Encoding.UTF8.GetBytes(enterprise + "," + id + "," + username + "," + password + Environment.NewLine));
                writer.Flush();
                writer.Close();

                asm = null;
            }
            catch { }
        }

        private void BuildCert(SqlConnection conn, SqlTransaction trans)
        {
            base.Connection = conn;

            DataTable dt = ExecuteDataTable("select fqdn, name from enterprise with(nolock) where id = " + this.enterpriseId, trans);

            if ((dt == null) || (dt.Rows.Count == 0)) //Não encontrou a empresa
                throw new Exception("Enterprise '" + enterpriseId + "' not found");


            System.Security.Cryptography.SHA1Managed sha = new System.Security.Cryptography.SHA1Managed();
            Byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(dt.Rows[0]["fqdn"].ToString()));
            String key = BitConverter.ToString(hash).Replace("-", "");


            EnterpriseKey keys = new EnterpriseKey(new Uri("//" + dt.Rows[0]["fqdn"].ToString()), dt.Rows[0]["name"].ToString());
            keys.BuildCerts();

            try
            {

                this.ServerPKCS12String = keys.ServerPKCS12Cert;

                Byte[] certData = Convert.FromBase64String(this.ServerPKCS12String);
                this.ServerCert = CATools.GetX509CertFromPKCS12(certData, key);
                this.ServerCertString = CATools.X509ToBase64(this.ServerCert);
                this.ServerPKCS12Cert = CATools.LoadCert(certData, key);

                this.ClientPKCS12String = keys.ClientPKCS12Cert;
                this.ClientPKCS12Cert = CATools.LoadCert(Convert.FromBase64String(this.ClientPKCS12String), key);

            }
            finally
            {
                
            }
        }

        public void Dispose(){
            this.ServerPKCS12Cert = null;
            this.ServerCert = null;
            this.ServerPKCS12String = null;
            this.ServerCertString = null;
            this.ClientPKCS12Cert = null;
            this.ClientPKCS12String = null;
        }

    }
}
