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
    public class CATools
    {

        public static Byte[] X509ToByte(X509Certificate cert)
        {
            Byte[] ret = null;

            using (BIO bio2 = BIO.MemoryBuffer())
            {
                cert.Write(bio2);
                ret = bio2.ReadBytes((Int32)bio2.NumberWritten).Array;
            }

            return ret;
        }


        public static String X509ToBase64(X509Certificate cert)
        {
            Byte[] data = X509ToByte(cert);

            return Convert.ToBase64String(data);
        }


        public static X509Certificate CloneCertificate(X509Certificate cert)
        {
            BIO bio = BIO.MemoryBuffer();
            cert.Write(bio);

            return new X509Certificate(bio);
        }

        public static X509Certificate LoadCert(String X509Filename)
        {
            FileInfo certFile = new FileInfo(X509Filename);

            if (certFile.Exists)
            {

                Byte[] bX509 = File.ReadAllBytes(certFile.FullName);

                BIO x509BIO = BIO.MemoryBuffer();
                x509BIO.Write(bX509);

                X509Certificate cert = X509Certificate.FromDER(x509BIO);

                return cert;
            }
            else
            {
                return null;
            }
        }

        public static X509Certificate LoadCert(Byte[] X509Data)
        {
            BIO x509BIO = BIO.MemoryBuffer();
            x509BIO.Write(X509Data);

            X509Certificate cert = new X509Certificate(x509BIO);

            return cert;
        }

        public static X509Certificate LoadCert(String PKCS12Filename, String password)
        {
            FileInfo caPkcs12 = new FileInfo(PKCS12Filename);

            if (caPkcs12.Exists)
            {

                Byte[] bPKCS12 = File.ReadAllBytes(caPkcs12.FullName);

                return LoadCert(bPKCS12, password);

            }
            else
            {
                return null;
            }
        }

        public static X509Certificate LoadCert(Byte[] PKCS12Data, String password)
        {

            BIO pkcs12BIO = BIO.MemoryBuffer();
            pkcs12BIO.Write(PKCS12Data);

            X509Certificate cert = X509Certificate.FromPKCS12(pkcs12BIO, password);

            return cert;

        }

        public static X509Certificate GetX509CertFromPKCS12(Byte[] PKCS12Data, String password)
        {
            BIO pkcs12BIO = BIO.MemoryBuffer();
            pkcs12BIO.Write(PKCS12Data);

            X509Certificate cert1 = X509Certificate.FromPKCS12(pkcs12BIO, password);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                using (BIO bio = BIO.MemoryBuffer())
                {
                    cert1.Write(bio);
                    Byte[] certData = bio.ReadBytes((Int32)bio.NumberWritten).Array;
                    bw.Write(certData);
                    bw.Close();
                }


                BIO certBio = BIO.MemoryBuffer();
                certBio.Write(ms.ToArray());

                return new X509Certificate(certBio);
            }

        }

        public static Boolean CheckSignedCertificate(X509Certificate caCert, X509Certificate signedCert, out String error)
        {
            Boolean chk = false;

            X509Store st = new X509Store();
            st.AddTrusted(caCert);

            String err = "";
            chk = st.Verify(signedCert, out err);

            error = err;

            if (!chk)
            {
                error += Environment.NewLine + "CA Cert: " + caCert.ToString();
                error += Environment.NewLine + "Signed Cert: " + signedCert.ToString();
            }

            return chk;
        }

        public static Byte[] Encrypt(Byte[] clearData, String key)
        {
            Byte[] iv = new Byte[16];
            Byte[] k = new Byte[32];
            Byte[] tmp = Convert.FromBase64String(key);

            Array.Copy(tmp, 0, iv, 0, 16);
            Array.Copy(tmp, 16, k, 0, 32);

            System.Security.Cryptography.RijndaelManaged aesEncryption = new System.Security.Cryptography.RijndaelManaged();
            aesEncryption.KeySize = 256;
            aesEncryption.BlockSize = 128;
            aesEncryption.Mode = System.Security.Cryptography.CipherMode.CBC;
            aesEncryption.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            aesEncryption.IV = iv;
            aesEncryption.Key = k;

            System.Security.Cryptography.ICryptoTransform crypto = aesEncryption.CreateEncryptor();
            return crypto.TransformFinalBlock(clearData, 0, clearData.Length);
        }

        public static Byte[] Dencrypt(Byte[] encryptedData, String key)
        {
            Byte[] iv = new Byte[16];
            Byte[] k = new Byte[32];
            Byte[] tmp = Convert.FromBase64String(key);

            Array.Copy(tmp, 0, iv, 0, 16);
            Array.Copy(tmp, 16, k, 0, 32);

            System.Security.Cryptography.RijndaelManaged aesEncryption = new System.Security.Cryptography.RijndaelManaged();
            aesEncryption.KeySize = 256;
            aesEncryption.BlockSize = 128;
            aesEncryption.Mode = System.Security.Cryptography.CipherMode.CBC;
            aesEncryption.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            aesEncryption.IV = iv;
            aesEncryption.Key = k;

            System.Security.Cryptography.ICryptoTransform decrypto = aesEncryption.CreateDecryptor();
            return decrypto.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        public static Byte[] AsymmetricEncrypt(X509Certificate cert, Byte[] clearData)
        {
            CryptoKey d = cert.PublicKey;
            RSA rsa = d.GetRSA();
            byte[] result = rsa.PublicEncrypt(clearData, RSA.Padding.PKCS1);
            rsa.Dispose();
            return result;
        }

        public static Byte[] AsymmetricDencrypt(X509Certificate cert, Byte[] chypherData)
        {
            if (!cert.HasPrivateKey)
                throw new Exception("Private key not found in " + cert.Subject.Common);

            CryptoKey d = cert.PrivateKey;
            RSA rsa = d.GetRSA();
            byte[] result = rsa.PrivateDecrypt(chypherData, RSA.Padding.PKCS1);
            rsa.Dispose();
            return result;
        }


        public static Boolean SHA1CheckHash(Byte[] data, String hash)
        {
            if (String.IsNullOrEmpty(hash))
                return false;

            System.Security.Cryptography.SHA1Managed sha1 = new System.Security.Cryptography.SHA1Managed();
            String cHash = BitConverter.ToString(sha1.ComputeHash(data)).Replace("-", "");

            return (hash.Replace("-", "").ToLower() == cHash.ToLower());
        }


        public static String MD5Checksum(Byte[] data)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "");
        }

        public static String SHA1Checksum(Byte[] data)
        {
            System.Security.Cryptography.SHA1Managed sha1 = new System.Security.Cryptography.SHA1Managed();
            return BitConverter.ToString(sha1.ComputeHash(data)).Replace("-", "");
        }

        public static String SHA256Checksum(Byte[] data)
        {
            System.Security.Cryptography.SHA256Managed sha = new System.Security.Cryptography.SHA256Managed();
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
        }

        public static String GetCypherKey()
        {
            System.Security.Cryptography.RijndaelManaged aesEncryption = new System.Security.Cryptography.RijndaelManaged();
            aesEncryption.KeySize = 256;
            aesEncryption.BlockSize = 128;
            aesEncryption.Mode = System.Security.Cryptography.CipherMode.CBC;
            aesEncryption.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            aesEncryption.GenerateIV();
            Byte[] iv = aesEncryption.IV;
            aesEncryption.GenerateKey();
            Byte[] k = aesEncryption.Key;

            Byte[] key = new Byte[16 + 32];
            Array.Copy(iv, 0, key, 0, 16);
            Array.Copy(k, 0, key, 16, 32);

            return Convert.ToBase64String(key);
        }

    }
}
