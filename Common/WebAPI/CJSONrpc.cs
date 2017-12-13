/// 
/// @file CJSONrpc.cs
/// <summary>
/// Implementações da classe CJSONrpc
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 15/10/2013
/// $Id: CJSONrpc.cs, v1.0 2013/10/15 Helvio Junior $

using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Linq;
using System.Text;
using SafeTrend.Data;

namespace SafeTrend.WebAPI
{
    /// <summary>
    /// Definição do delegate de log de execução
    /// </summary>
    public delegate void ExecutionLog(Boolean success, Int64 enterpriseId, String method, AccessControl acl, String jRequest, String jResponse);

    /// <summary>
    /// Classe que recebe a requisição no padrão JSON, verifica todos os 'Assembly' desta dll que sejam filhos da classe APIBase e executa o método chamado
    /// </summary>
    public class CJSONrpc
    {
        private class ControllerCollection
        {
            private List<Controller> _controllers;

            public List<Controller> Items { get { return _controllers; } }

            public ControllerCollection()
            {
                this._controllers = new List<Controller>();
            }

            public void Add(Controller c)
            {
                this._controllers.Add(c);
            }

            public void SetResponse(String id, Dictionary<String, Object> Response, Boolean success)
            {
                Controller c = _controllers.Find(c1 => (c1.Request.ContainsKey("id") && c1.Request["id"].ToString().ToLower() == id.ToLower()));
                if (c != null)
                {
                    c.Response = Response;
                    c.Success = success;
                }
                else if ((_controllers.Count == 1) && (String.IsNullOrWhiteSpace(id)))
                {
                    _controllers[0].Response = Response;
                    _controllers[0].Success = success;
                }
            }

            public Dictionary<String, Object> GetSingle()
            {
                if (_controllers.Count > 0)
                    return _controllers[0].Response;
                else
                    return new Dictionary<string, object>();
            }

            public List<Dictionary<String, Object>> GetMultiCall()
            {
                List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
                foreach (Controller c in _controllers)
                    if (c.Response != null) ret.Add(c.Response);

                return ret;
            }

        }

        private class Controller
        {
            public Dictionary<String, Object> Request;
            public Dictionary<String, Object> Response;
            public AccessControl acl;
            public Boolean Success;
            public String Method;

            public Controller(Dictionary<String, Object> request)
            {
                this.Request = request;
                this.Success = false;
                this.Response = new Dictionary<string, object>();
                this.Method = "";

                try
                {
                    if (request.ContainsKey("method"))
                        Method = request["method"].ToString();
                }
                catch { }
            }
        }

        private String _version = "1.0";
        private String _rData;
        private Int64 _enterpriseId;
        private JavaScriptSerializer _ser;

        private Boolean _multicall;
	    private Boolean _error;
        private DbBase _database;

        private ControllerCollection _controllers;

        //private List<Dictionary<String, Object>> _mResponse;
        //private Dictionary<String, Object> _sResponse;

        public event ExternalAccessControl ExternalAccessControl;
        public event ExecutionLog ExecutionLog;

        public Boolean IsError { get { return _error; } }

        /// <summary>
        /// Construtor da classe
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="requestData">Texto no formato JSON da requisição</param>
        /// <param name="enterpriseId">ID da empresa</param>
        public CJSONrpc(DbBase database, String requestData, Int64 enterpriseId)
        {

            if (String.IsNullOrEmpty(requestData))
                throw new NullReferenceException("requestData is null or empty");

            this._rData = requestData;
            this._enterpriseId = enterpriseId;
            //this._sResponse = new Dictionary<String, Object>();
            this._database = database;
            //this._mResponse = new List<Dictionary<String, Object>>();
            this._ser = new JavaScriptSerializer();

            this._controllers = new ControllerCollection();
        }

        /// <summary>
        /// Executa a operação solicitada pela requisição passada no contrutor
        /// </summary>
        public String Execute()
        {
            do
            {
                //Testa parse com multi-call
                
                try
                {
                    Dictionary<String, Object> request = this._ser.Deserialize<Dictionary<String, Object>>(this._rData);
                    //requests.Add(request);
                    _controllers.Add(new Controller(request));

                    this._multicall = false;
                }
                catch
                {
                    //Testa parse com multi-call
                    List<Dictionary<String, Object>> requests = new List<Dictionary<String, Object>>();
                    try
                    {
                        requests = this._ser.Deserialize<List<Dictionary<String, Object>>>(this._rData);
                        this._multicall = true;

                        foreach(Dictionary<String, Object> r in requests)
                            _controllers.Add(new Controller(r));
                    }
                    catch (Exception ex)
                    {
                        _setError("", ErrorType.ParseError, "", "", null, true);
                        break;
                    }
                }
                
                if (this._error)
                    break;

                foreach (Controller c in _controllers.Items)
                    _executeCall(c);
                
            } while (false); // o "do" é somente para poder realizar o "break" em qualquer ponto do código

            //Executa o evento de logs
            if (ExecutionLog != null)
            {
                try
                {
                    foreach (Controller c in _controllers.Items)
                        ExecutionLog(c.Success, _enterpriseId, c.Method, c.acl, _ser.Serialize(c.Request), _ser.Serialize(c.Response));
                }
                catch { }
            }

            if (this._multicall)
                return _ser.Serialize(_controllers.GetMultiCall());//return _ser.Serialize(_mResponse);
            else
                return _ser.Serialize(_controllers.GetSingle());//return _ser.Serialize(_sResponse);

        }

        /// <summary>
        /// Método pricado que executa cada uma das requisições separadamente.
        /// </summary>
        private void _executeCall(Controller controller)
        {

            Dictionary<String, Object> request = controller.Request;

            String id = "";
            if (request.ContainsKey("id"))
                id = request["id"].ToString();

            if (!validate(request))
                return;

            Dictionary<String, Object> _parameters = new Dictionary<string, object>();
            try
            {
                _parameters = (Dictionary<String, Object>)request["parameters"];
            }
            catch { }
            String _auth = (request.ContainsKey("auth") ? (String)request["auth"] : "");

            //Cria a instancia da classe com base no método requerido
            APIBase processor = APIBase.CreateInstance(request["method"].ToString());

            if (processor == null)
            {
                _setError(id, ErrorType.InvalidRequest, "JSON-rpc method class is unknow.", "", null, true);
                return;
            }

            Error onError = new Error(delegate(ErrorType type, String data, String debug, Dictionary<String, Object> additionslReturn)
            {
                _setError(id, type, data, debug, additionslReturn, false);          
            });

            ExternalAccessControl eAuth = new ExternalAccessControl(delegate(String method, String auth, AccessControl preCtrl, Dictionary<String, Object> paramters)
            {
                return ExternalAccessControl(method, auth, preCtrl, paramters);
            });

            //Define os eventos
            processor.Error += onError;
            if (ExternalAccessControl != null)
                processor.ExternalAccessControl += eAuth;

            //Realiza o processamento
            Object oResult = null;
            try
            {
                oResult = processor.Process(this._database, this._enterpriseId, request["method"].ToString(), _auth, _parameters);
            }
            catch(Exception ex) {
                _setError(id, ErrorType.InternalError, null, ex.Message + ex.StackTrace, null, false);  
            }

            //Limpa os eventos
            processor.Error -= onError;
            if (ExternalAccessControl != null)
                processor.ExternalAccessControl += eAuth;
            onError = null;
            eAuth = null;

            //Define o retorno
            if (!this.IsError)
                _setResponse(id, oResult);

            controller.acl = processor.Acl;
        }

        /// <summary>
        /// Valida os principais campos da requisição
        /// 1 - ID da empresa precisa ser maior que zero
        /// 2 - Versão da API deve estar correta
        /// 3 - Deve conter um método
        /// 4 - Deve conter parâmetros e este deve ser array ou dicionário
        /// </summary>
        private Boolean validate(Dictionary<String, Object> request)
        {
            String id = "";
            if (request.ContainsKey("id"))
                id = request["id"].ToString();

            
            if (this._enterpriseId == 0)
            {
                _setError(id, ErrorType.InvalidRequest, "JSON-rpc enterprise is unknow.", "", null, true);
                return false;
            }

            if (!request.ContainsKey("id"))
            {
                _setError(id, ErrorType.InvalidRequest, "JSON-rpc id is not specified.", "", null, true);
                return false;
            }

            if (!request.ContainsKey("apiver") && !request.ContainsKey("jsonrpc"))
            {
                _setError(id, ErrorType.InvalidRequest, "JSON-rpc version is not specified.", "", null, true);
                return false;
            }

            String jsonrpc = "";
            if (request.ContainsKey("jsonrpc"))
                jsonrpc = request["jsonrpc"].ToString();
            else if (request.ContainsKey("apiver"))
                jsonrpc = request["apiver"].ToString();


            if (jsonrpc != this._version)
            {
                _setError(id, ErrorType.InvalidRequest, String.Format("Expecting JSON-rpc version " + this._version + ", {0} is given.", jsonrpc), "", null, true);
                return false;
            }          

            if (!request.ContainsKey("method"))
            {
                _setError(id, ErrorType.InvalidRequest, "JSON-rpc method is not defined.", "", null, true);
                return false;
            }

            if (!request.ContainsKey("parameters"))
            {
                _setError(id, ErrorType.InvalidRequest, "JSON-rpc parameters is not defined.", "", null, true);
                return false;
            }

            if (!(request["parameters"] is Dictionary<String, Object>) && !(request["parameters"] is Array) && !(request["parameters"] is ArrayList))
            {
                _setError(id, ErrorType.InvalidRequest, "JSON-rpc parameters is not an Array.", "", null, true);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Define como erro para a requisição em questão
        /// </summary>
        private void _setError(String id, ErrorType type, String data, String debug)
        {
            _setError(id, type, data, debug, null, false);
        }

        /// <summary>
        /// Define como erro para a requisição em questão
        /// </summary>
        private void _setError(String id, ErrorType type, String data, String debug, Dictionary<String, Object> additionslReturn, Boolean force_err)
        {

            // Notifications MUST NOT be answered, but error MUST be generated on JSON parse error
            if (String.IsNullOrWhiteSpace(id) && (!force_err))
            {
                return;
            }

            this._error = true;

            Dictionary<String, Object> errorData = _getErrorData(type);
            if (!String.IsNullOrWhiteSpace(debug))
                errorData.Add("debug", debug);

            if (!String.IsNullOrWhiteSpace(data))
                errorData["data"] = data;

            if (additionslReturn != null)
                foreach (String k in additionslReturn.Keys)
                    if (!errorData.ContainsKey(k))
                        errorData.Add(k, additionslReturn[k]);

            _setResponse(id, errorData, "");

        }

        /// <summary>
        /// Define a resposta para a requisição em questão
        /// </summary>
        private void _setResponse(String id, Object result)
        {
            _setResponse(id, null, result);
        }

        /// <summary>
        /// Define a resposta para a requisição em questão
        /// </summary>
        private void _setResponse(String id, Object error, Object result)
        {
            Dictionary<String, Object> resp = new Dictionary<String, Object>();
            resp.Add("jsonrpc", this._version);
            resp.Add("apiver", this._version);//sistema legado
            resp.Add("id", id);

            if (error != null)
                resp.Add("error", error);
            else
                if (result != null)
                    resp.Add("result", result);

            this._controllers.SetResponse(id, resp, (error == null));

            /*
            if (this._multicall)
                this._mResponse.Add(resp);
            else
                this._sResponse = resp;*/
        }

        /// <summary>
        /// Resgata o template de erro
        /// </summary>
        public static Dictionary<String, Object> _getErrorData(ErrorType type)
        {
            Dictionary<String, Object> _err = new Dictionary<string, Object>();
            _err.Add("code", ((Int32)type).ToString());

            switch (type)
            {
                case ErrorType.JSPNRPCVersion:
                    _err.Add("message", "JSON RPC version");
                    _err.Add("data", "Invalid API version.");
                    break;

                case ErrorType.ParseError:
                    _err.Add("message", "Parse error");
                    _err.Add("data", "Invalid JSON. An error occurred on the server while parsing the JSON text.");
                    break;

                case ErrorType.InvalidRequest:
                    _err.Add("message", "Invalid Request.");
                    _err.Add("data", "The received JSON is not a valid JSON-RPC Request.");
                    break;

                case ErrorType.MethodNotFound:
                    _err.Add("message", "Method not found.");
                    _err.Add("data", "The requested remote-procedure does not exist / is not available");
                    break;

                case ErrorType.InvalidParameters:
                    _err.Add("message", "Invalid parameters.");
                    _err.Add("data", "Invalid method parameters.");
                    break;

                case ErrorType.InternalError:
                    _err.Add("message", "Internal error.");
                    _err.Add("data", "Internal JSON-RPC error.");
                    break;

                case ErrorType.ApplicationError:
                    _err.Add("message", "Application error.");
                    _err.Add("data", "No details");
                    break;

                case ErrorType.SystemError:
                    _err.Add("message", "System error.");
                    _err.Add("data", "No details");
                    break;

                case ErrorType.TransportError:
                    _err.Add("message", "Transport error.");
                    _err.Add("data", "No details");
                    break;

                default:
                    _err.Add("message", "Unknow error.");
                    _err.Add("data", "No details");
                    break;
            }
            return _err;
        }

    }
}
