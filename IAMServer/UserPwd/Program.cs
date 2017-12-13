using System;
using System.Collections.Generic;
using System.Text;
using IAM.Config;
using IAM.CA;
using IAM.LocalConfig;
using IAM.SQLDB;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using IAM.Password;
using IAM.GlobalDefs;

namespace UserPwd
{
    class Program
    {
        static void Main(string[] args)
        {


            ServerLocalConfig localConfig = new ServerLocalConfig();
            localConfig.LoadConfig();

            if ((localConfig.SqlServer == null) || (localConfig.SqlServer.Trim() == ""))
                StopOnError("Parâmetro 'sqlserver' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlDb == null) || (localConfig.SqlDb.Trim() == ""))
                StopOnError("Parâmetro 'sqldb' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlUsername == null) || (localConfig.SqlUsername.Trim() == ""))
                StopOnError("Parâmetro 'sqlusername' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlPassword == null) || (localConfig.SqlPassword.Trim() == ""))
                StopOnError("Parâmetro 'sqlpassword' não localizado no arquivo de configuração 'server.conf'", null);


            /*************
             * Gera os certificados do servidor
             */
            MSSQLDB db = new MSSQLDB(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
            db.openDB();
            db.Timeout = 300;

            
            Int64 entityId = 0;

            if (args.Length > 0)
                Int64.TryParse(args[0], out entityId);

            DataTable tmp = db.Select(String.Format("select e.*, e1.id enterprise_id from entity e inner join context c on c.id = e.context_id inner join enterprise e1 on e1.id = c.enterprise_id where e.id = {0}", entityId));

            if (tmp == null)
                StopOnError("Select is null", null);

            if (tmp.Rows.Count == 0)
                StopOnError("Select is empty", null);

            EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.conn, (Int64)tmp.Rows[0]["entity_id"]);

            Int64 context = (Int64)tmp.Rows[0]["context_id"];
            Int64 enterpriseId = (Int64)tmp.Rows[0]["enterprise_id"];

            Console.WriteLine("##############################");
            Console.WriteLine("C Pwd: " + tmp.Rows[0]["password"].ToString());

            Console.WriteLine("");
            Console.WriteLine("##############################");
            using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(tmp.Rows[0]["password"].ToString())))
                Console.WriteLine("Pwd: " + Encoding.UTF8.GetString(cApi.clearData));


            String text = "";
            do
            {
                //Console.Clear();
                Console.Write("Deseja redefinir a senha do usuário? (Y/N): ");
                text = Console.ReadLine().Trim();
                if (text.ToLower() == "y")
                {
                    break;
                }
                else if (text.ToLower() == "n")
                {
                    text = "";
                    break;
                }
                else
                {
                    text = "";
                }
            } while (text == "");

            if (text.ToLower() == "y")
                BuildPassword(db, null, context, entityId, enterpriseId);

            db.closeDB();

            StopOnError("", null);
        }


        public static void BuildPassword(MSSQLDB db, SqlTransaction trans, Int64 context, Int64 entityId, Int64 enterpriseId)
        {

            String pwdMethod = "random";
            String pwdValue = "";

            using (DataTable dtRules = db.Select("select password_rule from context c where c.id = " + context + " and (c.password_rule is not null and rtrim(LTRIM(c.password_rule)) <> '')", trans))
            {
                if ((dtRules != null) && (dtRules.Rows.Count > 0))
                {
                    String v = dtRules.Rows[0]["password_rule"].ToString().Trim();

                    if (v.IndexOf("[") != -1)
                    {
                        Regex rex = new Regex(@"(.*?)\[(.*?)\]");
                        Match m = rex.Match(v);
                        if (m.Success)
                        {
                            pwdMethod = m.Groups[1].Value.ToLower();
                            pwdValue = m.Groups[2].Value;
                        }
                    }
                    else
                    {
                        pwdMethod = v;
                    }
                }
            }

            switch (pwdMethod)
            {
                case "default":
                    //Nada a senha ja foi definida
                    break;

                case "field":
                    throw new NotImplementedException();
                    /*
                    Int64 fieldId = 0;
                    Int64.TryParse(pwdValue, out fieldId);
                    using (DataTable dtFields = db.Select("select * from identity_field where identity_id = " + this.IdentityId + " and field_id = " + fieldId, trans))
                        if ((dtFields != null) && (dtFields.Rows.Count > 0))
                        {
                            pwdValue = dtFields.Rows[0]["value"].ToString();
                        }*/
                    break;

                default: //Random
                    pwdValue = "";
                    break;
            }

            //Se a senha continua vazia, gera uma randômica
            if ((pwdValue == null) || (pwdValue == ""))
                pwdValue = RandomPassword.Generate(14, 16);

            Boolean MustChangePassword = true;

            String pwd = "";
            using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.conn, enterpriseId, trans))

            using (CryptApi cApi = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(pwdValue)))
                pwd = Convert.ToBase64String(cApi.ToBytes());


            String sql = "update entity set password = @password, change_password = getdate(), must_change_password = @must where id = @entityId";

            SqlParameterCollection par = GetSqlParameterObject();
            par.Add("@entityId", SqlDbType.BigInt).Value = entityId;

            par.Add("@password", SqlDbType.VarChar, pwd.Length).Value = pwd;
            par.Add("@must", SqlDbType.Bit).Value = MustChangePassword;

            db.AddUserLog(LogKey.User_PasswordChanged, null, "Engine", UserLogLevel.Info, 0, 0, context, 0, 0, entityId, 0, "Password changed", "", trans);

            db.ExecuteNonQuery(sql, CommandType.Text, par, trans);

        }


        private static void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
            }

            Console.WriteLine("Pressione ENTER para finalizar");
            Console.ReadLine();
        }
    }
}
