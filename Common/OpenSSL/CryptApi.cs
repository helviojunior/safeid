using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSSL.Core;
using OpenSSL.Crypto;
using OpenSSL.SSL;
using OpenSSL.X509;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security;

namespace IAM.CA
{
    public class CryptApi: IDisposable
    {
        public Byte[] clearData { get; internal set; }
        public Byte[] encryptedData { get; internal set; }
        public String encryptedKey { get; internal set; }
        public String clearkey { get; internal set; }
        public String clearDataHash { get; internal set; }

        public CryptApi(X509Certificate cert, Byte[] clearData)
        {
            clearkey = CATools.GetCypherKey();
            encryptedKey = Convert.ToBase64String(CATools.AsymmetricEncrypt(cert, Encoding.UTF8.GetBytes(clearkey)));

            clearDataHash = CATools.SHA1Checksum(clearData);

            encryptedData = CATools.Encrypt(clearData, clearkey);  
        }

        public CryptApi(X509Certificate cert, Byte[] encryptedData, String encryptedKey)
        {
            this.encryptedKey = encryptedKey;
            this.encryptedData = encryptedData;

            Byte[] k = Convert.FromBase64String(encryptedKey);

            if ((k == null) || (k.Length <= 0))
                throw new Exception("Encrypted key is empty");

            clearkey = Encoding.UTF8.GetString(CATools.AsymmetricDencrypt(cert, k));

            clearData = CATools.Dencrypt(encryptedData, clearkey);
            clearDataHash = CATools.SHA1Checksum(clearData);
        }

        public Byte[] ToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Byte[] tmp = Convert.FromBase64String(encryptedKey);
                Byte[] tmp1 = BitConverter.GetBytes((Int32)tmp.Length);
                ms.Write(tmp1, 0, tmp1.Length);
                ms.Write(tmp, 0, tmp.Length);

                tmp = Encoding.UTF8.GetBytes(clearDataHash);
                tmp1 = BitConverter.GetBytes((Int32)tmp.Length);
                ms.Write(tmp1, 0, tmp1.Length);
                ms.Write(tmp, 0, tmp.Length);

                tmp1 = BitConverter.GetBytes((Int32)encryptedData.Length);
                ms.Write(tmp1, 0, tmp1.Length);
                ms.Write(encryptedData, 0, encryptedData.Length);

                ms.Flush();
                return ms.ToArray();
            }
        }

        public void Dispose()
        {
            encryptedData = clearData = new Byte[0];
            encryptedKey = clearkey = null;
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public static CryptApi ParsePackage(X509Certificate cert, Byte[] package)
        {
            if (cert == null)
                throw new Exception("Certificate is empty");

            if ((package == null) || (package.Length < 10))
                throw new Exception("Package is empty");

            using (MemoryStream ms = new MemoryStream(package))
            using (BinaryReader r = new BinaryReader(ms))
            {
                Int32 len = r.ReadInt32();
                String encryptedKey = Convert.ToBase64String(r.ReadBytes(len));

                if (String.IsNullOrEmpty(encryptedKey))
                    throw new Exception("Encrypted key is empty");


                len = r.ReadInt32();
                String clearDataHash = Encoding.UTF8.GetString(r.ReadBytes(len));

                if (String.IsNullOrEmpty(clearDataHash))
                    throw new Exception("Clear-text hash is empty");

                len = r.ReadInt32();
                Byte[] encryptedData = r.ReadBytes(len);

                if ((encryptedData == null) || (encryptedData.Length <= 0))
                    throw new Exception("Encrypted data hash is empty");

                CryptApi api = new CryptApi(cert, encryptedData, encryptedKey);

                if (api.clearDataHash != clearDataHash)
                    throw new Exception("Invalid decrypted data chechsum");

                return api;
            }

        }

    }
}
