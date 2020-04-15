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
    public class BrepVertexToTrimsRelation
    {
        public int VertexIndex { get; set; }
        public Point3d VertexLocation { get; set; }

        public double minDeviationToEdges;
        public double maxDeviationToEdges;

        public double minDeviationToTrims;
        public double maxDeviationToTrims;
        public SurfacePoint closestTrimPoint2d;
        public List<BrepEdge> Edges; // edges that contact with vertex
        public List<Point3d> EdgesPoints; //points on edges that are closest to vertex

        public BrepVertexToTrimsRelation(int vertexIndex, Point3d vertexLocation)
        {
            Edges = new List<BrepEdge>();
            EdgesPoints = new List<Point3d>();
            VertexIndex = vertexIndex;
            VertexLocation = vertexLocation;
            minDeviationToEdges = Double.MaxValue;
            maxDeviationToEdges = Double.MinValue;

            minDeviationToTrims = Double.MaxValue;
            maxDeviationToTrims = Double.MinValue;
        }
    }
    
    public static class _BrepVertex
    {
        public static SurfacePoint _FindTrimSurfacePoint(this BrepVertex v, BrepFace face)
        {
            foreach (var loop in face.Loops_ThreadSafe())
            {
                foreach (var trim in loop.Trims_ThreadSafe())
                {
                    if (trim.Edge != null)
                    {
                        var indexStart = trim.Edge._GetStartVertex().VertexIndex;
                        var indexEnd = trim.Edge._GetEndVertex().VertexIndex;
                        if (v.VertexIndex == indexStart
                            || v.VertexIndex == indexEnd)
                        {
                            var trimStartPoint = face.PointAt(trim.PointAtStart.X, trim.PointAtStart.Y);
                            var trimEndPoint = face.PointAt(trim.PointAtEnd.X, trim.PointAtEnd.Y);
                            if (v.Location._DistanceTo(trimStartPoint) < v.Location._DistanceTo(trimEndPoint))
                            {
                                return new SurfacePoint(trim.PointAtStart);
                            }
                            else
                            {
                                return new SurfacePoint(trim.PointAtEnd);
                            }
                        }
                    }
                }
            }
            throw new Exception("Cannot find UV for BrepVertex (in method _BrepVertex._ToSurfacePoint)");
        }

        /// <summary>
        /// Finds vertex relation to edges
        /// </summary>
        /// <param name="v">tested vertex</param>
        /// <param name="face"></param>
        /// <returns>Relation to edges or null if this vertex doesnt belong to any edge</returns>
        public static BrepVertexToTrimsRelation _FindRelationsToCrvs(this BrepVertex v, BrepFace face)
        {
            BrepVertexToTrimsRelation res = null;
            var p = v.Location;

            foreach (var loop in face.Loops_ThreadSafe())
            {
                foreach (var trim in loop.Trims_ThreadSafe())
                {
                    if (trim.Edge != null)
                    {
                        var indexStart = trim.Edge._GetStartVertex().VertexIndex;
                        var indexEnd = trim.Edge._GetEndVertex().VertexIndex;
                        if (v.VertexIndex == indexStart
                            || v.VertexIndex == indexEnd)
                        {
                            //var distToEdgesStart = trim.Edge.StartVertex.Location.DistanceTo(p);
                            //var distToEdgesEnd = trim.Edge.EndVertex.Location.DistanceTo(p);
                            
                            var distToEdgesStart = trim.Edge.PointAt(trim.Edge.Domain.T0)._DistanceTo(p);
                            var distToEdgesEnd = trim.Edge.PointAt(trim.Edge.Domain.T1)._DistanceTo(p);
                            

                            var trimStartPoint2d = new Point2d(trim.PointAtStart.X, trim.PointAtStart.Y);
                            var trimEndPoint2d = new Point2d(trim.PointAtEnd.X, trim.PointAtEnd.Y);
                            var trimStartPoint3d = face.PointAt(trimStartPoint2d.X, trimStartPoint2d.Y);
                            var trimEndPoint3d = face.PointAt(trimEndPoint2d.X, trimEndPoint2d.Y);
                            var sameDirection = trim._IsSameDirectionToEdge();
                            if (!sameDirection)
                            {
                                var tmp2d = trimStartPoint2d;
                                trimStartPoint2d = trimEndPoint2d;
                                trimEndPoint2d = tmp2d;
                                var tmp3d = trimStartPoint3d;
                                trimStartPoint3d = trimEndPoint3d;
                                trimEndPoint3d = tmp3d;
                            }

                            var distToTrimsStart = trimStartPoint3d._DistanceTo(p);
                            var distToTrimsEnd = trimEndPoint3d._DistanceTo(p);

                            if (res == null)
                            {
                                res = new BrepVertexToTrimsRelation(v.VertexIndex, v.Location);
                            }
                            res.Edges.Add(trim.Edge);
                            res.EdgesPoints.Add( trim.Edge.PointAt(distToEdgesStart < distToEdgesEnd ? trim.Edge.Domain.T0 : trim.Edge.Domain.T1));

                            if (v.VertexIndex == indexStart)
                            {
                                if (distToTrimsStart < res.minDeviationToTrims)
                                {
                                    res.closestTrimPoint2d = new SurfacePoint(trimStartPoint2d);
                                }
                                res.minDeviationToEdges = Math.Min(distToEdgesStart, res.minDeviationToEdges);
                                res.maxDeviationToEdges = Math.Max(distToEdgesStart, res.maxDeviationToEdges);
                                res.minDeviationToTrims = Math.Min(distToTrimsStart, res.minDeviationToTrims);
                                res.maxDeviationToTrims = Math.Max(distToTrimsStart, res.maxDeviationToTrims);
                            }
                            if (v.VertexIndex == indexEnd)
                            {
                                if (distToTrimsEnd < res.minDeviationToTrims)
                                {
                                    res.closestTrimPoint2d = new SurfacePoint(trimEndPoint2d);
                                }
                                res.minDeviationToEdges = Math.Min(distToEdgesEnd, res.minDeviationToEdges);
                                res.maxDeviationToEdges = Math.Max(distToEdgesEnd, res.maxDeviationToEdges);
                                res.minDeviationToTrims = Math.Min(distToTrimsEnd, res.minDeviationToTrims);
                                res.maxDeviationToTrims = Math.Max(distToTrimsEnd, res.maxDeviationToTrims);
                            }
                        }
                    }
                }
            }

            if (res == null)
            {
                //throw new Exception("Cannot find relation for BrepVertex to Crvs (in method _BrepVertex._FindRelationsToCrvs)");
            }
            return res;
        }
    }
}
