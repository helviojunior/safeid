using System;
using System.Collections.Generic;
using System.Text;
using IAM.License;
using IAM.CA;
using OpenSSL.X509;
using System.IO;

namespace IAMGeraKey
{
    class Program
    {
        static void Main(string[] args)
        {
            CertificateAuthority ca = new CertificateAuthority("123456", "RQ`EI'F9f+{;9}7![ooa");
            ca.LoadOrCreateCA("license-cert.pfx", "SafeID IAM License Server");

            Uri installKey = null;
            try
            {
                installKey = new Uri(args[0]);
            }
            catch {
                Console.WriteLine("Erro ao realizar o tratamento da chave de instalação");
                Use();
            }

            uint qty = 0;
            try
            {
                qty = uint.Parse(args[1]);
            }
            catch
            {
                Console.WriteLine("Erro ao realizar o tratamento da chave de instalação");
                Use();
            }

            DateTime? date = null;
            Boolean temp = false;
            if (args.Length > 2)
                try
                {
                    date = DateTime.Parse(args[2]);
                    temp = true;
                }
                catch
                {
                    Console.WriteLine("Erro ao realizar o tratamento da chave de instalação");
                    Use();
                }


            X509Certificate key = IAMKey.GenerateLicense(ca, installKey, true, qty, temp, date);

            String sKey = CATools.X509ToBase64(key);

            try
            {
                IAMKeyData k = IAMKey.ExtractFromCert(sKey);
                Console.WriteLine("Licen\x00e7a gerada com sucesso");

            }
            catch(Exception ex) {
                Console.WriteLine("Falha na checagem de consistência: " + ex.Message);
                return;
            }


            using (FileStream stream = System.IO.File.Open(DateTime.Now.ToString("yyyyMMddHHmmss") + ".cer", FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(stream))
                writer.Write(Convert.FromBase64String(sKey));



            using (FileStream stream = System.IO.File.Open(DateTime.Now.ToString("yyyyMMddHHmmss") + ".cer.txt", FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(stream))
                writer.Write(Encoding.UTF8.GetBytes(sKey));


        }

        public static void Use()
        {
            Console.WriteLine("IAMGeraKey.exe [install_key] [lic_count] {end_date}");
        }

    }
}
