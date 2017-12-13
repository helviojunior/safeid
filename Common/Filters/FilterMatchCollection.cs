using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace IAM.Filters
{
    public class FilterMatchCollection : IEnumerable, IDisposable
    {
        private List<FilterMatch> elements;

        public FilterMatchCollection()
        {
            this.elements = new List<FilterMatch>();
        }

        public void AddMatch(FilterMatch m)
        {
            this.elements.Add(m);
        }

        public IEnumerator GetEnumerator()
        {
            return new FilterMatchEnumerator(this);
        }

        public void Dispose()
        {

        }

        private class FilterMatchEnumerator : IEnumerator
        {
            private int position = -1;
            private FilterMatchCollection m;

            public FilterMatchEnumerator(FilterMatchCollection m)
            {
                this.m = m;
            }

            public bool MoveNext()
            {
                if (position < m.elements.Count - 1)
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
                    return m.elements[position];
                }
            }
        }
    }
}
