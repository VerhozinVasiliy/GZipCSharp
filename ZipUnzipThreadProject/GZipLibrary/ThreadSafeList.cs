using System;
using System.Collections.Generic;

namespace GZipLibrary
{
    public class ThreadSafeList<T> : IList<T>
    {
        protected readonly List<T> _interalList = new List<T>();

        // Other Elements of IList implementation

        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        private static object _lock = new object();

        public List<T> Clone()
        {
            List<T> newList = new List<T>();

            lock (_lock)
            {
                _interalList.ForEach(x => newList.Add(x));
            }

            return newList;
        }

        public void Add(T item)
        {
            _interalList.Add(item);
        }

        public void Clear()
        {
            _interalList.Clear();
        }

        public bool Contains(T item)
        {
            return Clone().Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Clone().CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _interalList.Remove(item);
        }

        public int Count => _interalList.Count;

        public bool IsReadOnly { get; }

        public int IndexOf(T item)
        {
            return Clone().IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _interalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _interalList.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return Clone()[index]; }
            set
            {
                _interalList[index] = value;
            }
        }
    }
}
