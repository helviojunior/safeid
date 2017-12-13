using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using IAM.Log;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SplitCsv
{

    class ColData
    {
        public String Name;
        public Int32 Index;

        public ColData(Int32 Index, String Name)
        {
            this.Index = Index;
            this.Name = Name;
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

            if (String.IsNullOrWhiteSpace(localConfig.Delimiter))
                StopOnError("Parâmetro 'Delimiter' não localizado no arquivo de configuração 'config.conf'", null);

            DirectoryInfo SourcePath = null;
            try
            {
                SourcePath = new DirectoryInfo(localConfig.SourcePath);
            }
            catch (Exception ex)
            {
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
                TextLog.Log("SplitCsv", "Diretório '" + SourcePath.FullName + "' não encontrado");
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


                    TextLog.Log("SplitCsv", "Special: " + (special.Count > 0 ? String.Join(",", special) : "empty"));
                    TextLog.Log("SplitCsv", "Divisor: " + (divC.Count > 0 ? String.Join(",", divC) : "empty"));

                    List<FileInfo> files = new List<FileInfo>();

                    //Byte[] bData = File.ReadAllBytes(file.FullName);

                    TextLog.Log("SplitCsv", file.FullName);

                    Int64 lineN = 0;


                    using (FileStream fs = file.OpenRead())
                    using (StreamReader reader = new StreamReader(fs, Encoding.GetEncoding("iso-8859-1")))
                    {
                        dstFile = null;

                        TextLog.Log("SplitCsv", "reader.EndOfStream? " + reader.EndOfStream);
                        //Int32 lines = 0;
                        List<ColData> cols = new List<ColData>();

                        while (!reader.EndOfStream)
                        {
                            lineN++;

                            String line = reader.ReadLine();

                            try
                            {
                                String[] parts = line.Split(localConfig.Delimiter.ToCharArray());

                                if (cols.Count == 0)
                                {
                                    for (Int32 c = 0; c < parts.Length; c++)
                                        if (parts[c].Trim() != "")
                                            cols.Add(new ColData(c, parts[c].Trim()));

                                    //Ordena por indice
                                    cols.Sort(delegate(ColData s1, ColData s2) { return s1.Index.CompareTo(s2.Index); });
                                }
                                else
                                {

                                    String fprefix = "none";

                                    List<String> colData = new List<String>();

                                    foreach (ColData c in cols)
                                    {
                                        String value = "";
                                        if (c.Index < parts.Length)
                                        {
                                            value = parts[c.Index];

                                            if (special.Contains(c.Name.ToLower()))
                                                value = ClearText(value);

                                            if (divC.Contains(c.Name.ToLower()))
                                                fprefix = ClearText(value);
                                        }
                                        colData.Add(value);
                                    }
                                    
                                    //Build Filename
                                    Boolean writeHeader = false;
                                    dstFile = new FileInfo(Path.Combine(Path.Combine(toPath.FullName, fprefix), file.Name.Replace(file.Extension, "")));
                                    if (!dstFile.Directory.Exists)
                                        writeHeader = true;
                                    else if ((!dstFile.Exists) || (dstFile.Exists && dstFile.Length == 0))
                                        writeHeader = true;

                                    if (writeHeader)
                                    {

                                        List<String> colsH = new List<String>();

                                        foreach (ColData c in cols)
                                            colsH.Add(c.Name);

                                        WriteFile(dstFile, String.Join(localConfig.Delimiter, colsH));
                                    }

                                    WriteFile(dstFile, String.Join(localConfig.Delimiter, colData));

                                    if (!files.Exists(f => (f.FullName.ToLower() == dstFile.FullName.ToLower())))
                                        files.Add(dstFile);

                                }

                            }
                            catch (Exception ex)
                            {
                                TextLog.Log("SplitCsv", "Falha ao importar a linha '" + line + "' do arquivo '" + file.Name + "': " + ex.Message);
                            }

                        }

                        TextLog.Log("SplitCsv", "Processado " + lineN + " linhas");
                    }

                    TextLog.Log("SplitCsv", "Movendo arquivo " + file.FullName + " para " + file.FullName + ".imported");
                    File.Move(file.FullName, file.FullName + ".imported");

                    foreach (FileInfo f in files)
                    {
                        if (!f.Directory.Exists)
                            f.Directory.Create();

                        TextLog.Log("SplitCsv", "Movendo arquivo " + f.FullName + " para " + f.FullName + ".csv");
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
