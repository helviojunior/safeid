using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSSL.Core;
using OpenSSL.Crypto;
using OpenSSL.SSL;
using OpenSSL.X509;
using System.IO;

namespace IAM.CA
{
    public class CertificateAuthority : IDisposable
    {

        public class subjectAltName
        {
            public List<Uri> Uri { get; set; }
            public List<String> Dns { get; set; }
            public List<String> Text { get; set; }
            public List<String> Mail { get; set; }

            public subjectAltName()
            {
                Uri = new List<Uri>();
                Dns = new List<String>();
                Text = new List<String>();
                Mail = new List<String>();
            }
        }

        private DirectoryInfo certDir;
        private Configuration cfg;
        private X509CertificateAuthority rootCA = null;
        private String signedPassword = "123456";
        private String caPassword = "0D:=$H.hk3U'S*!JDdDV";

        public DirectoryInfo CertDir { get { return certDir; } set { certDir = value; } }
        public X509CertificateAuthority RootCA { get { return rootCA; } internal set { rootCA = value; } }
        public String SignedPassword { get { return signedPassword; } internal set { signedPassword = value; } }

        public CertificateAuthority() :
            this(null, null) { }

        public CertificateAuthority(String signedPassword) :
            this(signedPassword, null) { }

        public CertificateAuthority(String signedPassword, String caPassword)
        {

            if ((signedPassword != null) && (signedPassword != ""))
                this.signedPassword = signedPassword;

            if ((caPassword != null) && (caPassword != ""))
                this.caPassword = caPassword;

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            certDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "certs"));
            if (!certDir.Exists)
                certDir.Create();

            BuildCfg();

            // Create a configuration object using openssl.cnf file.
            cfg = new Configuration(Path.Combine(certDir.FullName, "openssl.cfg"));

        }
       
        public void Dispose()
        {
            if (RootCA != null)
                RootCA.Dispose();

        }

        public void WriteChain(X509Certificate cert, X509Chain chain)
        {

            FileInfo p7b = new FileInfo(Path.Combine(certDir.FullName, cert.Subject.Common + ".chain"));

            using (FileStream fs = new FileStream(p7b.FullName, FileMode.Create, FileAccess.ReadWrite))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                BIO bio = BIO.MemoryBuffer();

                foreach (X509Certificate c in chain)
                    c.Write(bio);

                Byte[] certData = bio.ReadBytes((Int32)bio.NumberWritten).Array;
                bw.Write(certData);
                bw.Close();
            }
        }

        public void CreateTree(X509Chain chain)
        {
            List<String> created = new List<String>();

            //Todos os roots
            X509Certificate cert = null;
            do
            {
                cert = null;
                foreach (X509Certificate c in chain)
                    if ((c.Subject.Common == c.Issuer.Common) && (!created.Exists(p => p == c.Subject.Common)))
                    {
                        cert = c;
                        break;
                    }

                if (cert != null)
                {
                    CreateCA(cert.Subject);
                    created.Add(cert.Subject.Common);
                }

            } while (cert != null);

            //Todos os filhos e netos
            CertificateAuthority ca = null;
            do
            {
                cert = null;
                foreach (X509Certificate c in chain)
                    if ((c.Subject.Common != c.Issuer.Common) && (created.Exists(p => p == c.Issuer.Common)) && (!created.Exists(p => p == c.Subject.Common)))
                    {
                        cert = c;
                        break;
                    }

                if (cert != null)
                {
                    ca = new CertificateAuthority();
                    ca.CertDir = certDir;
                    ca.LoadOrCreateCA(cert.Issuer);
                    ca.SignCert(cert.Subject);
                    created.Add(cert.Subject.Common);
                }

            } while (cert != null);

        }

        public void CreateCA(String CommonName)
        {
            X509Name DN = new X509Name();
            DN.Common = CommonName;
            DN.Organization = "IAM";
            DN.Country = "BR";

            CreateCA(DN);
        }


        public void CreateCA(X509Name Name)
        {
            FileInfo caPkcs12 = new FileInfo(Path.Combine(certDir.FullName, Name.Common + ".pfx"));

            if (caPkcs12.Exists)
                caPkcs12.Delete();

            if (RootCA != null)
                RootCA.Dispose();

            // Create a root certificate authority which will have a self signed certificate.
            RootCA = X509CertificateAuthority.SelfSigned(cfg, new SimpleSerialNumber(), CreateNewRSAKey(2048), MessageDigest.SHA256, Name, DateTime.Now, (DateTime.Now.AddYears(10) - DateTime.Now));

            BuildPKCS12AndSave(caPkcs12.FullName, this.caPassword, RootCA.Key, RootCA.Certificate);
                        
        }

        public void LoadOrCreateCA(String CommonName)
        {
            String fileName = Path.Combine(certDir.FullName, CommonName + ".pfx");
            LoadOrCreateCA(fileName, CommonName);
        }

        public void LoadOrCreateCA(X509Name Name)
        {
            String fileName = Path.Combine(certDir.FullName, Name.Common + ".pfx");
            LoadOrCreateCA(fileName, Name);
        }

        public void LoadOrCreateCA(String PKCS12Filename, String CommonName)
        {
            LoadOrCreateCA(PKCS12Filename, CommonName, null);
        }

        public void LoadOrCreateCA(String PKCS12Filename, String CommonName, subjectAltName altNames)
        {
            X509Name DN = new X509Name();
            DN.Common = CommonName;
            DN.Organization = "SafeId - IAM";
            DN.Country = "BR";

            LoadOrCreateCA(PKCS12Filename, DN, altNames);
        }

        public void LoadOrCreateCA(String PKCS12Filename, X509Name Name)
        {
            LoadOrCreateCA(PKCS12Filename, Name, null);
        }

        public void LoadOrCreateCA(String PKCS12Filename, X509Name Name, subjectAltName altNames)
        {
            FileInfo caPkcs12 = new FileInfo(PKCS12Filename);

            if (caPkcs12.Exists)
            {

                try
                {
                    Byte[] bPKCS12 = File.ReadAllBytes(caPkcs12.FullName);

                    // You need to write the CSR string to a BIO object as shown below.
                    BIO pkcs12BIO = BIO.MemoryBuffer();
                    pkcs12BIO.Write(bPKCS12);

                    X509Certificate cert = X509Certificate.FromPKCS12(pkcs12BIO, this.caPassword);

                    if (RootCA != null)
                        RootCA.Dispose();

                    RootCA = new X509CertificateAuthority(cert, cert.PrivateKey, new SimpleSerialNumber(1), cfg);
                }
                catch
                {
                    RootCA = null;
                }
            }

            if (RootCA == null)
            {
                X509V3ExtensionList ext = new X509V3ExtensionList();
                
                ext.Add(new X509V3ExtensionValue("nsComment", true, "SafeID - IAM Generated Certificate"));
                ext.Add(new X509V3ExtensionValue("basicConstraints", true, "CA:true"));
                //ext.Add(new X509V3ExtensionValue("keyUsage", true, "critical, cRLSign, keyCertSign, digitalSignature"));
                ext.Add(new X509V3ExtensionValue("subjectKeyIdentifier", true, "hash"));
                ext.Add(new X509V3ExtensionValue("authorityKeyIdentifier", true, "keyid,issuer:always"));

                if (altNames != null)
                {
                    foreach (Uri u in altNames.Uri)
                        ext.Add(new X509V3ExtensionValue("subjectAltName", true, "URI:" + u.AbsoluteUri.ToLower()));

                    foreach (String m in altNames.Mail)
                        ext.Add(new X509V3ExtensionValue("subjectAltName", true, "email:" + m));

                    foreach (String s in altNames.Dns)
                        ext.Add(new X509V3ExtensionValue("subjectAltName", true, "DNS:" + s));

                    foreach (String s in altNames.Text)
                        ext.Add(new X509V3ExtensionValue("subjectAltName", true, "otherName:1.2.3.4;UTF8:" + s));
                }

                RootCA = X509CertificateAuthority.SelfSigned(new SimpleSerialNumber(), CreateNewRSAKey(2048), MessageDigest.SHA1, Name, DateTime.Now.AddHours(-24), (DateTime.Now.AddYears(10) - DateTime.Now), ext);

                BuildPKCS12AndSave(caPkcs12.FullName, this.caPassword, RootCA.Key, RootCA.Certificate);

            }

        }

        public void LoadCA(String PKCS12Filename)
        {
            FileInfo caPkcs12 = new FileInfo(PKCS12Filename);

            if (caPkcs12.Exists)
            {

                try
                {
                    Byte[] bPKCS12 = File.ReadAllBytes(caPkcs12.FullName);

                    // You need to write the CSR string to a BIO object as shown below.
                    BIO pkcs12BIO = BIO.MemoryBuffer();
                    pkcs12BIO.Write(bPKCS12);

                    X509Certificate cert = X509Certificate.FromPKCS12(pkcs12BIO, this.caPassword);

                    if (RootCA != null)
                        RootCA.Dispose();

                    RootCA = new X509CertificateAuthority(cert, cert.PrivateKey, new SimpleSerialNumber(1), cfg);
                }
                catch
                {
                    RootCA = null;
                }
            }

        }

        public String SignCert(String Common, subjectAltName altNames, Boolean saveFile)
        {
            return SignCert(Common, false, altNames, saveFile, null);
        }

        public String SignCert(String Common, subjectAltName altNames)
        {
            return SignCert(Common, false, altNames, true, null);
        }

        public String SignCert(X509Name Name, subjectAltName altNames)
        {
            return SignCert(Name, false, altNames, true, null);
        }

        public String SignCert(String Common)
        {
            return SignCert(Common, false, null, true, null);
        }

        public String SignCert(X509Name Name)
        {
            return SignCert(Name, false, null, true, null);
        }

        public String SignCert(String Common, Boolean ca, subjectAltName altNames, Boolean saveFile, DateTime? expirationDate)
        {
            X509Name name = GetCertificateSigningRequestSubject(Common);
            return SignCert(name, ca, altNames, saveFile, expirationDate);
        }

        public String SignCert(X509Name Name, Boolean ca, subjectAltName altNames, Boolean saveFile, DateTime? expirationDate)
        {
            String certData = "";

            FileInfo file = new FileInfo(Path.Combine(certDir.FullName, Name.Common + ".pfx"));

            using (CryptoKey key = CreateNewRSAKey(4096))
            {
                int version = 2; // Version 2 is X.509 Version 3
                using (X509Request request = new X509Request(version, Name, key))
                using (X509Certificate certificate = RootCA.ProcessRequest(request, DateTime.Now.AddHours(-24), (expirationDate.HasValue ? expirationDate.Value : DateTime.Now + TimeSpan.FromDays(365)), MessageDigest.SHA1))
                {

                    if (ca)
                    {
                        certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "basicConstraints", true, "CA:true"));
                        certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "keyUsage", true, "critical, cRLSign, keyCertSign, digitalSignature"));
                        certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "certificatePolicies", true, "2.5.29.32.0"));
                    }
                    else
                    {
                        certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "basicConstraints", true, "CA:false"));
                    }

                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "issuerAltName", true, "issuer:copy"));
                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "nsComment", true, "SafeID - IAM Generated Certificate"));
                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "subjectKeyIdentifier", true, "hash"));
                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "authorityKeyIdentifier", true, "keyid,issuer:always"));
                    //certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "subjectAltName", true, "DNS:" + Name.Common));
                    
                    if (altNames != null)
                    {
                        foreach (Uri u in altNames.Uri)
                            certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "subjectAltName", true, "URI:" + u.AbsoluteUri.ToLower()));

                        foreach (String m in altNames.Mail)
                            certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "subjectAltName", true, "email:" + m));

                        foreach (String s in altNames.Dns)
                            certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "subjectAltName", true, "DNS:" + s));

                        foreach (String s in altNames.Text)
                            certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "subjectAltName", true, "otherName:1.2.3.4;UTF8:" + s));
                    }

                    /*
                     subjectAltName=email:copy,email:my@other.address,URI:http://my.url.here/
                     subjectAltName=IP:192.168.7.1
                     subjectAltName=IP:13::17
                     subjectAltName=email:my@other.address,RID:1.2.3.4
                     subjectAltName=otherName:1.2.3.4;UTF8:some other identifier*/


                    //certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "keyUsage", true, "nonRepudiation, digitalSignature, keyEncipherment, dataEncipherment, encipherOnly, decipherOnly, keyAgreement"));
                    //certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "extendedKeyUsage", true, "clientAuth"));
                    //certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "crlDistributionPoints", true, "URI:http://ok/certEnroll/ok-ca.crl"));

                    certificate.Sign(RootCA.Key, MessageDigest.SHA1);

                    if (saveFile)
                        certData = BuildPKCS12AndSave(file.FullName, this.signedPassword, key, certificate);
                    else
                        certData = BuildPKCS12(this.signedPassword, key, certificate);

                }

            }

            return certData;
        }

        public FileInfo SignCertFromRequest(FileInfo requestPath)
        {
            return SignCertFromRequest(requestPath, false);
        }

        public FileInfo SignCertFromRequest(Byte[] requestData)
        {
            return SignCertFromRequest(requestData, false);
        }

        public FileInfo SignCertFromRequest(FileInfo requestPath, Boolean ca)
        {
            Byte[] data = File.ReadAllBytes(requestPath.FullName);
            return SignCertFromRequest(data, ca);
        }

        public FileInfo SignCertFromRequest(Byte[] requestData, Boolean ca)
        {
            FileInfo file = null;

            using(BIO bio = new BIO(requestData))
            using (X509Request request = new X509Request(bio))
            {
                file = new FileInfo(Path.Combine(certDir.FullName, request.Subject.Common + ".cer"));

                using (X509Certificate certificate = RootCA.ProcessRequest(request, DateTime.Now.AddHours(-24), DateTime.Now + TimeSpan.FromDays(365), MessageDigest.SHA1))
                {

                    if (ca)
                    {
                        certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "basicConstraints", true, "CA:true"));
                        certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "keyUsage", true, "critical, cRLSign, keyCertSign, digitalSignature"));
                        certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "certificatePolicies", true, "2.5.29.32.0"));
                    }
                    else
                    {
                        certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "basicConstraints", true, "CA:false"));
                    }

                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "issuerAltName", true, "issuer:copy"));
                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "nsComment", true, "IAM Tester Generated Certificate"));
                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "subjectKeyIdentifier", true, "hash"));
                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "authorityKeyIdentifier", true, "keyid,issuer:always"));
                    certificate.AddExtension(new X509Extension(RootCA.Certificate, certificate, "subjectAltName", true, "DNS:" + request.Subject.Common));

                    certificate.Sign(RootCA.Key, MessageDigest.SHA1);

                    using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.ReadWrite))
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    using (BIO bio2 = BIO.MemoryBuffer())
                    {
                        certificate.Write(bio2);
                        Byte[] certData = bio2.ReadBytes((Int32)bio2.NumberWritten).Array;
                        bw.Write(certData);
                        bw.Close();
                    }

                    //Para atualizar com o tamanho e outros dados do arquivo
                    file = new FileInfo(file.FullName);
                }

            }

            return file;
        }

        private String BuildPKCS12(String password, CryptoKey key, X509Certificate cert)
        {
            String retCert = "";

            //Copia para memória
            using (MemoryStream ms = new MemoryStream())
            {
                using (PKCS12 pfx = new PKCS12(password, key, cert, new OpenSSL.Core.Stack<X509Certificate>()))
                using (BinaryWriter bw = new BinaryWriter(ms))
                using (BIO bio = BIO.MemoryBuffer())
                {
                    pfx.Write(bio);
                    Byte[] certData = bio.ReadBytes((Int32)bio.NumberWritten).Array;
                    bw.Write(certData);
                    bw.Close();
                }

                retCert = Convert.ToBase64String(ms.ToArray());

            }
            
            return retCert;

        }

        private String BuildPKCS12AndSave(String filename, String password, CryptoKey key, X509Certificate cert)
        {
            String retCert = "";

            retCert = BuildPKCS12(password, key, cert);

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
            using (BinaryWriter bw = new BinaryWriter(fs))
            using (BIO bio = BIO.MemoryBuffer())
            {
                Byte[] certData = Convert.FromBase64String(retCert);
                bw.Write(certData);
                bw.Close();
            }

            /*
            using (PKCS12 pfx = new PKCS12(password, key, cert, new OpenSSL.Core.Stack<X509Certificate>()))
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
            using (BinaryWriter bw = new BinaryWriter(fs))
            using (BIO bio = BIO.MemoryBuffer())
            {
                pfx.Write(bio);
                Byte[] certData = bio.ReadBytes((Int32)bio.NumberWritten).Array;
                bw.Write(certData);
                bw.Close();
            }*/


            using (FileStream fs = new FileStream(filename.Replace(".pfx", ".cer"), FileMode.Create, FileAccess.ReadWrite))
            using (BinaryWriter bw = new BinaryWriter(fs))
            using (BIO bio = BIO.MemoryBuffer())
            {
                cert.Write(bio);
                Byte[] certData = bio.ReadBytes((Int32)bio.NumberWritten).Array;
                bw.Write(certData);
                bw.Close();
            }

            return retCert;
        }

        private static X509Name GetCertificateSigningRequestSubject(String Common)
        {
            X509Name requestDetails = new X509Name();

            requestDetails.Common = Common;
            requestDetails.Country = "BR";
            requestDetails.StateOrProvince = "PR";
            requestDetails.Organization = "SafeId - IAM";
            requestDetails.OrganizationUnit = "Production";
            requestDetails.Locality = "PR";

            return requestDetails;
        }

        private static CryptoKey CreateNewRSAKey(int numberOfBits)
        {
            using (var rsa = new RSA())
            {
                BigNumber exponent = 0x10001; // this needs to be a prime number
                rsa.GenerateKeys(numberOfBits, exponent, null, null);

                return new CryptoKey(rsa);
            }
        }

        private void BuildCfg()
        {
            FileInfo cfgFile = new FileInfo(Path.Combine(certDir.FullName, "openssl.cfg"));
            if (!cfgFile.Exists)
            {
                StringBuilder cfgTxt = new StringBuilder();

                cfgTxt.AppendLine(@"# SSLeay example configuration file.");
                cfgTxt.AppendLine(@"# This is mostly being used for generation of certificate requests.");
                cfgTxt.AppendLine(@"#");
                cfgTxt.AppendLine(@"# Original source unknown.");
                cfgTxt.AppendLine(@"# Modified 2003-02-07 by Dylan Beattie (openssl@dylanbeattie.net)");
                cfgTxt.AppendLine(@"# http://www.dylanbeattie.net/docs/openssl_iis_ssl_howto.html");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"RANDFILE		= .rnd");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"####################################################################");
                cfgTxt.AppendLine(@"[ ca ]");
                cfgTxt.AppendLine(@"default_ca	= CA_default		# The default ca section");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"####################################################################");
                cfgTxt.AppendLine(@"[ CA_default ]");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"certs		= certs			# Where the issued certs are kept");
                cfgTxt.AppendLine(@"crl_dir		= crl			# Where the issued crl are kept");
                cfgTxt.AppendLine(@"database	= database.txt		# database index file.");
                cfgTxt.AppendLine(@"new_certs_dir	= certs			# default place for new certs.");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"certificate	= cacert.pem 	   	# The CA certificate");
                cfgTxt.AppendLine(@"serial		= serial.txt 		# The current serial number");
                cfgTxt.AppendLine(@"crl		= crl.pem 		# The current CRL");
                cfgTxt.AppendLine(@"private_key	= private\cakey.pem   	# The private key");
                cfgTxt.AppendLine(@"RANDFILE	= private\private.rnd 	# private random number file");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"x509_extensions	= x509v3_extensions	# The extentions to add to the cert");
                cfgTxt.AppendLine(@"default_days	= 365			# how long to certify for");
                cfgTxt.AppendLine(@"default_crl_days= 30			# how long before next CRL");
                cfgTxt.AppendLine(@"default_md	= md5			# which md to use.");
                cfgTxt.AppendLine(@"preserve	= no			# keep passed DN ordering");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"# A few difference way of specifying how similar the request should look");
                cfgTxt.AppendLine(@"# For type CA, the listed attributes must be the same, and the optional");
                cfgTxt.AppendLine(@"# and supplied fields are just that :-)");
                cfgTxt.AppendLine(@"policy		= policy_match");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"# For the CA policy");
                cfgTxt.AppendLine(@"[ policy_match ]");
                cfgTxt.AppendLine(@"commonName		= supplied");
                cfgTxt.AppendLine(@"emailAddress		= optional");
                cfgTxt.AppendLine(@"countryName		= optional");
                cfgTxt.AppendLine(@"stateOrProvinceName	= optional");
                cfgTxt.AppendLine(@"organizationName	= optional");
                cfgTxt.AppendLine(@"organizationalUnitName	= optional");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"# For the 'anything' policy");
                cfgTxt.AppendLine(@"# At this point in time, you must list all acceptable 'object'");
                cfgTxt.AppendLine(@"# types.");
                cfgTxt.AppendLine(@"[ policy_anything ]");
                cfgTxt.AppendLine(@"commonName		= supplied");
                cfgTxt.AppendLine(@"emailAddress		= optional");
                cfgTxt.AppendLine(@"countryName		= optional");
                cfgTxt.AppendLine(@"stateOrProvinceName	= optional");
                cfgTxt.AppendLine(@"localityName		= optional");
                cfgTxt.AppendLine(@"organizationName	= optional");
                cfgTxt.AppendLine(@"organizationalUnitName	= optional");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"####################################################################");
                cfgTxt.AppendLine(@"[ req ]");
                cfgTxt.AppendLine(@"default_bits		= 1024");
                cfgTxt.AppendLine(@"default_keyfile 	= privkey.pem");
                cfgTxt.AppendLine(@"distinguished_name	= req_distinguished_name");
                cfgTxt.AppendLine(@"attributes		= req_attributes");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"[ req_distinguished_name ]");
                cfgTxt.AppendLine(@"commonName			= Common Name (eg, your website's domain name)");
                cfgTxt.AppendLine(@"commonName_max			= 64");
                cfgTxt.AppendLine(@"emailAddress			= Email Address");
                cfgTxt.AppendLine(@"emailAddress_max		= 40");
                cfgTxt.AppendLine(@"countryName			= Country Name (2 letter code)");
                cfgTxt.AppendLine(@"countryName_min			= 2");
                cfgTxt.AppendLine(@"countryName_max			= 2");
                cfgTxt.AppendLine(@"countryName_default		= BR");
                cfgTxt.AppendLine(@"stateOrProvinceName		= State or Province Name (full name)");
                cfgTxt.AppendLine(@"localityName			= Locality Name (eg, city)");
                cfgTxt.AppendLine(@"0.organizationName		= Organization Name (eg, company)");
                cfgTxt.AppendLine(@"organizationalUnitName		= Organizational Unit Name (eg, section)");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"[ req_attributes ]");
                cfgTxt.AppendLine(@"challengePassword		= A challenge password");
                cfgTxt.AppendLine(@"challengePassword_min		= 4");
                cfgTxt.AppendLine(@"challengePassword_max		= 20");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"[ x509v3_extensions ]");
                cfgTxt.AppendLine(@"# under ASN.1, the 0 bit would be encoded as 80");
                cfgTxt.AppendLine(@"# nsCertType			= 0x40");
                cfgTxt.AppendLine(@"#nsBaseUrl");
                cfgTxt.AppendLine(@"#nsRevocationUrl");
                cfgTxt.AppendLine(@"#nsRenewalUrl");
                cfgTxt.AppendLine(@"#nsCaPolicyUrl");
                cfgTxt.AppendLine(@"#nsSslServerName");
                cfgTxt.AppendLine(@"#nsCertSequence");
                cfgTxt.AppendLine(@"#nsCertExt");
                cfgTxt.AppendLine(@"#nsDataType");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"[ v3_ca ]");
                cfgTxt.AppendLine(@"# Extensions for a typical CA");
                cfgTxt.AppendLine(@"");
                cfgTxt.AppendLine(@"certificatePolicies=2.5.29.32.0");
                cfgTxt.AppendLine(@"subjectKeyIdentifier=hash");
                cfgTxt.AppendLine(@"authorityKeyIdentifier=keyid:always,issuer");
                cfgTxt.AppendLine(@"basicConstraints=critical,CA:TRUE");
                cfgTxt.AppendLine(@"keyUsage = critical,cRLSign, keyCertSign, digitalSignature");


                BinaryWriter writer = new BinaryWriter(File.Open(cfgFile.FullName, FileMode.CreateNew));
                writer.Write(Encoding.UTF8.GetBytes(cfgTxt.ToString()));
                writer.Flush();
                writer.BaseStream.Dispose();
                writer.Close();
                writer = null;

                cfgTxt = null;
            }

        }

        /*
       public void Teste()
       {
           Byte[] hash_pwd = new Byte[] { 0xe1, 0x0a, 0xdc, 0x39, 0x49, 0xba, 0x59, 0xab, 0xbe, 0x56, 0xe0, 0x57, 0xf2, 0x0f, 0x88, 0x3e };
           Byte[] Salt = new Byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

           MessageDigestContext ctx = new MessageDigestContext(MessageDigest.SHA1);
           ctx.Init();
           ctx.Update(hash_pwd);
           ctx.Update(Salt);
           Byte[] tst1 = ctx.DigestFinal();
           String ret = "0x" + BitConverter.ToString(tst1).Replace("-", ", 0x") + Environment.NewLine;

           //ctx.Init();
           ctx.Update(tst1);
           Byte[] tst2 = ctx.DigestFinal();
           String ret2 = "0x" + BitConverter.ToString(tst2).Replace("-", ", 0x") + Environment.NewLine;

           //b7 5f 19 99 d6 c6 ee 86 37 6f f1 43 cf 78 ea 3a 5b cf 49

       }*/


    }
}
