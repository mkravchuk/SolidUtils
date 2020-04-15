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
    public static class _Face
    {
        public static Surface _Srf(this BrepFace face)
        {
            //var loops = face.Loops_ThreadSafe();
            //if (loops == null || loops.Count == 0) return face;
            //var trims = loops[0].Trims_ThreadSafe();
            //if (trims == null || trims.Count == 0) return face;
            //return trims[0]._Srf();

            return face.UnderlyingSurface();

        }

        public static Brep _Brep(this BrepFace face)
        {
            var loops = face.Loops_ThreadSafe();
            if (loops == null || loops.Count == 0) return null;
            var trims = loops[0].Trims_ThreadSafe();
            if (trims == null || trims.Count == 0) return null;
            return trims[0].Brep;
        }

        //public static string _GetName(this BrepFace face)
        //{
        //    var brep = face._Brep();
        //    if (brep == null) return "";
        //    how to get Object ???
        //    return face._GetName(brep._GetFaceName, 
        //}

        public static string _GetName(this BrepFace face, string objectName, Brep brep, bool shortNameIfPossible = true)
        {
            var facesCount = brep.Faces.Count;
            if (facesCount == 1 && shortNameIfPossible)
            {
                return objectName;
            }

            int nameLength = 1;
            if (facesCount >= 10) nameLength = 2;
            if (facesCount >= 100) nameLength = 3;
            if (facesCount >= 1000) nameLength = 4;
            if (facesCount >= 10000) nameLength = 5;
            return objectName + ":" + (face.FaceIndex + 1).ToString("D" + nameLength);
        }

        public static Point2d _GetMidPoint2d(this BrepFace face)
        {
            double u, v;
            if (face.ClosestPoint(face._GetCentroid(), out u, out v))
            {
                return new Point2d(u, v);
            }
            else
            {
                u = face.Domain(0).Mid;
                v = face.Domain(1).Mid;
                return new Point2d(u, v);
            }
        }

        public static Vector3d _NormalAt(this BrepFace face, Point2d midPoint)
        {
            var normal = face.NormalAt(midPoint.X, midPoint.Y);
            return normal;
        }

        public static Vector3d _NormalAt(this BrepFace face, Point3d point)
        {
            double u;
            double v;
            if (face.ClosestPoint(point, out u, out v))
            {
                var normal = face.NormalAt(u, v);
                return normal;
            }
            return Vector3d.Unset;
        }


        public static Vector3d _GetNormal(this BrepFace face)
        {
            var mid = face._GetMidPoint2d();
            return face._NormalAt(mid);
        }

        /// <summary>
        /// Save as '_GetNormal' but also returns midPoint of face
        /// </summary>
        /// <param name="face"></param>
        /// <param name="midPoint"></param>
        /// <returns></returns>
        public static Vector3d _GetNormalAndMidPoint3d(this BrepFace face, out Point3d midPoint)
        {
            var mid = face._GetMidPoint2d();
            midPoint = face.PointAt(mid.X, mid.Y);
            return face._NormalAt(mid);
        }


        public static Point3d _GetCentroid(this BrepFace face)
        {
            double area;
            return face._GetCentroidAndArea(out area);
        }

        public static Point3d _GetCentroidAndArea(this BrepFace face, out double area)
        {
            area = 0;
            //return GetAvaragePoint(face);
            var mesh = face.GetMesh(MeshType.Render);
            if (mesh == null)
            {
                mesh = face.GetMesh(MeshType.Any);
            }

            //if (mesh != null)
            //{
            //    return GetAvaragePoint(mesh); // a bit fast than 'AreaMassProperties.Compute'
            //}

            AreaMassProperties mas = (mesh != null && mesh.Faces.Count > 0)
                ? AreaMassProperties.Compute(mesh) // faster 10x times then compute from face
                : null    //AreaMassProperties.Compute(face)  - can be very-very-slow!
                ;
            if (mas != null)
            {
                area = mas.Area;
            }

            Point3d c = (mas != null)
                ? mas.Centroid
                : face.OuterLoop._GetCentroid(true); // In case when object is new and mesh is not generated yet - use avarage point calculations

            if (c == Point3d.Origin)
            {
                // how to get brep to try last chance to get centroid ???
            }

            //Point3f start = Point3f.Unset;
            //foreach (var v in mesh.Vertices)
            //{
            //    if (start == Point3f.Unset)
            //    {
            //        start = v;
            //    }
            //    else
            //    {
            //        start.X += v.X;
            //        start.Y += v.Y;
            //        start.Z += v.Z;
            //    }
            //}
            //c.X = start.X / mesh.Vertices.Count;
            //c.Y = start.Y / mesh.Vertices.Count;
            //c.Z = start.Z / mesh.Vertices.Count;

            //var mas = AreaMassProperties.Compute(face);
            //var c = mas.Centroid;

            double uclosest, vclosest;
            if (face.ClosestPoint(c, out uclosest, out vclosest))
            {
                c = face.PointAt(uclosest, vclosest);
            }
            return c;
        }

        /// <summary>
        ///  a bit fast than 'AreaMassProperties.Compute'
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        private static Point3d GetAvaragePoint(Mesh mesh)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (var i in mesh.Vertices)
            {
                x += i.X;
                y += i.Y;
                z += i.Z;
            }
            var c = mesh.Vertices.Count;
            return new Point3d(x / c, y / c, z / c);

            //var x = mesh.Vertices.Sum(o => o.X);
            //var y = mesh.Vertices.Sum(o => o.Y);
            //var z = mesh.Vertices.Sum(o => o.Z);
            //var c = mesh.Vertices.Count;
            //return new Point3d(x/c, y/c, z/c);
        }



        public static SurfacePoint _VertexToSurfacePoint(this BrepFace face, BrepVertex v)
        {
            return v._FindTrimSurfacePoint(face);
        }


        public static double _GetTotalLengthOfEdges(this BrepFace face)
        {
            double len = 0;
            foreach (var l in face.Loops_ThreadSafe())
            {
                foreach (var t in l.Trims_ThreadSafe())
                {
                    if (t.TrimType != BrepTrimType.Singular)
                    {
                        len += t.Edge._GetLength_ThreadSafe();
                    }
                }
            }

            return len;
        }

        public static SurfaceDomains _GetMinMaxUV(this BrepFace face, int divbyTrims = 2)
        {
            var crvs2d = new List<Curve>();
            // we should be carefull with multithreading - lets use safe access to loop trims
            foreach (var loop in face.Loops_ThreadSafe())
            {
                foreach (var trim in loop.Trims_ThreadSafe())
                {
                    crvs2d.Add(trim);
                }
            }
            return face._Srf()._GetMinMaxUV(crvs2d.ToArray(), divbyTrims);
        }

        public static double _DistanceTo_OutherLoop(this BrepFace face, Point3d p, List<Curve> outherLoopEdges = null)
        {
            var closest = face._ClosestPointToOutherLoop(p, outherLoopEdges);
            return closest._DistanceTo(p);
        }

        public static double _DistanceTo_OutherLoopPow2(this BrepFace face, Point3d p, List<Curve> outherLoopEdges = null)
        {
            var closest = face._ClosestPointToOutherLoop(p, outherLoopEdges);
            return closest._DistanceTo_Pow2(p);
        }


        public static Point3d _ClosestPointToOutherLoop(this BrepFace face, Point3d p, List<Curve> outherLoopEdges = null)
        {
            if (outherLoopEdges == null)
            {
                outherLoopEdges = face.OuterLoop._Trims_ThreadSafe()
               .Where(o => o.Edge != null)
               .Select(o => o.Edge.DuplicateCurve()).ToList();
            }

            var closestPoints = outherLoopEdges.Select(o =>
            {
                double t;
                o.ClosestPoint(p, out t);
                return o.PointAt(t);
            }).ToList();

            double minDistPow2 = Double.MaxValue;
            var closest = p;
            foreach (var pi in closestPoints)
            {
                var distPow2 = pi._DistanceTo_Pow2(p);
                if (distPow2 < minDistPow2)
                {
                    minDistPow2 = distPow2;
                    closest = pi;
                }
            }
            return closest;
        }

    }
}
