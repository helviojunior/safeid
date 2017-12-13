using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.CA
{
    public enum X509altNameTag
    {
        TAG_MASK = 0x1F,
        OtherName = 0,
        Rfc822Name = 1,
        DNSName = 2,
        X400Address = 3,
        DirectoryName = 4,
        EdiPartyName = 5,
        Uri = 6,
        IPAddress = 7,
        RegisteredID = 8
    }
}
