using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class _Array
    {
        public static T[] _Shift_MoveIndexToFirst<T>(this T[] a, int index)
        {
            var newA = new T[a.Length];
            for (var i = 0; i < a.Length; i++)
            {
                var oldIndex = index + i;
                if (oldIndex > a.Length - 1) oldIndex = oldIndex - a.Length;
                newA[i] = a[oldIndex];
            }
            return newA;
        }

        public static T[] _Shift_MoveIndexToLast<T>(this T[] a, int index)
        {
            index++;
            if (index > a.Length - 1) index = 0;
            return a._Shift_MoveIndexToFirst(index);
        }

        public static void _EnsureCount<T>(this List<T> a, int newCount)
        {
            var addedCount = newCount - a.Count;
            if (addedCount > 0)
            {
                if (a.Capacity < newCount)
                {
                    a.Capacity = newCount;
                }
                a.AddRange(new T[addedCount]);
            }

            //while (addCount > 0)
            //{
            //    a.Add(default(T));
            //    addCount--;
            //}
        }

        public static List<T> _Shift_MoveIndexToFirst<T>(this List<T> a, int index)
        {
            var newA = new List<T>(a.Count);
            newA._EnsureCount(a.Count);
            for (var i = 0; i < a.Count; i++)
            {
                var oldIndex = index + i;
                if (oldIndex > a.Count - 1) oldIndex = oldIndex - a.Count;
                newA[i] = a[oldIndex];
            }
            return newA;
        }

        public static List<T> _Shift_MoveIndexToLast<T>(this List<T> a, int index)
        {
            index++;
            if (index > a.Count - 1) index = 0;
            return a._Shift_MoveIndexToFirst(index);
        }
    }
}
