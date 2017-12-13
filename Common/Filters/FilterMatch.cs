using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.Filters
{
    public class FilterMatch : IDisposable
    {
        public Boolean Success { get; protected set; }
        public FilterRule Filter { get; protected set; }

        public FilterMatch(Boolean success, FilterRule filter)
        {
            this.Success = success;
            this.Filter = filter;
        }

        public void Dispose()
        {
            if (this.Filter != null)
                this.Filter.Dispose();

            this.Filter = null;
        }
    }
}
