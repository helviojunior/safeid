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
    public class ServerKey2 : IAMDatabase, IDisposable
    {

        private String hostname;
        private CertificateAuthority ca;

        public X509Certificate ServerPKCS12Cert { get; private set; }
        public X509Certificate ServerCert { get; private set; }
        public String ServerPKCS12String { get; private set; }
        public String ServerCertString { get; private set; }
        public Uri ServerInstallationKey { get; private set; }

        public ServerKey2(SqlConnection conn) :
            this(conn, Environment.MachineName, null) { }

        public ServerKey2(SqlConnection conn, SqlTransaction transaction) :
            this(conn, Environment.MachineName, transaction) { }

        public ServerKey2(SqlConnection conn, String hostname, SqlTransaction transaction)
            :base(conn)
        {

            DataTable dt = ExecuteDataTable("select server_cert, server_pkcs12_cert from server_cert with(nolock)", transaction);

            this.hostname = hostname;

            if ((dt != null) && (dt.Rows.Count > 0)) //Existe certificado, então lê
            {
                this.ServerCertString = dt.Rows[0]["server_cert"].ToString();
                this.ServerPKCS12String = dt.Rows[0]["server_pkcs12_cert"].ToString();

                this.ServerCert = CATools.LoadCert(Convert.FromBase64String(this.ServerCertString));
                this.ServerPKCS12Cert = CATools.LoadCert(Convert.FromBase64String(this.ServerPKCS12String), "w0):X,\\Q4^NoIO,):Z!.");

                this.ServerInstallationKey = GetInstallationCode(this.ServerPKCS12Cert);

                //Atualiza o certificado em arquivo (apenas para visualização do usuário)
                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(ServerKey2));
                    FileInfo certFile = new FileInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "server.cer"));
                    if (certFile.Exists)
                        certFile.Delete();

                    File.WriteAllBytes(certFile.FullName, Convert.FromBase64String(this.ServerCertString));
                }
                catch { }

            }
            else
            {
                //Cria

                this.BuildCert();

                DbParameterCollection par = new DbParameterCollection();
                par.Add("@server_cert", typeof(String), this.ServerCertString.Length).Value = this.ServerCertString;
                par.Add("@server_pkcs12_cert", typeof(String), this.ServerPKCS12String.Length).Value = this.ServerPKCS12String;

                ExecuteNonQuery("insert into server_cert (server_cert, server_pkcs12_cert) values (@server_cert, @server_pkcs12_cert)", CommandType.Text, par);

            }


        }

        public void RenewCert(SqlConnection conn)
        {
            base.Connection = conn;

            DataTable dt = ExecuteDataTable("select server_cert, server_pkcs12_cert from server_cert with(nolock)");

            if ((dt != null) && (dt.Rows.Count > 0)) //Existe certificado, então lê
            {
                //Resgata o certificado do banco
                X509Certificate atualServerPKCS12Cert = CATools.LoadCert(Convert.FromBase64String(dt.Rows[0]["server_pkcs12_cert"].ToString()), "w0):X,\\Q4^NoIO,):Z!.");

                //Primeiramente atualiza todas as senhas atuais para a senha usando o certificado da empresa
                SqlTransaction trans = null;
                
                /*
                conn.BeginTransaction();
                try
                {
                    //Criptografa a senha de todas as entidades
                    DataTable dtEnterprise = ExecuteDataTable("select * from enterprise with(nolock)", trans);
                    if (dtEnterprise == null)
                        throw new Exception("Erro on enterprise SQL");

                    foreach (DataRow drEnt in dtEnterprise.Rows)
                    {
                        Console.WriteLine("Enterprise " + drEnt["id"]);

                        using (EnterpriseKeyConfig ek = new EnterpriseKeyConfig(conn, (Int64)drEnt["id"], trans))
                        {

                            DataTable dtEnt = ExecuteDataTable("select e.id, e.login, e.password from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = " + drEnt["id"], trans);
                            if (dtEnt == null)
                                throw new Exception("Erro on SQL");

                            foreach (DataRow dr in dtEnt.Rows)
                            {
                                Console.Write("\t[SK] Entity " + dr["id"] + ": ");

                                CryptApi decryptApi = null;
                                try
                                {

                                    try
                                    {
                                        //Tenta decriptografia com certificado da empresa
                                        decryptApi = CryptApi.ParsePackage(ek.ServerPKCS12Cert, Convert.FromBase64String(dr["password"].ToString()));

                                        //Processo OK, a senha ja está usando o certificado da empresa
                                        Console.WriteLine("OK");
                                        continue;

                                    }
                                    catch
                                    {

                                        //Tenta decriptografia com o certificado geral do servidor
                                        //Se conseguir atualiza a senha para o certificado da empresa
                                        decryptApi = CryptApi.ParsePackage(atualServerPKCS12Cert, Convert.FromBase64String(dr["password"].ToString()));
                                    }

                                    using (CryptApi ecryptApi = new CryptApi(ek.ServerCert, decryptApi.clearData))
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

                                        Log(drEnt["id"].ToString(), dr["id"].ToString(), dr["login"].ToString(), Encoding.UTF8.GetString(decryptApi.clearData));
                                        Console.WriteLine("OK, Updated");
                                    }

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Err");
                                    throw ex;
                                }
                                finally
                                {
                                    if (decryptApi != null) decryptApi.Dispose();
                                }

                            }

                        }

                        Console.WriteLine("");
                    }

                    //Se tudo estiver OK, realiza o commit dos dados
                    trans.Commit();
                    Console.WriteLine("Commit");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Rollback");
                    if (trans != null) trans.Rollback();
                    throw ex;
                }*/

                //Atualiza o certificado global do servidor
                //e gera novo certificado da empresa e atualiza o mesmo
                trans = conn.BeginTransaction();
                Console.WriteLine("Update Global Server Certificate");
                try
                {
                    //Se a chave de instalaçõe é nula
                    if (this.ServerInstallationKey == null)
                        this.ServerInstallationKey = GetInstallationCode(atualServerPKCS12Cert);

                    //Cria o novo certificado, e a chave se não existir ainda
                    this.BuildCert();

                    //Exclui o certificado atual do banco
                    ExecuteNonQuery("delete from server_cert", CommandType.Text, null, trans);

                    //Salva o novo certificado
                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@server_cert", typeof(String), this.ServerCertString.Length).Value = this.ServerCertString;
                    par.Add("@server_pkcs12_cert", typeof(String), this.ServerPKCS12String.Length).Value = this.ServerPKCS12String;

                    ExecuteNonQuery("insert into server_cert (server_cert, server_pkcs12_cert) values (@server_cert, @server_pkcs12_cert)", CommandType.Text, par, trans);


                    Console.WriteLine("Commit");
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Rollback");
                    trans.Rollback();
                    throw ex;
                }

                try
                {

                    //Criptografa a senha de todas as entidades

                    DataTable dtEnterprise = ExecuteDataTable("select * from enterprise with(nolock)", trans);
                    if (dtEnterprise == null)
                        throw new Exception("Erro on enterprise SQL");

                    foreach (DataRow drEnt in dtEnterprise.Rows)
                    {
                        Console.WriteLine("Enterprise " + drEnt["id"]);

                        using (EnterpriseKeyConfig ek = new EnterpriseKeyConfig(conn, (Int64)drEnt["id"], trans))
                            ek.RenewCert(conn);

                        Console.WriteLine("");
                    }

                    try
                    {
                        System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(ServerKey2));
                        FileInfo certFile = new FileInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "server.cer"));
                        if (certFile.Exists)
                            certFile.Delete();

                        File.WriteAllBytes(certFile.FullName, Convert.FromBase64String(this.ServerCertString));
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
            else //Não foi encontrado certificado no banco, erro
            {
                //Como ao instanciar esta classe a verificação e criação do certificado ja foi realizada, não deve acontecer esse erro
                throw new Exception("Erro on find server certificate");
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

        private void BuildCert()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(ServerKey2));
            FileInfo p12File = new FileInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "server"+ DateTime.Now.ToString("yyyyMMddHHmss") +".pfx"));

            try
            {
                CertificateAuthority.subjectAltName alt = new CertificateAuthority.subjectAltName();
                if ((this.ServerInstallationKey == null))
                    this.NewInstallationKey();

                alt.Uri.Add(ServerInstallationKey);

                ca = new CertificateAuthority("123456", "w0):X,\\Q4^NoIO,):Z!.");
                ca.LoadOrCreateCA(p12File.FullName, this.hostname, alt);

                Byte[] certData = File.ReadAllBytes(p12File.FullName);

                this.ServerCert = CATools.GetX509CertFromPKCS12(certData, "w0):X,\\Q4^NoIO,):Z!.");
                this.ServerCertString = CATools.X509ToBase64(this.ServerCert);
                this.ServerPKCS12String = Convert.ToBase64String(certData);
                this.ServerPKCS12Cert = CATools.LoadCert(certData, "w0):X,\\Q4^NoIO,):Z!.");

            }
            finally
            {
                try
                {
                    File.Delete(p12File.FullName);
                    File.Delete(p12File.FullName.Replace(p12File.Extension, ".cer"));
                }
                catch { }

                p12File = null;
                asm = null;
            }
        }

        private void NewInstallationKey()
        {
            this.ServerInstallationKey = null;

            String r = "ADGHKNQTUY";
            String d = DateTime.Now.ToString("yyyyMMddHHmmss");
            String tKey = "";
            Byte[] rKey = new Byte[7];
            foreach (char c in d)
                tKey += (r[Int32.Parse(c.ToString())]);

            Random rnd = new Random();
            rnd.NextBytes(rKey);
            String dkey = "";
            rnd = null;

            dkey += tKey.Substring(0, 7) + BitConverter.ToString(rKey).Replace("-", "") + tKey.Substring(7);
            String fKey = "";
            for (Int32 i = 0; i < dkey.Length; i++)
                if ((i % 7) == 0)
                    fKey += "/" + dkey[i];
                else
                    fKey += dkey[i];

            this.ServerInstallationKey = new Uri("installKey://safeid/v100/" + fKey.Trim(" /".ToCharArray()));
        }

        public static Uri GetInstallationCode(X509Certificate cert)
        {
            Uri ic = null;

            foreach (OpenSSL.X509.X509Extension e in cert.Extensions)
            {
                if (e.NID == 85) //X509v3 Subject Alternative Name
                {
                    int maskedTag = e.Data[2] & (Int32)X509altNameTag.TAG_MASK;
                    if (((X509altNameTag)maskedTag) == X509altNameTag.Uri)
                    {
                        Int32 len = e.Data[3];
                        String sUri = Encoding.UTF8.GetString(e.Data, 4, len);
                        Uri tmp = new Uri(sUri);
                        if (tmp.Scheme.ToLower() == "installkey")
                        {
                            ic = tmp;
                            break;
                        }
                    }
                }
            }

            return ic;
        }

        public void Dispose(){
            this.hostname = null;
            this.ServerPKCS12Cert = null;
            this.ServerCert = null;
            this.ServerPKCS12String = null;
            this.ServerCertString = null;
        }

    }
}
