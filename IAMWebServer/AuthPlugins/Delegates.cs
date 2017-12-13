using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.AuthPlugins
{
    public delegate void AuthEvent(Object sender, AuthEventType type, String text);

}
