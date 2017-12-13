using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonBase;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using IAM.GlobalDefs;
using System.Web.Script.Serialization;
using ExpressionEvaluator;
using System.Security;

namespace UsrCmd
{
    
    class Program
    {
        static Boolean exec = true;
        static UserClass u;
        static void Main(string[] args)
        {

            
            do
            {
                String username = "";
                do
                {
                    Console.Write("Digite seu usuário: ");
                    username = Console.ReadLine();
                } while (String.IsNullOrWhiteSpace(username));

                String pwd = null;
                do
                {
                    Console.Write("Digite sua senha: ");
                    pwd = getPassword();
                } while (pwd.Length == 0);

                Console.WriteLine("");

                u = new UserClass(new Uri("http://im.fael.edu.br/api/json.aspx"), username, pwd);
                if (String.IsNullOrWhiteSpace(u.authKey))
                {
                    Console.WriteLine("Erro na autenticação");
                    Console.WriteLine("");
                }

            } while (String.IsNullOrWhiteSpace(u.authKey));

            StartCmd();
        }

        private static String getPassword()
        {
            List<String> pwd = new List<String>();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    pwd.RemoveAt(pwd.Count - 1);
                    Console.Write("\b \b");
                }
                else
                {
                    pwd.Add(i.KeyChar.ToString());
                    Console.Write("*");
                }
            }
            return String.Join("", pwd);
        }

        static void StartCmd()
        {
            
            do
            {
                Console.Write("SafeId> ");
                String cmd = Console.ReadLine();

                try
                {
                    switch (cmd.ToLower())
                    {
                        case "?":
                        case "help":
                            Dictionary<String, String> cmds = new Dictionary<string, string>();
                            cmds.Add("quit", "Sai do programa");
                            cmds.Add("cls", "Sai do programa");
                            cmds.Add("u.clear", "Limpa a seleção do usuário");
                            cmds.Add("u.search('nome ou login')", "Busca usuários");
                            cmds.Add("u.select(user_id)", "Seleciona usuários");
                            cmds.Add("u.info", "Lista as informações do usuário selecionado");
                            cmds.Add("u.resetpwd", "Redefine a senha para o padrão selecionado");
                            cmds.Add("u.unlock", "Desbloqueia o usuário");
                            cmds.Add("u.log", "Ultomos 100 logs gerados para este usuário");

                            foreach (String k in cmds.Keys)
                                Console.WriteLine(String.Format("{0,-30} {1,-" + (Console.WindowWidth - 30 - 10).ToString() + "}", k, cmds[k]));
                            break;

                        case "cls":
                            Console.Clear();
                            break;

                        case "":
                            //Nada
                            break;

                        case "quit":
                        case "exit":
                            exec = false;
                            Console.WriteLine("Saindo...");
                            break;

                        default:
                            var p = new CompiledExpression(cmd.ToLower());
                            p.RegisterType("u", u);
                            p.RegisterDefaultTypes();
                            p.Parse();
                            p.Compile();
                            Object ret = p.Eval();
                            Console.WriteLine(ret);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro: " + ex.Message);
                }

            } while (exec);
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
