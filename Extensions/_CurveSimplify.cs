using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class _CurveSimplify
    {
        static int[] Simplify_nums = { 2, 7, 13, 25, 51, 101, 201, 301 };// odd numbers
        public static Stopwatch Watch_NEW2 = new Stopwatch();
        public static Stopwatch Watch_NEW3 = new Stopwatch();
        public static long _Simplify_NEW2_ElapsedTicks;
        public static long _Simplify_NEW3_ElapsedTicks;
        /// <summary>
        /// Returns simplest version of crv where maxDeviation to original is less than 0.001
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="srf">for 2d curves srf used to convert 2d coo into 3d - this is for proper deviation measure (distance between iriginal and new curve)</param>
        /// <param name="workWithSingularSurfaces"></param>
        /// <returns></returns>
        public static NurbsCurve _Simplify(this Curve crv, Surface srf = null, bool workWithSingularSurfaces = true)
        {
            //return _Simplify_NEW1(crv, srf, workWithSingularSurfaces);
            //return _Simplify_NEW2(crv, srf, workWithSingularSurfaces);
            return _Simplify_NEW3(crv, srf, workWithSingularSurfaces); // works 2 times faster for 2d curves from previous version - _Simplify_NEW2

            //Watch_NEW2.Restart();
            //_Simplify_NEW2(crv, srf, workWithSingularSurfaces);
            //_Simplify_NEW2_ElapsedTicks += Watch_NEW2.ElapsedTicks;
            //Watch_NEW3.Restart();
            //var res = _Simplify_NEW3(crv, srf, workWithSingularSurfaces);
            //_Simplify_NEW3_ElapsedTicks += Watch_NEW3.ElapsedTicks;
            //return res;
        }

        //private static int Num;
        private static NurbsCurve _Simplify_NEW3(this Curve crv, Surface srf = null, bool workWithSingularSurfaces = false)
        {
            // log.temp("_Simplify_NEW   workWithSingularSurfaces = " + workWithSingularSurfaces);
            var ORIGIN = crv._ToNurbsCurve();
            var crvDimension = crv.Dimension;
            if (crv.Degree != 3) return ORIGIN;
            if (crvDimension != 2 && crvDimension != 3) return ORIGIN;
            if (crvDimension == 2 && srf == null)
            {
                log.wrong("_CurveSimplify._Simplify() - 2d curve requires 'srf' parameter");
                return ORIGIN;
            }
            if (crv._ZigZagDeformationExists(srf)) return ORIGIN;
            var numValueMIN = 0;
            var numValueMAX = Math.Min(Simplify_nums.Last(), ORIGIN.Points.Count / 2);
            if (numValueMIN > numValueMAX) return ORIGIN;

            NurbsCurve crv2d = null;
            NurbsCurve crv3d = null;

            const bool preserveTangents3d = false;
            const bool preserveTangents2d = false;
            int VALIDATION_SEGMENTS_COUNT3d = 20;
            int VALIDATION_SEGMENTS_COUNT2d = 20;
            double MAX_DEVIATION3d = 0.001;
            const double MAX_LENGTH_DIFF3d = 0.005;// in percent = 0.5%
            var checkControlPoints = false; // if control points also should be close to original curve


            SurfaceSingulars ssingular = null;
            if (crvDimension == 2)
            {
                ssingular = new SurfaceSingulars(srf);
                if (ssingular.HasSingulars)
                {
                    if (!workWithSingularSurfaces) return ORIGIN;
                    if (srf._PointsAreCloseSingularity_HighJump(ORIGIN.Points._SurfacePoints(), ssingular))
                    {
                        MAX_DEVIATION3d = 0.0001;
                    }
                    checkControlPoints = true;
                }

                crv2d = ORIGIN;// set only for dimension == 2
            }
            else
            {
                crv3d = ORIGIN;// set only for dimension == 3
            }
            var crv3dLen = ORIGIN._Get3dLength(srf);
            if (crv3dLen._IsZero())
            {
                return ORIGIN;
            }


            Point3d[] crv2d_3dPoints = null;
            Point3d[] crv3dPoints = null;
            VALIDATION_SEGMENTS_COUNT3d = Math.Max(VALIDATION_SEGMENTS_COUNT3d, Convert.ToInt32(Math.Min(1000, crv3dLen / 0.5)));
            VALIDATION_SEGMENTS_COUNT2d = Math.Max(VALIDATION_SEGMENTS_COUNT2d, Convert.ToInt32(Math.Min(1000, crv3dLen / 0.5)));
            if (crvDimension == 2)
            {
                if (crv2d == null) return ORIGIN;
                Point3d[] crv2dPoints = null;
                crv2d._DivideByCount_ThreadSafe(VALIDATION_SEGMENTS_COUNT2d, true, out crv2dPoints);
                if (crv2dPoints == null) return ORIGIN;
                crv2d_3dPoints = crv2dPoints.Select(o => srf.PointAt(o.X, o.Y)).ToArray();
            }
            else
            {
                crv3d._DivideByCount_ThreadSafe(VALIDATION_SEGMENTS_COUNT3d, true, out crv3dPoints);
                if (crv3dPoints == null) return ORIGIN;
            }

            foreach (var num in Simplify_nums)
            {
                if (num < numValueMIN || num > numValueMAX) continue;
                if (num == 2 && srf != null)
                {
                    if (srf.Degree(0) != 1 && srf.Degree(1) != 1) continue; // one degree has to be linear
                }
                //
                // Rebuild
                //
                NurbsCurve simple3d = null;
                NurbsCurve simple2d = null;
                if (crvDimension == 2 && crv2d != null)
                {

                    simple2d = (num == 2)
                                 ? new LineCurve(new Point2d(crv2d.PointAtStart.X, crv2d.PointAtStart.Y), new Point2d(crv2d.PointAtEnd.X, crv2d.PointAtEnd.Y))._ToNurbsCurve()
                                 : crv2d.Rebuild(num, 3, false);
                    if (simple2d == null) continue;

                    if (simple2d._ZigZagDeformationExists(srf))
                    {
                        //var crv2dNorm = crv2d._2dNormalize(srf);
                        //var simple2dNorm = crv2dNorm.Rebuild(num, 3, false);
                        //simple2d = simple2dNorm._2dUnnormalize(srf);
                        if (crv3d == null) crv3d = crv2d._2dTo3d(srf); if (crv3d == null) continue;// lazy initialization of crv3d
                        simple3d = crv3d.Rebuild(num, 3, preserveTangents3d);
                        if (simple3d == null) continue;
                        simple2d = srf._Convert3dCurveTo2d_WithoutRebuildAndSimplify(simple3d, null, crv, crv3d);
                        if (simple2d == null) continue;
                        simple2d = simple2d._Fix2dContorlPoints(srf, ssingular);
                        if (simple2d._ZigZagDeformationExists(srf))
                        {
                            //return simple2d;
                            continue;
                        }
                    }


                    simple2d.SetStartPoint(crv2d.PointAtStart);
                    simple2d.SetEndPoint(crv2d.PointAtEnd);

                    // Compare with original

                    // Simplification near to singularity must very very strict!!!  
                    // Control poins must be very close to original curve  - its guarantee that method '_Curve._Fix2dContorlPoints()' wont change any control point, and issue 'TrimControlPointsNotCorrectInSingularity' will be fixed
                    if (checkControlPoints)
                    {
                        if (crv3d == null) crv3d = crv2d._2dTo3d(srf); if (crv3d == null) continue; // lazy initialization of crv3d
                        if (!_Simplify_IsSimplifiedControlPointsCloseToOrigin(simple2d, srf, MAX_DEVIATION3d, crv3d))
                        {
                            continue;
                        }
                    }
                    // Compare with original
                    if (!_Simplify_IsSimplifiedCloseToOrigin_NEW3(simple2d, srf, MAX_DEVIATION3d, crv2d_3dPoints))
                    {
                        continue;
                    }
                }
                else if (crvDimension == 3 && crv3d != null)
                {
                    simple3d = (num == 2)
                                 ? new LineCurve(crv3d.PointAtStart, crv3d.PointAtEnd)._ToNurbsCurve()
                                 : crv3d.Rebuild(num, 3, preserveTangents3d);
                    if (simple3d == null) continue;
                    // Compare with original
                    if (!_Simplify_IsSimplifiedCloseToOrigin(simple3d, srf, preserveTangents3d,
                             crv3d, crv3dLen, crv3dPoints,
                             MAX_LENGTH_DIFF3d, MAX_DEVIATION3d))
                    {
                        continue;
                    }
                }

                // Return new simplified curve
                if (crvDimension == 2)
                {

                    //Layers.Debug.AddCurve(simple3d, "trim 3d", Color.Purple);
                    //var z3d = simple3d._ZigZagDeformationExists(srf);
                    //var z2d = simple2d._ZigZagDeformationExists(srf);
                    var res =  simple2d._Fix2dContorlPoints(srf, ssingular);
                    //Layers.Debug.AddCurve(crv3d, "crv3d", Color.PeachPuff);
                    //Layers.Debug.AddCurveControlPoints(res, srf, Color.Orchid);
                    //Layers.Debug.AddCurve(simple3d, "simple3d", Color.Orchid);
                    //var z2ds = res._ZigZagDeformationExists(srf);
                    //Layers.Debug.AddCurve(simple3d, "simple3d", Color.Aqua);
                    return res;
                }
                else
                {
                    return simple3d;
                }
            }

            // we fail - return origin curve
            return ORIGIN;
        }



        private static bool _Simplify_IsSimplifiedCloseToOrigin(NurbsCurve simple3d, Surface srf, bool preserveTangents, NurbsCurve origin, double originLength, Point3d[] originCrvPoints, double MAX_LENGTH_DIFF, double MAX_DEVIATION)
        {
            var SEGMENTS_COUNT = originCrvPoints.Length - 1;
            // Check length
            var simpleLength = simple3d._GetLength_ThreadSafe_Fast(); // no need to call _GetLength_ThreadSafe - our curve is in memory
            var diffLengthPercent = Math.Abs(originLength - simpleLength) / originLength;
            if (diffLengthPercent > MAX_LENGTH_DIFF) // if length diff is more than 1% - our new simplified curve not enought good to replace original
            {
                return false;
            }

            // Check zigzags
            if (simple3d._ZigZagDeformationExists(srf)) return false;

            double minDev;
            double maxDev;

            // Check deviation 1
            var d1 = origin._GetDistanceToBiggerCurve(simple3d, out minDev, out maxDev, SEGMENTS_COUNT, MAX_DEVIATION, originCrvPoints);
            var maxDeviation1 = maxDev;
            if (!d1 || maxDeviation1 > MAX_DEVIATION) return false;

            // Check deviation 2
            var d2 = simple3d._GetDistanceToBiggerCurve(origin, out minDev, out maxDev, SEGMENTS_COUNT, MAX_DEVIATION);
            var maxDeviation2 = maxDev;
            if (!d2 || maxDeviation2 > MAX_DEVIATION) return false;

            return true;
        }


        private static bool _Simplify_IsSimplifiedCloseToOrigin_NEW3(NurbsCurve simple2d, Surface srf, double MAX_DEVIATION, Point3d[] origin2d_3dPoints)
        {
            var SEGMENTS_COUNT = origin2d_3dPoints.Length - 1;
            Point3d[] simple2dPoints;
            simple2d._DivideByCount_ThreadSafe(SEGMENTS_COUNT, true, out simple2dPoints);
            if (simple2dPoints == null || simple2dPoints.Length != origin2d_3dPoints.Length)
            {
                return false;
            }
            var simple2d_3dPoints = simple2dPoints.Select(o => srf.PointAt(o.X, o.Y)).ToArray();
            for (int i = 0; i < simple2d_3dPoints.Length; i++)
            {
                var point3d_simple = simple2d_3dPoints[i];
                var point3d_origin = origin2d_3dPoints[i];
                var dist = point3d_simple._DistanceTo(point3d_origin);
                if (dist > MAX_DEVIATION)
                {
                    return false;
                }
            }
            return true;
        }


        private static bool _Simplify_IsSimplifiedControlPointsCloseToOrigin(NurbsCurve crv, Surface srf, double MAX_DEVIATION, NurbsCurve crv3dOriginal)
        {
            // ver 1 - detailed for debugging
            //var maxDist = -1.0;
            //foreach (var p in crv.Points)
            //{
            //    var p3d = p.Location;
            //    if (crv.Dimension == 2)
            //    {
            //        p3d = srf.PointAt(p3d.X, p3d.Y);
            //    }
            //    var dist = crv3dOriginal._DistanceTo(p3d, MAX_DEVIATION, Double.MaxValue);
            //    if (dist > MAX_DEVIATION)
            //    {
            //        return false;
            //    }
            //    if (dist > maxDist)
            //    {
            //        maxDist = dist;
            //    }
            //}

            // ver 2 - fast
            var crvDimension = crv.Dimension;
            foreach (var p in crv.Points)
            {
                var p3d = p.Location;
                if (crvDimension == 2)
                {
                    p3d = srf.PointAt(p3d.X, p3d.Y);
                }
                double t;
                if (!crv3dOriginal.ClosestPoint(p3d, out t, MAX_DEVIATION))
                {
                    return false;
                }
            }
            return true;
        }

        #region OLD CODE
        /*  OLD CODE
         * 
        private static NurbsCurve _Simplify_NEW1(this Curve crv, Surface srf = null, bool workWithSingularSurfaces = false)
        {
            var ORIGIN = crv._ToNurbsCurve();
            if (crv.Degree != 3) return ORIGIN;
            if (crv.Dimension != 2 && crv.Dimension != 3) return ORIGIN;
            var ORIGINLength = ORIGIN._GetLength_ThreadSafe();
            if (ORIGIN.Points.Count > 1000)
            {
                var temp = 0;
            }
            //var numValueMIN = ORIGIN.Points.Count / 25;
            var numValueMIN = 0;
            var numValueMAX = Math.Min(Simplify_nums.Last(), ORIGIN.Points.Count / 2);
            if (numValueMIN > numValueMAX) return ORIGIN;

            var crv3d = ORIGIN;
            var crv3dLen = ORIGINLength;
            if (crv3dLen._IsZero()) return ORIGIN;

            const bool preserveTangents3d = true;
            const bool preserveTangents2d = false;
            int SEGMENTS_COUNT3d = 20;
            int SEGMENTS_COUNT2d = 50;
            const double MAX_DEVIATION3d = 0.001;
            double MAX_DEVIATION2d = 0.01;
            const double MAX_LENGTH_DIFF3d = 0.005;// in percent = 0.5%
            const double MAX_LENGTH_DIFF2d = 0.01;// in percent = 1%

            //double M = 1;
            SurfaceSingulars ssingular = null;
            if (crv.Dimension == 2)
            {
                ssingular = new SurfaceSingulars(srf);
                if (ssingular.HasSingulars && !workWithSingularSurfaces) return ORIGIN;

                crv3d = crv3d._2dTo3d(srf);
                if (crv3d == null) return ORIGIN;
                crv3dLen = crv3d._GetLength_ThreadSafe();// no need to call _GetLength_ThreadSafe - our curve is in memory
                if (crv3dLen._IsZero()) return ORIGIN;

                if (srf != null)
                {
                    var domMinLength = Math.Min(srf.Domain(0).Length, srf.Domain(1).Length);
                    var domMaxLength = Math.Max(srf.Domain(0).Length, srf.Domain(1).Length);
                    MAX_DEVIATION2d *= domMinLength / domMaxLength;
                }
                var T0 = srf.PointAt(srf.Domain(0).T0, srf.Domain(1).T0);
                var T1 = srf.PointAt(srf.Domain(0).T1, srf.Domain(1).T1);
                var L = T0._DistanceTo(T1);
                var C = Math.Sqrt(Math.Pow(srf.Domain(0).Length, 2) + Math.Pow(srf.Domain(1).Length, 2));
                var M = C / L;
            }

            Point3d[] crv2dORIGINPoints = null;
            Point3d[] crv3dPoints = null;
            SEGMENTS_COUNT3d = Math.Max(SEGMENTS_COUNT3d, Convert.ToInt32(Math.Min(1000, crv3dLen / 0.5)));
            SEGMENTS_COUNT2d = SEGMENTS_COUNT3d;
            crv3d.DivideByCount(SEGMENTS_COUNT3d, true, out crv3dPoints);
            if (crv3dPoints == null) return ORIGIN;
            if (crv.Dimension == 2)
            {
                ORIGIN.DivideByCount(SEGMENTS_COUNT2d, true, out crv2dORIGINPoints);
                if (crv2dORIGINPoints == null) return ORIGIN;
            }

            foreach (var num in Simplify_nums)
            {
                if (num < numValueMIN || num > numValueMAX) continue;

                // Rebuild
                var simple = crv3d.Rebuild(num, 3, preserveTangents3d);
                if (simple == null) continue;

                if (!_Simplify_IsSimplifiedCloseToOrigin(simple, srf, preserveTangents3d,
                    crv3d, crv3dLen, crv3dPoints,
                    MAX_LENGTH_DIFF3d, MAX_DEVIATION3d, SEGMENTS_COUNT3d))
                {
                    continue;
                }

                if (crv.Dimension == 2)
                {
                    var simple2d = srf._Convert3dCurveTo2d(simple, ssingular);
                    if (simple2d == null) continue;
                    simple2d.SetStartPoint(ORIGIN.PointAtStart);
                    simple2d.SetEndPoint(ORIGIN.PointAtEnd);
                    if (!_Simplify_IsSimplifiedCloseToOrigin(simple2d, srf, preserveTangents2d,
                        ORIGIN, ORIGINLength, crv2dORIGINPoints,
                        MAX_LENGTH_DIFF2d, MAX_DEVIATION2d, SEGMENTS_COUNT2d))
                    {
                        continue;
                    }
                    simple = simple2d;
                }

                return simple;
            }

            // we fail - return origin curve
            return ORIGIN;
        }
     
                //private static int Num;
        private static NurbsCurve _Simplify_NEW2(this Curve crv, Surface srf = null, bool workWithSingularSurfaces = false)
        {
            // log.temp("_Simplify_NEW   workWithSingularSurfaces = " + workWithSingularSurfaces);
            var ORIGIN = crv._ToNurbsCurve();
            if (crv.Degree != 3) return ORIGIN;
            if (crv.Dimension != 2 && crv.Dimension != 3) return ORIGIN;
            if (crv._ZigZagDeformationExists(srf)) return ORIGIN;
            //if (ORIGIN.Points.Count > 1000)
            //{
            //    var temp = 0;
            //}
            //var numValueMIN = ORIGIN.Points.Count / 25;
            var numValueMIN = 0;
            var numValueMAX = Math.Min(Simplify_nums.Last(), ORIGIN.Points.Count / 2);
            if (numValueMIN > numValueMAX) return ORIGIN;

            NurbsCurve crv2d = null;
            var crv3d = ORIGIN;

            const bool preserveTangents3d = false;
            const bool preserveTangents2d = false;
            int SEGMENTS_COUNT3d = 20;
            int SEGMENTS_COUNT2d = 50;
            double MAX_DEVIATION3d = 0.001;
            const double MAX_LENGTH_DIFF3d = 0.005;// in percent = 0.5%
            var checkControlPoints = false; // if control points also should be close to original curve


            //double M = 1;
            SurfaceSingulars ssingular = null;
            if (crv.Dimension == 2)
            {
                ssingular = new SurfaceSingulars(srf);
                if (ssingular.HasSingulars)
                {
                    if (!workWithSingularSurfaces) return ORIGIN;

                    if (srf._PointsAreCloseSingularity_HighJump(ORIGIN.Points._SurfacePoints(), ssingular))
                    {
                        MAX_DEVIATION3d = 0.0001;
                        //Num++;
                        //crv._ToNurbsCurve().Points._SurfacePoints().ForEach(o => Layers.Debug.AddTextPoint("" + Num, srf.PointAt(o.u, o.v), Color.Aqua));
                        //Layers.Debug.AddCurve(crv3d._2dTo3d(srf), "crv3d._2dTo3d(srf)", Color.Aqua);
                    }
                    checkControlPoints = true;
                }




                crv2d = ORIGIN;
                crv3d = crv3d._2dTo3d(srf);
                if (crv3d == null)
                {
                    return ORIGIN;
                }
                if (srf != null)
                {
                    var domMinLength = Math.Min(srf.Domain(0).Length, srf.Domain(1).Length);
                    var domMaxLength = Math.Max(srf.Domain(0).Length, srf.Domain(1).Length);
                }
                var T0 = srf.PointAt(srf.Domain(0).T0, srf.Domain(1).T0);
                var T1 = srf.PointAt(srf.Domain(0).T1, srf.Domain(1).T1);
                var L = T0._DistanceTo(T1);
                var C = Math.Sqrt(Math.Pow(srf.Domain(0).Length, 2) + Math.Pow(srf.Domain(1).Length, 2));
                var M = C / L;
            }

            var crv3dLen = crv3d._GetLength_ThreadSafe_Fast();
            if (crv3dLen._IsZero())
            {
                return ORIGIN;
            }


            Point3d[] crv2dPoints = null;
            Point3d[] crv3dPoints = null;
            SEGMENTS_COUNT3d = Math.Max(SEGMENTS_COUNT3d, Convert.ToInt32(Math.Min(1000, crv3dLen / 0.5)));
            SEGMENTS_COUNT2d = Math.Max(SEGMENTS_COUNT2d, Convert.ToInt32(Math.Min(1000, crv3dLen / 0.1)));
            crv3d.DivideByCount(SEGMENTS_COUNT3d, true, out crv3dPoints);
            if (crv3dPoints == null)
            {
                return ORIGIN;
            }
            //if (crv.Dimension == 2)
            //{
            //    if (crv2d == null) return ORIGIN;
            //    crv2d.DivideByCount(SEGMENTS_COUNT2d, true, out crv2dPoints);
            //    if (crv2dPoints == null) return ORIGIN;
            //}

            foreach (var num in Simplify_nums)
            {
                if (num < numValueMIN || num > numValueMAX) continue;

                // Rebuild
                NurbsCurve simple3d = null;
                NurbsCurve simple2d = null;
                if (crv.Dimension == 2 && crv2d != null)
                {
                    simple2d = crv2d.Rebuild(num, 3, false);
                    if (simple2d == null) continue;

                    if (simple2d._ZigZagDeformationExists(srf))
                    {
                        //var crv2dNorm = crv2d._2dNormalize(srf);
                        //var simple2dNorm = crv2dNorm.Rebuild(num, 3, false);
                        //simple2d = simple2dNorm._2dUnnormalize(srf);

                        simple3d = crv3d.Rebuild(num, 3, preserveTangents3d);
                        if (simple3d == null) continue;
                        simple2d = srf._Convert3dCurveTo2d_WithoutRebuildAndSimplify(simple3d, null, crv, crv3d);
                        if (simple2d == null) continue;
                        simple2d = simple2d._Fix2dContorlPoints(srf, ssingular);
                        if (simple2d._ZigZagDeformationExists(srf))
                        {
                            //return simple2d;
                            continue;
                        }
                    }


                    simple2d.SetStartPoint(crv2d.PointAtStart);
                    simple2d.SetEndPoint(crv2d.PointAtEnd);
                    //simple2d = simple2d._Fix2dContorlPoints(srf, ssingular);
                    simple3d = simple2d._2dTo3d(srf);
                    if (simple3d == null) continue;

                }
                else if (crv.Dimension == 3)
                {
                    simple3d = crv3d.Rebuild(num, 3, preserveTangents3d);
                    if (simple3d == null) continue;
                }

                // Compare with original
                if (simple3d == null) continue;

                if (!_Simplify_IsSimplifiedCloseToOrigin(simple3d, srf, preserveTangents3d,
                    crv3d, crv3dLen, crv3dPoints,
                    MAX_LENGTH_DIFF3d, MAX_DEVIATION3d))
                {
                    continue;
                }

                // Simplification near to singularity must very very strict!!!  
                // Control poins must be very close to original curve  - its guarantee that method '_Curve._Fix2dContorlPoints()' wont change any control point, and issue 'TrimControlPointsNotCorrectInSingularity' will be fixed
                if (checkControlPoints)
                {
                    if (!_Simplify_IsSimplifiedControlPointsCloseToOrigin(simple2d, srf, MAX_DEVIATION3d, crv3d))
                    {
                        continue;
                    }
                }

                // Return new simplified curve
                if (crv.Dimension == 2)
                {

                    //Layers.Debug.AddCurve(simple3d, "trim 3d", Color.Purple);
                    //var z3d = simple3d._ZigZagDeformationExists(srf);
                    //var z2d = simple2d._ZigZagDeformationExists(srf);
                    var res =  simple2d._Fix2dContorlPoints(srf, ssingular);
                    //Layers.Debug.AddCurve(crv3d, "crv3d", Color.PeachPuff);
                    //Layers.Debug.AddCurveControlPoints(res, srf, Color.Orchid);
                    //Layers.Debug.AddCurve(simple3d, "simple3d", Color.Orchid);
                    //var z2ds = res._ZigZagDeformationExists(srf);
                    //Layers.Debug.AddCurve(simple3d, "simple3d", Color.Aqua);
                    return res;
                }
                else
                {
                    return simple3d;
                }
            }

            // we fail - return origin curve
            return ORIGIN;
        }

        */
        #endregion

        /// <summary>
        /// Returns complex version of crv where maxDeviation to original is less than 0.001
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="desiredControlPointsCount"></param>
        /// <returns></returns>
        public static NurbsCurve _Complexify(this Curve crv, int desiredControlPointsCount)
        {
            var crvNurb = crv._ToNurbsCurve();
            if (desiredControlPointsCount < crvNurb.Points.Count)
            {
                return crvNurb;
            }

            var preserveTangents = true;
            const int SEGMENTS_COUNT = 100;
            const double MAX_ALLOWED_DEVIATION = 0.001;
            if (crv.Dimension == 2)
            {
                preserveTangents = false;
            }

            Point3d[] crvPoints;
            crvNurb._DivideByCount_ThreadSafe(SEGMENTS_COUNT, true, out crvPoints);
            var nums = new List<int>() { desiredControlPointsCount, desiredControlPointsCount * 2, desiredControlPointsCount * 3 }; // odd numbers
            foreach (var num in nums)
            {
                if (num < crvNurb.Points.Count) break;
                var resComplex = crvNurb.Rebuild(num, crvNurb.Degree, preserveTangents);
                if (resComplex == null) continue;

                //var dist = crvNurb._GetAvgDistancesBetweenCurves(resSimple, segmentsCount);
                //if (dist < 0.0001)
                //{
                //    return resSimple;
                //}
                double minDeviation;
                double maxDeviation;
                if (crvNurb._GetDistanceToBiggerCurve(resComplex, out minDeviation, out maxDeviation, SEGMENTS_COUNT, MAX_ALLOWED_DEVIATION, crvPoints))
                {
                    if (maxDeviation < MAX_ALLOWED_DEVIATION)
                    {
                        return resComplex;
                    }
                }
            }
            return crvNurb;
        }




    }
}
