using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace AkAuthAgent
{
    public class AuthServerItem : AuthBase
    {
        private Socket _mainSock;
        private Byte[] _receivedBuffer;
        private Boolean _isRunning;
        private Int32 count;
        //private AuthUsers _users;
        private IPEndPoint _remoteEP;

        //public AuthUsers users { get { return _users; } set { _users = value; } }

        public event AuthServer.UserValidate OnUserValidate;
        public event AuthServer.ListUsers OnListUsers;
        public event AuthServer.ListGroups OnListGroups;
        public event AuthServer.ConnectionStarted OnConnectionStarted;
        public event AuthServer.Info OnInfo;

        public AuthServerItem(Socket sock, String password) {
            this._mainSock = sock;
            this._receivedBuffer = new Byte[8000];
            this._isRunning = true;

            base.password = password;

            count = 0;

            _remoteEP = (IPEndPoint)sock.RemoteEndPoint;
            
            base.set_auth_type(AuthType.Server);
        }

        public void WriteFile(Byte[] data, Int32 offset, Int32 size)
        {
            BinaryWriter writer = new BinaryWriter(File.Open(count + ".txt", FileMode.Create));
            String dado = "Byte[] data = new Byte[] { 0x" + BitConverter.ToString(data, offset, size).Replace("-", ", 0x") + " };";
            writer.Write(Encoding.ASCII.GetBytes(dado));
            writer.Close();
            count++;

            //Envia teste p/ o linux
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse("10.1.1.20"), 1021);

            sock.Send(data, offset, size, SocketFlags.None);
            sock.Close();
        }

        public void Start()
        {

            //recebe_pacote chave de sessão
            Byte[] buffer = new Byte[24]; 

            Int32 size = _mainSock.Receive(buffer);

            if (size < 24)
                throw new Exception("Erro ao receber chave de sessão");

            //WriteFile(buffer, 0, size);

            header_key header = new header_key(decriptaHeader(buffer, buffer.Length));
            aut_seta_n(header.n);

            //Separa chave
            chave = new chave_aut(header.chave);

            Int32 cnt = 0;
            if (OnConnectionStarted != null)
                OnConnectionStarted(_remoteEP, ref cnt);

            //Prepara pacote para envio ao cliente
            ag_greeting greeting = new ag_greeting();
            greeting.versao = 4;
            greeting.sub_ver = 0;
            greeting.release = 1;
            greeting.plataforma = (byte)1; //1 = lunix, 2 = Windows
            greeting.n_users = cnt;

            Byte[] sendBuffer = greeting.ToBytes();

            envia_pacote(_mainSock, sendBuffer, sendBuffer.Length, FWAUT_OP_AG_GREETING, FWAUT_ST_OK);

            EsperaPacotes();

        }

        private void Dispose() {
            _isRunning = false;
            _mainSock.Shutdown(SocketShutdown.Both);
            _mainSock.Close();

            if (OnInfo != null)
                OnInfo(_remoteEP, "Disconected " + _remoteEP.ToString());
            
        }

        private void EsperaPacotes()
        {
            while (_isRunning)
            {
                try
                {
                    packet pkt = recebe_pacote(_mainSock);

                    if (pkt == null)
                    {
                        Dispose();
                        return;
                    }

                    switch (pkt.operacao)
                    {
                        case FWAUT_OP_PING:
                            envia_pacote(_mainSock, null, 0, FWAUT_OP_PING, FWAUT_ST_OK);
                            Dispose();
                            break;

                        case FWAUT_OP_REQ:
                        case FWAUT_OP_REQ_GROUPS:
                            //Retorna os grupos que o usuário existe
                            //return FWAUT_ST_OK - Autenticacao OK
                            //return FWAUT_ST_BAD_PWD - Senha invalida
                            //return FWAUT_NO_USER - Usuario inexistente
                            String[] request = Encoding.ASCII.GetString(pkt.body).Trim("\0".ToCharArray()).Split("\0".ToCharArray());

                            //AuthUserResult res = _users.ValidaUser(request[0], request[1]);

                            AuthUserResult res = new AuthUserResult(request[0]);
                            if (OnUserValidate != null)
                                OnUserValidate(_remoteEP, request[0], request[1], ref res);

                            ushort status = FWAUT_ST_OK;
                            Byte[] grpRet = new Byte[0];
                            switch (res.Result)
                            {
                                case AuthResult.OK:
                                    status = FWAUT_ST_OK;
                                    String grupos = "";
                                    foreach (String item in res.Groups)
                                    {
                                        grupos += item + "\0";
                                    }
                                    grupos += "\0\0";
                                    grpRet = Encoding.GetEncoding(869).GetBytes(grupos);
                                    break;

                                case AuthResult.BadPassword:
                                    status = FWAUT_ST_BAD_PWD;
                                    break;

                                case AuthResult.NoUser:
                                    status = FWAUT_ST_NO_USER;
                                    break;
                            }


                            envia_pacote(_mainSock, grpRet, grpRet.Length, pkt.operacao, status);
                            break;


                        case FWAUT_OP_GET_USERS:
                            List<String> users = new List<String>();

                            if (OnListUsers != null)
                                OnListUsers(_remoteEP, ref users);

                            String tstU = "";
                            foreach (String item in users)
                            {
                                tstU += item + "\0";
                            }
                            tstU += "\0\0";

                            Byte[] usr = Encoding.GetEncoding(869).GetBytes(tstU);
                            envia_pacote(_mainSock, usr, usr.Length, FWAUT_OP_GET_USERS, FWAUT_ST_OK);
                            //trata_lst_users(ns);
                            break;

                        case FWAUT_OP_GET_GROUPS:
                            List<String> groups = new List<String>();

                            if (OnListGroups != null)
                                OnListGroups(_remoteEP, ref groups);

                            String tst = "";
                            foreach (String item in groups)
                            {
                                tst += item + "\0";
                            }
                            tst += "\0\0";

                            //Teste de encoding do FW
                            Byte[] tmp = new Byte[0];
                            foreach (EncodingInfo encInfo in Encoding.GetEncodings())
                            {
                                Int32 offSet = tmp.Length;
                                Byte[] b = encInfo.GetEncoding().GetBytes(encInfo.CodePage.ToString() + ": ção");
                                Array.Resize(ref tmp, offSet + (b.Length + 1));
                                Array.Copy(b, 0, tmp, offSet, b.Length);
                                tmp[b.Length + offSet] = 0;
                            }

                            Byte[] grp = Encoding.GetEncoding(869).GetBytes(tst);
                            //Byte[] grp = tmp;
                            envia_pacote(_mainSock, grp, grp.Length, FWAUT_OP_GET_GROUPS, FWAUT_ST_OK);
                            //trata_lst_groups(ns);
                            break;

                        case FWAUT_OP_GET_SKEY:
                            /*
                            if (len != sizeof(ag_get_skey))
                            {
                                if ((erro = envia_pacote(ns, NULL, 0, op, FWAUT_ST_ERROR)) != 0)
                                {
                                    if (erro == 2)
                                        syslog(fila | LOG_ERR, "%s\n", ag_msg[AG_MSG_ERRO_MEMORIA]);
                                    exit(1);
                                }
                            }
                            else
                            {
                                retorna_opie(ns, (ag_get_skey*)dados);
                            }*/
                            envia_pacote(_mainSock, null, 0, pkt.operacao, FWAUT_ST_ERROR);
                            break;

                        default:
                            envia_pacote(_mainSock, null, 0, pkt.operacao, FWAUT_ST_ERROR);
                            break;
                    }
                }
                catch {
                    _isRunning = false;
                }
            }

        }


        //não utilizado
        private void OnDataReceived(IAsyncResult ar)
        {
            Int32 size;
            try
            {
                size = _mainSock.EndReceive(ar);
            }
            catch
            {
                size = -1;
            }
            if (size <= 0)
            { //Connection is dead :(
                //Dispose();
                return;
            }



        }

    }
}
