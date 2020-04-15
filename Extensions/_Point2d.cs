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
    public static class _Point2d
    {
        public static int GetVertexIndex(this Point2d point, BrepVertexList Vertices, double tolerence = 0.01)
        {
            //var point3d = Trim.Face.PointAt(point.X, point.Y);
            return 0;
        }


        public static Point3d _ToPoint3d(this Point2d p)
        {
            return new Point3d(p.X, p.Y, 0);
        }

        public static string _ToStringX(this Point2d p)
        {
            return p._ToStringX(2);
        }

        public static string _ToStringX(this Point2d p, int length)
        {
            var text = p.X._ToStringX(length)
                       + "*" + p.Y._ToStringX(length);
            return text;
        }
    }
}
