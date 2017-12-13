using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IAM.Log
{
    public static class TextLog
    {
        public static void Log(String module, String text)
        {
            Log(module, null, text);
        }

        public static void Log(String module, String submodule, String text)
        {
            try
            {
                Int32 pid = 0;
                try
                {
                    pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                }
                catch { }

                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(TextLog));

                String path = Path.Combine(Path.GetDirectoryName(asm.Location), "logs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                BinaryWriter writer = new BinaryWriter(File.Open(Path.Combine(path, DateTime.Now.ToString("yyyyMMdd") + "-" + module + "-" + pid + ".log"), FileMode.Append));
                writer.Write(Encoding.UTF8.GetBytes(DateTime.Now.ToString("o") + " ==> [" + module + (submodule != null ? "-" + submodule : "") + (pid > 0 ? " -> " + pid : "") +"] " + text.Replace("\r", "").Replace("\n", " ") + Environment.NewLine));
                writer.Flush();
                writer.Close();

                asm = null;
            }
            catch { }
        }
    }
}
