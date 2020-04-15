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
    public static class _Vector3d
    {
        public static bool _IsSameDirection(Point3d T0srf, Point3d T1srf, Point3d T03d, Point3d T13d)
        {
            //
            // V1 - vectors
            // 
            var t2d = T1srf - T0srf;
            if (!t2d.Unitize())
            {
                // Zero length vector always be same direction
                return true;
            }

            var t3d = T13d - T03d;
            if (!t3d.Unitize())
            {
                // Zero length vector always be same direction
                return true;
            }
            var angle = t2d._AngleOfUnitizedVectors(t3d);
            var anleInDegree = angle._RadianToDegree();
            return anleInDegree < 90;

            //
            // V2 - distances
            // 
            //var minDistToT0srf = Math.Min(T0srf._DistanceTo(T03d), T0srf._DistanceTo(T13d));
            //var minDistToT1srf = Math.Min(T1srf._DistanceTo(T03d), T1srf._DistanceTo(T13d));
            //if (minDistToT0srf < minDistToT1srf)
            //{
            //    return T0srf._DistanceTo(T03d) < T0srf._DistanceTo(T13d);
            //}
            //else
            //{
            //    return T1srf._DistanceTo(T13d) < T1srf._DistanceTo(T03d);
            //}
        }

        public static string _ToStringX(this Vector3d v, int length = 3)
        {
            var text = v.X._ToStringX(length)
                       + "," + v.Y._ToStringX(length)
                       + "," + v.Z._ToStringX(length);
            return text;
        }

        /// <summary>
        /// Calculates angle between 2 vectors (same as 'Vector3d.VectorAngle' but faster)
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>Angle in radians</returns>
        public static double _AngleInRadians(this Vector3d v1, Vector3d v2)
        {
            //Vector3d.VectorAngle - slow
            var dot = (v1 * v2) / (Math.Sqrt(v1.SquareLength*v2.SquareLength));
            if (dot > 1) dot = 1;
            if (dot < -1) dot = -1;
            var angle = Math.Acos(dot);
            //An angle, θ, measured in radians, such that 0 ≤θ≤π
            //NaN if d < -1 or d > 1 or d equals NaN.
            return angle;
        }

        public static double _Dot(this Vector3d v1, Vector3d v2)
        {
            return v1 * v2;
        }

        public static double _AngleInDegrees(this Vector3d v1, Vector3d v2)
        {
            return _AngleInRadians(v1, v2)._RadianToDegree();
        }

        /// <summary>
        /// Calculates angle between 2 vectors (same as 'Vector3d.VectorAngle' but faster)
        /// Faster version of 'Vector3d.VectorAngle' since doesnt do Unitize for vectors (method do heavy call to 'UnsafeNativeMethods')
        /// An angle, θ, measured in radians, such that 0 ≤θ≤π
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>Angle in radians</returns>
        public static double _AngleOfUnitizedVectors(this Vector3d v1, Vector3d v2)
        {            
            var dot = v1 * v2;
            if (dot > 1) dot = 1;
            if (dot < -1) dot = -1;
            var angle = Math.Acos(dot);
            //An angle, θ, measured in radians, such that 0 ≤θ≤π
            //NaN if d < -1 or d > 1 or d equals NaN.
            return angle;
        }
    }
}
