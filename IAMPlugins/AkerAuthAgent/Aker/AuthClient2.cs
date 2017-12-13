using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AkAuthAgent
{
    public class AuthClient2 : AuthBase
    {
        
        private IPEndPoint remoteEP;
        private Timer pingTimer;
        private ag_greeting greeting;
        private Boolean communicating;

        private Socket internalSock;

        public delegate void Ping(packet pkt, Exception ex);
        public event Ping OnPing;

        public AuthClient2(IPAddress ip, String password)
            : this(new IPEndPoint(ip, 1021))
        {
            base.password = password;
        }

        public AuthClient2(IPAddress ip, Int32 port, String password)
            : this(new IPEndPoint(ip, port))
        {
            base.password = password;
        }

        public AuthClient2(IPEndPoint remoteEP)
        {
            this.remoteEP = remoteEP;
            base.set_auth_type(AuthType.Client);
        }

        public void Connect()
        {
            _Connect(ref internalSock);
            communicating = false;

            pingTimer = new Timer(new TimerCallback(pingTimerEvent), null, AUTH_TIME_PING * 1000, AUTH_TIME_PING * 1000);
        }


        public void Disconnect()
        {
            internalSock.Shutdown(SocketShutdown.Both);
            internalSock.Disconnect(true);
        }

        public String[] ListaGrupos()
        {
            try
            {
                communicating = true;

                String groups = "";

                envia_pacote(internalSock, null, 0, FWAUT_OP_GET_GROUPS, FWAUT_ST_OK);

                packet pkt = null;
                do
                {
                    pkt = recebe_pacote(internalSock);

                    if (((pkt == null) || (pkt.status != FWAUT_ST_OK && pkt.status != FWAUT_ST_MORE)))
                        throw new Exception("Erro a listar os grupos");

                    groups += Encoding.GetEncoding("iso-8859-1").GetString(pkt.body);

                } while ((pkt != null) && (pkt.status == FWAUT_ST_MORE));

                Int32 end = groups.IndexOf("\0\0");
                if (end == -1)
                    end = groups.Length;

                groups = groups.Substring(0, end);

                String[] grupos = groups.Trim("\0".ToCharArray()).Split("\0".ToCharArray());

                return grupos;
            }
            finally
            {
                communicating = false;
            }
        }

        public AuthTestStatus TesteUsuario(String username, String password)
        {
            try
            {
                communicating = true;

                AuthTestStatus retStatus = null;

                String[] grupos = new String[0];

                Byte[] data = Encoding.GetEncoding("iso-8859-1").GetBytes(username + "\0" + password + "\0\0");

                envia_pacote(internalSock, data, data.Length, FWAUT_OP_REQ, FWAUT_ST_OK);
                packet pkt = recebe_pacote(internalSock);

                switch (pkt.status)
                {
                    case FWAUT_ST_BAD_PWD:
                        retStatus = new AuthTestStatus(pkt.status);
                        //throw new Exception("Senha inválida");
                        break;

                    case FWAUT_ST_NO_USER:
                        retStatus = new AuthTestStatus(pkt.status);
                        //throw new Exception("Usuário inexistente");
                        break;

                    case FWAUT_ST_OK:
                        String groups = Encoding.GetEncoding("iso-8859-1").GetString(pkt.body);

                        Int32 end = groups.IndexOf("\0\0");
                        if (end == -1)
                            end = groups.Length;

                        groups = groups.Substring(0, end);

                        grupos = groups.Trim("\0".ToCharArray()).Split("\0".ToCharArray());
                        retStatus = new AuthTestStatus(pkt.status, grupos);
                        break;
                }

                if (retStatus == null)
                    retStatus = new AuthTestStatus(999);

                return retStatus;
            }
            finally
            {
                communicating = false;
            }
        }

        public String[] ListaUsuarios()
        {
            try
            {
                communicating = true;

                envia_pacote(internalSock, null, 0, FWAUT_OP_GET_USERS, FWAUT_ST_OK);
                String users = "";

                packet pkt = null;
                do
                {
                    pkt = recebe_pacote(internalSock);

                    if (((pkt == null) || (pkt.status != FWAUT_ST_OK && pkt.status != FWAUT_ST_MORE)))
                        throw new Exception("Erro ao listar os usuário");

                    users += Encoding.GetEncoding("iso-8859-1").GetString(pkt.body);

                } while ((pkt != null) && (pkt.status == FWAUT_ST_MORE));

                Int32 end = users.IndexOf("\0\0");
                if (end == -1)
                    end = users.Length;

                users = users.Substring(0, end);

                String[] usuarios = users.Trim("\0".ToCharArray()).Split("\0".ToCharArray());

                return usuarios;
            }
            finally
            {
                communicating = false;
            }
        }


        private void _Connect(ref Socket iSock)
        {
            iSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            iSock.Connect(remoteEP);

            chave = gera_chave(password);

            header_key header = new header_key();
            header.chave = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            header.n = 1;
            header.nop = BitConverter.ToUInt16(header.chave, 0);
            Byte[] buffer = header.ToBytes();

            buffer = aut_encripta_buffer(buffer, buffer.Length, 0, chave);

            String tst = "0x" + BitConverter.ToString(buffer).Replace("-", ", 0x");


            aut_seta_n(header.n);

            //Envia chave se sessão
            iSock.Send(buffer, 0, buffer.Length, SocketFlags.None);

            //Separa chave, com a propria chave gerada anteriormente
            //Para criptografia posteriores
            chave = new chave_aut(header.chave);
            
            packet pkt = recebe_pacote(iSock);
            aut_seta_n(pkt.n);

            if (pkt.operacao == 5)
            {
                greeting = new ag_greeting(pkt.body);
            }

        }

        private void pingTimerEvent(Object state)
        {
            if (communicating)
                return;

            try
            {

                envia_pacote(internalSock, null, 0, FWAUT_OP_PING, FWAUT_ST_OK);
                packet pkt = recebe_pacote(internalSock);

                if (OnPing != null)
                    OnPing(pkt, null);
            }
            catch (Exception ex)
            {
                if (OnPing != null)
                    OnPing(null, ex);
            }
        }

    }
}
