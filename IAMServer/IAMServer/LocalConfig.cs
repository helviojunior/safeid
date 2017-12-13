using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IAM.Server
{
    public class LocalConfig
    {
        public String SqlServer { get; internal set; }
        public String SqlDb { get; internal set; }
        public String SqlUsername { get; internal set; }
        public String SqlPassword { get; internal set; }

        public void LoadConfig()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(LocalConfig));

            StreamReader reader = File.OpenText(Path.Combine(Path.GetDirectoryName(asm.Location), "server.conf"));
            while (!reader.EndOfStream)
            {
                String line = reader.ReadLine().Trim("\t ".ToCharArray()).ToLower();
                if ((line[0] != ";".ToCharArray()[0]) || (line[0] != "#".ToCharArray()[0]))
                {
                    String[] data = line.Split("=".ToCharArray());

                    switch (data[0].Trim())
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

                    }

                }
            }


        }
    }
}
