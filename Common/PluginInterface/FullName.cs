using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class FullName
    {
        public String familyName;
        public String givenName;

        public String fullName { get { return givenName + " " + familyName; } }

        public FullName(String familyName, String givenName)
        {
            this.familyName = familyName;
            this.givenName = givenName;
        }

        public FullName(String fullName)
        {
            String[] names = fullName.Split(" ".ToCharArray(), 2);

            this.familyName = (names.Length > 1 ? names[1] : names[0]);
            this.givenName = names[0];
        }
    }
}
