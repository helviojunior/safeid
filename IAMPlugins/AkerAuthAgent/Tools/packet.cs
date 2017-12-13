using System;
using System.Collections.Generic;
using System.Text;

namespace AkAuthAgent
{
    public class packet : header_aut
    {
        private Byte[] _body;

        public Byte[] body { get { return _body; } set { _body = value; } }

        public packet(Byte[] buffer)
            : base(buffer)
        { }

        public Object Clone()
        {
            packet pkt = new packet(base.ToBytes());
            pkt.body = this.body;
            return pkt;
        }

        public Boolean CheckMD5(chave_aut chave)
        {
            if (_body == null)
                _body = new Byte[0];

            header_aut tmp = (header_aut)this.Clone();
            tmp.md5 = new Byte[0];
            tmp.nop = 0;

            Byte[] header = tmp.ToBytes();
            Byte[] md5calc = new Byte[header.Length + _body.Length];
            Array.Copy(header, 0, md5calc, 0, header.Length);
            Array.Copy(_body, 0, md5calc, header.Length, _body.Length);


            Byte[] msgBuffer = new Byte[16 + 16 + md5calc.Length];
            Array.Copy(chave.akey, 0, msgBuffer, 0, 16);
            Array.Copy(md5calc, 0, msgBuffer, 16, md5calc.Length);
            Array.Copy(chave.akey, 0, msgBuffer, 16 + md5calc.Length, 16);

            md5 md5 = new md5(msgBuffer);
            md5.calc();

            return (BitConverter.ToString(md5.getHash()) == BitConverter.ToString(base.md5));
        }

    }
}
