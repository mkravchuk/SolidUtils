using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    /// <summary>
    /// http://stackoverflow.com/questions/489258/linq-distinct-on-a-particular-property
    /// var query = people.DistinctBy(p => p.Id);
    /// var query = people.DistinctBy(p => new { p.Id, p.Name });
    /// </summary>
    public static class _Linq
    {
        public static void _Distinct<T>(this List<T> list)
        {
            var unique = list.Distinct().ToList();
            list.Clear();
            list.AddRange(unique);
        }

        public static IEnumerable<TSource> _DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static List<T> _Duplicate<T>(this List<T> list)
        {
            var res = new List<T>(list.Count);
            //for (int i = 0; i < list.Count; i++)
            //{
            //    res.Add(list[i]);
            //}
            res.AddRange(list);
            return res;
        }
    }
}
