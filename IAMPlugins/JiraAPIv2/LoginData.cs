using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace JiraAPIv2
{
    [Serializable()]
    class LoginData
    {
        [OptionalField()]
        public Boolean loginSucceeded;

        [OptionalField()]
        public Boolean captchaFailure;

    }
}
