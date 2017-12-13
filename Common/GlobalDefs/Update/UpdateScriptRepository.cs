using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.Configuration;

namespace SafeTrend.Data.Update
{
    public static class UpdateScriptRepository
    {
        /// <summary>
        /// Creates the list of scripts that should be executed on app start. Ordering matters!
        /// </summary>
        public static IEnumerable<IUpdateScript> GetScriptsBySqlProviderName(String ProviderName)
        {
            List<IUpdateScript> scripts = new List<IUpdateScript>();

            switch (ProviderName.ToLower())
            {
                case "sqlclient":
                case "system.data.sqlclient":

                    //Busca automaticamente todos os scripts deste mesmo provider
                    //
                    try
                    {
                        List<Type> types = new List<Type>();

                        //Seleciona todos os tipos de todos os assemblies carregados
                        //Filtrado se é classe e pelo nome do método desejado
                        Assembly asm = Assembly.GetExecutingAssembly();
                        try
                        {
                            types.AddRange(from t in asm.GetTypes()
                                           where t.IsClass //&& t.Name == "InsertDefaultData"
                                           select t
                                            );
                        }
                        catch { }

                        /*
                        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                            try
                            {
                                types.AddRange(from t in asm.GetTypes()
                                               where t.IsClass
                                               select t
                                                );
                            }
                            catch { }*/

                        foreach (Type type in types)
                        {
                            if (!type.IsClass) continue;

                            Type baseType = type;
                            while (baseType != null)
                            {
                                Type Icheck = baseType.GetInterface("IUpdateScript");

                                if ((Icheck != null) && (Icheck.Equals(typeof(IUpdateScript))))
                                {
                                    object obj = Activator.CreateInstance(type);
                                    if (((IUpdateScript)obj).Provider.ToLower() == ProviderName.ToLower())
                                        scripts.Add((IUpdateScript)obj);
                                    else
                                        obj = null;
                                }
                                baseType = baseType.BaseType;
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("PluginManager error: " + ex.Message);
                    }

                    //Realiza a ordenação dos itens com base no número serial
                    scripts.Sort(delegate(IUpdateScript s1, IUpdateScript s2) { return s1.Serial.CompareTo(s2.Serial); });

                    break;

                default:
                    throw new NotImplementedException(string.Format("The provider '{0}' is not supported yet", ProviderName));
            }

            return scripts;
        }
    }
}