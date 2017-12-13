using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IAM.LocalConfig
{
    public class ServerLocalConfig
    {
        public String SqlServer { get; internal set; }
        public String SqlDb { get; internal set; }
        public String SqlUsername { get; internal set; }
        public String SqlPassword { get; internal set; }
        public Int32 EngineMaxThreads { get; internal set; }

        public ServerLocalConfig()
        {
            this.SqlServer = null;
            this.SqlDb = null;
            this.SqlUsername = null;
            this.SqlPassword = null;
            this.EngineMaxThreads = 30;
        }

        public void LoadConfig()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(ServerLocalConfig));

            StreamReader reader = File.OpenText(Path.Combine(Path.GetDirectoryName(asm.Location), "server.conf"));
            while (!reader.EndOfStream)
            {
                String line = reader.ReadLine().Trim("\t ".ToCharArray());
                if (line.Trim() == "")
                    continue;

                if ((line[0] != ";".ToCharArray()[0]) || (line[0] != "#".ToCharArray()[0]))
                {
                    String[] data = line.Split("=".ToCharArray());

                    switch (data[0].Trim().ToLower())
                    {
                        case "sqlserver":
                            this.SqlServer = data[1].Trim();
                            break;

                        case "sqldb":
                            this.SqlDb = data[1].Trim();
                            break;

                        case "sqlusername":
                            this.SqlUsername = data[1].Trim();
                            break;

                        case "sqlpassword":
                            this.SqlPassword = data[1].Trim();
                            break;

                        case "enginemaxthreads":
                            try
                            {
                                this.EngineMaxThreads = Int32.Parse(data[1].Trim());
                            }
                            catch { }
                            break;

                    }

                }
            }

            if (this.EngineMaxThreads < 0)
                this.EngineMaxThreads = 1;

        }
    }
}
