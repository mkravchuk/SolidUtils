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
    public class _Extrusion_GetSizes
    {
        public Curve Crv;
        internal double Length;
        public string  Name;
        public double  Size;
    }

    internal class _Extrusion_TrimsSort
    {
        public bool IsReversed;
        public BrepTrim trim;
        public double length;
        public override string ToString()
        {
            return length._ToStringX(3);
        }
    }

    public static class _Extrusion
    {
        public static _Extrusion_GetSizes[] _GetSizes(this Extrusion e)
        {
            var pointAtStart = e.PathStart;
            var brep = e.ToBrep();

            // Get edges contacted with start point of Extrusion
            var trims = new List<_Extrusion_TrimsSort>();
            foreach (var f in brep.Faces)
            {
                foreach (var l in f._Loops_ThreadSafe())
                {
                    foreach (var t in l._Trims_ThreadSafe())
                    {
                        if (t.Edge != null)
                        {
                            if (t.Edge.PointAtStart._DistanceTo(pointAtStart)._IsZero())
                            {
                                trims.Add(new _Extrusion_TrimsSort() { trim = t });
                            }
                            else if (t.Edge.PointAtEnd._DistanceTo(pointAtStart)._IsZero())
                            {
                                trims.Add(new _Extrusion_TrimsSort() { trim = t, IsReversed = true });
                            }

                        }
                    }
                }
            }

            // Set lengths for every edge
            foreach (var trim in trims)
            {
                trim.length = trim.trim.Edge._GetLength_ThreadSafe();
            }

            // Sort
            trims.Sort((a, b) =>
            {
                if (a.length._IsSame(b.length))
                {
                    return 0;
                }
                return a.length - b.length < 0 ? 1 : -1;
            });

            // Remove duplicated edges
            for (int i = trims.Count - 1; i >= 1; i--) // loop exluding first
            {
                var trim = trims[i];
                var trimPrev = trims[i - 1];
                var trim_StartPoint = !trim.IsReversed ? trim.trim.Edge.PointAtStart : trim.trim.Edge.PointAtEnd;
                var trim_EndPoint = !trim.IsReversed ? trim.trim.Edge.PointAtEnd : trim.trim.Edge.PointAtStart;
                var trimPrev_StartPoint = !trimPrev.IsReversed ? trimPrev.trim.Edge.PointAtStart : trimPrev.trim.Edge.PointAtEnd;
                var trimPrev_EndPoint = !trimPrev.IsReversed ? trimPrev.trim.Edge.PointAtEnd : trimPrev.trim.Edge.PointAtStart;
                if (trim_StartPoint.DistanceTo(trimPrev_StartPoint)._IsZero()
                    && trim_EndPoint.DistanceTo(trimPrev_EndPoint)._IsZero())
                {
                    trims.RemoveAt(i);
                }
            }


            // Reverse if needed and return results
            var res =  trims.Select(o =>
            {
                var crv = o.trim.Edge.DuplicateCurve();
                if (o.IsReversed)
                {
                    crv.Reverse();
                }
                return new _Extrusion_GetSizes
                {
                    Crv = crv,
                    Length = o.length
                };
            }).ToArray();


            // Set names and values
            foreach (var s in res)
            {
                s.Size = s.Length;
            }
            if (res.Length == 3)
            {
                res[0].Name = "Width";
                res[1].Name = "Height";
                res[2].Name = "Thickness";
            }
            else if (res.Length == 2)
            {
                res[0].Name = "Width";
                res[1].Name = "Radius";
                res[1].Size = res[1].Length / (2 * Math.PI);
            }

            return res;
        }
    }
}
