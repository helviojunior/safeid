using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AdpToCsv
{
    public class LocalConfig
    {
        public String SourcePath { get; internal set; }
        public String SourceExtension { get; internal set; }
        public String DestinationPath { get; internal set; }
        public String ClearSpecial { get; internal set; }
        public String FolderDivColumn { get; internal set; }

        public LocalConfig()
        {
            this.SourcePath = null;
            this.SourceExtension = "*.txt";
            this.DestinationPath = null;
            this.ClearSpecial = "";
            this.FolderDivColumn = "";
        }

        public void LoadConfig()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(LocalConfig));

            StreamReader reader = File.OpenText(Path.Combine(Path.GetDirectoryName(asm.Location), "config.conf"));
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
                        case "sourcepath":
                            this.SourcePath = data[1].Trim();
                            break;

                        case "sourceextension":
                            this.SourceExtension = data[1].Trim();
                            break;

                        case "destinationpath":
                            this.DestinationPath = data[1].Trim();
                            break;

                        case "clearspecial":
                            this.ClearSpecial = data[1].Trim();
                            break;

                        case "folderdivcolumn":
                            this.FolderDivColumn = data[1].Trim();
                            break;

                    }

                }
            }

        }
    }
}
