/// 
/// @file WebPageAPI.cs
/// <summary>
/// Implementações da classe WebPageAPI. 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 24/10/2013
/// $Id: WebPageAPI.cs, v1.0 2013/10/24 Helvio Junior $


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using SafeTrend.WebAPI;
using System.Web;
using System.Web.UI;
using IAM.GlobalDefs;
using System.Data;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.WebAPI
{
    /// <summary>
    /// Esta tem o perfil se ser uma classe genérica para ser utilizada em qualquer parte da console web.
    /// A arquitetura do sistema define que tanto as consoles de administração web, quanto a API propriamente dita utilizam internamente a classe CJSONrpc.cs
    /// Desta forma há somente um ponto de código para gerenciar e garante que a API e a console web estão sempre atualizadas
    /// Esta classe contém somente métodos státicos
    /// </summary>
    public static class WebPageAPI
    {
        /// <summary>
        /// Cria uma nova instância do 'delegate' para a autenticação externa
        /// O delegate realiza a verificação da autenticação do usuário e controle de permissão através do RBAC
        /// </summary>
        /// <param name="conn">Conexão com banco de dados MS-SQL</param>
        /// <param name="page">Página na qual a requisição foi iniciada</param>
        /// <param name="enterpriseId">ID da empresa</param>
        private static ExternalAccessControl GetDelegateInstance(DbBase database, Page page, Int64 enterpriseId)
        {
            ExternalAccessControl eAuth = new ExternalAccessControl(delegate(String method, String auth, AccessControl preCtrl, Dictionary<String, Object> parameters)
            {
                //Para efeitos de teste vou sempre retornar true
                //return true;

                LoginData login = null;

                if ((page.Session["login"] != null) && (page.Session["login"] is LoginData))
                    login = (LoginData)page.Session["login"];


                //Conceitualmente o usuário pode realizar as operações com o seu próprio usuário sem necessitar de permissão no RBAC
                //Operações com troca de senha, logs e etc...
                //Essa prerrogativa vale somente para os métodos "user."
                if ((!String.IsNullOrWhiteSpace(method)) && (parameters != null) && (method.ToLower().Split('.')[0] == "user") && (parameters.ContainsKey("userid")))
                {
                    String user = parameters["userid"].ToString();
                    if (!String.IsNullOrWhiteSpace(user))
                    {

                        Int64 userid = 0;
                        try
                        {
                            userid = Int64.Parse(user);

                            if (login.Id == userid)
                                return new AccessControl(login.Id, true);
                        }
                        catch { }
                    }
                }

                if ((!preCtrl.Result) && (login != null))
                {
                    using(IAMRBAC rbac = new IAMRBAC())
                        return new AccessControl(login.Id, rbac.UserCan(database, login.Id, enterpriseId, "admin", method));
                }
                else
                {
                    return preCtrl;
                }
            });

            return eAuth;
        }

        
        /// <summary>
        /// Método utilizado para execução interna através da console de administração
        /// </summary>
        /// <param name="conn">Conexão com banco de dados MS-SQL</param>
        /// <param name="page">Página na qual a requisição foi iniciada</param>
        /// <param name="jRequest">Texto no formato JSON da requisição</param>
        public static String ExecuteLocal(DbBase database, Page page, String jRequest)
        {
            return ExecuteLocal(database, page, jRequest, null);
        }
        
        /// <summary>
        /// Método utilizado para execução interna através da console de administração
        /// </summary>
        /// <param name="conn">Conexão com banco de dados MS-SQL</param>
        /// <param name="page">Página na qual a requisição foi iniciada</param>
        /// <param name="jRequest">Texto no formato JSON da requisição</param>
        public static String ExecuteLocal(DbBase database,  Page page, String jRequest, ExecutionLog logDelegate)
        {
            try
            {

                ExecutionLog eLogs = new ExecutionLog(delegate(Boolean success, Int64 enterpriseIdLog, String method, AccessControl acl, String jRequestLog, String jResponseLog)
                {

                    if (!success)
                    {
                        using (IAMDatabase db = (IAMDatabase)database)
                            db.AddUserLog(LogKey.Debug, null, "API", UserLogLevel.Debug, 0, enterpriseIdLog, 0, 0, 0, 0, 0, "API Call (" + method + "). Result success? " + success, "{\"Request\":" + jRequestLog + ", \"Response\":" + jResponseLog + "}", 0, null);
                    }

                    if (logDelegate != null)
                        logDelegate(success, enterpriseIdLog, method, acl, jRequestLog, jResponseLog);

                });


                Int64 enterpriseId = 0;
                if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                    enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

                CJSONrpc jsonRpc = new CJSONrpc(database, jRequest, enterpriseId);

                ExternalAccessControl eAuth = GetDelegateInstance(database, page, enterpriseId);
                
                jsonRpc.ExternalAccessControl += eAuth;
                jsonRpc.ExecutionLog += eLogs;
                String ret = jsonRpc.Execute();
                jsonRpc.ExternalAccessControl -= eAuth;
                jsonRpc.ExecutionLog -= eLogs;

                eAuth = null;

                return ret;
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
            }
        }

        
        /// <summary>
        /// Método utilizado pela API externa, este método interage diretamente com o page.Request e page.Response
        /// </summary>
        /// <param name="conn">Conexão com banco de dados MS-SQL</param>
        /// <param name="page">Página na qual a requisição foi iniciada</param>
        public static void Execute(DbBase database, Page page)
        {
            Execute(database, page, null);
        }

        /// <summary>
        /// Método utilizado pela API externa, este método interage diretamente com o page.Request e page.Response
        /// </summary>
        /// <param name="conn">Conexão com banco de dados MS-SQL</param>
        /// <param name="page">Página na qual a requisição foi iniciada</param>
        public static void Execute(DbBase database, Page page, ExecutionLog logDelegate)
        {
            //Checa se o content type está adequado
            Dictionary<String, String> allowed_content = new Dictionary<String, String>();

            allowed_content.Add("application/json-rpc", "json-rpc");
            allowed_content.Add("application/json", "json-rpc");
            allowed_content.Add("application/jsonrequest", "json-rpc");

            //Permite somente o método POST
            if (page.Request.HttpMethod != "POST")
            {
                page.Response.Status = "412 Precondition Failed";
                page.Response.StatusCode = 412;
                page.Response.End();
                return;
            }

            String contentType = page.Request.ContentType.ToLower().Trim().Split(";".ToCharArray())[0];

            //Permite somente quando o ContentType estiver na listagem definida
            if (!allowed_content.ContainsKey(contentType))
            {
                page.Response.Status = "412 Precondition Failed";
                page.Response.StatusCode = 412;
                page.Response.End();
                return;
            }

            //Verifica se este IP está bloqueado, se sim rejeita a conexão
            /*if (dsfdsafsd)
            {
                page.Response.Status = "403 Access denied";
                page.Response.StatusCode = 403;
                page.Response.End();
                return;
            }*/


            if (allowed_content[contentType] == "json-rpc")
            {
                page.Response.ContentType = "application/json; charset=UTF-8";
                page.Response.ContentEncoding = Encoding.UTF8;

                try
                {
                    
                    using (Stream stm = page.Request.InputStream)
                    using (StreamReader reader = new StreamReader(stm, Encoding.UTF8))
                    {
                        String rData = reader.ReadToEnd();

                        Int64 enterpriseId = 0;
                        if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                            enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

                        CJSONrpc jsonRpc = new CJSONrpc(database, rData, enterpriseId);

                        ExternalAccessControl eAuth = GetDelegateInstance(database, page, enterpriseId);

                        jsonRpc.ExternalAccessControl += eAuth;
                        if (logDelegate != null)
                            jsonRpc.ExecutionLog += logDelegate;
                        String ret = jsonRpc.Execute();
                        jsonRpc.ExternalAccessControl -= eAuth;
                        if (logDelegate != null)
                            jsonRpc.ExecutionLog -= logDelegate;

                        Byte[] bRet = Encoding.UTF8.GetBytes(ret);

                        page.Response.Status = "200 OK";
                        page.Response.StatusCode = 200;
                        page.Response.OutputStream.Write(bRet, 0, bRet.Length);
                        page.Response.OutputStream.Flush();
                    }
                }
                catch (Exception ex)
                {
                    page.Response.Status = "500 Internal Error";
                    page.Response.StatusCode = 500;
                }
                finally
                {
                }
            }
        }
    }
}
