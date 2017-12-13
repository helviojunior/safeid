using System;
using System.IO;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Configuration;
using System.Text;

namespace IAM.Config
{
    public class ConfigCryptography
    {
        static Configuration config = null;

        static public String GetAppSettings(String key)
        {
            if (config == null)
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            try
            {
                String tst = Encoding.UTF8.GetString(Convert.FromBase64String(ConfigurationSettings.AppSettings[key]));

                return DecryptStringFromBytes(Convert.FromBase64String(ConfigurationSettings.AppSettings[key]));
            }
            catch
            {
                try
                {
                    return config.AppSettings.Settings[key].Value;
                }
                catch {
                    return "";
                }
            }
        }

        static public void SetAppSettings(String key, String text)
        {
            SetAppSettings(key, text, true);
        }

        static public void SetAppSettings(String key, String text, Boolean useCrypt)
        {
            if (config == null)
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			
            if (text == "")
            {
            	try {
            		if (config.AppSettings.Settings[key] == null)
            		config.AppSettings.Settings.Add(key, "");
            	} catch (Exception) {
            		
            	}
            	
                config.AppSettings.Settings[key].Value = "";
            }
            else
            {

                if (useCrypt)
                {
                    config.AppSettings.Settings[key].Value = Convert.ToBase64String(EncryptStringToBytes(text));
                }
                else
                {
                    config.AppSettings.Settings[key].Value = text;
                }
            }
        }

        static public void RefreshConfig()
        {
            // Force a reload of a changed section.
            ConfigurationManager.RefreshSection("appSettings");
        }

        static public void SaveConfig()
        {
            // Save the configuration file.
            config.Save(ConfigurationSaveMode.Minimal, true);
            // Force a reload of a changed section.
            ConfigurationManager.RefreshSection("appSettings");
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
                rijAlg.Key = Encoding.UTF8.GetBytes("OZ:aaXbXkj!P.bUQ=n%&;T+R+xBM`/Mg");
                rijAlg.IV = Encoding.UTF8.GetBytes("kdJBCX0k%tNzfdJj");

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
                rijAlg.Key = Encoding.UTF8.GetBytes("OZ:aaXbXkj!P.bUQ=n%&;T+R+xBM`/Mg");
                rijAlg.IV = Encoding.UTF8.GetBytes("kdJBCX0k%tNzfdJj");

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
