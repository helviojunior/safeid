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
            clearkey = Encoding.UTF8.GetString(CATools.AsymmetricDencrypt(cert, Convert.FromBase64String(encryptedKey)));

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

        public static CryptApi ParsePackage(X509Certificate cert, Byte[] package)
        {
            using (MemoryStream ms = new MemoryStream(package))
            using (BinaryReader r = new BinaryReader(ms))
            {
                Int32 len = r.ReadInt32();
                String encryptedKey = Convert.ToBase64String(r.ReadBytes(len));

                len = r.ReadInt32();
                String clearDataHash = Encoding.UTF8.GetString(r.ReadBytes(len));

                len = r.ReadInt32();
                Byte[] encryptedData = r.ReadBytes(len);


                CryptApi api = new CryptApi(cert, encryptedData, encryptedKey);

                if (api.clearDataHash != clearDataHash)
                    throw new Exception("Invalid decrypted data chechsum");

                return api;
            }

        }

    }
}
