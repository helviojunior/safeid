using System;
using System.Security.Cryptography;
using System.Text;

namespace AkAuthAgent
{
    class md5
    {
        byte[] abyte0;
        Byte[] hash;

        public md5(String s)
        {
            abyte0 = Encoding.ASCII.GetBytes(s);
        }

        public md5(byte[] abyte0)
        {
            this.abyte0 = abyte0;
        }

        public void calc()
        {
            
            MD5 md5 = MD5.Create();
            Byte[] tst = md5.ComputeHash(abyte0);
            hash = tst;


            Byte[] bA = new Byte[4];
            Array.Copy(tst, 0, bA, 0, bA.Length);

            Byte[] bB = new Byte[4];
            Array.Copy(tst, 4, bB, 0, bB.Length);

            Byte[] bC = new Byte[4];
            Array.Copy(tst, 8, bC, 0, bC.Length);

            Byte[] bD = new Byte[4];
            Array.Copy(tst, 12, bD, 0, bD.Length);

            A = BitConverter.ToInt32(bA, 0);
            B = BitConverter.ToInt32(bB, 0);
            C = BitConverter.ToInt32(bC, 0);
            D = BitConverter.ToInt32(bD, 0);

        }

        public Byte[] getHash()
        {
            return hash;
        }

        public Int32[] getregs()
        {
            Int32[] ai = {
            A, B, C, D
        };
            return ai;
        }

        public Int32 A;
        public Int32 B;
        public Int32 C;
        public Int32 D;

    }
}