using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorBasePackageData : IDisposable
    {
        public String dataName;
        public String dataValue;
        public String dataType;

        public PluginConnectorBasePackageData(String dataName, String dataValue, String dataType)
        {
            this.dataName = dataName;
            this.dataType = dataType;
            this.dataValue = dataValue;

            try
            {
                switch (dataType.ToLower())
                {
                    case "datetime":

                        //Testa se é data e hora
                        try
                        {
                            System.Globalization.CultureInfo cultureinfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                            DateTime tmp = DateTime.Parse(dataValue, cultureinfo);

                            //se for uma data e hora válida retorna ela
                            this.dataValue = tmp.ToString("o");
                        }
                        catch { }

                        try
                        {
                            System.Globalization.CultureInfo cultureinfo = new System.Globalization.CultureInfo("pt-BR");
                            DateTime tmp = DateTime.Parse(dataValue, cultureinfo);

                            //se for uma data e hora válida retorna ela
                            this.dataValue = tmp.ToString("o");
                        }
                        catch { }

                        try
                        {
                            System.Globalization.CultureInfo cultureinfo = new System.Globalization.CultureInfo("en-US");
                            DateTime tmp = DateTime.Parse(dataValue, cultureinfo);

                            //se for uma data e hora válida retorna ela
                            this.dataValue = tmp.ToString("o");
                        }
                        catch { }

                        break;
                }
            }
            catch { }
        }

        public void Dispose()
        {
            this.dataName = null;
            this.dataValue = null;
            this.dataType = null;
        }

        public override string ToString()
        {
            return this.dataName + "/" + this.dataType + "=" + this.dataValue;
        }
    }
}
