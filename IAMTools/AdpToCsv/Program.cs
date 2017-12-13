using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using IAM.Log;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AdpToCsv
{
    class ColData
    {
        public Int32 stPos;
        public Int32 length;

        public ColData(Int32 stPos, Int32 length)
        {
            this.stPos = stPos;
            this.length = length;
        }
    }

    class Program
    {
        static LocalConfig localConfig;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);


            /*************
             * Carrega configurações
             */

            localConfig = new LocalConfig();
            localConfig.LoadConfig();

            if (String.IsNullOrWhiteSpace(localConfig.SourcePath))
                StopOnError("Parâmetro 'SourcePath' não localizado no arquivo de configuração 'config.conf'", null);

            if (String.IsNullOrWhiteSpace(localConfig.DestinationPath))
                StopOnError("Parâmetro 'DestinationPath' não localizado no arquivo de configuração 'config.conf'", null);

            if (String.IsNullOrWhiteSpace(localConfig.SourceExtension))
                StopOnError("Parâmetro 'SourceExtension' não localizado no arquivo de configuração 'config.conf'", null);

            DirectoryInfo SourcePath = null;
            try
            {
                SourcePath = new DirectoryInfo(localConfig.SourcePath);
            }
            catch(Exception ex) {
                StopOnError("Parâmetro 'SourcePath' inválido", ex);
            }

            DirectoryInfo DestinationPath = null;
            try
            {
                DestinationPath = new DirectoryInfo(localConfig.DestinationPath);
            }
            catch (Exception ex)
            {
                StopOnError("Parâmetro 'DestinationPath' inválido", ex);
            }

            if (!SourcePath.Exists)
            {
                TextLog.Log("AdpCSV", "Diretório '" + SourcePath.FullName + "' não encontrado");
                Console.WriteLine("Diretório '" + SourcePath.FullName + "' não encontrado");
                return;
            }

            if (!DestinationPath.Exists)
                DestinationPath.Create();

            foreach (FileInfo f in SourcePath.GetFiles(localConfig.SourceExtension))
                ProcessFile(f, DestinationPath);

        }

        static void ProcessFile(FileInfo file, DirectoryInfo toPath)
        {
            Console.WriteLine(file.Name);

            
                FileInfo dstFile = null;

                try
                {

                    //Encoding enc = GetEncoding(file);

                    List<String> special = new List<String>();
                    if (!String.IsNullOrWhiteSpace(localConfig.ClearSpecial))
                        foreach (String s in localConfig.ClearSpecial.Split(",".ToCharArray()))
                            if (!String.IsNullOrWhiteSpace(s))
                                special.Add(s.Trim().ToLower());

                    List<String> divC = new List<String>();
                    if (!String.IsNullOrWhiteSpace(localConfig.FolderDivColumn))
                        foreach (String s in localConfig.FolderDivColumn.Split(",".ToCharArray()))
                            if (!String.IsNullOrWhiteSpace(s))
                                divC.Add(s.Trim().ToLower());

                    Dictionary<String, ColData> positions = new Dictionary<String, ColData>();

                    Dictionary<String, StringBuilder> filesData = new Dictionary<string, StringBuilder>();
                    String header = "";

                    List<FileInfo> files = new List<FileInfo>();

                    //Byte[] bData = File.ReadAllBytes(file.FullName);

                    TextLog.Log("AdpCSV", file.FullName);

                    Int64 lineN = 0;


                    using (FileStream fs = file.OpenRead())
                    using (StreamReader reader = new StreamReader(fs, Encoding.GetEncoding("iso-8859-1")))
                    {
                        dstFile = null;

                        TextLog.Log("AdpCSV", "reader.EndOfStream? " + reader.EndOfStream);
                        Int32 lines = 0;
                        while (!reader.EndOfStream)
                        {
                            lineN++;

                            try
                            {
                                if (positions.Count == 0)
                                {
                                    TextLog.Log("AdpCSV", "Calc positions");

                                    String line1 = reader.ReadLine();
                                    String line2 = "";
                                    if (!reader.EndOfStream)
                                        line2 = reader.ReadLine();

                                    lineN++;

                                    if (String.IsNullOrWhiteSpace(line2))
                                        break;

                                    if (line2.IndexOf("---") == -1)
                                        break;

                                    String[] columns = line2.Trim().Split(" ".ToCharArray());
                                    TextLog.Log("AdpCSV", "Colunas: " + columns.Length);

                                    Int32 offSet = 0;
                                    foreach (String c in columns)
                                    {

                                        if (c.Length > 0)
                                        {
                                            try
                                            {
                                                String colName = line1.Substring(offSet, c.Length).Trim(" ".ToCharArray());
                                                if (!positions.ContainsKey(colName))
                                                    positions.Add(colName, new ColData(offSet, c.Length));
                                                else
                                                    positions[colName] = new ColData(offSet, c.Length);

                                                TextLog.Log("AdpCSV", "Coluna: " + colName);

                                            }
                                            catch (Exception ex)
                                            {
                                                TextLog.Log("AdpCSV", "Erro ao processar a coluna: offSet = " + offSet + ", Length = " + c.Length);
                                            }
                                        }

                                        offSet += c.Length + 1;
                                    }

                                    List<String> cols = new List<String>();

                                    foreach (String k in positions.Keys)
                                        cols.Add(k);

                                    header = String.Join(",", cols);
                                    //WriteFile(dstFile, String.Join(",",cols));

                                    TextLog.Log("AdpCSV", header);

                                }
                                else
                                {
                                    String line = reader.ReadLine();
                                    String fprefix = "none";

                                    if (String.IsNullOrWhiteSpace(line))
                                        continue;

                                    List<String> cols = new List<String>();

                                    foreach (String k in positions.Keys)
                                    {
                                        ColData cd = positions[k];
                                        String value = "";
                                        if ((cd.stPos + cd.length) <= line.Length)
                                        {
                                            value = line.Substring(cd.stPos, cd.length).Trim();
                                            if (special.Contains(k.ToLower()))
                                                value = ClearText(value);

                                            if (divC.Contains(k.ToLower()))
                                                fprefix = ClearText(value);
                                        }
                                        cols.Add(value);
                                    }


                                    //Build Filename
                                    Boolean writeHeader = false;
                                    dstFile = new FileInfo(Path.Combine(Path.Combine(toPath.FullName, fprefix), file.Name.Replace(file.Extension, "")));
                                    if (!dstFile.Directory.Exists)
                                        writeHeader = true;
                                    else if ((!dstFile.Exists) || (dstFile.Exists && dstFile.Length == 0))
                                        writeHeader = true;

                                    if (writeHeader)
                                        WriteFile(dstFile, header);

                                    WriteFile(dstFile, String.Join(",", cols));
                                    lines++;

                                    if (!files.Exists(f => (f.FullName.ToLower() == dstFile.FullName.ToLower())))
                                        files.Add(dstFile);

                                }


                            }
                            catch (Exception ex)
                            {
                                TextLog.Log("AdpCSV", "Erro processando linha " + lineN + " em " + file.FullName);
                                UnhandledException.WriteEvent(null, new Exception("Erro processando linha " + lineN + " em " + file.FullName, ex), false);
                                throw ex;
                            }
                        }

                        TextLog.Log("AdpCSV", "Processado " + lines + " linhas");
                    }

                    TextLog.Log("AdpCSV", "Movendo arquivo " + file.FullName + " para " + file.FullName + ".imported");
                    File.Move(file.FullName, file.FullName + ".imported");

                    foreach (FileInfo f in files)
                    {
                        if (!f.Directory.Exists)
                            f.Directory.Create();

                        TextLog.Log("AdpCSV", "Movendo arquivo " + f.FullName + " para " + f.FullName + ".csv");
                        File.Move(f.FullName, f.FullName + ".csv");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro on proccess file '" + file.FullName + "'");
                    UnhandledException.WriteEvent(null, new Exception("Erro on proccess file '" + file.FullName + "'", ex), false);

                    if (dstFile != null)
                        try
                        {
                            dstFile.Delete();
                        }
                        catch { }
                }
        }

        static String ClearText(String text)
        {
            string s = text.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < s.Length; k++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(s[k]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(s[k]);
                }
            }

            String s1 = sb.ToString();
            s1 = Regex.Replace(s1, "[^0-9a-zA-Z]+", "");

            return s1;
        }

        static void WriteFile(FileInfo file, String text)
        {
            if (!file.Directory.Exists)
                file.Directory.Create();

            using (FileStream fs = File.Open(file.FullName, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
                writer.WriteLine(text);
        }

        static Encoding GetEncoding(FileInfo f)
        {
            System.Text.Encoding enc = null;
            using (System.IO.FileStream file = new System.IO.FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                if (file.CanSeek)
                {
                    byte[] bom = new byte[4]; // Get the byte-order mark, if there is one 
                    file.Read(bom, 0, 4);
                    if ((bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) || // utf-8 
                        (bom[0] == 0xff && bom[1] == 0xfe) || // ucs-2le, ucs-4le, and ucs-16le 
                        (bom[0] == 0xfe && bom[1] == 0xff) || // utf-16 and ucs-2 
                        (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)) // ucs-4 
                    {
                        enc = System.Text.Encoding.Unicode;
                    }
                    else
                    {
                        enc = System.Text.Encoding.ASCII;
                    }

                    // Now reposition the file cursor back to the start of the file 
                    file.Seek(0, System.IO.SeekOrigin.Begin);
                }
                else
                {
                    // The file cannot be randomly accessed, so you need to decide what to set the default to 
                    // based on the data provided. If you're expecting data from a lot of older applications, 
                    // default your encoding to Encoding.ASCII. If you're expecting data from a lot of newer 
                    // applications, default your encoding to Encoding.Unicode. Also, since binary files are 
                    // single byte-based, so you will want to use Encoding.ASCII, even though you'll probably 
                    // never need to use the encoding then since the Encoding classes are really meant to get 
                    // strings from the byte array that is the file. 

                    enc = System.Text.Encoding.ASCII;
                }

            return enc;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }


        private static void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("AdpToCsv", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("AdpToCsv", text);
            }

            Process.GetCurrentProcess().Kill();
        }

    }
}
