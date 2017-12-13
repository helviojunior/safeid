using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using CASPluginIAM;
using CAS.PluginInterface;

namespace Teste
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<String, Object> config = new Dictionary<string,object>();
            config.Add("api", new Uri("http://im.fael.edu.br/api/json.aspx"));
            config.Add("username", "helvio.junior");
            config.Add("password", "@H31v1001!");

            /*
            IAMPlugin tst = new IAMPlugin();
            tst.SetStart(new DirectoryInfo("."), new Uri("http://im.fael.edu.br/login/"), config, null);
            CASUserInfo res = tst.FindUser("teste001");
            */
        }
    }
}
