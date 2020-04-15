using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{

    public interface IFastLinkedItem<T> 
        where T : class, IFastLinkedItem<T>
    {
        T LinkedItem_Prev { get; set; }
        T LinkedItem_Next { get; set; }
        FastLinkedList<T> LinkedItem_Owner { get; set; }
    }



    public class FastLinkedList_IEnumerator<T> : IEnumerator<T>
        where T : class, IFastLinkedItem<T>
    {
        private readonly FastLinkedList<T> list;
        T _current;
        public FastLinkedList_IEnumerator(FastLinkedList<T> list)
        {
            this.list = list;
           _current = null;
        }
        #region IEnumerator
        public T Current
        {
            get { return _current; }
        }

        public bool MoveNext()
        {
            var next = (_current == null)
                ? list.First
                : _current.LinkedItem_Next;
            if (next == null)
            {
                return false;
            }
            _current = next;
            return true;
        }

        public void Reset()
        {
            _current = null;
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {

        }

        #endregion

    }




    public class FastLinkedList<T> :  ICollection<T>
        where T : class, IFastLinkedItem<T>
    {
        public T First { get; private set; }
        public T Last { get; private set; }
        public int Count { get; private set; }
        public bool IsReadOnly { get; private set; }

        public FastLinkedList()
        {
            First = null;
            Last = null;
            Count = 0;
            IsReadOnly = false;
        }

        #region IEnumerable
        public IEnumerator<T> GetEnumerator()
        {
            return new FastLinkedList_IEnumerator<T>(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region ICollection

        public void Clear()
        {
            First = null;
            Last = null;
            Count = 0;
        }

        public bool Contains(T item)
        {
            return this.Any(o => item == o);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var current = First;
            if (current == null) return;
            int index = 0;
            while (current != null)
            {
                if (index >= arrayIndex) array[index] = current;
                index++;
                current = current.LinkedItem_Next;
            }

        }

        public void Add(T item)
        {
            if (item == null) return;
            if (First == null)
            {
                First = item;
                item.LinkedItem_Prev = null;
            }
            else
            {
                Last.LinkedItem_Next = item;
                item.LinkedItem_Prev = Last;
            }
            Last = item;
            item.LinkedItem_Next = null;
            item.LinkedItem_Owner = this;
            Count++;
        }

        public bool Remove(T item)
        {
            if (item == null) return false;
            if (item.LinkedItem_Owner == null) return false; // item should be registered 

            if (First == item && Last == item) // only one item in the list
            {
                First = null;
                Last = null;
            }
            else if (item.LinkedItem_Prev == null) // first 
            {
                First = First.LinkedItem_Next;
                First.LinkedItem_Prev = null;
            }
            else if (item.LinkedItem_Next == null) // last
            {
                Last = Last.LinkedItem_Prev;
                Last.LinkedItem_Next = null;
            }
            else
            {
                item.LinkedItem_Prev.LinkedItem_Next = item.LinkedItem_Next;
                item.LinkedItem_Next.LinkedItem_Prev = item.LinkedItem_Prev;
            }

            item.LinkedItem_Prev = null;
            item.LinkedItem_Next = null;
            item.LinkedItem_Owner = null;
            Count--;
            return true;
        }

        #endregion

        public int RemoveAny(Func<T, bool> removeCondition)
        {
            int saveCount = Count;
            var current = First;
            while (current != null)
            {
                var next = current.LinkedItem_Next;
                if (removeCondition(current))
                {
                    Remove(current);
                }
                current = next;
            }
            return Count-saveCount;  //retrun removed count
        }
    }
}
