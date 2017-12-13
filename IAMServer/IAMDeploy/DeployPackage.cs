using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;

//using IAM.SQLDB;
using IAM.PluginInterface;
using IAM.CA;
using IAM.Config;
using IAM.GlobalDefs;
using SafeTrend.Json;

namespace IAM.Deploy
{
    [Serializable()]
    static class DeployPackage
    {
        //public static PluginConnectorBaseDeployPackage GetPackage(IAMDatabase db, Int64 proxyId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, Boolean passwordAfterLogin, DateTime? lastChangePassword, String deploy_password_hash)
        public static PluginConnectorBaseDeployPackage GetPackage(IAMDatabase db, Int64 proxyId, Int64 resourcePluginId, Int64 entityId, Int64 identityId, Boolean passwordAfterLogin, DateTime? lastChangePassword, String deploy_password_hash, Boolean useSalt, Boolean saltOnEnd, String salt)
        {

            PluginConnectorBaseDeployPackage pkg = new PluginConnectorBaseDeployPackage();

            List<String> deployInfo = new List<string>();//"Identity addedd in deploy package with ";
            String deployText = "";
            try
            {

                DataTable dtEnt = db.Select("select e.*, c.enterprise_id, rp.plugin_id, i.id identity_id, i.temp_locked, c.name context_name, e1.name enterprise_name, block_inheritance = case when exists (select 1 from identity_block_inheritance bi with(nolock) where bi.identity_id = i.id) then cast(1 as bit) else cast(0 as bit) end from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id inner join [identity] i with(nolock) on i.entity_id = e.id inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id inner join enterprise e1 with(nolock) on c.enterprise_id = e1.id where e.id = " + entityId + " and i.id = " + identityId);
                if ((dtEnt == null) || (dtEnt.Rows.Count == 0))
                    throw new Exception("Entity/Identity not found");

                //DataTable dtPlugin = db.Select("select p.* from plugin p where p.id = " + pluginId);
                DataTable dtPlugin = db.Select("select distinct p.*, rp.resource_id from plugin p inner join resource_plugin rp on rp.plugin_id = p.id inner join resource r on rp.resource_id = r.id inner join entity e on e.context_id = r.context_id where rp.id = " + resourcePluginId + " and e.id = " + entityId);
                if ((dtPlugin == null) || (dtPlugin.Rows.Count == 0))
                    throw new Exception("Plugin not found or not linked in the same context of entity");

                if ((Boolean)dtEnt.Rows[0]["block_inheritance"])
                    throw new Exception("Inheritance blocked");

                Int64 resourceId = (Int64)dtPlugin.Rows[0]["resource_id"];
                Int64 pluginId = (Int64)dtPlugin.Rows[0]["id"];

                //Define as pripriedades gerais
                pkg.registryId = dtEnt.Rows[0]["id"] + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                pkg.entityId = entityId;
                pkg.identityId = identityId;
                pkg.fullName = new FullName(dtEnt.Rows[0]["full_name"].ToString());
                pkg.login = dtEnt.Rows[0]["login"].ToString();
                pkg.lastChangePassword = (lastChangePassword.HasValue ? lastChangePassword.Value.ToString("o") : null);

                
                pkg.locked = (Boolean)dtEnt.Rows[0]["locked"];
                pkg.temp_locked = (Boolean)dtEnt.Rows[0]["temp_locked"];
                pkg.mustChangePassword = (Boolean)dtEnt.Rows[0]["must_change_password"];
                pkg.deleted = (Boolean)dtEnt.Rows[0]["deleted"];

                pkg.enterprise = dtEnt.Rows[0]["enterprise_name"].ToString();
                pkg.context = dtEnt.Rows[0]["context_name"].ToString();

                if ((Boolean)dtEnt.Rows[0]["deleted"])
                    db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Info, proxyId, 0, 0, resourceId, pluginId, (Int64)dtEnt.Rows[0]["id"], (Int64)dtEnt.Rows[0]["identity_id"], "Deploy to delete identity");

                //Container
                pkg.container = "";
                try
                {
                    DataTable dtUserContainer = db.Select("select top 1 c.* from [container] c with(nolock) inner join entity_container ec with(nolock) on c.id = ec.container_id where ec.entity_id = " + entityId);
                    if ((dtUserContainer != null) && (dtUserContainer.Rows.Count > 0))
                    {
                        List<String> path = new List<string>();
                        path.Add(dtUserContainer.Rows[0]["name"].ToString());

                        if ((Int64)dtUserContainer.Rows[0]["parent_id"] > 0)
                        {
                            DataTable dtContainers = db.Select("select c.* from container c with(nolock)");
                            if ((dtContainers != null) || (dtContainers.Rows.Count > 0))
                            {
                                Func<Int64, Boolean> chields = null;
                                chields = new Func<Int64, Boolean>(delegate(Int64 root)
                                {

                                    foreach (DataRow dr in dtContainers.Rows)
                                        if (((Int64)dr["id"] == root))
                                        {
                                            path.Add(dr["name"].ToString());
                                            chields((Int64)dr["parent_id"]);
                                            break;
                                        }

                                    return true;
                                });

                                chields((Int64)dtUserContainer.Rows[0]["parent_id"]);
                            }
                        }

                        path.Reverse();
                        pkg.container = "\\" + String.Join("\\", path);
                    }
                }
                catch { }

                //Senha
                pkg.password = "";
                if ((dtEnt.Rows[0]["password"] != DBNull.Value) && (dtEnt.Rows[0]["password"].ToString().Trim() != ""))
                {
                    //Este recurso x plugin só permite o deploy da SENHA após o primeiro login
                    if ((!passwordAfterLogin) || ((passwordAfterLogin) && (dtEnt.Rows[0]["last_login"] != DBNull.Value)))
                    {
                        try
                        {
                            String pwd = "";
                            using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.Connection, (Int64)dtEnt.Rows[0]["enterprise_id"]))
                            using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(dtEnt.Rows[0]["password"].ToString())))
                                pwd = Encoding.UTF8.GetString(cApi.clearData);

                            //Verifica se usará SALT
                            if (useSalt)
                            {
                                if (!String.IsNullOrWhiteSpace(salt))
                                {
                                    if (saltOnEnd)
                                    {
                                        deployInfo.Add("password + SALT");
                                        pwd = pwd + salt.Trim();
                                    }
                                    else
                                    {
                                        deployInfo.Add("SALT + password");
                                        pwd = salt.Trim() + pwd;
                                    }
                                }
                                else
                                {
                                    deployInfo.Add("salt is empty");
                                }
                            }
                            else
                            {
                                deployInfo.Add("no salt");
                            }

                            if (!String.IsNullOrEmpty(deploy_password_hash))
                            {
                                switch (deploy_password_hash.ToLower())
                                {
                                    case "md5":
                                        using (MD5 hAlg = MD5.Create())
                                            pkg.password = ComputeHash(hAlg, pwd).ToUpper();
                                        pkg.hash_alg = HashAlg.MD5;
                                        deployInfo.Add("MD5 password");
                                        break;

                                    case "sha1":
                                        using (SHA1 hAlg = SHA1.Create())
                                            pkg.password = ComputeHash(hAlg, pwd).ToUpper();
                                        pkg.hash_alg = HashAlg.SHA1;
                                        deployInfo.Add("SHA1 password");
                                        break;

                                    case "sha256":
                                        using (SHA256 hAlg = SHA256.Create())
                                            pkg.password = ComputeHash(hAlg, pwd).ToUpper();
                                        pkg.hash_alg = HashAlg.SHA256;
                                        deployInfo.Add("SHA256 password");
                                        break;

                                    case "sha512":
                                        using (SHA512 hAlg = SHA512.Create())
                                            pkg.password = ComputeHash(hAlg, pwd).ToUpper();
                                        pkg.hash_alg = HashAlg.SHA512;
                                        deployInfo.Add("SHA512 password");
                                        break;

                                    default:
                                        //Nenhum algoritmo de hash
                                        pkg.password = pwd;
                                        pkg.hash_alg = HashAlg.None;
                                        deployInfo.Add("clear text password");
                                        break;
                                }
                            }
                            else
                            {
                                pkg.password = pwd;
                                pkg.hash_alg = HashAlg.None;
                                deployInfo.Add("clear text password");
                            }

                            
                            deployText += "User password added in deploy" + Environment.NewLine;
                            //db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Info, proxyId, 0, 0, resourceId, pluginId, (Int64)dtEnt.Rows[0]["id"], (Int64)dtEnt.Rows[0]["identity_id"], "User password added in deploy");
                        }
                        catch (Exception ex)
                        {
                            deployInfo.Add("no password");
                            deployText += "User password not deployed because a erro on decrypt password: " + ex.Message + Environment.NewLine;
                            //db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Warning, proxyId, 0, 0, resourceId, pluginId, (Int64)dtEnt.Rows[0]["id"], (Int64)dtEnt.Rows[0]["identity_id"], "User password not deployed because a erro on decrypt password: " + ex.Message);
                        }
                    }
                    else
                    {
                        deployInfo.Add("no password");
                        deployText += "User password not deployed because the user is not logged in yet" + Environment.NewLine;
                        //db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Debug, proxyId, 0, 0, resourceId, pluginId, (Int64)dtEnt.Rows[0]["id"], (Int64)dtEnt.Rows[0]["identity_id"], "User password not deployed because the user is not logged in yet");
                    }
                }
                else
                {
                    deployInfo.Add("no password");
                    deployText += "User password is empty and not deployed" + Environment.NewLine;
                    //db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Debug, proxyId, 0, 0, resourceId, pluginId, (Int64)dtEnt.Rows[0]["id"], (Int64)dtEnt.Rows[0]["identity_id"], "User password is empty and not deployed");
                }


                //Busca todas as propriedades com o mapping deste plugin, porém com dados vindos exclusivos da entidade
                DataTable dtEntField = db.Select("select pf.data_name, efe.value, pf.data_type from entity_field efe inner join entity e on efe.entity_id = e.id inner join (select m.field_id, m.data_name, f.data_type from resource_plugin rp inner join resource r on rp.resource_id = r.id inner join resource_plugin_mapping m on m.resource_plugin_id = rp.id and m.is_password = 0 inner join field f on m.field_id = f.id where rp.id =  " + resourcePluginId + ") pf on pf.field_id = efe.field_id where e.id =  " + pkg.entityId + " group by pf.data_name, efe.value, pf.data_type");
                if ((dtEntField != null) && (dtEntField.Rows.Count > 0))
                {
                    foreach (DataRow drEf in dtEntField.Rows)
                    {
                        if (!pkg.entiyData.Exists(d => (d.dataName == drEf["data_name"].ToString())))
                            pkg.entiyData.Add(new PluginConnectorBasePackageData(drEf["data_name"].ToString(), ConvertoToString(dtEntField.Columns["value"], drEf), drEf["data_type"].ToString()));
                    }
                }


                //Busca todas as propriedades com o mapping deste plugin, porém com dados vindos dos plugins de entrada
                //Exclui os itens de nome e senha por ja terem sido colocados acima
                dtEntField = db.Select("select pf.data_name, ife.value, pf.data_type, rp.priority from identity_field ife inner join [identity] i on ife.identity_id = i.id inner join entity e on i.entity_id = e.id inner join resource_plugin rp on i.resource_plugin_id = rp.id inner join (select m.field_id, m.data_name, f.data_type from resource_plugin rp inner join resource r on rp.resource_id = r.id inner join resource_plugin_mapping m on m.resource_plugin_id = rp.id and m.is_password = 0 inner join field f on m.field_id = f.id where rp.id =  " + resourcePluginId + ") pf on pf.field_id = ife.field_id where rp.enable_import = 1 and i.entity_id =  " + pkg.entityId + " and not exists (select 1 from identity_block_inheritance bi where bi.identity_id = i.id) group by pf.data_name, ife.value, pf.data_type, rp.priority order by rp.priority desc, pf.data_name");
                if ((dtEntField != null) && (dtEntField.Rows.Count > 0))
                {
                    foreach (DataRow drEf in dtEntField.Rows)
                    {
                        if (!pkg.importsPluginData.Exists(d => (d.dataName == drEf["data_name"].ToString())))
                            pkg.importsPluginData.Add(new PluginConnectorBasePackageData(drEf["data_name"].ToString(), ConvertoToString(dtEntField.Columns["value"], drEf), drEf["data_type"].ToString()));
                    }
                }

                //Busca todas as propriedades vinculadas a este identity
                //Exclui os itens de nome e senha por ja terem sido colocados acima
                dtEntField = db.Select("select m.data_name, ife.value, f.data_type from identity_field ife inner join [identity] i on ife.identity_id = i.id inner join entity e on i.entity_id = e.id inner join resource_plugin rp on rp.id = i.resource_plugin_id and ife.field_id <> rp.name_field_id inner join resource r on r.context_id = e.context_id and rp.resource_id = r.id inner join resource_plugin_mapping m on m.resource_plugin_id = rp.id and m.field_id = ife.field_id and m.is_password = 0 inner join field f on ife.field_id = f.id where i.entity_id =  " + pkg.entityId + " and i.id = " + identityId + " group by m.data_name, ife.value, f.data_type");
                if ((dtEntField != null) && (dtEntField.Rows.Count > 0))
                {
                    foreach (DataRow drEf in dtEntField.Rows)
                        pkg.pluginData.Add(new PluginConnectorBasePackageData(drEf["data_name"].ToString(), ConvertoToString(dtEntField.Columns["value"], drEf), drEf["data_type"].ToString()));
                }

                //Busca todas as propriedades vinculadas aos outras identity
                //Exclui os itens de nome e senha por ja terem sido colocados acima
                dtEntField = db.Select("select m.data_name, ife.value, f.data_type from identity_field ife inner join [identity] i on ife.identity_id = i.id inner join entity e on i.entity_id = e.id inner join resource_plugin rp on rp.id = i.resource_plugin_id and ife.field_id <> rp.name_field_id inner join resource r on r.context_id = e.context_id and rp.resource_id = r.id inner join resource_plugin_mapping m on m.resource_plugin_id = rp.id and m.field_id = ife.field_id and m.is_password = 0 inner join field f on ife.field_id = f.id where i.entity_id =  " + pkg.entityId + " and i.id <> " + identityId + " and not exists (select 1 from identity_block_inheritance bi where bi.identity_id = i.id) group by m.data_name, ife.value, f.data_type");
                if ((dtEntField != null) && (dtEntField.Rows.Count > 0))
                {
                    foreach (DataRow drEf in dtEntField.Rows)
                        pkg.properties.Add(new PluginConnectorBasePackageData(drEf["data_name"].ToString(), ConvertoToString(dtEntField.Columns["value"], drEf), drEf["data_type"].ToString()));
                }

                //Busca todas as propriedades (independente do identity) usando o mapping deste plugin
                //Exclui o senha por ja tere sido colocado acima
                dtEntField = db.Select("select pf.data_name, ife.value, pf.data_type from identity_field ife inner join [identity] i on ife.identity_id = i.id inner join entity e on i.entity_id = e.id inner join (select m.field_id, m.data_name, f.data_type from resource_plugin rp inner join resource r on rp.resource_id = r.id inner join resource_plugin_mapping m on m.resource_plugin_id = rp.id and m.is_password = 0 inner join field f on m.field_id = f.id where rp.id = " + resourcePluginId + ") pf on pf.field_id = ife.field_id where i.entity_id =  " + pkg.entityId + " and not exists (select 1 from identity_block_inheritance bi where bi.identity_id = i.id) group by pf.data_name, ife.value, pf.data_type");
                if ((dtEntField != null) && (dtEntField.Rows.Count > 0))
                {
                    foreach (DataRow drEf in dtEntField.Rows)
                        pkg.properties.Add(new PluginConnectorBasePackageData(drEf["data_name"].ToString(), ConvertoToString(dtEntField.Columns["value"], drEf), drEf["data_type"].ToString()));
                }


                //Busca todas as propriedades da tabela entity_field (exclusiva para dados manuais) usando o mapping deste plugin
                //Exclui o senha por ja tere sido colocado acima
                dtEntField = db.Select("select pf.data_name, efe.value, pf.data_type from entity_field efe inner join entity e on efe.entity_id = e.id inner join (select m.field_id, m.data_name, f.data_type from resource_plugin rp inner join resource r on rp.resource_id = r.id inner join resource_plugin_mapping m on m.resource_plugin_id = rp.id and m.is_password = 0 inner join field f on m.field_id = f.id where rp.id = " + resourcePluginId + ") pf on pf.field_id = efe.field_id where efe.entity_id = " + pkg.entityId + "  group by pf.data_name, efe.value, pf.data_type");
                if ((dtEntField != null) && (dtEntField.Rows.Count > 0))
                {
                    foreach (DataRow drEf in dtEntField.Rows)
                        pkg.properties.Add(new PluginConnectorBasePackageData(drEf["data_name"].ToString(), ConvertoToString(dtEntField.Columns["value"], drEf), drEf["data_type"].ToString()));
                }


                //Busca somente as propriedades marcadas como ID ou Unique property
                //Exclui os itens de nome e senha por ja terem sido colocados acima
                dtEntField = db.Select("select m.data_name, ife.value, f.data_type from identity_field ife inner join [identity] i on ife.identity_id = i.id inner join entity e on i.entity_id = e.id inner join resource_plugin rp on rp.id = i.resource_plugin_id and ife.field_id <> rp.name_field_id inner join resource r on r.context_id = e.context_id and rp.resource_id = r.id inner join resource_plugin_mapping m on m.resource_plugin_id = rp.id and m.field_id = ife.field_id and m.is_password = 0 and (m.is_unique_property = 1 or m.is_unique_property = 1) inner join field f on ife.field_id = f.id where i.entity_id =  " + pkg.entityId + " and not exists (select 1 from identity_block_inheritance bi where bi.identity_id = i.id) group by m.data_name, ife.value, f.data_type");
                if ((dtEntField != null) && (dtEntField.Rows.Count > 0))
                {
                    foreach (DataRow drEf in dtEntField.Rows)
                        pkg.ids.Add(new PluginConnectorBasePackageData(drEf["data_name"].ToString(), ConvertoToString(dtEntField.Columns["value"], drEf), drEf["data_type"].ToString()));
                }

                
                //RBAC
                //Ações das roles desta identity para este resource x plugin
                DataTable dtRoleAction = db.Select("select i.id identity_id, r.* from [identity] i inner join [entity] e on e.id = i.entity_id inner join identity_role ir on ir.identity_id = i.id  inner join (select rp.id resource_plugin_id, rp.plugin_id, rp.resource_id, r.name role_name, rpa.id action_id, rpa.role_id, rpa.action_key, rpa.action_add_value, rpa.action_del_value, rpa.additional_data from resource_plugin rp inner join resource_plugin_role rpr on rpr.resource_plugin_id = rp.id inner join resource_plugin_role_action rpa on rpa.resource_plugin_id = rp.id inner join [role] r on r.id = rpa.role_id and r.id = rpr.role_id) r on r.role_id = ir.role_id where r.resource_plugin_id = " + resourcePluginId + " AND e.id = " + entityId);
                if ((dtRoleAction != null) && (dtRoleAction.Rows.Count > 0))
                {
                    foreach (DataRow drR in dtRoleAction.Rows)
                    {
                        pkg.pluginAction.Add(new PluginConnectorBaseDeployPackageAction(PluginActionType.Add, drR["role_name"].ToString(), drR["action_key"].ToString(), drR["action_add_value"].ToString(), (drR["additional_data"] != DBNull.Value ? drR["additional_data"].ToString() : null)));
                        //db.AddUserLog(LogKey.Role_Deploy, null, "Deploy", UserLogLevel.Info, proxyId, 0, 0, resourceId, pluginId, (Int64)dtEnt.Rows[0]["id"], (Int64)dtEnt.Rows[0]["identity_id"], "Role: " + drR["role_name"].ToString());
                        deployInfo.Add("role " + drR["role_name"].ToString());
                        deployText += "role " + drR["role_name"].ToString() + Environment.NewLine;
                    }
                }

                db.AddUserLog(LogKey.Role_Deploy, null, "Deploy", UserLogLevel.Info, proxyId, 0, 0, resourceId, pluginId, (Int64)dtEnt.Rows[0]["id"], (Int64)dtEnt.Rows[0]["identity_id"], "Identity addedd in deploy package with: " + String.Join(", ", deployInfo), deployText);
            }
            finally
            {
                if (deployInfo != null) deployInfo.Clear();
                deployInfo = null;

                deployText = "";
            }

            return pkg;
        }

        static private String ConvertoToString(DataColumn column, DataRow dr)
        {
            String ret = "";
            
            ret = dr[column.ColumnName].ToString();

            try
            {
                if (column.DataType == typeof(System.DateTime))
                {
                    ret = ((DateTime)dr[column.ColumnName]).ToString("o");
                    return ret;
                }
            }
            catch { }


            //Testa se é data e hora
            try
            {
                System.Globalization.CultureInfo cultureinfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                DateTime tmp = DateTime.Parse(dr[column.ColumnName].ToString(), cultureinfo);

                //se for uma data e hora válida retorna ela
                ret = tmp.ToString("o");
                return ret;
            }
            catch { }

            try
            {
                System.Globalization.CultureInfo cultureinfo = new System.Globalization.CultureInfo("pt-BR");
                DateTime tmp = DateTime.Parse(dr[column.ColumnName].ToString(), cultureinfo);

                //se for uma data e hora válida retorna ela
                ret = tmp.ToString("o");
                return ret;
            }
            catch { }

            try
            {
                System.Globalization.CultureInfo cultureinfo = new System.Globalization.CultureInfo("en-US");
                DateTime tmp = DateTime.Parse(dr[column.ColumnName].ToString(), cultureinfo);

                //se for uma data e hora válida retorna ela
                ret = tmp.ToString("o");
                return ret;
            }
            catch { }

            return ret;

        }

        static private String ComputeHash(HashAlgorithm alg, String text)
        {


            // Convert the input string to a byte array and compute the hash.
            byte[] data = alg.ComputeHash(Encoding.UTF8.GetBytes(text));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();

        }

    }
}
