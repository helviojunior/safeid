using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace IAM.Filters
{

    [Serializable]
    public class FilterRuleCollection : IEnumerable, IDisposable
    {
        private List<FilterRule> elements;

        public FilterRuleCollection()
        {
            this.elements = new List<FilterRule>();
        }

        public void AddFilterRule(FilterRule f)
        {
            this.elements.Add(f);
        }

        public IEnumerator GetEnumerator()
        {
            return new FilterRuleEnumerator(this);
        }

        public void Dispose()
        {

        }

        private class FilterRuleEnumerator : IEnumerator
        {
            private int position = -1;
            private FilterRuleCollection f;

            public FilterRuleEnumerator(FilterRuleCollection f)
            {
                this.f = f;
            }

            public bool MoveNext()
            {
                if (position < f.elements.Count - 1)
                {
                    position++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                position = -1;
            }

            public object Current
            {
                get
                {
                    return f.elements[position];
                }
            }
        }
    }
}
