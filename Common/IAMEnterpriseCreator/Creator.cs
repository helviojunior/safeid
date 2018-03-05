using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Data;
using System.Data.SqlClient;
using IAM.Config;
using IAM.CA;
//using IAM.SQLDB;
using IAM.GlobalDefs;
using SafeTrend.Data;

namespace IAM.EnterpriseCreator
{
    public class Creator
    {
        private IAMDatabase db;
        private String name;
        private String fqdn;
        private String language;
        private String ServerPKCS12Cert;
        private String ServerCert;
        private String ClientPKCS12Cert;
        private Boolean firstEnterprise = true;

        public Creator(IAMDatabase db, String name, String fqdn, String language)
        {

            //Test FQDN
            try
            {
                Uri tmp = new Uri("http://" + fqdn + "/");
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid FQDN", ex);
                return;
            }

            //Test language
            try
            {
                CultureInfo ci = new CultureInfo(language);
            }
            catch(Exception ex) {
                throw new Exception("Invalid language", ex);
                return;
            }


            //Tudo ok podemos continuar
            this.name = name;
            this.fqdn = fqdn;
            this.language = language;
            this.db = db;
        }

        public void BuildCertificates()
        {
            //Cria os certificados digitais

            //firstEnterprise
            Int64 enterpriseCount = db.ExecuteScalar<Int64>("select count(*) from [enterprise]", CommandType.Text, null, null);
            if (enterpriseCount > 0)
                firstEnterprise = false;

            EnterpriseKey ent = new EnterpriseKey(new Uri("//" + this.fqdn), this.name, firstEnterprise);
            ent.BuildCerts(); //Cria os certificados

            this.ServerPKCS12Cert = ent.ServerPKCS12Cert;
            this.ServerCert = ent.ServerCert;
            this.ClientPKCS12Cert = ent.ClientPKCS12Cert;

        }

        public void Commit()
        {
            //Grava as informações no banco de dados
            SqlTransaction trans = db.Connection.BeginTransaction();
            try
            {
                //Cria a empresa
                DbParameterCollection par = new DbParameterCollection();
                par.Add("@name", typeof(String), this.name.Length).Value = this.name;
                par.Add("@fqdn", typeof(String), this.fqdn.Length).Value = this.fqdn;
                par.Add("@server_pkcs12_cert", typeof(String), this.ServerPKCS12Cert.Length).Value = this.ServerPKCS12Cert;
                par.Add("@server_cert", typeof(String), this.ServerCert.Length).Value = this.ServerCert;
                par.Add("@client_pkcs12_cert", typeof(String), this.ClientPKCS12Cert.Length).Value = this.ClientPKCS12Cert;
                par.Add("@language", typeof(String), this.language.Length).Value = this.language;
                par.Add("@auth_plugin", typeof(String)).Value = "auth://iam/plugins/internal";

                Int64 enterpriseId = db.ExecuteScalar<Int64>("sp_new_enterprise", CommandType.StoredProcedure, par, trans);


                //Insere os campos padrões da empresa
                par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@field_name", typeof(String)).Value = "Nome";
                par.Add("@data_type", typeof(String)).Value = "String";
                par.Add("@public", typeof(Boolean)).Value = false;
                par.Add("@user", typeof(Boolean)).Value = false;

                DataTable dtField = db.ExecuteDataTable("[sp_new_field]", CommandType.StoredProcedure, par, trans);
                Int64 nameFieldId = (Int64)dtField.Rows[0]["id"];

                par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@field_name", typeof(String)).Value = "Login";
                par.Add("@data_type", typeof(String)).Value = "String";
                par.Add("@public", typeof(Boolean)).Value = false;
                par.Add("@user", typeof(Boolean)).Value = false;
                dtField = db.ExecuteDataTable("[sp_new_field]", CommandType.StoredProcedure, par, trans);
                Int64 loginFieldId = (Int64)dtField.Rows[0]["id"];

                par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@field_name", typeof(String)).Value = "E-mail";
                par.Add("@data_type", typeof(String)).Value = "String";
                par.Add("@public", typeof(Boolean)).Value = false;
                par.Add("@user", typeof(Boolean)).Value = false;
                db.ExecuteNonQuery("[sp_new_field]", CommandType.StoredProcedure, par, trans);

                par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@field_name", typeof(String)).Value = "Senha";
                par.Add("@data_type", typeof(String)).Value = "String";
                par.Add("@public", typeof(Boolean)).Value = false;
                par.Add("@user", typeof(Boolean)).Value = false;
                db.ExecuteNonQuery("[sp_new_field]", CommandType.StoredProcedure, par, trans);


                //Cria o contexto
                par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@name", typeof(String), 7).Value = "Default";
                par.Add("@password_rule", typeof(String), 15).Value = "default[123456]";
                par.Add("@pwd_length", typeof(Int32)).Value = 8;
                par.Add("@pwd_upper_case", typeof(Boolean)).Value = true;
                par.Add("@pwd_lower_case", typeof(Boolean)).Value = true;
                par.Add("@pwd_digit", typeof(Boolean)).Value = true;
                par.Add("@pwd_symbol", typeof(Boolean)).Value = true;
                par.Add("@pwd_no_name", typeof(Boolean)).Value = true;

                Int64 contextId = db.ExecuteScalar<Int64>("sp_new_context", CommandType.StoredProcedure, par, trans);


                //Cria a role de sistema de administrador desta empresa
                par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@name", typeof(String)).Value = "Enterprise Admin";
                par.Add("@system_admin", typeof(Boolean)).Value = false;
                par.Add("@enterprise_admin", typeof(Boolean)).Value = true;

                Int64 sysRoleId = db.ExecuteScalar<Int64>("sp_new_sys_role", CommandType.StoredProcedure, par, trans);
                

                //Cria o usuário administrador
                par = new DbParameterCollection();
                par.Add("@context_id", typeof(Int64)).Value = contextId;
                par.Add("@alias", typeof(String)).Value = "Admin";
                par.Add("@login", typeof(String)).Value = "admin";
                par.Add("@full_name", typeof(String)).Value = "Admin";

                using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.Connection, enterpriseId, trans))
                using (CryptApi cApi = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes("123456")))
                    par.Add("@password", typeof(String)).Value = Convert.ToBase64String(cApi.ToBytes());

                Int64 entityId = db.ExecuteScalar<Int64>("sp_new_entity", CommandType.StoredProcedure, par, trans);

                
                //Vincula o usuário na role de sistema como enterprise admin
                db.ExecuteNonQuery("insert into sys_entity_role (entity_id, role_id) values(" + entityId + "," + sysRoleId + ")", CommandType.Text, null, trans);

                //Cria informação na tabela entity_field para o usuário poder aparecer nas consultas
                db.ExecuteNonQuery("insert into entity_field (entity_id, field_id, value) values(" + entityId + "," + nameFieldId + ",'Admin')", CommandType.Text, null, trans);
                db.ExecuteNonQuery("insert into entity_field (entity_id, field_id, value) values(" + entityId + "," + loginFieldId + ",'admin')", CommandType.Text, null, trans);

                //Cria o usuário de integração do CAS
                par = new DbParameterCollection();
                par.Add("@context_id", typeof(Int64)).Value = contextId;
                par.Add("@alias", typeof(String)).Value = "Integração CAS";
                par.Add("@login", typeof(String)).Value = "integracao.cas";
                par.Add("@full_name", typeof(String)).Value = "Integração CAS";

                using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.Connection, enterpriseId, trans))
                using (CryptApi cApi = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes("123456")))
                    par.Add("@password", typeof(String)).Value = Convert.ToBase64String(cApi.ToBytes());

                Int64 casEntityId = db.ExecuteScalar<Int64>("sp_new_entity", CommandType.StoredProcedure, par, trans);

                //Vincula o usuário na role de sistema como enterprise admin
                db.ExecuteNonQuery("insert into sys_entity_role (entity_id, role_id) values(" + casEntityId + "," + sysRoleId + ")", CommandType.Text, null, trans);

                //Cria informação na tabela entity_field para o usuário poder aparecer nas consultas
                db.ExecuteNonQuery("insert into entity_field (entity_id, field_id, value) values(" + casEntityId + "," + nameFieldId + ",'Admin')", CommandType.Text, null, trans);
                db.ExecuteNonQuery("insert into entity_field (entity_id, field_id, value) values(" + casEntityId + "," + loginFieldId + ",'admin')", CommandType.Text, null, trans);

                //Cria as regras padrões de criação de login
                db.ExecuteNonQuery("INSERT INTO [login_rule]([context_id],[name],[rule],[order]) VALUES (" + contextId + ",'First name, lastname','first_name,dot,last_name',1)", CommandType.Text, null, trans);
                db.ExecuteNonQuery("INSERT INTO [login_rule]([context_id],[name],[rule],[order]) VALUES (" + contextId + ",'Fistname, second name','first_name,dot,second_name',2)", CommandType.Text, null, trans);
                db.ExecuteNonQuery("INSERT INTO [login_rule]([context_id],[name],[rule],[order]) VALUES (" + contextId + ",'First name, last name, index','first_name,dot,last_name,index',3)", CommandType.Text, null, trans);

                //Cria as regras padrões de criação de e-mail
                db.ExecuteNonQuery("INSERT INTO [st_mail_rule]([context_id],[name],[rule],[order]) VALUES (" + contextId + ",'First name, lastname','first_name,dot,last_name',1)", CommandType.Text, null, trans);
                db.ExecuteNonQuery("INSERT INTO [st_mail_rule]([context_id],[name],[rule],[order]) VALUES (" + contextId + ",'Fistname, second name','first_name,dot,second_name',2)", CommandType.Text, null, trans);
                db.ExecuteNonQuery("INSERT INTO [st_mail_rule]([context_id],[name],[rule],[order]) VALUES (" + contextId + ",'First name, last name, index','first_name,dot,last_name,index',3)", CommandType.Text, null, trans);

                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw ex;
            }

        }
    }
}
