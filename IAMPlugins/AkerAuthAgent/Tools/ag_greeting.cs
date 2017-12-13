using System;
using System.Collections.Generic;
using System.Text;

namespace AkAuthAgent
{
    public class ag_greeting
    {
        public byte versao;			/* Versao do autenticador (usar define acima) */
        public byte sub_ver;			/* Sub-versao (idem) */
        public byte release;			/* Release (idem) */
        public byte plataforma;		/* Plataforma onde o agente esta rodando 1 = AKER_UNIX, 2 = AKER_NT*/
        public Int32 n_users;		/* Numero de usuarios cadastrados */

        public ag_greeting()
        {
        }

        public ag_greeting(Byte[] buffer)
        {
            versao = buffer[0];
            sub_ver = buffer[1];
            release = buffer[2];
            plataforma = buffer[3];

            Byte[] bUsers = new Byte[4];

            Array.Copy(buffer, 4, bUsers, 0, 4);
            Array.Reverse(bUsers);
            n_users = BitConverter.ToInt32(bUsers, 0);
        }

        public Byte[] ToBytes()
        {
            Byte[] retorno = new Byte[8];
            retorno[0] = versao;
            retorno[1] = sub_ver;
            retorno[2] = release;
            retorno[3] = plataforma;

            Byte[] bUsers = new Byte[4];
            Array.Copy(BitConverter.GetBytes(n_users), 0, bUsers, 0, 4);
            Array.Reverse(bUsers);

            Array.Copy(bUsers, 0, retorno, 4, 4);
            return retorno;
        }
    }
}
