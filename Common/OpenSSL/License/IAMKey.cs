using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using OpenSSL.X509;
using IAM.CA;

namespace IAM.License
{
    public enum IAMVersion
    {
        v100 = 100
    }

    public class IAMKeyData
    {
        public Boolean IsServerKey;
        public UInt32 NumLic;
        public Boolean IsTemp;
        public String InstallKey;
        public DateTime? TempDate;
    }

    public static class IAMKey
    {

        public static X509Certificate GenerateLicense(CertificateAuthority ca, Uri installCode, Boolean isServerKey, UInt32 numLic, Boolean isTemp, DateTime? tempDate)
        {
            String installKey = null;
            Uri license = null;
            System.Reflection.Assembly asm = null;
            FileInfo p12File = null;
            try
            {
                String[] iParts = installCode.AbsolutePath.Trim("/".ToCharArray()).Split("/".ToCharArray());

                IAMVersion version = IAMVersion.v100;
                switch (iParts[0].ToLower())
                {
                    case "v1":
                    case "v100":
                        version = IAMVersion.v100;
                        break;

                    default:
                        throw new Exception("Install code version unrecognized");
                        break;
                }

                installKey = String.Join("/", iParts, 1, iParts.Length - 1);

                //Em caso de licença com data de expiração, adiciona 20 horas no tempo para evitar problemas com fuso
                tempDate += TimeSpan.FromHours(20);

                license = new Uri("license://safeid/" + version.ToString() + "/" + GeraKey(installKey, isServerKey, numLic, isTemp, tempDate, version));

                try
                {
                    CertificateAuthority.subjectAltName alt = new CertificateAuthority.subjectAltName();
                    alt.Uri.Add(installCode);
                    alt.Uri.Add(license);

                    String pkcs12Cert = ca.SignCert("SafeID IAM License", false, alt, false, (isTemp && tempDate.HasValue ? tempDate.Value : DateTime.Now + TimeSpan.FromDays(36500)));

                    return CATools.GetX509CertFromPKCS12(Convert.FromBase64String(pkcs12Cert), ca.SignedPassword);

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
            finally
            {
                installKey = null;
            }
        }

        public static IAMKeyData ExtractFromCert(String base64CertData)
        {
            X509Certificate cert = null;
            Uri installCode = null;
            Uri license = null;
            String key = null;
            String installKey = null;
            try
            {
                try
                {
                    cert = CATools.LoadCert(Convert.FromBase64String(base64CertData));
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro on load certificate: " + ex.Message);
                }

                installCode = GetDataCode(cert, "installkey");

                if (installCode == null)
                    throw new Exception("Install code not found in certificate");

                license = GetDataCode(cert, "license");

                if (license == null)
                    throw new Exception("License not found in certificate");

                String[] parts = license.AbsolutePath.Trim("/".ToCharArray()).Split("/".ToCharArray());
                String[] iParts = installCode.AbsolutePath.Trim("/".ToCharArray()).Split("/".ToCharArray());

                IAMVersion version = IAMVersion.v100;
                switch (parts[0].ToLower())
                {
                    case "v100":
                        version = IAMVersion.v100;
                        break;

                    default:
                        throw new Exception("License version unrecognized");
                        break;
                }


                key = String.Join("/", parts, 1, parts.Length - 1);
                installKey = String.Join("/", iParts, 1, iParts.Length - 1);

                return CheckKey(installKey, version, key);
            }
            finally
            {
                cert = null;
                installCode = null;
                license = null;
                key = null;
                installKey = null;
            }
        }


        private static Uri GetDataCode(X509Certificate cert, String schema)
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
                        if (tmp.Scheme.ToLower() == schema.ToLower())
                        {
                            ic = tmp;
                            break;
                        }
                    }
                }
            }

            return ic;
        }

        private static IAMKeyData CheckKey(String installKey, IAMVersion version, String sKey)
        {
            IAMKeyData kData = new IAMKeyData();
            byte[] key = new byte[0];

            kData.InstallKey = "installkey://safeid/" + version.ToString() + "/" + installKey;

            key = StringToByteArray(sKey.Replace("-", "").Replace("/", "").Replace("\\", ""));

            kData.NumLic = (UInt32)((key[4] << 8) | key[6]);
            UInt32 totalSeconds = (UInt32)((key[2] << 24) | (key[12] << 16) | (key[9] << 8) | (key[7]));
            kData.IsServerKey = (key[3] == 1);

            if (totalSeconds > 0)
            {
                kData.IsTemp = true;
                kData.TempDate = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(totalSeconds);
            }
            else
            {
                kData.IsTemp = false;
            }

            String cKey = GeraKey(installKey, kData.IsServerKey, kData.NumLic, kData.IsTemp, kData.TempDate, version);

            if (cKey.ToUpper().Replace("-", "").Replace("/", "").Replace("\\", "") != sKey.ToUpper().Replace("-", "").Replace("/", "").Replace("\\", ""))
                throw new Exception("Invalid key");

            return kData;
        }


        private static string GeraKey(String installKey, Boolean isServerKey, UInt32 numLic, Boolean isTemp, DateTime? tempDate, IAMVersion version)
        {


            byte[] buffer = new byte[0];
            uint totalSeconds = 0;

            if (isTemp)
            {
                DateTime? nullable = tempDate;
                DateTime time = new DateTime(0x7b2, 1, 1, 0, 0, 0);
                TimeSpan? nullable3 = nullable.HasValue ? new TimeSpan?(nullable.GetValueOrDefault() - time) : null;
                totalSeconds = (uint) nullable3.Value.TotalSeconds;
                //totalSeconds += 0x1517f;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                if (version == IAMVersion.v100)
                {
                    stream.Write(secret1_v100, 0, secret1_v100.Length);
                }

                byte[] bytes = Encoding.ASCII.GetBytes(installKey.ToLower().Replace("-", "").Replace("/", "").Replace("\\", ""));
                stream.Write(bytes, 0, bytes.Length);
                ushort num2 = (ushort) (numLic ^ 0x33bb);
                bytes = BitConverter.GetBytes(num2);
                stream.Write(bytes, 0, bytes.Length);
                uint num3 = 0;
                num3 = totalSeconds ^ 0xffbb0033;
                bytes = BitConverter.GetBytes(num3);
                stream.Write(bytes, 0, bytes.Length);

                if (version == IAMVersion.v100)
                {
                    stream.Write(secret2_v100, 0, secret2_v100.Length);
                }
                
                num2 = (ushort) (numLic ^ 0xbb33);
                bytes = BitConverter.GetBytes(num2);
                stream.Write(bytes, 0, bytes.Length);
                num3 = totalSeconds ^ 0x33ffbb;
                bytes = BitConverter.GetBytes(num3);
                stream.Write(bytes, 0, bytes.Length);
                bytes = Encoding.ASCII.GetBytes(installKey.Replace("-", "").Replace("/", "").Replace("\\", ""));
                stream.Write(bytes, 0, bytes.Length);

                if (version == IAMVersion.v100)
                {
                    stream.Write(secret3_v100, 0, secret3_v100.Length);
                }

                stream.Flush();
                buffer = stream.ToArray();
            }

            byte[] buffer3 = new SHA1CryptoServiceProvider().ComputeHash(buffer);
            buffer3[3] = ToByte((uint)((isServerKey ? 1 : 0) & 0xff));
            buffer3[4] = ToByte((numLic >> 8) & 0xff);
            buffer3[6] = ToByte(numLic & 0xff);
            buffer3[2] = ToByte((totalSeconds >> 0x18) & 0xff);
            buffer3[12] = ToByte((totalSeconds >> 0x10) & 0xff);
            buffer3[9] = ToByte((totalSeconds >> 8) & 0xff);
            buffer3[7] = ToByte(totalSeconds & 0xff);
            

            String key = BitConverter.ToString(buffer3).Replace("-","");

            string str5 = "";
            for (int j = 0; j < key.Length; j++)
            {
                if ((j > 0) && ((j % 8) == 0))
                {
                    str5 = str5 + '/';
                }
                str5 = str5 + key[j];
            }
            key = str5;

            return key;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private static byte ToByte(uint t)
        {
            return BitConverter.GetBytes(t)[0];
        }

         private static byte[] secret1_v100 = new byte[] { 
            0x91, 0x72, 0xC2, 0xE2, 0x79, 0x70, 0x38, 0xA6, 0x13, 0x8E, 0x73, 0xA3, 0x63, 0xA3, 0xD1, 
            0xED, 0xAE, 0x37, 0xD6, 0x11, 0x44, 0xD4, 0x6A, 0xD7, 0xD9, 0x17, 0x05, 0x57, 0x96, 0xD5, 
            0xF8, 0x3D, 0x73, 0x9F, 0x5E, 0x6E, 0x2D, 0x60, 0x7F, 0xEB, 0x14, 0x51, 0xB7, 0xE4, 0x11, 
            0x3E, 0x69, 0x44, 0x69, 0xA3, 0xEE, 0xF8, 0x59, 0xCF, 0x41, 0xBC, 0x08, 0xEB, 0x09, 0x61, 
            0x6B, 0xE0, 0x10, 0x3D, 0x96, 0x35, 0x2F, 0xC4, 0x45, 0x63, 0xBF, 0x4E, 0xB8, 0xEB, 0xFD, 
            0xF3, 0x1D, 0x85, 0xC6, 0x9A, 0xAE, 0xC1, 0xEE, 0xF3, 0xE6, 0x00, 0xE3, 0xA4, 0x5D, 0xA2, 
            0x65, 0x41, 0x56, 0x1E, 0x80, 0x33, 0x41, 0x79, 0x4E, 0xDB, 0x0F, 0xA4, 0xFF, 0x06, 0xE3, 
            0xA0, 0x40, 0x6E, 0xD1, 0x4E, 0x9E, 0x82, 0x25, 0x6F, 0xB2, 0xAA, 0xF2, 0x1D, 0x57, 0x95, 
            0x52, 0x8A, 0x67, 0xA2, 0xFD, 0x7E, 0xF7, 0x9A, 0x6A, 0xC9, 0xB2, 0xA4, 0x36, 0xEA, 0x8A, 
            0x0A, 0xC1, 0xE8, 0x0F, 0x46, 0xBF, 0x75, 0xD2, 0x0E, 0x03, 0xE2, 0x1C, 0xE7, 0x6B, 0xD5, 
            0x41, 0x23, 0x22, 0xB8, 0x89, 0x84, 0x3C, 0x5C, 0x08, 0x65, 0xA9, 0xA6, 0x03, 0x07, 0x9C, 
            0xFA, 0x4C, 0x3A, 0xE4, 0xA8, 0xE8, 0x09, 0x0D, 0x10, 0xD6, 0xDC, 0xB8, 0x59, 0x9E, 0x1B
         };

        private static byte[] secret2_v100 = new byte[] { 
            0x91, 0x72, 0xC2, 0xE2, 0x79, 0x70, 0x38, 0xA6, 0x13, 0x8E, 0x73, 0xA3, 0x63, 0xA3, 0xD1, 
            0xED, 0xAE, 0x37, 0xD6, 0x11, 0x44, 0xD4, 0x6A, 0xD7, 0xD9, 0x17, 0x05, 0x57, 0x96, 0xD5, 
            0xF8, 0x3D, 0x73, 0x9F, 0x5E, 0x6E, 0x2D, 0x60, 0x7F, 0xEB, 0x14, 0x51, 0xB7, 0xE4, 0x11, 
            0x3E, 0x69, 0x44, 0x69, 0xA3, 0xEE, 0xF8, 0x59, 0xCF, 0x41, 0xBC, 0x08, 0xEB, 0x09, 0x61, 
            0x6B, 0xE0, 0x10, 0x3D, 0x96, 0x35, 0x2F, 0xC4, 0x45, 0x63, 0xBF, 0x4E, 0xB8, 0xEB, 0xFD, 
            0xF3, 0x1D, 0x85, 0xC6, 0x9A, 0xAE, 0xC1, 0xEE, 0xF3, 0xE6, 0x00, 0xE3, 0xA4, 0x5D, 0xA2, 
            0x65, 0x41, 0x56, 0x1E, 0x80, 0x33, 0x41, 0x79, 0x4E, 0xDB, 0x0F, 0xA4, 0xFF, 0x06, 0xE3, 
            0xA0, 0x40, 0x6E, 0xD1, 0x4E, 0x9E, 0x82, 0x25, 0x6F, 0xB2, 0xAA, 0xF2, 0x1D, 0x57, 0x95, 
            0x52, 0x8A, 0x67, 0xA2, 0xFD, 0x7E, 0xF7, 0x9A, 0x6A, 0xC9, 0xB2, 0xA4, 0x36, 0xEA, 0x8A, 
            0x0A, 0xC1, 0xE8, 0x0F, 0x46, 0xBF, 0x75, 0xD2, 0x0E, 0x03, 0xE2, 0x1C, 0xE7, 0x6B, 0xD5, 
            0x41, 0x23, 0x22, 0xB8, 0x89, 0x84, 0x3C, 0x5C, 0x08, 0x65, 0xA9, 0xA6, 0x03, 0x07, 0x9C, 
            0xFA, 0x4C, 0x3A, 0xE4, 0xA8, 0xE8, 0x09, 0x0D, 0x10, 0xD6, 0xDC, 0xB8, 0x59, 0x9E, 0x1B
         };

        private static byte[] secret3_v100 = new byte[] { 
            0x91, 0x72, 0xC2, 0xE2, 0x79, 0x70, 0x38, 0xA6, 0x13, 0x8E, 0x73, 0xA3, 0x63, 0xA3, 0xD1, 
            0xED, 0xAE, 0x37, 0xD6, 0x11, 0x44, 0xD4, 0x6A, 0xD7, 0xD9, 0x17, 0x05, 0x57, 0x96, 0xD5, 
            0xF8, 0x3D, 0x73, 0x9F, 0x5E, 0x6E, 0x2D, 0x60, 0x7F, 0xEB, 0x14, 0x51, 0xB7, 0xE4, 0x11, 
            0x3E, 0x69, 0x44, 0x69, 0xA3, 0xEE, 0xF8, 0x59, 0xCF, 0x41, 0xBC, 0x08, 0xEB, 0x09, 0x61, 
            0x6B, 0xE0, 0x10, 0x3D, 0x96, 0x35, 0x2F, 0xC4, 0x45, 0x63, 0xBF, 0x4E, 0xB8, 0xEB, 0xFD, 
            0xF3, 0x1D, 0x85, 0xC6, 0x9A, 0xAE, 0xC1, 0xEE, 0xF3, 0xE6, 0x00, 0xE3, 0xA4, 0x5D, 0xA2, 
            0x65, 0x41, 0x56, 0x1E, 0x80, 0x33, 0x41, 0x79, 0x4E, 0xDB, 0x0F, 0xA4, 0xFF, 0x06, 0xE3, 
            0xA0, 0x40, 0x6E, 0xD1, 0x4E, 0x9E, 0x82, 0x25, 0x6F, 0xB2, 0xAA, 0xF2, 0x1D, 0x57, 0x95, 
            0x52, 0x8A, 0x67, 0xA2, 0xFD, 0x7E, 0xF7, 0x9A, 0x6A, 0xC9, 0xB2, 0xA4, 0x36, 0xEA, 0x8A, 
            0x0A, 0xC1, 0xE8, 0x0F, 0x46, 0xBF, 0x75, 0xD2, 0x0E, 0x03, 0xE2, 0x1C, 0xE7, 0x6B, 0xD5,
            0x41, 0x23, 0x22, 0xB8, 0x89, 0x84, 0x3C, 0x5C, 0x08, 0x65, 0xA9, 0xA6, 0x03, 0x07, 0x9C,
            0xFA, 0x4C, 0x3A, 0xE4, 0xA8, 0xE8, 0x09, 0x0D, 0x10, 0xD6, 0xDC, 0xB8, 0x59, 0x9E, 0x1B
         };

    }
}

