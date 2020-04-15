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
    public static class _BrepLoop
    {
        

        /// <summary>
        /// Return index of vertexes used by this loop
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="indexesCircular">each index that will be in this list point to circular vertexes: crv start and end points are same - zero length or cirvular or wrong defined points</param>
        /// <returns></returns>
        public static List<int> _GetVertexesIndex(this BrepLoop loop, out List<int> indexesCircular)
        {
            var vertices = loop.Brep.Vertices;
            var indexes = new List<int>();
            indexesCircular = new List<int>(); //crv start and end points are same - zero length or cirvular or wrong defined points

            foreach (var t in loop.Trims_ThreadSafe())
            {
                //check for supported trim types
                switch (t.TrimType)
                {
                    case BrepTrimType.Boundary:
                    case BrepTrimType.Mated:
                    case BrepTrimType.Seam:
                    case BrepTrimType.Singular:
                        break;
                    default:
                        throw new Exception("Exception: class _BrepLoop._GetVertexesIndex - Not supported type in trims: " + t.ObjectType);
                }

                int indexBegin = t._StartVertexIndex();
                if (!indexes.Contains(indexBegin))
                {
                    indexes.Add(indexBegin);
                }

                int indexEnd = t._EndVertexIndex();
                if (!indexes.Contains(indexEnd))
                {
                    indexes.Add(indexEnd);
                }

                if (indexBegin == indexEnd && t.TrimType != BrepTrimType.Singular)
                {
                    indexesCircular.Add(indexBegin);
                }
            }

            indexes.Sort();
            return indexes;
        }

        public static double _GetLength3d(this BrepLoop loop)
        {
            double res = 0;
            foreach (var trim in loop.Trims_ThreadSafe())
            {
                if (trim.TrimType == BrepTrimType.Singular) continue;
                res += trim.Edge._GetLength_ThreadSafe();
            }
            return res;
        }

        public static Curve[] _GetJoinedTrims3d(this BrepLoop loop, double tol = 1)
        {
            var crvs = new List<Curve>();
            foreach (var trim in loop.Trims_ThreadSafe())
            {
                if (trim.TrimType == BrepTrimType.Singular) continue;
                //var crv3d = Srf._Convert2dCurveTo3d(trim.Crv);
                var crv3d = trim._2dTo3d(trim._Srf())._Complexify(100);
                crvs.Add(crv3d);
            }

            var res = Curve.JoinCurves(crvs, tol);
            return res;
        }

        public static Curve[] _GetJoinedEdges(this BrepLoop loop, double tol = 1)
        {
            var crvs = new List<Curve>();
            foreach (var trim in loop.Trims_ThreadSafe())
            {
                if (trim.TrimType == BrepTrimType.Singular) continue;
                var crv3d = trim.Edge._Complexify(100);
                crvs.Add(crv3d);
            }

            var res = Curve.JoinCurves(crvs, tol);
            return res;
        }


        /// <summary>
        ///  Get middle point of edges in 3d space.
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="accurate">Accurate takes more time but have great precision.</param>
        /// <returns></returns>
        public static Point3d _GetCentroid(this BrepLoop loop, bool accurate)
        {
            if (loop == null)
            {
                log.wrong("_BrepLoop._GetCentroid - loop is null");
                return Point3d.Origin;
            }
            var loopEdges = loop._Trims_ThreadSafe()
                .Where(o=>o.Edge != null)
                .Select(o => (Curve)o.Edge).ToArray();
            return loop.Face._Srf()._GetCentroid(loopEdges, accurate);
        }

    }
}
