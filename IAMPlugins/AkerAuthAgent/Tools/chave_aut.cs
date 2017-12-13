using System;
using System.Collections.Generic;
using System.Text;

namespace AkAuthAgent
{
    public class chave_aut
    {
        public Blowfish key;		/* Chave de criptografia */
        public Byte[] akey;		/* Chave de autenticacao (16 bytes) */

        //Chave vinda do FW
        /* A funcao abaixo separa a chave basica recebida (16 bytes) em 3 chaves 
   distintas, uma para autenticacao e duas para criptografia */
        public chave_aut(Byte[] baseKey)
        {
            Byte[] b1 = new Byte[17];
            Array.Copy(baseKey, 0, b1, 0, 16);
            b1[16] = 81;

            md5 md5_1 = new md5(b1);
            md5_1.calc();

            Byte[] tmp = md5_1.getHash();

            key = new Blowfish();
            key.initializeKey(tmp);

            Byte[] b2 = new Byte[17];
            Array.Copy(baseKey, 0, b2, 0, 16);
            b2[16] = 34;

            md5 md5_2 = new md5(b2);
            md5_2.calc();
            akey = md5_2.getHash();

        }
    }
}
