using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace AkAuthAgent
{
    public enum AuthType
    {
        None = 0,
        Client = 1,
        Server = 2
    }

    abstract public class AuthBase
    {
        internal Int32 AUTH_TIME_PING = 60;	/* Quando enviar ping, original 60 */

        /* Definicoes dos TAGS de status do protocolo Agente-Autenticador */

        internal const ushort FWAUT_ST_OK = 0;       /* Autenticacao OK */
        internal const ushort FWAUT_ST_BAD_PWD = 1;       /* Senha invalida */
        internal const ushort FWAUT_ST_NO_USER = 2;       /* Usuario nao cadastrado */
        internal const ushort FWAUT_ST_MORE = 3;       /* Existem mais pacotes */
        internal const ushort FWAUT_ST_ERROR = 4;	/* Erro generico */
        internal const ushort FWAUT_ST_NEW_PIN = 5;	/* Servidor SecurID exige novo pin */
        internal const ushort FWAUT_ST_NEXT_TOKEN = 6;	/* Servidor SecurID exige next token */

        /* Definicao dos TAGS das operacoes do protocolo Agente-Autenticador */

        internal const ushort FWAUT_OP_REQ = 1;	/* Requisicao de autenticacao */
        internal const ushort FWAUT_OP_GET_USERS = 2;	/* Requisicao de lista de usuarios */
        internal const ushort FWAUT_OP_GET_GROUPS = 3;	/* Requisicao de lista de grupos */
        internal const ushort FWAUT_OP_PING = 4;	/* Ping */
        internal const ushort FWAUT_OP_AG_GREETING = 5;	/* Greeting do agente */
        internal const ushort FWAUT_OP_REQ_GROUPS = 6;	/* Requisita grupos de um usuario */
        internal const ushort FWAUT_OP_SET_PIN = 7;	/* Atualiza PIN para SecurID */
        internal const ushort FWAUT_OP_NEXT_TOKEN = 8;	/* Envia next token para SecurID */
        internal const ushort FWAUT_OP_KILL = 9;       /* Mata o processo authc (nao utilizado pelo firewall, apenas pelo fwtunneld) */
        internal const ushort FWAUT_OP_GET_SKEY = 10;	/* Le challenge S/Key */

        internal Int32 nr;		/* Numero de sequencia para recebimento */
        internal Int32 ne;		/* Numero de sequencia para envio */
        internal chave_aut chave;		/* Chave de autenticacao/criptografia */
        internal String password;

        internal AuthType type = AuthType.None;

        public delegate void Packet(header_aut header, Byte[] body);
        public event Packet OnPacketSent;
        public event Packet OnPacketReceive;

        internal void set_auth_type(AuthType type)
        {
            this.type = type;
        }

        internal Byte[] aut_encripta_buffer(Byte[] buffer, Int32 len, Int32 vet, chave_aut chave)
        {
            Byte[] iBuffer = new Byte[len];
            Array.Copy(buffer, iBuffer, len);

            byte[] iv = new byte[8];
            Array.Copy(BitConverter.GetBytes((Int32)vet), 0, iv, 0, 4);
            Array.Copy(BitConverter.GetBytes((Int32)(~vet)), 0, iv, 4, 4);

            BlowfishCBC bl = new BlowfishCBC(chave.key, iv);
            bl.encrypt(iBuffer);

            return iBuffer;
        }

        internal Byte[] aut_decripta_buffer(Byte[] buffer, Int32 len, Int32 vet, chave_aut chave)
        {
            Byte[] iBuffer = new Byte[len];
            Array.Copy(buffer, iBuffer, len);

            byte[] iv = new byte[8];
            Array.Copy(BitConverter.GetBytes((Int32)vet), 0, iv, 0, 4);
            Array.Copy(BitConverter.GetBytes((Int32)(~vet)), 0, iv, 4, 4);

            BlowfishCBC bl = new BlowfishCBC(chave.key, iv);
            bl.decrypt(iBuffer);

            return iBuffer;
        }

        internal packet recebe_pacote(Socket sock)
        {
            if (type == AuthType.None)
                throw new Exception("Auth type not defined");

            packet pkt = null;
            Byte[] buffer = new Byte[32]; // Tamanho do header

            sock.ReceiveTimeout = 0;
            Int32 size = sock.Receive(buffer, SocketFlags.None);

            if (size >= 32)
            {
                
                //Decripta e trata o header separadamente do body (32 bytes)
                Byte[] decHeader = aut_decripta_buffer(buffer, 32, 0, chave);
                pkt = new packet(decHeader);

                //Define o nr e ne
                if (type == AuthType.Client)
                    nr = htonlInt32(pkt.n);

                if (type == AuthType.Server)
                    if (++nr == 0xFFFFFFFF) nr = 1;

                if (pkt.tam_pack - 32 > 0)
                {
                    size = 0;
                    Int32 expectedSize = pkt.tam_pack - 32;
                    MemoryStream stm = new MemoryStream();
                    buffer = new Byte[pkt.tam_pack - 32];

                    //Loop para caso a quantidade de dados recebida seja menor que o esperado
                    do
                    {
                        size = sock.Receive(buffer);
                        if (size == 0)
                            throw new Exception("Invalid packet size");

                        stm.Write(buffer, 0, size);

                        //Redefine o tamanho do buffer para não receber mais dados do que o necessário
                        buffer = new Byte[expectedSize - stm.Length];

                    } while (stm.Length < expectedSize);

                    buffer = stm.ToArray();
                    stm.Dispose();
                    stm.Close();
                    stm = null;

                    //Define o vetor do IV
                    //Byte[] bVet = BitConverter.GetBytes(nr);
                    //Array.Reverse(bVet);
                    //Int32 vet = BitConverter.ToInt32(bVet, 0);

                    //Decriptografa e trata o body
                    pkt.body = aut_decripta_buffer(buffer, buffer.Length, htonlInt32(nr), chave);
                }

                if (!pkt.CheckMD5(chave))
                    throw new Exception("MD5 inválido");
            }

            if (OnPacketReceive != null)
                OnPacketReceive((header_aut)pkt.Clone(), pkt.body);

            return pkt;
        }
        /*
        internal void envia_pacote(NetworkStream ns, Byte[] dados, Int32 size, ushort op, ushort status)
        {
            Byte[] buffer = _enviapacote(dados, size, op, status);

            ns.Write(buffer, 0, buffer.Length);
        }*/

        internal void envia_pacote(Socket sock, Byte[] dados, Int32 size, ushort op, ushort status)
        {

            Byte[] buffer = _enviapacote(dados, size, op, status);

            sock.Send(buffer, SocketFlags.None);

            //Incrementa apos enviar o pacote
            if (type == AuthType.Client)
                if (++ne == 0xFFFFFFFF) ne = 1;

            
        }

        private Byte[] _enviapacote(Byte[] dados, Int32 size, ushort op, ushort status)
        {
            header_aut header = new header_aut();

            if (type == AuthType.Server)
                if (++ne == 0xFFFFFFFF) ne = 1;

            header.n = ne;
            header.operacao = op;
            header.status = status;
            header.tam_pad = (ushort)(8 - (size % 8));		/* Calcula tamanho do padding */
            if (header.tam_pad == 8) header.tam_pad = 0;
            header.tam_pad = 0;
            header.tam_pack = size + header.tam_pad + 32; //32 = tamanho do header_aut;
            Int32 bsend = header.tam_pack;
            header.nop = 0;

            Byte[] headerBytes = header.ToBytes();


            if (OnPacketSent != null)
                OnPacketSent(header, dados);


            //Monta o buffer decriptado p/ calculo do md5
            Byte[] md5CalcBuffer = new Byte[headerBytes.Length + size];
            Array.Copy(headerBytes, 0, md5CalcBuffer, 0, headerBytes.Length);
            if (size > 0)
                Array.Copy(dados, 0, md5CalcBuffer, headerBytes.Length, dados.Length);

            //Cacula md5
            Byte[] msgBuffer = new Byte[16 + 16 + md5CalcBuffer.Length];
            Array.Copy(chave.akey, 0, msgBuffer, 0, 16);
            Array.Copy(md5CalcBuffer, 0, msgBuffer, 16, md5CalcBuffer.Length);
            Array.Copy(chave.akey, 0, msgBuffer, 16 + md5CalcBuffer.Length, 16);

            md5 md5 = new md5(msgBuffer);
            md5.calc();
            header.md5 = md5.getHash();
            header.nop = BitConverter.ToUInt16(header.md5, 0);

            //Resgata o header atualizado
            headerBytes = header.ToBytes();

            //Encripta header
            Byte[] encHeader = aut_encripta_buffer(headerBytes, headerBytes.Length, 0, chave);
            Byte[] encBody = new Byte[0];
            if (size > 0)
                encBody = aut_encripta_buffer(dados, size, htonlInt32(ne), chave);

            Byte[] buffer = new Byte[encHeader.Length + encBody.Length];
            Array.Copy(encHeader, 0, buffer, 0, encHeader.Length);
            if (size > 0)
                Array.Copy(encBody, 0, buffer, encHeader.Length, encBody.Length);

            return buffer;
        }

        internal Int32 htonlInt32(Int32 n)
        {
            Byte[] bVet = BitConverter.GetBytes(n);
            Array.Reverse(bVet);
            return BitConverter.ToInt32(bVet, 0);
        }

        internal chave_aut gera_chave(String texto)
        {

            Byte[] tmp = Encoding.ASCII.GetBytes(password);//Senha cadastrada na entidade
            Byte[] md5Buffer = new Byte[tmp.Length + 2];
            md5Buffer[0] = 8;
            Array.Copy(tmp, 0, md5Buffer, 1, tmp.Length);
            md5Buffer[tmp.Length + 1] = 8;

            md5 md5_1 = new md5(md5Buffer);
            md5_1.calc();

            return new chave_aut(md5_1.getHash());
        }

        internal Byte[] decriptaHeader(Byte[] buffer, Int32 size)
        {
            Byte[] pkt = new Byte[size];
            Array.Copy(buffer, pkt, size);

            chave = gera_chave(password);

            pkt = aut_decripta_buffer(pkt, pkt.Length, 0, chave);

            return pkt;
        }

        /* A funcao abaixo seta o valor inicial para nr e ne */

        internal void aut_seta_n(Int32 n)
        {
            nr = ne = n;
        }

    }
}
