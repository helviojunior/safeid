using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.Filters
{

    public enum FilterConditionType
    {
        Equal = 0,
        NotEqual,
        Contains,
        NotContains,
        StartWith,
        EndWith,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual
    }

    public enum FilterSelector
    {
        OR = 0,
        AND
    }

    public enum DataType
    {
        Text = 0,
        DateTime,
        Numeric
    }

}
