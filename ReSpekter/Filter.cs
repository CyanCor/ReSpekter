using System.Collections.Generic;
using System.Linq;

namespace CyanCor.ReSpekter
{
    public class Filter<T> : IFilter<T>
    {
        private List<IFilter<T>> _childFilters = new List<IFilter<T>>();
        private List<FilterSubjectDelegate<T>> _acceptFilters = new List<FilterSubjectDelegate<T>>();

        public void Add(IFilter<T> filter)
        {
            if (filter != null)
            {
                _childFilters.Add(filter);
            }
        }

        public void Remove(IFilter<T> filter)
        {
            _childFilters.Remove(filter);
        }

        public void Add(FilterSubjectDelegate<T> acceptDelegate)
        {
            if (acceptDelegate != null)
            {
                _acceptFilters.Add(acceptDelegate);
            }
        }

        public void Remove(FilterSubjectDelegate<T> acceptDelegate)
        {
            _acceptFilters.Remove(acceptDelegate);
        }

        public bool Check(T subject)
        {
            if (_childFilters.Any(filter => filter.Check(subject)))
            {
                return true;
            }

            if (_acceptFilters.Any(@delegate => @delegate(subject)))
            {
                return true;
            }

            return false;
        }
    }
}