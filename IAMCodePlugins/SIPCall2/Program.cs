using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using libphonenumber;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using SafeTrend.SIP;
using SafeTrend.SIP.Message;
using SafeTrend.SIP.Media;

namespace SIPCall2
{
    class Program
    {
        static void Main(string[] args)
        {

            throw new NotImplementedException("");

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            DirectoryInfo tmpPath = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "audio"));

            if (!tmpPath.Exists)
            {
                tmpPath.Create();

                //Descompacta os zips em uma estrutura temporária
                //DecompressData(Resource1.audios, tmpPath);



            }

            //Carrega arquivo de config
            if (args.Length == 0)
                return;

            if (!File.Exists(args[0]))
                return;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(args[0], Encoding.UTF8));

            XmlNode configNode = doc.SelectSingleNode("/call/config");
            XmlNode commandsNode = doc.SelectSingleNode("/call/commands");
            if ((configNode == null) || (commandsNode == null))
                return;

            String target = "";
            String server = "";
            String username = "";
            String password = "";

            foreach (XmlNode n in doc.SelectNodes("/call/config/item"))
            {
                XmlNode key = n.SelectSingleNode("key");
                XmlNode value = n.SelectSingleNode("value");

                switch (key.InnerText.ToLower())
                {
                    case "target":
                        target = value.InnerText;
                        break;

                    case "host":
                        server = value.InnerText;
                        break;

                    case "username":
                        username = value.InnerText;
                        break;

                    case "password":
                        password = value.InnerText;
                        break;
                }
            }

            SIP_Debug.DebugLevel = 5;

            List<String> cmds = new List<string>();
            foreach (XmlNode n in commandsNode.ChildNodes)
                cmds.Add(n.InnerText);

            SIP_Stack stack = new SIP_Stack(server, 5060, username, password);

            try
            {
                using (SIP_Dial dial = new SIP_Dial(ref stack, target))
                {
                    dial.Dial(true, cmds);
                    dial.WaitEnd();
                    //dial.DialOK
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        static public void DecompressData(Byte[] data, DirectoryInfo tmpDir)
        {

            if (!tmpDir.Exists)
                tmpDir.Create();

            MemoryStream stream = new MemoryStream(data);

            using (ZipInputStream inputStream = new ZipInputStream(stream))
            {
                ZipEntry theEntry;

                while ((theEntry = inputStream.GetNextEntry()) != null)
                {
                    if (theEntry.IsFile)
                    {
                        ExtractFile(inputStream, theEntry, tmpDir.FullName);
                    }
                    else if (theEntry.IsDirectory)
                    {
                        ExtractDirectory(inputStream, theEntry, tmpDir.FullName);
                    }
                }
            }
        }


        static private bool ExtractFile(ZipInputStream inputStream, ZipEntry theEntry, string targetDir)
        {
            // try and sort out the correct place to save this entry
            string entryFileName;

            if (Path.IsPathRooted(theEntry.Name))
            {
                string workName = Path.GetPathRoot(theEntry.Name);
                workName = theEntry.Name.Substring(workName.Length);
                entryFileName = Path.Combine(Path.GetDirectoryName(workName), Path.GetFileName(theEntry.Name));
            }
            else
            {
                entryFileName = theEntry.Name;
            }

            string targetName = Path.Combine(targetDir, entryFileName);

            string fullPath = Path.GetDirectoryName(Path.GetFullPath(targetName));
#if TEST
			Console.WriteLine("Decompress targetfile name " + entryFileName);
			Console.WriteLine("Decompress targetpath " + fullPath);
#endif

            // Could be an option or parameter to allow failure or try creation
            if (Directory.Exists(fullPath) == false)
            {
                try
                {
                    Directory.CreateDirectory(fullPath);
                }
                catch
                {
                    return false;
                }
            }


            if (entryFileName.Length > 0)
            {
#if TEST
				Console.WriteLine("Extracting...");
#endif
                using (FileStream streamWriter = File.Create(targetName))
                {
                    byte[] data = new byte[4096];
                    int size;

                    do
                    {
                        size = inputStream.Read(data, 0, data.Length);
                        streamWriter.Write(data, 0, size);
                    } while (size > 0);
                }

                File.SetLastWriteTime(targetName, theEntry.DateTime);
            }
            return true;
        }

        static private void ExtractDirectory(ZipInputStream inputStream, ZipEntry theEntry, string targetDir)
        {
            // For now do nothing.
        }
    }
}
