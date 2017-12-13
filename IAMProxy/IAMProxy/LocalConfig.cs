using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IAM.Proxy
{
    public class LocalConfig
    {
        public String Server { get; internal set; }
        public String Hostname { get; internal set; }
        public Boolean UseHttps { get; internal set; }
        public String ServerCertificate { get; internal set; }
        public String ClientCertificate { get; internal set; }

        public void LoadConfig()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(LocalConfig));
            
            StreamReader reader = File.OpenText(Path.Combine(Path.GetDirectoryName(asm.Location), "proxy.conf"));
            while(!reader.EndOfStream)
            {
                String line = reader.ReadLine().Trim("\t ".ToCharArray());
                if ((line[0] != ";".ToCharArray()[0]) || (line[0] != "#".ToCharArray()[0])) 
                {
                    String[] data = line.Split("=".ToCharArray(), 2);

                    switch (data[0].Trim().ToLower())
                    {
                        case "server":
                            this.Server = data[1].Trim();
                            break;

                        case "hostname":
                            this.Hostname = data[1].Trim();
                            break;

                        case "c1":
                            this.ServerCertificate = data[1].Trim();
                            break;

                        case "c2":
                            this.ClientCertificate = data[1].Trim();
                            break;

                        case "usehttps":
                            switch (data[1].Trim())
                            {
                                case "1":
                                case "sim":
                                case "yes":
                                    UseHttps = true;
                                    break;

                                default:
                                    UseHttps = false;
                                    break;
                            }
                            break;
                    }

                }
            }
            

        }
    }
}
