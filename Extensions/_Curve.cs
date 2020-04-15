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
using Rhino.Geometry.Intersect;

namespace SolidUtils
{
    public struct CurvesConnectionInfo
    {
        public readonly CurveEnd Crv1End;
        public readonly CurveEnd Crv2End;
        public readonly double c1T;
        public readonly double c2T;
        public readonly Point3d Crv1Point;
        public readonly Point3d Crv2Point;
        public readonly double Distance;

        //public double Angle
        //{
        //    get
        //    {
        //        var c1 = crv1.CurvatureAt(connInfo.c1T);
        //        var c2 = crv2.CurvatureAt(connInfo.c2T);
        //        var angle = c1._Angle(c2);
        //    }
        //}

        public CurvesConnectionInfo(Curve crv1, Curve crv2, CurveEnd crv1End, CurveEnd crv2End)
        {
            Crv1End = crv1End;
            Crv2End = crv2End;
            c1T = crv1._T(crv1End);
            c2T = crv2._T(crv2End);
            Crv1Point = crv1.PointAt(c1T);
            Crv2Point = crv2.PointAt(c2T);
            Distance = Crv1Point._DistanceTo(Crv2Point);
        }
    }

    public static class _Curve
    {
        public static CurveEnd _GetClosestCurveEnd(this Curve crv, Point3d point)
        {
            var distToStart = crv.PointAtStart._DistanceTo_Pow2(point);
            var distToEnd = crv.PointAtEnd._DistanceTo_Pow2(point);
            var res = (distToStart < distToEnd) ? CurveEnd.Start : CurveEnd.End;
            return res;
        }

        public static Point3d _P0(this Curve crv)
        {
            return crv.PointAt(crv.Domain.T0);
        }
        public static Point3d _P1(this Curve crv)
        {
            return crv.PointAt(crv.Domain.T1);
        }

        /// <summary>
        /// Return 3d point for 2d and 3d curves.
        /// Same as _PX but always converts 2d points into 3d.
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="end"></param>
        /// <param name="srf"></param>
        /// <returns>3d point always</returns>
        public static Point3d _P3d(this Curve crv, CurveEnd end, Surface srf)
        {
            return crv._P3d(crv._T(end), srf);
        }
        public static Point3d _P3d(this Curve crv, double t, Surface srf)
        {
            var pointXD =  crv.PointAt(t);
            return (crv.Dimension == 2)
                ? srf._PointAt(pointXD)
                : pointXD;
        }

        public static Point3d _P(this Curve crv, CurveEnd end)
        {
            return crv.PointAt(crv._T(end));
        }

        public static Point3d _P(this Curve crv, double t)
        {
            return crv.PointAt(t);
        }

        public static Point3d _PointAtPercent(this Curve crv, Percent p)
        {
            return new CurveNormalized(crv).PointAt(p);
        }

        public static double _TAtPercent(this Curve crv, Percent p)
        {
            return new CurveNormalized(crv).T(p);
        }
        public static Point3d _PointAtMid(this Curve crv)
        {
            //return crv.PointAt(crv.Domain.Mid); - old, wrong way because Domain.Mid can be only at 30% of curve length
            return new CurveNormalized(crv).PointAtMid;
        }

        public static double _T(this Curve crv, CurveEnd end)
        {
            switch (end)
            {
                case CurveEnd.Start:
                    return crv.Domain.T0;
                case CurveEnd.End:
                    return crv.Domain.T1;
                case CurveEnd.None:
                    throw new Exception("CurveEnd parameter cannot be NONE - in method _Curve._T(Curve crv, CurveEnd end)");
            }
            return 0;
        }

        public static double _T(this Curve crv, Point3d point)
        {
            double res;
            if (!crv.ClosestPoint(point, out res))
            {
                throw new Exception("_Curve._T(Point3d point)  failed to get T from 3d point!");
            }
            return res;
        }


        public static Vector3d _Tangent(this Curve crv, CurveEnd end)
        {
            return crv.TangentAt(crv._T(end));
        }


        /// <summary>
        /// Tangent to face in middle of the curve.
        /// Tangent direction is from face.
        /// Perpendicular to curve tangent.
        ///
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="face"></param>
        /// <param name="pointCrvMid"></param>
        /// <param name="tangentToFace"></param>
        /// <returns></returns>
        public static bool _TangentToFace(this Curve crv, BrepFace face, out Point3d pointCrvMid, out Vector3d tangentToFace)
        {
            tangentToFace = Vector3d.Zero;

            // Get Plane in middle of curve
            var crvnorm = new CurveNormalized(crv);
            pointCrvMid = crvnorm.PointAtMid;
            Point3d pointCrvMidOnSurface;
            if (!face._ClosestPoints(pointCrvMid, out pointCrvMidOnSurface))
            {
                pointCrvMidOnSurface = pointCrvMid;
            }
            var plane = new Plane(pointCrvMidOnSurface, crvnorm.TangentAt(0.5));

            //Layers.Debug.AddPoint(pointCrvMid, Color.Red);
            //Layers.Debug.AddVector(crvnorm.TangentAt(0.5), pointCrvMid);
            //Layers.Debug.AddVector(plane.XAxis, pointCrvMid, "XAxis");
            //Layers.Debug.AddVector(plane.YAxis, pointCrvMid, "YAxis");
            //Layers.Debug.AddVector(plane.ZAxis, pointCrvMid, "ZAxis");

            // Get Intersection of face with Plane
            Curve[] intersectionCurves;
            Point3d[] intersectionPoints;
            var brepFace = face.DuplicateFace(false);
            var isSurfaceCurve = false;
            if (!Intersection.BrepPlane(brepFace, plane, 0.00001, out intersectionCurves, out intersectionPoints) // try to get face curves
                || intersectionCurves.Length == 0)
            {
                isSurfaceCurve = true;
                var brepSrf = face._Srf().ToBrep();
                if (!Intersection.BrepPlane(brepSrf, plane, 0.00001, out intersectionCurves, out intersectionPoints) // try to get surface curves is getting face curves failed
                    || intersectionCurves.Length == 0)
                {
                    return false;
                }
            }

            // Extract face-curve that starts from out mid point
            Curve faceCurve = pointCrvMidOnSurface._ClosestCurve(intersectionCurves);
            if (faceCurve != null && isSurfaceCurve) // for surface curve we will try to trim it and get face curve (becuase detecting direction of tangent base on surface curve is hard)
            {
                Curve[] intersectionCurvesFace;
                Point3d[] intersectionPointsFace;
                if (Intersection.CurveBrepFace(faceCurve, face, 0.00001, out intersectionCurvesFace, out intersectionPointsFace)
                    && intersectionCurvesFace.Length != 0)
                {
                    faceCurve = pointCrvMidOnSurface._ClosestCurve(intersectionCurvesFace);
                }
            }

            // Make tangent
            if (faceCurve != null)
            {
                double t;
                if (!faceCurve.ClosestPoint(pointCrvMidOnSurface, out t))
                {
                    t = faceCurve.Domain.T1;
                }
                tangentToFace = faceCurve.TangentAt(t);

                // now we have to check if we need to reverse tangent
                var distToStart = faceCurve.PointAtStart._DistanceTo(pointCrvMidOnSurface);
                var distToEnd = faceCurve.PointAtEnd._DistanceTo(pointCrvMidOnSurface);
                if (distToStart < distToEnd)
                {
                    tangentToFace.Reverse();
                }

                //DEBUG
                //var debugCurve = faceCurve;
                //if (distToStart < distToEnd)
                //{
                //    debugCurve = faceCurve.DuplicateCurve();
                //    debugCurve.Reverse();
                //}
                //Layers.Debug.AddCurve(debugCurve, Color.Red, ObjectDecoration.EndArrowhead);
                //ENDDEBUG

                return true;
            }

            return false;
        }


        /// <summary>
        /// Get 3d length of curve. Can be 2d or 3d curve.
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="srf"></param>
        /// <returns></returns>
        public static double _Get3dLength(this Curve crv, Surface srf)
        {
            if (crv.Dimension == 2)
            {
                if (srf == null)
                {
                    throw new Exception("srf parameter must be not null in method _Curve._Get3dLength(crv, srf)");
                }
                var res = crv._Get3dLengthOf2dCurve(srf);
                return res;
            }
            return crv._GetLength_ThreadSafe();
        }


        /// <summary>
        /// Get proper devide count for curve to interpolate it witout issues.
        /// For 2d curves need to pass 'srf' argument
        /// </summary>
        /// <param name="crv">2d or 3d curve</param>
        /// <param name="srf">Need to be provided for 2d curves</param>
        /// <param name="tol"></param>
        /// <param name="minVal"></param>
        /// <returns></returns>
        public static int _GetDivBy(this Curve crv, Surface srf = null, double tol = 0.01, int minVal = 20)
        {
            var len = crv._Get3dLength(srf);
            return crv._GetDivBy(len, tol, minVal);
        }

        /// <summary>
        /// Get proper devide count for curve to interpolate it witout issues.
        /// For 2d curves need to pass 'srf' argument
        /// Faster verion - because it uses already calculated 3d curve length
        /// </summary>
        /// <param name="crv">2d or 3d curve</param>
        /// <param name="crv3dLength">3d length of curve</param>
        /// <param name="srf">Need to be provided for 2d curves</param>
        /// <param name="tol"></param>
        /// <param name="minVal"></param>
        /// <returns></returns>
        public static int _GetDivBy(this Curve crv, double crv3dLength, double tol = 0.01, int minVal = 20)
        {
            var res = Convert.ToInt32(crv3dLength / tol);
            res = Math.Max(res, minVal);
            res = Math.Min(res, 1000);
            return res;
        }

        public static Point3d _GetCurveExtEndPoint(this Curve crv, CurveEnd side, double length, Curve crvAlternate = null)
        {
            var crvE = crv.Extend(side, length, CurveExtensionStyle.Smooth);
            if (crvE != null)
            {
                switch (side)
                {
                    case CurveEnd.Start:
                        return crvE.PointAtStart;
                    case CurveEnd.End:
                        return crvE.PointAtEnd;
                }
            }
            else if (crvAlternate != null)
            {
                switch (side)
                {
                    case CurveEnd.Start:
                        var pStart = crvAlternate.PointAtStart;

                        var pStartBack = crvAlternate._PointAtPercent(0.1);
                        //AddPoint(Doc, pStart, Color.Red);
                        //AddPoint(Doc, pStartBack, Color.Blue);
                        var directionStart = (pStart - pStartBack);
                        directionStart.Unitize();
                        return crv.PointAtStart + directionStart * length;
                    case CurveEnd.End:
                        var pEnd = crvAlternate.PointAtEnd;
                        var pEndBack = crvAlternate._PointAtPercent(0.9);
                        var directionEnd = (pEnd - pEndBack);
                        directionEnd.Unitize();
                        return crv.PointAtEnd + directionEnd * length;
                }
            }
            else
            {
                switch (side)
                {
                    case CurveEnd.Start:
                        return crv.PointAtStart;
                    case CurveEnd.End:
                        return crv.PointAtEnd;
                }
            }
            return Point3d.Unset;
        }

        

        public static double _DistanceTo(this Curve crv3d, Point3d point, out Point3d pointOnCurve)
        {
            double t;
            if (!crv3d.ClosestPoint(point, out t))
            {
                var distToStart = crv3d.PointAtStart._DistanceTo(point);
                var distToEnd = crv3d.PointAtEnd._DistanceTo(point);
                if (distToStart < distToEnd)
                {
                    pointOnCurve = crv3d.PointAtStart;
                    return distToStart;
                }
                else
                {
                    pointOnCurve = crv3d.PointAtEnd;
                    return distToEnd;
                }                
            }
            pointOnCurve = crv3d.PointAt(t);
            var res = pointOnCurve._DistanceTo(point);
            return res;
        }

        public static double _DistanceTo(this Curve crv3d, Point3d point)
        {
            Point3d pointOnCurve;
            return crv3d._DistanceTo(point, out pointOnCurve);
        }

        public static double _DistanceTo(this Curve crv3d, Point3d point, double maximumDistance, double defaultValue)
        {
           
            double t;
            if (!crv3d.ClosestPoint(point, out t, maximumDistance))
            {
                return defaultValue;
            }
            var res = crv3d.PointAt(t)._DistanceTo(point);
            return res;
        }



        public static double _DistanceTo(this Curve crv3d, Surface srf, double defValue, int divBy = 0)
        {
            double res;
            if (!crv3d._TryDistanceToSrf(srf, out res, divBy))
            {
                return defValue;
            }
            return res;
        }

        public static bool _TryDistanceToSrf(this Curve crv3d, Surface srf, out double maxDistance, int divBy = 0)
        {
            double minDistance;
            if (divBy == 0)
            {
                divBy = crv3d._GetDivBy(srf);
            }
            var res = crv3d._TryDistanceToSrf(srf, out minDistance, out maxDistance, divBy);
            return res;
        }

        public static bool _TryDistanceToSrf(this Curve crv3d, Surface srf,
            out double minDistance, out double maxDistance, int divideByCount = 10, double maxAllowedDeviation = 0,
            Point3d[] curvePoints = null)
        {
            minDistance = 0;
            maxDistance = 0;
            bool foundDeviation = false;
            if (maxAllowedDeviation < _Double.ZERO)
            {
                maxAllowedDeviation = Double.MaxValue;
            }
            if (curvePoints == null)
            {
                crv3d._DivideByCount_ThreadSafe(divideByCount, true, out curvePoints);
            }
            if (curvePoints == null)
            {
                return false;
            }

            foreach (var p in curvePoints)
            {
                double u, v;
                if (!srf.ClosestPoint(p, out u, out v))
                {
                    return false;
                }
                var deviation = srf.PointAt(u, v)._DistanceTo(p);
                if (deviation > maxAllowedDeviation)
                {
                    maxDistance = deviation;
                    return false;
                }
                if (!foundDeviation)
                {
                    minDistance = deviation;
                    maxDistance = deviation;
                }
                else
                {
                    if (deviation < minDistance) minDistance = deviation;
                    if (deviation > maxDistance) maxDistance = deviation;
                }
                foundDeviation = true;
            }

            return foundDeviation;
        }


        public static bool _GetDistancesBetweenCurves(this Curve crv, Curve crvOther, out double minDeviation, out double maxDeviation, int crvMindivCount = 10, double maxAllowedDeviation = 0)
        {
            var cMax = crv;
            var cMin = crvOther;
            if (cMax._GetLength_ThreadSafe() < cMin._GetLength_ThreadSafe())
            {
                cMax = crvOther;
                cMin = crv;
            }
            return cMin._GetDistanceToBiggerCurve(cMax, out minDeviation, out maxDeviation, crvMindivCount, maxAllowedDeviation);
        }

        public static double _GetDistancesSummBetweenCurves(this Curve crv, Curve crvOther, int divideByCount = 10)
        {
            var cMax = crv;
            var cMin = crvOther;
            if (cMax._GetLength_ThreadSafe() < cMin._GetLength_ThreadSafe())
            {
                cMax = crvOther;
                cMin = crv;
            }

            double sum = 0;
            var t = cMin.Domain.T0;
            var tIncr = cMin.Domain.Length / divideByCount; // here we can allow to use t - because we dont need to be excact in positioning
            for (int i = 0; i < divideByCount; i++)
            {
                double tOther;
                var p = cMin.PointAt(t);
                if (cMax.ClosestPoint(p, out tOther))
                {
                    var dist = cMax.PointAt(tOther)._DistanceTo(p);
                    sum += dist;
                }
                t += tIncr;
            }
            return sum;
        }

        public static double _GetAvgDistancesBetweenCurves(this Curve crv, Curve crvOther, int divideByCount = 10)
        {
            return crv._GetDistancesSummBetweenCurves(crvOther, divideByCount) / divideByCount;
        }

        /// <summary>
        /// Get distance between curves.
        /// It is direct mathod compare to '_GetDistancesBetweenCurves' because doesnt calculates what curve is smaller and what is bigger
        /// Parameter curve must bigger, otherwise results will be wrong.
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="crvMax">crv with bigger length</param>
        /// <param name="minDeviation"></param>
        /// <param name="maxDeviation"></param>
        /// <param name="crvMindivCount">For how many pieces should be devided minCrv - more pieces higher precision (better result)</param>
        /// <param name="maxAllowedDeviation">If some of the points bigger than 'maxAllowedDeviation' this method will return false</param>
        /// <param name="minCurvePoints">Already prepared points on crvMin. If this parameter provided then 'crvMindivCount' will be ignored</param>
        /// <returns>True if results are provided, otherwise if False - distance between curves inpossible to calculate, probably because they are to far from each others</returns>
        public static bool _GetDistanceToBiggerCurve(this Curve crv, Curve crvMax,
            out double minDeviation, out double maxDeviation, int crvMindivCount = 10, double maxAllowedDeviation = 0,
            Point3d[] minCurvePoints = null)
        {
            minDeviation = 0;
            maxDeviation = 0;
            bool foundDeviation = false;
            bool isProvided_maxAllowedDeviation = true;
            if (maxAllowedDeviation < _Double.ZERO)
            {
                isProvided_maxAllowedDeviation = false;
                maxAllowedDeviation = Double.MaxValue;
            }
            if (minCurvePoints == null)
            {
                crv._DivideByCount_ThreadSafe(crvMindivCount, true, out minCurvePoints);
            }
            if (minCurvePoints == null)
            {
                return false;
            }

            // v1 - very slow (slower than v2 in 10 times)
            //double max_distance;
            //double max_distance_parameter_a;
            //double max_distance_parameter_b;
            //double min_distance;
            //double min_distance_parameter_a;
            //double min_distance_parameter_b;

            //if (Curve.GetDistancesBetweenCurves(crv, crvMax, maxAllowedDeviation, out max_distance,
            //    out max_distance_parameter_a, out max_distance_parameter_b,
            //    out min_distance, out min_distance_parameter_a, out min_distance_parameter_b))
            //{
            //}
            //else {
            //    var temp = 0;
            //}

            // v2 - fast (10 times)
            foreach (var p in minCurvePoints)
            {
                double t;
                if (isProvided_maxAllowedDeviation)
                {
                    if (!crvMax.ClosestPoint(p, out t, maxAllowedDeviation))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!crvMax.ClosestPoint(p, out t)) continue;
                }
                var deviation = crvMax.PointAt(t)._DistanceTo(p);
                if (deviation > maxAllowedDeviation)
                {
                    return false;
                }
                if (!foundDeviation)
                {
                    minDeviation = deviation;
                    maxDeviation = deviation;
                }
                else
                {
                    if (deviation < minDeviation) minDeviation = deviation;
                    if (deviation > maxDeviation) maxDeviation = deviation;
                }
                foundDeviation = true;
            }

            return foundDeviation;
        }

        public static bool _IsCloseToBiggerCurve(this Curve crv, Curve crvMax, double maxAllowedDeviation, int crvMindivCount = 10)
        {
            Point3d[] minCurvePoints;
            crv._DivideByCount_ThreadSafe(crvMindivCount, true, out minCurvePoints);
            return crv._IsCloseToBiggerCurve(crvMax, maxAllowedDeviation, minCurvePoints);
        }

        public static bool _IsCloseToBiggerCurve(this Curve crv, Curve crvMax, double maxAllowedDeviation, Point3d[] minCurvePoints)
        {
            if (minCurvePoints == null)
            {
                return false;
            }
            var foundDeviation = false;
            var maxAllowedDeviation_Pow2 = maxAllowedDeviation * maxAllowedDeviation;
            foreach (var p in minCurvePoints)
            {
                double t;
                if (!crvMax.ClosestPoint(p, out t, maxAllowedDeviation))
                {
                    return false;
                }
                var deviation_Pow2 = crvMax.PointAt(t)._DistanceTo_Pow2(p);
                if (deviation_Pow2 > maxAllowedDeviation_Pow2)
                {
                    return false;
                }
                foundDeviation = true;
            }

            return foundDeviation;
        }

        public static bool _IsCloseToPoints3d(this Curve crv3d, Point3d[] points3d, double maxAllowedDeviation)
        {
            var crvDimension = crv3d.Dimension;
            if (crvDimension != 3)
            {
                throw new Exception("_Curve._IsCloseToPoints   cruve.Dimension must be == 3");
            }
            foreach (var p3d in points3d)
            {
                double t;
                if (!crv3d.ClosestPoint(p3d, out t, maxAllowedDeviation))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool _TryProjectOnSrf(this Curve crv, Surface srf, double tol, out List<SurfacePoint2d3dSrf> points)
        {
            points = null;
            int DIVIDE_BY_COUNT = Math.Max(Convert.ToInt32(Math.Min(1000, crv._GetLength_ThreadSafe() / tol)), 100);
            if (crv.Degree == 1)
            {
                DIVIDE_BY_COUNT = 10;
            }
            Point3d[] crvPoints;
            var r = crv._DivideByCount_ThreadSafe(DIVIDE_BY_COUNT, true, out crvPoints);
            if (crvPoints == null) return false;

            points = new List<SurfacePoint2d3dSrf>(DIVIDE_BY_COUNT);
            double u, v;
            for (int i = 0; i < crvPoints.Length; i++)
            {
                var p = crvPoints[i];
                if (srf.ClosestPoint(p, out u, out v))
                {
                    var pOnFace = srf.PointAt(u, v);
                    points.Add(new SurfacePoint2d3dSrf(p, pOnFace, u, v));
                }
            }

            return true;
        }

        public static NurbsCurve _Fix2dContorlPoints(this Curve trim,
            Surface srf, SurfaceSingulars singulars = null, Curve edge = null,
            bool roundStart = false, bool roundEnd = false)
        {
            var trimNurb = trim._ToNurbsCurve();
            if (singulars == null)
            {
                singulars = new SurfaceSingulars(srf);
            }
            //if (!singulars.HasSingulars)
            //{
            //    return trimNurb;
            //}

            //
            // Degree 1, 2
            //
            if (trimNurb.Degree == 1 || trimNurb.Degree == 2)
            {
                return trimNurb._Fix2dContorlPoints_12Degree(srf, singulars, edge, roundStart, roundEnd);
            }

            //
            // Degree >= 3
            //
            if (trimNurb.Degree >= 3)
            {
                var crv = trimNurb;

                //var r = crv.DivideByCount(Math.Max(20, crv.Points.Count * 3), true, out crvPoints); // increase CP's by 3 - make shure our quality is better from original crv
                //if (crvPoints == null) return trimNurb._Fix2dContorlPoints_12Degree(srf, singulars, edge, roundStart, roundEnd);
                var sps = crv.Points._SurfacePoints();

                var isSPSFixed = srf._FixSurfacePoints(ref sps, false, singulars, null, edge, roundStart, roundEnd);
                if (!isSPSFixed)
                {
                    return crv;
                }


                // Lets try to build 2d curve without interpolation on surface
                if (true)
                {
                    var pointsProjected = sps.Select(o => srf.PointAt(o.u, o.v)).ToList();


                    var crv3dBasedOn2d = Curve.CreateControlPointCurve(pointsProjected, 3)._ToNurbsCurve();
                    if (crv3dBasedOn2d.Points.Count == sps.Count)
                    {
                        var crv2d = new NurbsCurve(2, crv3dBasedOn2d.IsRational, crv3dBasedOn2d.Degree + 1, crv3dBasedOn2d.Points.Count);
                        for (int i = 0; i < sps.Count; i++)
                        {
                            crv2d.Points.SetPoint(i, sps[i].u, sps[i].v, 0, crv3dBasedOn2d.Points[i].Weight);
                        }
                        var index = 0;
                        foreach (var knot in crv3dBasedOn2d.Knots)
                        {
                            crv2d.Knots[index] = knot;
                            index++;
                        }

                        //DEBUG
                        //Point3d[] crvPointsCHECK;
                        //var spsCheck = crv2d.Points.Select(o => new SurfacePoint(o.Location)).ToList();

                        // for detailed crvs - we can be sure that fix is ready
                        if (sps.Count > 100)
                        {
                            return crv2d;
                        }

                        // if new crv is not required anymore fix - return it!:)
                        if (!srf._FixSurfacePoints(ref sps, false, singulars, null, edge, roundStart, roundEnd))
                        {
                            return crv2d;
                        }

                        // check carefully if changes are big
                        var pointsProjected2 = sps.Select(o => srf.PointAt(o.u, o.v)).ToList();
                        var maxDistPow2 = 0.0;
                        for (int i = 0; i < pointsProjected.Count; i++)
                        {
                            var distPow2 = pointsProjected[i]._DistanceTo_Pow2(pointsProjected2[i]);
                            if (distPow2 > maxDistPow2)
                            {
                                maxDistPow2 = distPow2;
                            }
                        }
                        var maxDist = Math.Sqrt(maxDistPow2);
                        // if out new crv is not required anymore fix - return it!:)
                        if (maxDist < 0.001) // if second call to '_FixSurfacePoints' didnt change anything - return result!:)
                        {
                            return crv2d;
                        }
                    }
                }
















                // avoid InterpolatedCurveOnSurfaceUV on singular trims - results are unpredictable
                //if (singulars.HasSingulars) 
                //{

                //    return trim._ToNurbsCurve();
                //}







                var point2d = sps.Select(o => new Point2d(o.u, o.v)).ToList();
                var newCrv3d = srf.InterpolatedCurveOnSurfaceUV(point2d, 0.0000001); // high tolerance is a must! since interpolation works not nice in singularity area

                if (newCrv3d == null)
                {
                    return crv;
                }
                //if (_CurveZigZagCleaner._ZigZagDeformationsFind(newCrv3d.Points.Select(o => o.Location).ToArray()) != null)
                //{
                //    var temp = 0;
                //}
                var newCrv3dNEW = newCrv3d.Rebuild(crv.Points.Count * 3, crv.Degree, false); // increase CP's by 3 - make sure our quality is better from original crv
                if (newCrv3dNEW != null)
                {
                    newCrv3d = newCrv3dNEW;
                    //if (_CurveZigZagCleaner._ZigZagDeformationsFind(newCrv3dNEW.Points.Select(o => o.Location).ToArray()) != null)
                    //{
                    //    var temp = 0;
                    //}
                }

                var newCrv2d = srf._Convert3dCurveTo2d_WithoutRebuildAndSimplify(newCrv3d, singulars, null, edge);
                newCrv2d.SetStartPoint(new Point3d(sps[0].u, sps[0].v, 0));
                newCrv2d.SetEndPoint(new Point3d(sps[sps.Count - 1].u, sps[sps.Count - 1].v, 0));
                //var originalLength = trimNurb._GetLength_ThreadSafe();
                //var newLength = newCrv2d._GetLength_ThreadSafe();
                return newCrv2d;
                //    //for (int i = 1; i < sps.Count; i++)
                //    //{
                //    //    var p0 = new Point2d(sps[i-1].u, sps[i-1].v);
                //    //    var p1 = new Point2d(sps[i].u, sps[i].v);
                //    //    var newLength =  p0.DistanceTo(p1); // here length in 2d coordinates since we are using 2d curve
                //    //}
                //}
            }

            return trimNurb;
        }

        private static NurbsCurve _Fix2dContorlPoints_12Degree(this NurbsCurve trimNurb,
            Surface srf, SurfaceSingulars singulars = null, Curve edge = null,
            bool roundStart = false, bool roundEnd = false)
        {
            var crv = trimNurb;
            var trimControlPoints = crv.Points;
            var sps = trimControlPoints._SurfacePoints();

            var crv2d = new NurbsCurve(2, crv.IsRational, crv.Degree + 1, crv.Points.Count);
            var isSPSFixed = srf._FixSurfacePoints(ref sps, false, singulars, null, edge, roundStart, roundEnd);
            if (!isSPSFixed)
            {
                return crv;
            }
            for (int i = 0; i < sps.Count; i++)
            {
                var u = sps[i].u;
                var v = sps[i].v;
                var weight = trimControlPoints[i].Weight;
                crv2d.Points.SetPoint(i, u, v, 0, weight);
            }
            var index = 0;
            foreach (var knot in crv.Knots)
            {
                crv2d.Knots[index] = knot;
                index++;
            }
            if (crv.Degree == 1)
            {
                crv2d.Knots[0] = 0;
                var p0 = new Point2d(crv2d.Points[0].Location);
                var p1 = new Point2d(crv2d.Points[1].Location);
                var newLength = p0.DistanceTo(p1); // here length in 2d coordinates since we are using 2d curve
                crv2d.Knots[1] = newLength;
            }
            return crv2d;
        }

        /// <summary>
        /// Test if crvs are connected with tolerance
        /// </summary>
        /// <param name="crv1"></param>
        /// <param name="crv2"></param>
        /// <param name="connectionInfo">Connection info</param>
        /// <param name="tol">maximum distance between crvs</param>
        /// <returns>Connection info</returns>
        public static bool _AreConnected(this Curve crv1, Curve crv2, out CurvesConnectionInfo connectionInfo, double tol)
        {
            connectionInfo = new CurvesConnectionInfo();
            var ds = new CurvesConnectionInfo[4]
            {                
                new CurvesConnectionInfo(crv1, crv2, CurveEnd.Start, CurveEnd.Start),
                new CurvesConnectionInfo(crv1, crv2, CurveEnd.Start, CurveEnd.End),
                new CurvesConnectionInfo(crv1, crv2, CurveEnd.End, CurveEnd.Start),
                new CurvesConnectionInfo(crv1, crv2, CurveEnd.End, CurveEnd.End),
            };
            double minDistance = 0;
            var minDeviationIndex = -1;
            for (int i = 0; i < ds.Length; i++)
            {
                if (minDeviationIndex == -1 || ds[i].Distance < minDistance)
                {
                    minDistance = ds[i].Distance;
                    minDeviationIndex = i;
                }
            }
            var res = (minDistance < tol);
            if (res)
            {
                connectionInfo = ds[minDeviationIndex];
            }
            return res;
        }

        /// <summary>
        /// Calculates angle (in radians) between 2 crvs
        /// 
        /// </summary>
        /// <param name="crv1"></param>
        /// <param name="crv2"></param>
        /// <param name="tol">maximum distance between crvs</param>
        /// <returns>Angle in radians, or double'NAN' if curves are not connected (distance between crvs is more than tol)</returns>
        public static double _AngleBetweenCurves(this Curve crv1, Curve crv2, double tol = 0.1)
        {
            CurvesConnectionInfo connInfo;
            if (crv1._AreConnected(crv2, out connInfo, tol))
            {
                var t1 = crv1.TangentAt(connInfo.c1T);
                var t2 = crv2.TangentAt(connInfo.c2T);
                //var angle = Vector3d.VectorAngle(t1, t2);
                var angle = t1._AngleOfUnitizedVectors(t2); //fast coz doesnt do Unitize for vectors, since Tangents already unitized vectors
                return angle;
            }
            return double.NaN;
        }

        /// <summary>
        /// Calculates angle (in radians) between 2 crvs at already calculated join positions.
        /// </summary>
        /// <param name="crv1"></param>
        /// <param name="crv2"></param>
        /// <param name="crv1At"></param>
        /// <param name="crv2At"></param>
        /// <returns>Angle in radians</returns>
        public static double _AngleBetweenCurves(this Curve crv1, CurveEnd crv1At, Curve crv2, CurveEnd crv2At)
        {
            var c1T = crv1._T(crv1At);
            var c2T = crv2._T(crv2At);
            var t1 = crv1.TangentAt(c1T);
            var t2 = crv2.TangentAt(c2T);
            //var angle = Vector3d.VectorAngle(t1, t2);
            var angle = t1._AngleOfUnitizedVectors(t2); //fast coz doesnt do Unitize for vectors, since Tangents already unitized vectors
            return angle;
        }

        public static double _Get3dLengthOf2dCurve(this Curve crv2d, Surface srf)
        {
            Point3d[] devidedPoints2d;
            Point3d[] devidedPoints3d;
            return crv2d._Get3dLengthOf2dCurve(srf, out devidedPoints2d, out devidedPoints3d);
        }

        /// <summary>
        /// Calculate approx Length of 2d curve in 3d dimension
        /// </summary>
        /// <param name="crv2d"></param>
        /// <param name="srf"></param>
        /// <param name="devidedPoints2d"></param>
        /// <param name="devidedPoints3d"></param>
        /// <param name="divby"></param>
        /// <returns></returns>
        public static double _Get3dLengthOf2dCurve(this Curve crv2d, Surface srf, out Point3d[] devidedPoints2d, out Point3d[] devidedPoints3d, int divby = 20)
        {
            devidedPoints2d = null;
            devidedPoints3d = null;

            crv2d._DivideByCount_ThreadSafe(divby, true, out devidedPoints2d);
            if (devidedPoints2d == null) return 0;

            devidedPoints3d = devidedPoints2d.Select(o => srf.PointAt(o.X, o.Y)).ToArray();

            double len = 0;
            for (int i = 1; i < devidedPoints3d.Length; i++)
            {
                var pointPrev = devidedPoints3d[i - 1];
                var point = devidedPoints3d[i];
                len += pointPrev._DistanceTo(point);
            }
            return len;
        }

        /// <summary>
        /// Converts 2d curve (trim) to 3d (edge)
        /// </summary>
        /// <param name="crv2d"></param>
        /// <param name="srf"></param>
        /// <param name="tol"></param>
        /// <returns></returns>        
        public static NurbsCurve _2dTo3d(this Curve crv2d, Surface srf, double tol = 0.001)
        {
            var crv2dNurb = crv2d._ToNurbsCurve();
            NurbsCurve res = null;

            // Try1
            if (res == null)
            {
                Point3d[] devidedPoints2d;
                Point3d[] devidedPoints3d;
                var divby = Math.Max(20, crv2dNurb.Points.Count);
                divby = Math.Min(1000, divby);
                var len3d = crv2dNurb._Get3dLengthOf2dCurve(srf, out devidedPoints2d, out devidedPoints3d, divby);
                int cycle = 0;
                while (cycle < 10 && devidedPoints2d != null)
                {
                    int multiplier = (cycle == 0) ? 1 : cycle * 5;
                    cycle++;

                    var res1 = Curve.CreateControlPointCurve(devidedPoints3d, 3);
                    if (res1 != null)
                    {
                        if (res1._IsCloseToPoints3d(devidedPoints3d, tol))
                        {
                            res = res1._ToNurbsCurve();
                            break; // we successfully converted 2d curve to 3d - so let's stop our search
                        }
                    }
                    //if we didnt find result with close to max divby value 1000 - return fail
                    if (divby >= 800)
                    {
                        break;
                    }

                    divby = 20 * multiplier;
                    var divby3d = Convert.ToInt32(Math.Min(10 * 1024, len3d) / (0.01 / multiplier));
                    if (divby3d > divby * 1.5) divby = divby3d;
                    divby = Math.Min(1000, divby);
                    // for long curves prevent imidiate jump to 1000 points - do it step-by-step
                    if (divby > 3 * devidedPoints3d.Length)
                    {
                        divby = 3 * devidedPoints3d.Length;
                    }

                    crv2d._DivideByCount_ThreadSafe(divby, true, out devidedPoints2d);
                    if (devidedPoints2d == null) break;
                    devidedPoints3d = devidedPoints2d.Select(o => srf.PointAt(o.X, o.Y)).ToArray();
                }
            }

            // Try2
            if (res == null)
            {
                var pointsProjected = new List<Point3d>();
                for (int i = 0; i < crv2dNurb.Points.Count; i++)
                {
                    var point = crv2dNurb.Points[i].Location;
                    var point3d = srf.PointAt(point.X, point.Y);
                    pointsProjected.Add(point3d);
                    //Utils.HighlightLayer.AddPoint(Doc, point3d, colorTrim);
                }

                var res2 = Curve.CreateControlPointCurve(pointsProjected, 3);
                if (res2 != null)
                {
                    res = res2._ToNurbsCurve();
                }
            }

            if (res == null) return null;
            return res._ToNurbsCurve();

            //var pointsProjected = new List<Point3d>();
            //foreach (var point in devidedPoints)
            //{
            //    var point3d = srf.PointAt(point.X, point.Y);
            //    pointsProjected.Add(point3d);
            //    //Utils.HighlightLayer.AddPoint(Doc, point3d, colorTrim);
            //}

            //var res = Curve.CreateControlPointCurve(pointsProjected);
            //return res;
        }


        /// <summary>
        /// Normalize 2d curve (trim) before work on it (Rebuild)
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="srf"></param>
        /// <returns></returns>
        public static NurbsCurve _2dNormalize(this Curve crv, Surface srf)
        {
            double uM, vM;
            srf._Get2dNormalizedMultipliyers(out uM, out vM);
            return crv._MultiplyControlPoints(uM, vM, 0);
        }

        /// <summary>
        /// Unnormalize 2d curve (trim) after work on it (Rebuild)
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="srf"></param>
        /// <returns></returns>
        public static NurbsCurve _2dUnnormalize(this Curve crv, Surface srf)
        {
            double uM, vM;
            srf._Get2dNormalizedMultipliyers(out uM, out vM);
            return crv._MultiplyControlPoints(1 / uM, 1 / vM, 0);
        }

        public static NurbsCurve _MultiplyControlPoints(this Curve crv, double xM, double yM, double zM)
        {
            var crvNurb = crv._ToNurbsCurve();

            var res = new NurbsCurve(crv.Dimension, crvNurb.IsRational, crv.Degree + 1, crvNurb.Points.Count);
            var  index = 0;
            foreach (var point3d in crvNurb.Points)
            {
                res.Points.SetPoint(index, point3d.Location.X * xM, point3d.Location.Y * yM, point3d.Location.Z * zM, point3d.Weight);
                index++;
            }

            index = 0;
            foreach (var knot in crvNurb.Knots)
            {
                res.Knots[index] = knot;
                index++;
            }

            return res;
        }

        public static NurbsCurve _ToNurbsCurve(this Curve crv)
        {
            return crv._ToNurbsCurveDirect();

            var edge = crv as BrepEdge;
            if (edge != null)
            {
                var cd = crv.DuplicateCurve();
                return cd._ToNurbsCurveDirect();
                //var index = edge.ComponentIndex().Index;
                //var crv3d = edge.Brep.Curves3D[index];
                //var c = edge.Brep.Curves3D.Count;
                //var rev = edge.ProxyCurveIsReversed;
                //var domEdge = edge.Domain;
                //var dom3d = crv3d.Domain;

                //if (!edge.ProxyCurveIsReversed
                //    && crv3d.Domain == edge.Domain)
                //{
                //    return crv3d._ToNurbsCurveDirect();
                //}
                //else
                //{
                //    var warning = 0;
                //}
            }

            var trim = crv as BrepTrim;
            if (trim != null)
            {
                var cd = crv.DuplicateCurve();
                return cd._ToNurbsCurveDirect();
                //var index = trim.ComponentIndex().Index;
                //var crv2d = trim.Brep.Curves2D[index];
                //if (crv2d != null
                //    && !trim.ProxyCurveIsReversed
                //    && crv2d.Domain == trim.Domain)
                //{
                //    return crv2d._ToNurbsCurveDirect();
                //}
            }

            return crv._ToNurbsCurveDirect();
        }

        private static NurbsCurve _ToNurbsCurveDirect(this Curve crv)
        {
            NurbsCurve crvNurb = crv is NurbsCurve
               ? (NurbsCurve)crv
               : crv.ToNurbsCurve();
            return crvNurb;
        }

        public static bool _NeedFixClosedCurve(this Curve crv, double? length = null)
        {
            if (crv == null)
            {
                return false;
            }

            if (crv.IsClosed)
            {
                var crvLength = length ?? crv._GetLength_ThreadSafe();
                var crvDimension = crv.Dimension;

                if (crvDimension == 2)
                {
                    var Start_End_Distance = crv._P(CurveEnd.End)._DistanceTo(crv._P(CurveEnd.Start));
                    if (crvLength < 0.01
                        && Start_End_Distance < 0.001
                        )
                    {
                        return true;
                    }
                }

                //debug
                //var crvNurb = crv._ToNurbsCurve(); 
                //var Start_End_Distance2= crv.PointAtStart._DistanceTo(crv.PointAtEnd);

                // for 3-dimension curve we need to make additional check
                if (crvDimension == 3)
                {
                    if (crvLength < 0.1) // speed optimization - calculate 'Start_End_Distance' only at demand - safe 98% of time
                    {
                        var Start_End_Distance = crv._P(CurveEnd.End)._DistanceTo(crv._P(CurveEnd.Start));
                        if (Start_End_Distance < 0.01
                        )
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static Curve _FixClosedCurve(this Curve crv, bool forceFix = false, double? length = null)
        {
            if ((forceFix || crv._NeedFixClosedCurve(length))
                && crv != null)
            {
                var dim = crv.Dimension;
                if (dim == 2)
                {
                    crv = new LineCurve(crv.PointAtStart._ToPoint2d(), crv.PointAtEnd._ToPoint2d());
                }
                if (dim == 3)
                {
                    crv = new LineCurve(crv.PointAtStart, crv.PointAtEnd);
                }
            }
            return crv;
        }


        public static NurbsCurve _FixUVOutOfDomain(this Curve crv, Interval domainU, Interval domainV)
        {
            var res = crv._ToNurbsCurve();
            var uMin = domainU.T0;
            var uMax = domainU.T1;
            var vMin = domainV.T0;
            var vMax = domainV.T1;
            var changed = false;
            for (int i = 0; i < res.Points.Count; i++)
            {
                bool changedU, changedV;
                var p = res.Points[i];
                var u = p.Location.X._Limit(uMin, uMax, out changedU);
                var v = p.Location.Y._Limit(vMin, vMax, out changedV);
                if (changedU || changedV)
                {
                    changed = true;
                    res.Points.SetPoint(i, new Point3d(u, v, 0));
                }
            }
            return res;
        }

        public static bool _IsClosed(this Curve crv)
        {
            if (crv.IsClosed)
            {
                var distBetweenStartAndEnd = crv.PointAt(crv.Domain.T0)._DistanceTo(crv.PointAt(crv.Domain.T1));
                return distBetweenStartAndEnd < 0.001
                    //&& edge.GetLength() > 0.01
                       ;
            }
            return false;
        }

        public static bool _IsClosed(this Curve crv, double distBetweenStartAndEnd)
        {
            if (crv.IsClosed)
            {
                return distBetweenStartAndEnd < 0.001;
            }
            return false;
        }

        public static Curve _ExtendToPoint(this Curve crv, CurveEnd from, Point3d toXD, Surface srf = null,
            string debugComment = "",
            bool SHOW_DEBUG = false)
        {
            //const bool SHOW_DEBUG = true;

            if (crv == null)
            {
                return null;
            }
            bool is3d = (crv.Dimension == 3);
            bool is2d = !is3d;

            // parameters validation
            if (is2d && srf == null)
            {
                throw new Exception("_Curve._ExtendToPoint() - for 2d curves this method requires 'srf' parameter!");
            }

            var to3d = is3d ? toXD : srf._PointAt(toXD); // we have to compare distances always in 3d coordinates to achieve correct results

            var crvExtended = crv;
            if (is3d) // we can extend only 3d curves - since in 2d coordinates extension looks weired and unpredictable
            {
                var crvLengthORIGINXD = crv._GetLength_ThreadSafe(); // we will use it for compare xd lengths (2d or 3d)
                var distToExtendXD = crv._P(from)._DistanceTo(toXD);// here 3d and 2d differs - we use appropriate coordinates (2d or 3d)
                var distCurrent3d = crv._P3d(from, srf)._DistanceTo(to3d);

                var bestTypeStr = "";

                foreach (var extTypeStr in new[] { 
                    "Current",  // corresponds to currrent line - this case is for shortenest original curve instead of extending. At first we will try to shorten an original curve
                    "Line",       // corresponds to type CurveExtensionStyle.Line;
                    "Arc",        // corresponds to type CurveExtensionStyle.Arc;
                   // "Smooth",  // corresponds to type CurveExtensionStyle.Smooth;
                })
                {
                    var extType = CurveExtensionStyle.Line;
                    if (extTypeStr == "Line") extType = CurveExtensionStyle.Line;
                    if (extTypeStr == "Arc") extType = CurveExtensionStyle.Arc;
                    if (extTypeStr == "Smooth") extType = CurveExtensionStyle.Smooth;
                    var crvExt = (extTypeStr == "Current")
                        ? crv.DuplicateCurve()
                        : crv.Extend(from, distToExtendXD * 2, extType).DuplicateCurve(); // extend by 2x times to cover gaps of arc extensions
                    if (crvExt.Dimension != crv.Dimension) // i dont know why - but sometimes after Curve.Extend 2d curve became 3d(!) - so we have to fix this issue.
                    {
                        crvExt = crvExt._SetDimension(crv.Dimension);
                    }
                    if (crvExt == null) continue;
                    var crvExt3d = is3d ? crvExt : crvExt._2dTo3d(srf, 0.001); // smaller tolerance than default since we just need appoximate values
                    if (crvExt3d == null) continue;

                    if (SHOW_DEBUG)
                    {
                        var color = Color.Black;
                        if (extType == CurveExtensionStyle.Line) color = Color.YellowGreen;
                        if (extType == CurveExtensionStyle.Arc) color = Color.Green;
                        if (extType == CurveExtensionStyle.Smooth) color = Color.Violet;
                        if (crvExt.Dimension == 2)
                        {
                            Layers.Debug.AddCurve(crvExt._2dTo3d(srf), color);
                        }
                        else
                        {
                            Layers.Debug.AddCurve(crvExt, color);
                        }
                    }


                    var distExtI3d = crvExt._P3d(from, srf)._DistanceTo(to3d);
                    if (distExtI3d < distCurrent3d)
                    {
                        distCurrent3d = distExtI3d;
                        crvExtended = crvExt;
                        bestTypeStr = extTypeStr;
                    }


                    double closestT;
                    crvExt3d.ClosestPoint(to3d, out closestT); // here we use 3d extended curve to get correct closest T value
                    var distExtAndTrimedI3d = crvExt3d._P(closestT)._DistanceTo(to3d);
                    if (distExtAndTrimedI3d > 0)
                    {
                        var fromOpposite = from == CurveEnd.Start ? CurveEnd.End : CurveEnd.Start;
                        var t0Including = crvExt._T(fromOpposite);
                        var t1Including = closestT;

                        if (t0Including > t1Including)
                        {
                            var tmp = t0Including;
                            t0Including = t1Including;
                            t1Including = tmp;
                        }

                        // here duplicate curve to avoid multithreads issues in Curve::Trim method (method Trim executed Curve::GetLength method which is not thread safe)
                        var crvTrimed = crvExt.DuplicateCurve().Trim(t0Including, t1Including);
                        if (crvTrimed != null
                            && crvTrimed._GetLength_ThreadSafe() > crvLengthORIGINXD * 0.5 // in 3d or in 2d
                            ) // no need to call _GetLength_ThreadSafe - our curve is in memory
                        {
                            var distTrimI = crvTrimed._P3d(from, srf)._DistanceTo(to3d);
                            if (distTrimI < distCurrent3d)
                            {
                                distCurrent3d = distTrimI;
                                crvExtended = crvTrimed;
                                bestTypeStr = extTypeStr + "-trimmed";
                                // if extention of line is actually shortnest - let skip extension of line - just return trimeed original curve
                                if (extTypeStr == "Current")
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                //log.temp("_Curve._ExtendToPoint: crv {0} extended using method: {1},  new length {2:0.000}", debugComment, bestTypeStr, crvExtended._GetLength_ThreadSafe());
                if (SHOW_DEBUG && crvExtended != crv)
                {
                    var color = Color.Red;
                    if (crvExtended.Dimension == 2)
                    {
                        Layers.Debug.AddCurve(crvExtended._2dTo3d(srf), color);
                    }
                    else
                    {
                        Layers.Debug.AddCurve(crvExtended, color);
                    }
                }
            }
            return crvExtended;
        }

        /// <summary>
        /// Extends curve
        /// </summary>
        /// <param name="crv">2d or 3d curve</param>
        /// <param name="from"></param>
        /// <param name="toXD">2d or 3d point</param>
        /// <param name="extend">increase or decrease lenght of curve before extension - use this parameters always to improve results (works only for 3d curves)</param>
        /// <param name="srf">required for 2d curves</param>
        /// <param name="debugComment">comment for log.temp() - helps debug and indentify issues by moving crvs. log should be manually activated in code by uncommenting line.</param>
        /// <param name="SHOW_DEBUG"></param>
        /// <param name="do_simplify"></param>
        /// <returns></returns>
        public static Curve _MoveEnd(this Curve crv, CurveEnd from, Point3d toXD, bool extend, Surface srf = null,
            string debugComment = "",
            bool SHOW_DEBUG = false,
            bool do_simplify = true)
        {
            //const bool SHOW_DEBUG = true;

            if (crv == null)
            {
                return null;
            }
            bool is3d = (crv.Dimension == 3);
            bool is2d = !is3d;

            // parameters validation
            if (is2d && srf == null)
            {
                throw new Exception("_Curve._MoveEnd() - for 2d curves this method requires 'srf' parameter!");
            }

            var crvExtended = crv;
            if (extend && is3d) // we can extend only 3d curves - since in 2d coordinates extension looks weired and unpredictable
            {
                crvExtended = crv._ExtendToPoint(from, toXD, srf, debugComment, SHOW_DEBUG);
            }


            var direction = toXD - crvExtended._P(from);
            var divby = crvExtended._GetDivBy(srf);
            //log.temp("divby: " + divby);
            Point3d[] points;
            crvExtended._DivideByCount_ThreadSafe(divby, true, out points);

            // exclude extended curve if it is not possible to devide it
            if (points == null && crvExtended != crv)
            {
                crvExtended = crv;
                direction = toXD - crv._P(from);
                crv._DivideByCount_ThreadSafe(divby, true, out points);
            }

            if (points == null) return null;

            double endI = points.Length - 1;
            for (int i = 0; i < points.Length; i++)
            {
                var shift = (from == CurveEnd.End)
                    ? direction * (i / endI)
                    : direction * ((endI - i) / endI);
                points[i] = points[i] + shift;
            }

            var res = Curve.CreateControlPointCurve(points, 3);
            if (res != null)
            {
                if (crv.Dimension == 2)
                {
                    res = res._SetDimension(2);
                }
                if (crv.Dimension == 2 && srf != null)
                {
                    res = res._Fix2dContorlPoints(srf);
                }
                if (do_simplify)
                {
                    res = res._Simplify(srf, true);
                }
            }

            //    var sps = points.Select(o => new SurfacePoint(o)).ToList();
            //    var fixedSPS = srf._FixSurfacePoints(ref sps);

            //    var points2d = edgeDevidedUV.Select(o => new Point2d(o.u, o.v)).ToList();
            //    crv3d = srf.InterpolatedCurveOnSurfaceUV(points2d, 0.0000001);

            //    if (crv3d == null)
            //    {
            //        var point3d = edgeDevidedUV.Select(o => srf.PointAt(o.u, o.v)).ToList();
            //        crv3d = srf.InterpolatedCurveOnSurface(point3d, 0.0001);
            //    //      Side.Face.Surface.InterpolatedCurveOnSurfaceUV(
            //}

            return res;
        }

        public static Curve _FixStartEndPoints(this Curve crv, Surface srf, Curve crvWithCorrectStartEndPoints, out string failReason, bool fixStart, bool fixEnd, bool do_simplify = true)
        {
            failReason = "";

            if (!fixStart && !fixEnd) return crv;

            var divby = crv._GetDivBy(srf);
            //log.temp("divby: " + divby);
            Point3d[] points;
            crv._DivideByCount_ThreadSafe(divby, true, out points);

            if (points == null)
            {
                failReason = "_Curve._FixTrimEnds: Failed to devide curve";
                return null;
            }

            int endI = points.Length / 2;
            var directionStart = crvWithCorrectStartEndPoints.PointAtStart - crv.PointAtStart;
            var directionEnd = crvWithCorrectStartEndPoints.PointAtEnd - crv.PointAtEnd;
            for (int i = 0; i <= endI; i++)
            {
                if (fixStart) points[i] += directionStart * (1 - i / (double)endI);
                if (fixEnd) points[points.Length - 1 - i] += directionEnd * (1 - i / (double)endI);
            }

            var res = Curve.CreateControlPointCurve(points, 3);
            if (res == null)
            {
                failReason = "_Curve._FixTrimEnds: Failed to create curve using fixed point by method Curve.CreateControlPointCurve";
                return null;
            }

            if (crv.Dimension == 2)
            {
                res = res._SetDimension(2);
            }
            if (crv.Dimension == 2 && srf != null)
            {
                res = res._Fix2dContorlPoints(srf);
            }
            if (do_simplify)
            {
                res = res._Simplify(srf, true);
            }

            if (fixStart) res.SetStartPoint(crvWithCorrectStartEndPoints.PointAtStart);
            if (fixEnd) res.SetEndPoint(crvWithCorrectStartEndPoints.PointAtEnd);

            return res;
        }

        /// <summary>
        /// Change dimension of curve, but doesnt change curve itself.
        /// Usually used to change dimension from 2d to 3d, because some Rhino function returns 2d curves althought they are 3d - and we must to fix this issue.
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="newDimension"></param>
        /// <returns></returns>
        public static NurbsCurve _SetDimension(this Curve crv, int newDimension)
        {
            if (crv.Dimension == newDimension)
            {
                return crv._ToNurbsCurve();
            }
            var crv3d = crv._ToNurbsCurve();
            var crv2d = new NurbsCurve(newDimension, crv3d.IsRational, crv3d.Degree + 1, crv3d.Points.Count);
            var  index = 0;
            foreach (var point3d in crv3d.Points)
            {
                crv2d.Points.SetPoint(index, point3d.Location.X, point3d.Location.Y, point3d.Location.Z, point3d.Weight);
                index++;
            }

            index = 0;
            foreach (var knot in crv3d.Knots)
            {
                crv2d.Knots[index] = knot;
                index++;
            }

            return crv2d;
        }

        /// <summary>
        /// Create new curve from points of source curve.
        /// This help to get curve without artefacts at ends (some of the curves are changing their direction at end).
        /// Returns null if reinterpolation is impossible.
        /// </summary>
        /// <param name="crv">source curve</param>
        /// <param name="failReason"></param>
        /// <param name="pointsCount">how many control points should have a new curve</param>
        /// <returns>New curve or null</returns>
        public static NurbsCurve _TryReinterpolate(this Curve crv, out string failReason, int pointsCount = 10)
        {
            Point3d[] points;
            if (!crv._TryDivideByCount(pointsCount - 1, out points, out failReason))
            {
                return null;
            }

            var res = _TryCreateInterpolatedCurve_SensetiveButFineQuality(points, out failReason, false);
            if (res == null)
            {
                return null;
            }

            failReason = "";
            return res;
        }

        public static bool _IsValid(this Curve crv)
        {
            string failReason;
            return crv._IsValid(out failReason);
        }

        public static bool _IsValid(this Curve crv, out string failReason)
        {
            if (crv == null)
            {
                failReason = "crv is null";
                return false;
            }

            if (crv._ZigZagDeformationExists())
            {
                //DEBUG
                //Layers.Debug.AddCurve(crv);
                //Layers.Debug.AddPoints(points);
                failReason = "curve has zigzags";
                return false;
            }

            failReason = "";
            return true;
        }

        public static NurbsCurve _TryCreateInterpolatedCurve_SensetiveButFineQuality(Point3d[] points, out string failReason, bool simplifyCurve = true)
        {
            var crv = Curve.CreateInterpolatedCurve(points, 3);
            if (!crv._IsValid(out failReason)) return null;

            var res = crv._ToNurbsCurve();
            res.IncreaseDegree(3);

            if (simplifyCurve)
            {
                res = res._Simplify();
            }
            return res;
        }

        public static NurbsCurve _TryCreateControlPointCurve_AvarageQuality(Point3d[] points, out string failReason, bool simplifyCurve = true)
        {
            var crv = Curve.CreateControlPointCurve(points, 3);
            if (!crv._IsValid(out failReason)) return null;

            var res = crv._ToNurbsCurve();
            res.IncreaseDegree(3);

            if (simplifyCurve)
            {
                res = res._Simplify();
            }
            return res;
        }


        /// <summary>
        /// Replace control points in curve.
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="newControlPoints"></param>
        /// <returns></returns>
        public static NurbsCurve _ReplaceControlPoints(this Curve crv, Point3d[] newControlPoints)
        {
            var crvnurb = crv._ToNurbsCurve();
            if (newControlPoints.Length != crvnurb.Points.Count)
            {
                var error = String.Format("_Curve._ReplaceControlPoints : newControlPoints.Length != crvnurb.Points.Count: {0} != {1}", newControlPoints.Length, crvnurb.Points.Count);
                throw new Exception(error);
            }

            var crvNew = new NurbsCurve(crv.Dimension, crvnurb.IsRational, crvnurb.Degree + 1, crvnurb.Points.Count);
            for (int i = 0; i < newControlPoints.Length; i++)
            {
                var newPoint = newControlPoints[i];
                //DEBUG
                var oldPoint = crvnurb.Points[i].Location;
                crvNew.Points.SetPoint(i, newPoint.X, newPoint.Y, newPoint.Z, crvNew.Points[i].Weight);


            }

            var index = 0;
            foreach (var knot in crvnurb.Knots)
            {
                crvNew.Knots[index] = knot;
                index++;
            }

            return crvNew;
        }


        public static bool _TryDivideByCount(this Curve crv, int divby, out Point3d[] points, out double[] ts, out string failReason)
        {
            //if (crv.Dimension == 3 && crv.Degree == 1)
            //{
            //    var domain = crv.Domain;
            //    ts = new double[divby + 1];
            //    ts[0] = domain.T0;
            //    points = new Point3d[divby + 1];
            //    points[0] = crv.PointAtStart;
            //    var crv3dLen = crv.PointAtEnd._DistanceTo(crv.PointAtStart);
            //    var direction = crv.PointAtEnd - crv.PointAtStart;
            //    direction.Unitize();
            //    for (int i = 1; i <= divby; i++)
            //    {
            //        ts[i] = ts[0] + (domain.Length*i)/divby;
            //        points[i] = points[0] + direction*crv3dLen*i/divby;
            //    }
            //    failReason = "";
            //    return true;
            //}


            failReason = "";
            ts = crv._DivideByCount_ThreadSafe(divby, true, out points);

            if (points == null)
            {
                failReason = "failed to devide crv";
                ts = null;
                points = null;
                return false;
            }

            if (points.Length != ts.Length)
            {
                failReason = "points.Length != ts.Length";
                ts = null;
                points = null;
                return false;
            }

            return true;
        }

        public static bool _TryDivideByCount(this Curve crv, int divby, out Point3d[] points, out string failReason)
        {
            double[] ts;
            return crv._TryDivideByCount(divby, out points, out ts, out failReason);
        }

        public static bool _TryDivideByCount(this Curve crv, int divby, out Point3d[] points)
        {
            string failReason;
            return crv._TryDivideByCount(divby, out points, out failReason);
        }

        public static Point3d[] _GetDividePoints(this Curve crv, int divby)
        {
            Point3d[] points;
            if (crv != null && crv._TryDivideByCount(divby, out points))
            {
                return points;
            }
            return new Point3d[0];
        }

        /// <summary>
        /// Join 2 curves as reinterpolated ones - create control points, merge them, and then reinterpolate new curve
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="crvToJoin"></param>
        /// <returns></returns>
        public static NurbsCurve _Join(this Curve crv, Curve crvToJoin)
        {
            var crvs = new[] { crv, crvToJoin };
            var crvsLengths = new[] { crv._GetLength_ThreadSafe(), crvToJoin._GetLength_ThreadSafe() };
            var totalLength = crvsLengths.Sum(); // for both sides lets use E length - it is almost same length as for trims and a lot-lot faster then initializing 'side.T'
            if (totalLength._IsZero()) return null;

            double tol = crvsLengths.Max() * 0.1; //10% of max length
            CurvesConnectionInfo connInfo;
            if (!crv._AreConnected(crvToJoin, out connInfo, tol)) return null;
            var crvsStartAtEnds = new[] { connInfo.Crv1End._Reverse(), connInfo.Crv2End };

            double addedLength = 0;
            var divBy = Convert.ToInt32(Math.Min(10000, totalLength / 0.01));
            divBy = Math.Max(divBy, 20);
            divBy = Math.Min(divBy, 500);
            var segmentLength = totalLength / divBy;
            var cutLength = (totalLength * 0.05) / (2 * 2); // 5% of total length will be cuted
            cutLength = Math.Min(cutLength, 0.1); //  cut length should be reasonable small. here we protect long lines from incorrect joining. without limitation cut length can reach length of 2.5, what leads to visible curve deformations
            var skipPoints = Convert.ToInt32(cutLength / segmentLength);
            if (skipPoints == 0) skipPoints = 1;

            // Devide all curves on segments (very small curves dont add)
            var allPoints = new List<Point3d>(1000);
            allPoints.Add(crvs.First()._P(crvsStartAtEnds.First())); // add first point - to ensure we dint lost it when skipping small curves
            for (int ci = 0; ci < crvs.Length; ci++)
            {
                var isFirst = (ci == 0);
                var isLast = (ci == crvs.Length - 1);
                var divby = Convert.ToInt32(crvsLengths[ci] / segmentLength);
                Point3d[] crv3dPoints = null;
                crvs[ci]._DivideByCount_ThreadSafe(divby, true, out crv3dPoints);
                if (crv3dPoints != null)
                {
                    var copyFrom = 0;
                    var copyTo = crv3dPoints.Length - 1;

                    if (crvsStartAtEnds[ci] == CurveEnd.End)
                    {
                        var temp = copyFrom;
                        copyFrom = -copyTo;
                        copyTo = -temp;
                    }
                    if (!isFirst) copyFrom += skipPoints;
                    if (!isLast) copyTo -= skipPoints;
                    if (copyFrom > copyTo)
                    {
                        continue;
                    }
                    for (int i = copyFrom; i <= copyTo; i++)
                    {
                        var index = Math.Abs(i);
                        allPoints.Add(crv3dPoints[index]);
                    }
                    addedLength += crvsLengths[ci];
                }
            }
            allPoints.Add(crvs.Last()._P(crvsStartAtEnds.Last()._Reverse())); // add last point - to ensure we dint lost it when skipping small curves
            if (allPoints.Count > 2 && allPoints[0]._IsSame(allPoints[1])) allPoints.RemoveAt(0); // try remove duplicated first point
            if (allPoints.Count > 2 && allPoints[allPoints.Count - 1]._IsSame(allPoints[allPoints.Count - 2])) allPoints.RemoveAt(allPoints.Count - 1); // try remove duplicated last point

            // at least (80% of curves should be added to create new curve)
            if (addedLength / totalLength > 0.8)
            {
                //
                // Approximate new crv
                //
                string failReason;
                if (crvsStartAtEnds.First() == CurveEnd.End)
                {
                    allPoints.Reverse();
                }
                var res = _Curve._TryCreateControlPointCurve_AvarageQuality(allPoints.ToArray(), out failReason);
                if (res == null)
                {
                    return null;
                }
                //Layers.Debug.AddCurve(res);
                //Layers.Debug.AddPoints(allPoints);
                return res;
            }

            return null;
        }

        /// <summary>
        /// Split 3d curve using points
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="points"></param>
        /// <param name="splitedCrvs"></param>
        /// <param name="failReason"></param>
        /// <returns></returns>
        public static bool _SplitCrv(this Curve crv, List<Point3d> points, out List<Curve> splitedCrvs, out string failReason)
        {
            splitedCrvs = null;
            var ts3d = new List<double>();
            foreach (var point in points)
            {
                double t;
                if (!crv.ClosestPoint(point, out t))
                {
                    failReason = "failed to find 't' parameter on curve";
                    return false;
                }
                ts3d.Add(t);
            }
            var crvs = crv.Split(ts3d);
            if (crvs.Length - 1 != points.Count)
            {
                failReason = "failed to split curve using 't' parameter";
                return false;
            }
            splitedCrvs = new List<Curve>(crvs);
            failReason = "";
            return true;
        }

    }
}
