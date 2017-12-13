using System;
using System.Collections.Generic;
using System.Text;

namespace AkAuthAgent
{
    public class header_aut
    {
        public Int32 n;			/* Numero sequencial do procotolo */
        public ushort operacao;		/* Codigo da operacao */
        public ushort status;		/* Status da operacao (no caso de resposta) */
        public Byte[] md5;			/* (16 bytes) Checksum MD5 dos dados + header */
        public Int32 tam_pack;		/* Tamanho do pacote */
        public ushort tam_pad;		/* Tamanho do padding */
        public ushort nop;			/* Nao usado. Padding do header para 8 bytes */

        public header_aut()
        {
        }

        public header_aut(Byte[] buffer)
        {
            md5 = new Byte[16];

            Byte[] tmp = new Byte[4];
            Array.Copy(buffer, 0, tmp, 0, 4);
            //Array.Reverse(tmp);
            n = BitConverter.ToInt32(tmp, 0);

            tmp = new Byte[2];
            Array.Copy(buffer, 4, tmp, 0, 2);
            Array.Reverse(tmp);
            operacao = BitConverter.ToUInt16(tmp, 0);

            tmp = new Byte[2];
            Array.Copy(buffer, 6, tmp, 0, 2);
            Array.Reverse(tmp);
            status = BitConverter.ToUInt16(tmp, 0);

            Array.Copy(buffer, 8, md5, 0, 16);

            tmp = new Byte[4];
            Array.Copy(buffer, 24, tmp, 0, 4);
            Array.Reverse(tmp);
            tam_pack = BitConverter.ToInt32(tmp, 0);

            tmp = new Byte[2];
            Array.Copy(buffer, 28, tmp, 0, 2);
            Array.Reverse(tmp);
            tam_pad = BitConverter.ToUInt16(tmp, 0);

            tmp = new Byte[2];
            Array.Copy(buffer, 30, tmp, 0, 2);
            Array.Reverse(tmp);
            nop = BitConverter.ToUInt16(tmp, 0);

        }

        public Byte[] ToBytes()
        {
            Int32 offSet = 0;

            Byte[] retorno = new Byte[32];
            Byte[] tmp = BitConverter.GetBytes(n);
            Array.Reverse(tmp);//htonl
            Array.Copy(tmp, 0, retorno, offSet, 4);
            offSet += 4;

            tmp = BitConverter.GetBytes(operacao);
            Array.Reverse(tmp);//htonl
            Array.Copy(tmp, 0, retorno, offSet, 2);
            offSet += 2;

            tmp = BitConverter.GetBytes(status);
            Array.Reverse(tmp);//htonl
            Array.Copy(tmp, 0, retorno, offSet, 2);
            offSet += 2;

            if ((md5 != null) && (md5.Length == 16))
                Array.Copy(md5, 0, retorno, offSet, 16);
            offSet += 16;

            tmp = BitConverter.GetBytes(tam_pack);
            Array.Reverse(tmp);//htonl
            Array.Copy(tmp, 0, retorno, offSet, 4);
            offSet += 4;

            tmp = BitConverter.GetBytes(tam_pad);
            Array.Reverse(tmp);//htonl
            Array.Copy(tmp, 0, retorno, offSet, 2);
            offSet += 2;

            tmp = BitConverter.GetBytes(nop);
            Array.Reverse(tmp);//htonl
            Array.Copy(tmp, 0, retorno, offSet, 2);
            offSet += 2;

            return retorno;
        }

        public header_aut Clone()
        {
            return new header_aut(this.ToBytes());
        }

    }
}
