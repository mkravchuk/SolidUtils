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
    public static class _NurbsCurvePointList
    {
        public static Point3d[] _Locations(this NurbsCurvePointList points)
        {
            //return points.Select(o => o.Location).ToArray();
            var count = points.Count;
            var res = new Point3d[count];
            for (int i = 0; i < count; i++)
            {
                res[i] = points[i].Location;
            }
            return res;
        }
        public static List<SurfacePoint> _SurfacePoints(this NurbsCurvePointList points)
        {
            var count = points.Count;
            var res = new List<SurfacePoint>(count);
            for (int i = 0; i < count; i++)
            {
                res.Add(new SurfacePoint(points[i].Location));
            }
            return res;
        }
    }
}
