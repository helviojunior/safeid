using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallWizard
{
    public interface ICreateScript
    {
        string Provider { get; }
        string Command { get; }
        string Precondition { get; }
        double Serial { get; }
    }
}
