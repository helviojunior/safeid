using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSSL.X509;

namespace IAM.CA
{
    public class EnterpriseKey: IDisposable
    {

        private String dnsName;
        private String name;
        private String key;
        private CertificateAuthority ca;

        public String ClientPKCS12Cert { get; private set; }
        public String ServerPKCS12Cert { get; private set; }
        public String ServerCert { get; private set; }

        public EnterpriseKey(Uri Uri, String Name)
        {
            this.dnsName = Uri.Host;
            this.name = Name;

            //A senha dos certificados é o hash da URI da empresa
            System.Security.Cryptography.SHA1Managed sha = new System.Security.Cryptography.SHA1Managed();
            Byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(this.dnsName));
            key = BitConverter.ToString(hash).Replace("-", "");

            ca = new CertificateAuthority(key);
            ca.LoadOrCreateCA("IAMServerCertificateRoot.pfx", "IAM Server Certificate Root");

        }

        public void BuildCerts()
        {
            this.BuildClientCert();
            this.BuildServerCert();
        }

        private void BuildClientCert()
        {
            CertificateAuthority.subjectAltName alt = new CertificateAuthority.subjectAltName();
            alt.Dns.Add(this.dnsName);

            ClientPKCS12Cert = ca.SignCert(this.name + " (proxy)", false, alt, false, DateTime.Now.AddYears(10));
        }

        private void BuildServerCert()
        {
            CertificateAuthority.subjectAltName alt = new CertificateAuthority.subjectAltName();
            alt.Dns.Add(this.dnsName);

            ServerPKCS12Cert = ca.SignCert(this.name + " (server)", false, alt, false, DateTime.Now.AddYears(10));

            Byte[] tmp = Convert.FromBase64String(ServerPKCS12Cert);

            X509Certificate cert = CATools.GetX509CertFromPKCS12(tmp, key);
            ServerCert = CATools.X509ToBase64(cert);
        }

        public void Dispose(){
            this.dnsName = null;
            this.name = null;
        }

    }
}
