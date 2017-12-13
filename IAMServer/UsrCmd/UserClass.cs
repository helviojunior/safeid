using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonBase;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using IAM.GlobalDefs;
using IAM.GlobalDefs.WebApi;
using System.Web.Script.Serialization;
using System.Security;

namespace UsrCmd
{
    class UserClass
    {
        public String UserName;
        public Int64 UserId;
        //public Int64 ContextId;
        public String UserLogin;
        public Uri server;
        
        public String authKey;
        private String username;
        private String password;

        public UserClass(Uri server, String username, String password)
        {
            this.UserName = null;
            this.UserLogin = null;
            this.UserId = 0;
            //this.ContextId = 0;
            
            this.server = server;
            this.username = username;
            this.password = password;

            this.authKey = GetAuthKey();
            if (authKey == "")
                Console.WriteLine("Erro ao recuperar a chave de autenticação");

        }
        
        public string clear()
        {
            this.UserName = null;
            this.UserLogin = null;
            this.UserId = 0;

            return "Seleção de usuário limpa";
        }

        public string search(String text)
        {
            if (authKey == "")
            {
                Console.WriteLine("Chave de autenticação inválida");
                return "";
            }

            var loginRequest = new
            {
                apiver = "1.0",
                method = "user.search",
                parameters = new
                {
                    name = text
                },
                auth = this.authKey,
                id = 1
            };

            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            SearchResult ret = JSON.JsonWebRequest<SearchResult>(server, jData, "application/json", null, "POST");
            if (ret == null)
            {
                Console.WriteLine("Retorno vazio");
                return "";
            } else if (ret.error != null)
            {
                Console.WriteLine(ret.error.data);
                Console.WriteLine(ret.error.message);
                return "";
            }
            else if (ret.result == null || ret.result.Count == 0)
            {
                Console.WriteLine("Nenhum usuário encontrado");
                return "";
            }

            Console.WriteLine(String.Format("{0,-6} {1,-30} {2,-30}", "ID", "Login", "Nome"));
            foreach (UserData user in ret.result)
                Console.WriteLine(String.Format("{0,-6} {1,-30} {2,-30}", user.userid, user.login, user.full_name));

            return "";
        }

        
        public string select(Int64 id)
        {
            if (authKey == "")
            {
                Console.WriteLine("Chave de autenticação inválida");
                return "";
            }

            var loginRequest = new
            {
                apiver = "1.0",
                method = "user.get",
                parameters = new
                {
                    userid = id
                },
                auth = this.authKey,
                id = 1
            };

            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            GetResult ret = JSON.JsonWebRequest<GetResult>(server, jData, "application/json", null, "POST");
            if (ret == null)
            {
                Console.WriteLine("Retorno vazio");
                return "";
            }
            else if (ret.error != null)
            {
                Console.WriteLine(ret.error.data);
                Console.WriteLine(ret.error.message);
                return "";
            }
            else if (ret.result == null || ret.result.info == null)
            {
                Console.WriteLine("Nenhum usuário encontrado com o ID " + id);
                return "";
            }

            this.UserId = (Int64)ret.result.info.userid;
            this.UserName = ret.result.info.full_name;
            this.UserLogin = ret.result.info.login;
            //this.ContextId = (Int64)dtUsers.Rows[0]["context_id"];

            return "Usuário selcionado: " + UserId + " --> " + UserName;
        }

        
        public string info()
        {
            if (UserId == 0)
                return "Nenhum usuário selecionado";

            if (authKey == "")
            {
                Console.WriteLine("Chave de autenticação inválida");
                return "";
            }

            var loginRequest = new
            {
                apiver = "1.0",
                method = "user.get",
                parameters = new
                {
                    userid = UserId
                },
                auth = this.authKey,
                id = 1
            };

            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            GetResult ret = JSON.JsonWebRequest<GetResult>(server, jData, "application/json", null, "POST");
            if (ret == null)
            {
                Console.WriteLine("Retorno vazio");
                return "";
            }
            else if (ret.error != null)
            {
                Console.WriteLine(ret.error.data);
                Console.WriteLine(ret.error.message);
                return "";
            }
            else if (ret.result == null || ret.result.info == null)
            {
                Console.WriteLine("Nenhum usuário encontrado com o ID " + UserId);
                return "";
            }


            String mask = "{0,-30} | {1,-" + (Console.WindowWidth - 30 - 10).ToString() + "}";
            Console.WriteLine(String.Format(mask, new String("-".ToCharArray()[0], 30), new String("-".ToCharArray()[0], (Console.WindowWidth - 30 - 10))));
            Console.WriteLine(String.Format(mask, "Propriedade", "Valor"));
            Console.WriteLine(String.Format(mask, new String("-".ToCharArray()[0], 30), new String("-".ToCharArray()[0], (Console.WindowWidth - 30 - 10))));

            Console.WriteLine(String.Format(mask, "Nome completo", ret.result.info.full_name));
            Console.WriteLine(String.Format(mask, "Login", ret.result.info.login));
            Console.WriteLine(String.Format(mask, "Bloqueado", ret.result.info.locked));
            Console.WriteLine(String.Format(mask, "Criado em", ((DateTime)DateTime.Now).AddSeconds(ret.result.info.create_date).ToString("yyyy-MM-dd HH:mm:ss")));
            Console.WriteLine(String.Format(mask, "Troca de senha", ((DateTime)DateTime.Now).AddSeconds(ret.result.info.change_password).ToString("yyyy-MM-dd HH:mm:ss")));
            Console.WriteLine(String.Format(mask, "Ultimo login", ((DateTime)DateTime.Now).AddSeconds(ret.result.info.last_login).ToString("yyyy-MM-dd HH:mm:ss")));

            Console.WriteLine("");

            if (ret.result.properties != null)
            {

                mask = "{0,-15} | {1,-30} | {2,-" + (Console.WindowWidth - 46 - 10).ToString() + "}";
                Console.WriteLine(String.Format(mask, new String("-".ToCharArray()[0], 15), new String("-".ToCharArray()[0], 30), new String("-".ToCharArray()[0], (Console.WindowWidth - 46 - 10))));
                Console.WriteLine(String.Format(mask, "Recurso", "Propriedade", "Valor"));
                Console.WriteLine(String.Format(mask, new String("-".ToCharArray()[0], 15), new String("-".ToCharArray()[0], 30), new String("-".ToCharArray()[0], (Console.WindowWidth - 46 - 10))));

                foreach (UserDataProperty p in ret.result.properties)
                    Console.WriteLine(String.Format(mask,  corte(p.resource_name, 15), p.name, corte(p.value, 28)));


                Console.WriteLine("");

            }

            //vw_entity_roles
            if (ret.result.roles != null)
            {

                mask = "{0,-15} | {1,-" + (Console.WindowWidth - 39 - 10).ToString() + "}";
                Console.WriteLine(String.Format(mask, new String("-".ToCharArray()[0], 6), new String("-".ToCharArray()[0], (Console.WindowWidth - 39 - 10))));
                Console.WriteLine(String.Format(mask, "Recurso", "Perfil"));
                Console.WriteLine(String.Format(mask, new String("-".ToCharArray()[0], 6), new String("-".ToCharArray()[0], (Console.WindowWidth - 39 - 10))));

                foreach (UserDataRole r in ret.result.roles)
                    Console.WriteLine(String.Format(mask, corte(r.name, 15), corte(r.name, 30)));

            }

            return "";
        }

        
        public string logs()
        {
            if (UserId == 0)
                return "Nenhum usuário selecionado";

            if (authKey == "")
            {
                Console.WriteLine("Chave de autenticação inválida");
                return "";
            }

            var loginRequest = new
            {
                apiver = "1.0",
                method = "user.logs",
                parameters = new
                {
                    userid = UserId,
                    page_size = 100
                },
                auth = this.authKey,
                id = 1
            };

            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            Logs ret = JSON.JsonWebRequest<Logs>(server, jData, "application/json", null, "POST");
            if (ret == null)
            {
                Console.WriteLine("Retorno vazio");
                return "";
            }
            else if (ret.error != null)
            {
                Console.WriteLine(ret.error.data);
                Console.WriteLine(ret.error.message);
                return "";
            }
            else if (ret.result == null || ret.result.info == null)
            {
                Console.WriteLine("Nenhum usuário encontrado com o ID " + UserId);
                return "";
            }


            String mask = "{0,-10} | {1,-5} | {2,-18} | {3,-" + (Console.WindowWidth - 39 - 10).ToString() + "}";
            Console.WriteLine(String.Format(mask, new String("-".ToCharArray()[0], 10), new String("-".ToCharArray()[0], 5), new String("-".ToCharArray()[0], 18), new String("-".ToCharArray()[0], (Console.WindowWidth - 39 - 10))));
            Console.WriteLine(String.Format(mask, "Source", "Level", "Data", "Text"));
            Console.WriteLine(String.Format(mask, new String("-".ToCharArray()[0], 10), new String("-".ToCharArray()[0], 5), new String("-".ToCharArray()[0], 18), new String("-".ToCharArray()[0], (Console.WindowWidth - 39 - 10))));

            foreach (LogItem l in ret.result.logs)
                Console.WriteLine(String.Format(mask, corte(l.source, 10), corte(((UserLogLevel)(l.level)).ToString(), 4), ((DateTime)DateTime.Now.AddSeconds(l.date)).ToString("yyyy-MM-dd HH:mm"), corte(l.text, 30)));

            return "";
        }

        
        public string resetpwd()
        {
            if (UserId == 0)
                return "Nenhum usuário selecionado";

            if (authKey == "")
            {
                Console.WriteLine("Chave de autenticação inválida");
                return "";
            }


            if (Confirm("Confirma reset de senha?"))
            {

                var loginRequest = new
                {
                    apiver = "1.0",
                    method = "user.changepassword",
                    parameters = new
                    {
                        userid = UserId,
                        password = IAM.Password.RandomPassword.Generate(16)
                    },
                    auth = this.authKey,
                    id = 1
                };

                JavaScriptSerializer _ser = new JavaScriptSerializer();
                String jData = _ser.Serialize(loginRequest);

                Logs ret = JSON.JsonWebRequest<Logs>(server, jData, "application/json", null, "POST");
                if (ret == null)
                {
                    Console.WriteLine("Retorno vazio");
                    return "";
                }
                else if (ret.error != null)
                {
                    Console.WriteLine(ret.error.data);
                    Console.WriteLine(ret.error.message);
                    return "";
                }
                else if (ret.result == null || ret.result.info == null)
                {
                    Console.WriteLine("Nenhum usuário encontrado com o ID " + UserId);
                    return "";
                }


                return "Senha redefinida com sucesso";

            }
            else
            {
                return "";
            }

            
        }


        public string unlock()
        {
            if (UserId == 0)
                return "Nenhum usuário selecionado";

            if (authKey == "")
            {
                Console.WriteLine("Chave de autenticação inválida");
                return "";
            }



            if (Confirm("Confirma desbloqueio do usuário?"))
            {
                var loginRequest = new
                {
                    apiver = "1.0",
                    method = "user.unlock",
                    parameters = new
                    {
                        userid = UserId
                    },
                    auth = this.authKey,
                    id = 1
                };

                JavaScriptSerializer _ser = new JavaScriptSerializer();
                String jData = _ser.Serialize(loginRequest);

                Logs ret = JSON.JsonWebRequest<Logs>(server, jData, "application/json", null, "POST");
                if (ret == null)
                {
                    Console.WriteLine("Retorno vazio");
                    return "";
                }
                else if (ret.error != null)
                {
                    Console.WriteLine(ret.error.data);
                    Console.WriteLine(ret.error.message);
                    return "";
                }

            }

            return "Usuário desbloqueado com sucesso";
        }

        public string corte(String text, Int32 len)
        {
            if (text.Length < len)
                return text;

            return text.Substring(0, len);
        }


        public static Boolean Confirm(String text)
        {
            String cmd = "";
            do
            {
                Console.Write(text + " [Y/N]: ");
                cmd = Console.ReadLine();
            } while ((cmd.ToLower() != "y") && (cmd.ToLower() != "n"));

            if (cmd.ToLower() == "y")
                return true;

            return false;
        }


        private String GetAuthKey()
        {
            String authKey = "";

            //Efetua o login
            var loginRequest = new
            {
                apiver = "1.0",
                method = "user.login",
                parameters = new
                {
                    user = username,
                    password = password,
                    userData = true //Define se deseja ou não retornar os principais dados do usuário
                },
                id = 1
            };

            JavaScriptSerializer _ser = new JavaScriptSerializer();
            String jData = _ser.Serialize(loginRequest);

            AuthResult ret = JSON.JsonWebRequest<AuthResult>(server, jData, "application/json", null, "POST");
            if (ret == null)
            {
                Console.WriteLine("Retorno vazio");
                return "";
            }
            else if (ret.error != null)
            {
                Console.WriteLine(ret.error.data);
                Console.WriteLine(ret.error.message);
                return "";
            }
            else if (!String.IsNullOrWhiteSpace(ret.result.sessionid))
            {
                authKey = ret.result.sessionid;
            }

            return authKey;
        }



    }

}
