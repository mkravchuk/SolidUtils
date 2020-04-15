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
    public static class _BrepEdge
    {
        public static BrepTrim _FindTrim(this BrepEdge edge)
        {
            return edge.Brep.Trims.First(o => o.Edge == edge);
        }

        public static BrepVertex _GetStartVertex(this BrepEdge edge)
        {
            return edge.StartVertex;

            var trim = edge._FindTrim();
            var point =trim.Face.PointAt(trim.PointAtStart.X, trim.PointAtStart.Y);
            return trim.Brep.Vertices[point._GetVertexIndex(trim.Brep.Vertices, 1, true)];
        }
        public static BrepVertex _GetEndVertex(this BrepEdge edge)
        {
            return edge.EndVertex;

            var trim = edge._FindTrim();
            var point = trim.Face.PointAt(trim.PointAtEnd.X, trim.PointAtEnd.Y);
            return trim.Brep.Vertices[point._GetVertexIndex(trim.Brep.Vertices, 1, true)];
        }

    }
}
