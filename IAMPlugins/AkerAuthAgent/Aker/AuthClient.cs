using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace AkAuthAgent
{
    public class AuthClient : AuthBase
    {
        
        private IPEndPoint remoteEP;
        private Timer pingTimer;
        private ag_greeting greeting;

        public AuthClient(IPAddress ip, String password)
            : this(new IPEndPoint(ip, 1021))
        {
            base.password = password;
        }

        public AuthClient(IPAddress ip, Int32 port, String password)
            : this(new IPEndPoint(ip, port))
        {
            base.password = password;
        }

        public AuthClient(IPEndPoint remoteEP)
        {
            this.remoteEP = remoteEP;
            base.set_auth_type(AuthType.Client);
        }

        public void Connect()
        {
            pingTimer = new Timer();
            pingTimer.Elapsed += new ElapsedEventHandler(pingTimer_Elapsed);
            pingTimer.Interval = AUTH_TIME_PING * 1000;
            pingTimer.Start();
            pingTimer_Elapsed(null, null);
        }

        public String[] ListaGrupos()
        {
            Socket sock = null;
            _Connect(ref sock);

            String groups = "";

            envia_pacote(sock, null, 0, FWAUT_OP_GET_GROUPS, FWAUT_ST_OK);
            
            packet pkt = null;
            do
            {
                pkt = recebe_pacote(sock);

                if (((pkt == null) || (pkt.status != FWAUT_ST_OK && pkt.status != FWAUT_ST_MORE)))
                    throw new Exception("Erro a listar os grupos");

                groups += Encoding.GetEncoding("iso-8859-1").GetString(pkt.body);

            } while ((pkt != null) && (pkt.status == FWAUT_ST_MORE));

            Int32 end = groups.IndexOf("\0\0");
            if (end == -1)
                end = groups.Length;

            groups = groups.Substring(0, end);

            String[] grupos = groups.Trim("\0".ToCharArray()).Split("\0".ToCharArray());

            _Disconnect(sock);
            return grupos;
        }

        public AuthTestStatus TesteUsuario(String username, String password)
        {
            AuthTestStatus retStatus = null;

            String[] grupos = new String[0];
            Socket sock = null;
            _Connect(ref sock);

            Byte[] data = Encoding.GetEncoding("iso-8859-1").GetBytes(username + "\0" + password + "\0\0");

            envia_pacote(sock, data, data.Length, FWAUT_OP_REQ, FWAUT_ST_OK);
            packet pkt = recebe_pacote(sock);

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

            _Disconnect(sock);
            return retStatus;
        }

        public String[] ListaUsuarios()
        {
            Socket sock = null;
            _Connect(ref sock);

            envia_pacote(sock, null, 0, FWAUT_OP_GET_USERS, FWAUT_ST_OK);
            String users = "";

            packet pkt = null;
            do
            {
                pkt = recebe_pacote(sock);

                if (((pkt == null) || (pkt.status != FWAUT_ST_OK && pkt.status != FWAUT_ST_MORE)))
                    throw new Exception("Erro ao listar os usuário");

                users += Encoding.GetEncoding("iso-8859-1").GetString(pkt.body);

            } while ((pkt != null) && (pkt.status == FWAUT_ST_MORE));

            Int32 end = users.IndexOf("\0\0");
            if (end == -1)
                end = users.Length;

            users = users.Substring(0, end);

            String[] usuarios = users.Trim("\0".ToCharArray()).Split("\0".ToCharArray());

            _Disconnect(sock);
            return usuarios;
        }

        private void _Disconnect(Socket iSock)
        {
            iSock.Shutdown(SocketShutdown.Both);
            iSock.Disconnect(true);
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

        private void pingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Socket iSock = null;
            _Connect(ref iSock);

            //Console.WriteLine("Ping");

            //NetworkStream ns = client.GetStream();

            envia_pacote(iSock, null, 0, FWAUT_OP_PING, FWAUT_ST_OK);
            packet pkt = recebe_pacote(iSock);

            if ((pkt == null) || (pkt.status != FWAUT_ST_OK))
                throw new Exception("Ping Error");

            //Console.WriteLine("Pong");

            _Disconnect(iSock);
        }

    }
}
