using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Web;
using CAS.PluginInterface;

namespace SafeTrend.Data.Update
{
    public class ServiceSynchronizer
    {
        //IRepositoryRepository _repositoryRepository = DependencyResolver.Current.GetService<IRepositoryRepository>();

        public virtual void Run(DbBase ctx, String configPath)
        {
            CheckForNewServices(ctx, configPath);
        }

        private void CheckForNewServices(DbBase ctx, String configPath)
        {
            
            string[] confFiles = Directory.GetFiles(configPath, "*.conf");

            foreach (string file in confFiles)
            {

                try
                {
                    CASPluginConfig cfg = new CASPluginConfig();
                    try
                    {
                        cfg.LoadFromXML(new FileInfo(file));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error parsing config file '" + file + "'", ex);
                    }

                    
                    Uri svc = CASPluginService.Normalize(cfg.Service);

                    //Verifica se o contexto é novo
                    if (ctx.ExecuteScalar<Int64>("select count(*) from [CAS_Context] where [Name] = '" + cfg.Context + "'") == 0)
                    {
                        //Adiciona
                        ctx.ExecuteNonQuery("INSERT INTO [CAS_Context] ([Name],[Host]) VALUES ('" + cfg.Context + "','" + Environment.MachineName + "');");
                    }


                    //Verifica se o serviço é novo
                    if (ctx.ExecuteScalar<Int64>("select count(*) from [CAS_Service] where [Uri] = '" + svc.AbsoluteUri + "'") == 0)
                    {

                        //Adiciona o serviço
                        ctx.ExecuteNonQuery("INSERT INTO [CAS_Service] ([Context_Name],[Uri],[Plugin_Assembly],[Permit_Password_Recover],[External_Password_Recover],[Password_RecoverUri],[Permit_Change_Password],[Admin]) VALUES ('" + cfg.Context + "','" + svc.AbsoluteUri + "','" + cfg.PluginAssembly + "'," + (cfg.PermitPasswordRecover ? 1 : 0) + "," + (cfg.ExternalPasswordRecover ? 1 : 0) + ",'" + cfg.PasswordRecoverUri + "'," + (cfg.PermitChangePassword ? 1 : 0) + ",'" + cfg.Admin + "');");
                        
                    }
                    else
                    {
                        //Atualiza o serviço
                        ctx.ExecuteNonQuery("update [CAS_Service] set [Context_Name] = '" + cfg.Context + "', [Plugin_Assembly] = '" + cfg.PluginAssembly + "',[Permit_Password_Recover] = " + (cfg.PermitPasswordRecover ? 1 : 0) + ",[External_Password_Recover] = " + (cfg.ExternalPasswordRecover ? 1 : 0) + ",[Password_RecoverUri] = '" + cfg.PasswordRecoverUri + "',[Permit_Change_Password] = " + (cfg.PermitChangePassword ? 1 : 0) + ",[Admin] = '" + cfg.Admin + "' where [Uri] = '" + svc.AbsoluteUri + "'");

                        //Apaga as propriedades
                        ctx.ExecuteNonQuery("delete from [CAS_Service_Attributes] where [Service_Uri] = '" + svc.AbsoluteUri + "'");
                    }

                    //Adiciona as propriedades
                    foreach(String key in cfg.Attributes.Keys)
                        ctx.ExecuteNonQuery("INSERT INTO [CAS_Service_Attributes] ([Service_Uri],[Key],[Value]) VALUES ('" + svc.AbsoluteUri + "','" + key + "','" + (cfg.Attributes[key] is DateTime ? ((DateTime)cfg.Attributes[key]).ToString("o") : cfg.Attributes[key].ToString()) + "');");


                }
                catch(Exception ex) {
                    throw ex;
                }
            }

        }
    }
}