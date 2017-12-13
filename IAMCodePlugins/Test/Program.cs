using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

using IAM.CodeManager;
using cZenviaSMS;
using cSIPCall;


namespace Test
{
    class Program
    {
        
        
        static void Main(string[] args)
        {
            StartCall("e:\\config-teste.xml");
            return;

            Dictionary<String, Object> config = new Dictionary<string, object>();
            config.Add("host", "10.0.0.200");
            config.Add("username", "iam");
            config.Add("password", "1234");

            List<String> iData = new List<string>();
            iData.Add("41-98932694");
            iData.Add("(41) 3086-1947");

            CodeManagerPluginBase tst = new SIPCall();
            List<CodeData> cd = tst.ParseData(iData);

            tst.SendCode(config, iData, cd[0].DataId, "Teste");

            while(true)
                Console.ReadLine();
        }



        private static void StartCall(String configFile)
        {

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();

            ProcessStartInfo psi = new ProcessStartInfo();
            //psi.Verb = "runas";
            psi.Arguments = configFile;
            psi.FileName = Path.Combine(Path.GetDirectoryName(asm.Location), "SIPCall2.exe");

            Process p = new Process();
            p.StartInfo = psi;
            p.Start();

        }

    }
}
