using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;

namespace IAM.Config
{
    [Serializable()]
    public class IAMConfig
    {
        private string sqlServer;
        private string sqlUsername;
        private string sqlDbName;
        private string sqlPassword;

        [NonSerialized()]
        private string basePath;

        public String SQLServer { get { return sqlServer; } set { sqlServer = value; } }
        public String SQLUsername { get { return sqlUsername; } set { sqlUsername = value; } }
        public String SQLDbName { get { return sqlDbName; } set { sqlDbName = value; } }
        public String SQLPassword { get { return Decrypt(sqlPassword); } set { sqlPassword = Encrypt(value); } }

        public IAMConfig()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            this.basePath = Path.GetDirectoryName(asm.Location);
        }

        public IAMConfig(String basePath)
        {
            this.basePath = basePath;
        }

        public void SaveConfig()
        {
            SaveConfig(Path.Combine(this.basePath, "IAMConfig.cfg"));
        }

        public void SaveConfig(String filename)
        {
            FileInfo file = new FileInfo(filename);
            if (!file.Directory.Exists)
                file.Directory.Create();
            file = null;

            IFormatter formato = new SoapFormatter();
            Byte[] returnBytes = new Byte[0];
            MemoryStream stream = new MemoryStream();
            formato.Serialize(stream, this);

            BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create));
            writer.Write(stream.ToArray());
            writer.Flush();
            writer.BaseStream.Dispose();
            writer.Close();
            writer = null;

            stream.Dispose();
            stream.Close();
            stream = null;

        }


        public Boolean ConfigExists()
        {
            return File.Exists(Path.Combine(this.basePath, "IAMConfig.cfg"));
        }

        public void LoadConfig()
        {
            LoadConfig(Path.Combine(this.basePath, "IAMConfig.cfg"));
        }

        public void LoadConfig(String filename)
        {
            IFormatter formato = new SoapFormatter();

            MemoryStream file = null;

            Int32 cnt = 0;

            while ((cnt < 5) && (file == null))
            {
                try
                {
                    file = new MemoryStream(File.ReadAllBytes(filename));
                    //file = File.Open(filename, FileMode.Open, FileAccess.Read);
                }
                catch (Exception ex)
                {
                    cnt++;
                    if (cnt == 5)
                        throw ex;
                }
            }

            LoadConfig(file);

        }

        public void LoadConfig(Byte[] rawData)
        {
            LoadConfig(new MemoryStream(rawData));
        }

        public void LoadConfig(MemoryStream rawData)
        {
            IFormatter formato = new SoapFormatter();

            IAMConfig item = (IAMConfig)formato.Deserialize(rawData);
            rawData.Dispose();

            rawData.Close();
            rawData = null;

            this.sqlServer = item.sqlServer;
            this.sqlUsername = item.sqlUsername;
            this.sqlDbName = item.sqlDbName;
            this.sqlPassword = item.sqlPassword;

            //Realiza o teste de decriptografia da senha
            //caso não tenha sucesso indica que a senha está em clear text então criptografa e salva
            try
            {
                Decrypt(this.sqlPassword);
            }
            catch {
                this.sqlPassword = Encrypt(this.sqlPassword);
                SaveConfig();
            }

        }

        public void BuildStartConfig()
        {
            this.SQLServer = "127.0.0.1";
            this.SQLDbName = "IAM";
            this.SQLUsername = "Sql-Username";
            this.SQLPassword = "";

            this.SaveConfig();
        }


        static public String Encrypt(String clearText)
        {
            if ((clearText == null) || (clearText == ""))
            {
                return "";
            }
            else
            {

                return Convert.ToBase64String(EncryptStringToBytes(clearText));
            }
        }

        static public String Decrypt(String chyperText)
        {
            if ((chyperText == null) || (chyperText == ""))
            {
                return "";
            }
            else
            {

                return DecryptStringFromBytes(Convert.FromBase64String(chyperText));
            }
        }

        static byte[] EncryptStringToBytes(String plainText)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");

            byte[] encrypted;
            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.BlockSize = 128;
                rijAlg.KeySize = 256;
                rijAlg.Key = Encoding.UTF8.GetBytes("?c:4X?STxVFN=^Kts;V^Abd@1tuv==Od");
                rijAlg.IV = Encoding.UTF8.GetBytes("p!s^.J:N&KDn*lOv");

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        static String DecryptStringFromBytes(byte[] cipherText)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");


            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.BlockSize = 128;
                rijAlg.KeySize = 256;
                rijAlg.Key = Encoding.UTF8.GetBytes("?c:4X?STxVFN=^Kts;V^Abd@1tuv==Od");
                rijAlg.IV = Encoding.UTF8.GetBytes("p!s^.J:N&KDn*lOv");

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }

    }
}
