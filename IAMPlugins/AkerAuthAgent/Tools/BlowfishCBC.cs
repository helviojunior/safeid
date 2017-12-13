using System;

namespace AkAuthAgent
{
    class BlowfishCBC : Blowfish
    {
        public BlowfishCBC(Blowfish obj, byte[] IV)
            :base()
        {
            this.P = obj._P;
            this.S0 = obj._S0;
            this.S1 = obj._S1;
            this.S2 = obj._S2;
            this.S3 = obj._S3;
            base.SetIV(IV);
        }

        public BlowfishCBC(byte[] initializeKey, byte[] IV)
        {
            base.initializeKey(initializeKey);
            base.SetIV(IV);
        }

        public void decrypt(byte[] abyte0)
        {
            base.decryptCBC(abyte0, 0, abyte0.Length, abyte0, 0);
        }

        public void encrypt(byte[] abyte0)
        {
            base.encryptCBC(abyte0, 0, abyte0.Length, abyte0, 0);
        }
    }
}