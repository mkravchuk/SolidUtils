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
    public static class _Point3d
    {
        public static bool _IsSame(this Point3d p, Point3d p2)
        {
            return p.X._IsSame(p2.X)
                   && p.Y._IsSame(p2.Y)
                   && p.Z._IsSame(p2.Z);
        }

        public static bool _IsSame(this Point3d p, Point3d p2, double tol)
        {
            return p.X._IsSame(p2.X, tol)
                   && p.Y._IsSame(p2.Y, tol)
                   && p.Z._IsSame(p2.Z, tol);
        }

        public static double _SummOfXYZ(this Point3d p)
        {
            return p.X + p.Y + p.Z;
        }

        public static double _DistanceToMin(this Point3d p, params Point3d[] points)
        {
            var res = Double.MaxValue;
            foreach (var pi in points)
            {
                var dist = p._DistanceTo(pi);
                if (dist < res)
                {
                    res = dist;
                }
            }
            return res;
        }

        public static double _DistanceToPlane(this Point3d p, Point3d planePoint, Vector3d planeNormalNormalized)
        {
            //https://stackoverflow.com/questions/9605556/how-to-project-a-3d-point-to-a-3d-plane

            // 1) Make a vector from your orig point to the point of interest:
            // v = point - orig(in each dimension);
            Vector3d v = p - planePoint; // vector from point to plane point

            // 2) Take the dot product of that vector with the unit normal vector n :
            // dist = vx*nx + vy*ny + vz*nz; dist = scalar distance from point to plane along the normal
            double dist = v * planeNormalNormalized; // scalar distance from point to plane along the normal

            return dist;
        }

        /// <summary>
        /// Same as 'Point3d.DistanceTo' but without any check - always requaired tested code and 3 coordinates
        /// </summary>
        /// <param name="p"></param>
        /// <param name="toPoint"></param>
        /// <returns></returns>
        public static double _DistanceTo(this Point3d p, Point3d toPoint)
        {
            double dx = p.X - toPoint.X;
            double dy = p.Y - toPoint.Y;
            double dz = p.Z - toPoint.Z;
            return Math.Sqrt(dx*dx + dy*dy + dz*dz);
            //return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
        }

        public static double _DistanceTo(this Point3d p, Surface srf, double defValue)
        {
            return srf._DistanceTo(p, defValue);
        }

        public static double _DistanceTo(this Point3d p, Curve crv)
        {
            return crv._DistanceTo(p);
        }

        public static double _DistanceTo(this Point3d p, Curve crv, double maximumDistance, double defaultValue)
        {
            return crv._DistanceTo(p, maximumDistance, defaultValue);
        }


        /// <summary>
        /// Same as '_DistanceTo' but with power 2 ('dx^2 + dy^2 + dz^2')
        /// Used for speed optimization
        /// </summary>
        /// <param name="p"></param>
        /// <param name="toPoint"></param>
        /// <returns></returns>
        public static double _DistanceTo_Pow2(this Point3d p, Point3d toPoint)
        {
            double dx = p.X - toPoint.X;
            double dy = p.Y - toPoint.Y;
            double dz = p.Z - toPoint.Z;
            return dx*dx + dy*dy + dz*dz;
        }

        public static double _DistanceTo(this Point3d p, Point3d[] toPoints)
        {
            if (toPoints == null || toPoints.Length == 0) return 0;
            return Math.Sqrt(p._DistanceTo_Pow2(toPoints));
        }

        public static double _DistanceTo_Pow2(this Point3d p, Point3d[] toPoints)
        {
            if (toPoints == null || toPoints.Length == 0) return 0;
            return toPoints.Min(o => o._DistanceTo_Pow2(p));
        }

        public static string _ToStringX(this Point3d p, int length = 3)
        {
            var text = p.X._ToStringX(length)
                       + "," + p.Y._ToStringX(length)
                       + "," + p.Z._ToStringX(length);
            return text;
        }

        public static Point2d _ToPoint2d(this Point3d p)
        {
            return new Point2d(p.X, p.Y);
        }

        public static int _GetVertexIndex(this Point3d point, BrepVertexList Vertices, double tol = 10, bool throwExceptionIfNotFound = true)
        {
            double min_distance = 0;
            int min_index = -1;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                var distance = v.Location._DistanceTo(point);
                if (min_index == -1 || (distance < min_distance && distance < tol))
                {
                    min_distance = distance;
                    min_index = i;
                }
            }
            if (min_index == -1 && throwExceptionIfNotFound)
            {
                throw new Exception("_GetVertexIndex method cannot find vertex with specified point");
            }
            return min_index;
        }


        public static bool _IsCloseToCurve(this Point3d point, Curve crvMax, double maxAllowedDeviation)
        {
            double t;
            if (!crvMax.ClosestPoint(point, out t, maxAllowedDeviation))
            {
                return false;
            }
            var deviation = crvMax.PointAt(t)._DistanceTo(point);
            if (deviation > maxAllowedDeviation)
            {
                return false;
            }

            return true;
        }


        public static Curve _ClosestCurve(this Point3d point, Curve[] crvs, double maxAllowedDeviation = Double.MaxValue)
        {
            Curve closestCrv = null;
            double minDist = Double.MaxValue;
            foreach (var crv in crvs)
            {
                var dist = point._DistanceTo(crv, minDist, Double.MaxValue - 1);
                //Layers.Debug.AddCurve(crvi, "intersectionCurve");
                if (dist < minDist && dist < maxAllowedDeviation)
                {
                    closestCrv = crv;
                    minDist = dist;
                }
            }
            return closestCrv;
        }

        public static Point3d _RoundToDomain(this Point3d value, Interval domainU, Interval domainV)
        {
            var newU = value.X;
            var newV = value.Y;
            newU = newU._RoundToDomainMinMax(domainU, 0.001);
            newV = newV._RoundToDomainMinMax(domainV, 0.001);
            return new Point3d(newU, newV, 0);
        }

        public static bool _TryGetAngle(Point3d aStart, Point3d aEnd, Point3d bStart, Point3d bEnd, out double angleInDegree)
        {
            angleInDegree = 0;

            var a = aEnd - aStart;
            if (!a.Unitize())
            {
                // Zero length vector
                return false;
            }

            var b = bEnd - bStart;
            if (!b.Unitize())
            {
                // Zero length vector
                return false;
            }

            var angle = a._AngleOfUnitizedVectors(b);
            angleInDegree = angle._RadianToDegree();
            return true;
        }

        /// <summary>
        /// Sort points in 3d - get shortest path from start point of list to the end point
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <returns>Indexes of sorted positions</returns>
        public static List<int> _Sort(List<Point3d> points)
        {
            var res = new List<int>();
            var taken = new List<bool>();
            for (int i = 0; i < points.Count; i++)
            {
                taken.Add(false);
            }
            res.Add(0);
            for (int i = 1; i < points.Count - 1; i++)
            {
                Point3d p = points[res.Last()];
                int bestIndex = -1;
                double shortestDist = 0;
                for (int isub = 1 ; isub < points.Count-1; isub++)
                {
                    if (taken[isub]) continue;
                    double dist =  p._DistanceTo(points[isub]);
                    if (dist < shortestDist || bestIndex == -1)
                    {
                        shortestDist = dist;
                        bestIndex = isub;
                    }
                }
                taken[bestIndex] = true;
                res.Add(bestIndex);
            }
            res.Add(points.Count-1);
            return res;
        }
    }
}
