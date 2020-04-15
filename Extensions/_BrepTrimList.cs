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
    public static class _BrepTrimList
    {


        public static List<BrepTrim> _SelectNonSingularTrims(this BrepTrimList trims)
        {
            trims._InitThreadSafe();
            var res = new List<BrepTrim>();
            foreach (var trim in trims)
            {
                switch (trim.TrimType)
                {
                    case BrepTrimType.Boundary:
                    case BrepTrimType.Mated:
                    case BrepTrimType.Seam:
                        res.Add(trim);
                        //var edgeLength = trim.Edge.GetLength();
                        //var trimLength = trim.GetLength();
                        //if (edgeLength < 0.0001)
                        //{
                        //    var nothing = 0;
                        //    // do not add very small edges
                        //}
                        //else
                        //{
                        //    trimsUnsorted.Add(trim);
                        //}
                        break;
                    case BrepTrimType.Singular:
                        // nothing - dont copy singular trims
                        int i = 0;
                        break;
                    default:
                        throw new Exception("Exception: class Object_BrepLoop - Not supported type in trims: " + trim.ObjectType);
                }
            }
            return res;
        }

        public static List<BrepTrim> _SortByCrvs3d(this BrepTrimList trims, out List<bool> trimsReversedFlag, out bool crvs3dHasReversedDirection)
        {
            var reversedDirectionCount = 0;
            trimsReversedFlag = new List<bool>();
            var trimsUnsorted = trims._SelectNonSingularTrims();


            var trimsSorted = new List<BrepTrim>();
            var searchForIndex = -1;
            //var debugIndexes = new List<int>();
            //var debugIs = new List<int>();
            while (trimsUnsorted.Count > 0)
            {
                bool found = false;
                int foundIndex = 0;
                for (var i = 0; i < trimsUnsorted.Count; i++)
                {
                    var trim = trimsUnsorted[i];
                    //var indexStart = trim.Edge.PointAtStart._GetVertexIndex(trim.Brep.Vertices);
                    //var indexEnd = trim.Edge.PointAtEnd._GetVertexIndex(trim.Brep.Vertices);
                    var indexStart = trim.Edge._GetStartVertex().VertexIndex;
                    var indexEnd = trim.Edge._GetEndVertex().VertexIndex;
                    if (searchForIndex == -1)
                    {
                        searchForIndex = indexStart;
                    }
                    if (!found && (indexStart == searchForIndex || indexEnd == searchForIndex))
                    {
                        found = true;
                        foundIndex = i;
                        // continue to search for circular crvs - that PointStart == PointEnd
                    }
                    if (indexStart == searchForIndex && indexStart == indexEnd)
                    {
                        found = true;
                        foundIndex = i;
                        break; //if found circular crv stop search - circular crvs have the highest priority
                    }
                }

                if (found)
                {
                    var trim = trimsUnsorted[foundIndex];
                    var indexStart = trim.Edge._GetStartVertex().VertexIndex;
                    var indexEnd = trim.Edge._GetEndVertex().VertexIndex;
                    var distToStart = trim.Edge.PointAtStart._DistanceTo(trim.Face.PointAt(trim.PointAtStart.X, trim.PointAtStart.Y));
                    var distToEnd = trim.Edge.PointAtStart._DistanceTo(trim.Face.PointAt(trim.PointAtEnd.X, trim.PointAtEnd.Y));
                    if (distToStart < distToEnd) reversedDirectionCount--;
                    if (distToStart > distToEnd) reversedDirectionCount++;

                    //debugIndexes.Add(indexStart);
                    //debugIndexes.Add(indexEnd);
                    trimsUnsorted.RemoveAt(foundIndex);
                    //debugIs.Add(foundIndex);
                    trimsSorted.Add(trim);

                    // Detect reversed flag: a) end->start - straight connection b)end->end - reversed connection
                    var straightConnection = (indexStart == searchForIndex);
                    // for circular crvs simple detection if connection is staight doesnt work - so lets do precision calculation
                    if (indexStart == indexEnd)
                    {

                    }
                    if (straightConnection)
                    {
                        trimsReversedFlag.Add(false);
                        searchForIndex = indexEnd;
                    }
                    else
                    {
                        trimsReversedFlag.Add(true);
                        searchForIndex = indexStart;
                    }
                }
                if (!found)
                {
                    throw new Exception(
                        "Exception:  Cannot find next trim! (Issue_SurfaceBad_BadTrim_Singular.SortTrims)");
                }
            }
            crvs3dHasReversedDirection = reversedDirectionCount > 0;
            return trimsSorted;
        }

        public static List<Point3d[]> _GetTrimControlPoints(this BrepTrimList loopTrims)
        {
            loopTrims._InitThreadSafe();

            var res = new List<Point3d[]>();
            foreach (var trim in loopTrims)
            {
                var points = trim.TrimType == BrepTrimType.Singular
                    ? new[] { trim.PointAtStart, trim.PointAtEnd }
                    : trim._ToNurbsCurve().Points.Select(o => o.Location).ToArray();
                res.Add(points);
            }
            return res;
        }


    }
}
