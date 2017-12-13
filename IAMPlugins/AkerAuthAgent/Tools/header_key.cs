using System;
using System.Collections.Generic;
using System.Text;

namespace AkAuthAgent
{
    public class header_key
    {
        public Int32 n;/* Valor inicial para n */
        public Byte[] chave; /* 16 bytes Chave de sessao a ser derivada */
        public Int32 nop;/* Nao usado. Padding do header para 8 bytes */

        public header_key()
        {
        }

        public header_key(Byte[] buffer)
        {
            if (buffer.Length != 24)
                throw new Exception("header error");

            chave = new Byte[16];
            Byte[] tmp = new Byte[4];
            Array.Copy(buffer, 0, tmp, 0, 4);
            Array.Reverse(tmp);
            n = BitConverter.ToInt32(tmp, 0);

            Array.Copy(buffer, 4, chave, 0, 16);
            nop = BitConverter.ToInt32(buffer, 20);
        }

        public Byte[] ToBytes()
        {
            Int32 offSet = 0;

            Byte[] retorno = new Byte[24];
            Array.Copy(BitConverter.GetBytes(n), 0, retorno, offSet, 4);
            offSet += 4;

            Array.Copy(chave, 0, retorno, offSet, 16);
            offSet += 16;

            Array.Copy(BitConverter.GetBytes(nop), 0, retorno, offSet, 2);

            return retorno;
        }
    }
}
