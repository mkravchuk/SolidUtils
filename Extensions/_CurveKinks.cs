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
    public class CurveKinkData
    {
        public Curve Crv;
        public CurveEnd CrvEnd;
        public Curve CrvFixed;
        public double DegreeChange;
        public double DegreeDeviation_FromAvg_Closest25ofCurve;
        public double Closest25ofCurve_DegreeAvgChange;
        public double Closest25ofCurve_DegreeDeviationMaxFromAvg;
        public Point3d Point;
        public Vector3d TangentCurrent;
        public Vector3d TangentExcepted;
        public string DegreesChangesStr;
        public int DeviationBiggerNTimes;
    }

    public static class _CurveKinks
    {
        private static readonly CurveEnd[] ends = { CurveEnd.Start, CurveEnd.End };
        private const bool DEBUG = false;
        private const int DIVBY_TEST = 20; // 5% of domain length


        /// <summary>
        /// Finds all kinks in a curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="crvLength">provide curve length if you have for speed optimization</param>
        /// <returns>List of kinks or Null </returns>
        public static List<CurveKinkData> _Kinks_Find(this Curve curve, double? crvLength = null)
        {
            // dont work with very small curves
            var crvNormal = new CurveNormalized(curve, crvLength);
            if (crvNormal.Length < 0.01) return null;
            var crvDomain = curve.Domain;

            // lines dont have kinks
            if (crvNormal.Degree == 1) return null;

            // Fast check
            var PstepFast = 1.0 / DIVBY_TEST; //step from start and end curve
            var pStart1 = 0;
            var pStart2 = PstepFast;
            var pEnd2 = 1 - PstepFast;
            var pEnd1 = 1;
            var pStart1Tangent = curve.TangentAtStart;
            //var pStart2Tangent = crvNormal.TangentAt(pStart2); //- v1 - slow
            //var pEnd2Tangent = crvNormal.TangentAt(pEnd2); //- v1 - slow
            var pStart2Tangent = curve.TangentAt(crvDomain.T0 + crvDomain.Length * PstepFast); //- v2 - faster
            var pEnd2Tangent = curve.TangentAt(crvDomain.T1 - crvDomain.Length * PstepFast); //- v2 - faster
            var pEnd1Tangent = curve.TangentAtEnd;
            var degreeStart = pStart1Tangent._AngleOfUnitizedVectors(pStart2Tangent)._RadianToDegree();
            var degreeEnd = pEnd2Tangent._AngleOfUnitizedVectors(pEnd1Tangent)._RadianToDegree();
            if (degreeStart < 5 && degreeEnd < 5) return null;

            // Detailed check

            //lets calculate tangents
            var tangents = new Vector3d[DIVBY_TEST + 1];
            tangents[0] = pStart1Tangent; //speed optimization - lets reuse tangent (skipp duplicate tangent calculation)
            tangents[tangents.Length - 1] = pEnd1Tangent; //speed optimization - lets reuse tangent (skipp duplicate tangent calculation)
            Point3d[] points;
            double[] ts;
            string failReason;
            if (curve._TryDivideByCount(DIVBY_TEST, out points, out ts, out failReason) // try get 'ts' - it is faster then make separate call to curveNormal.T()
                && points.Length == tangents.Length) 
            {
                var Pstep = 1.0 / DIVBY_TEST; //5% of domain length
                for (int i = 1; i < tangents.Length - 1; i++) // from second for prelast for speed optimization (first and last is calculated just before)
                {
                    //DEBUG  - test if 'crvNormal.T()' works same as 'curve.DivideByCount()'
                    //var p = Pstep * i;
                    //var tsNormal = crvNormal.T(p);
                    //var tdiff = Math.Abs(tsNormal - ts[i]);
                    //if (tdiff > curve.Domain.Length/1000)
                    //{
                    //    var temp = 0;
                    //}
                    //ENDDEBUG
                    tangents[i] = curve.TangentAt(ts[i]);
                }
            }
            else //  if DivideByCount fails - lets use crvNormal - its slower but works same
            {
                var Pstep = 1.0 / DIVBY_TEST; //5% of domain length

                for (int i = 1; i < tangents.Length - 1; i++) // from second for prelast for speed optimization (first and last is calculated just before)
                {
                    var p = Pstep * i;
                    tangents[i] = crvNormal.TangentAt(p);
                }
            }

            //lets calculate an avarage tangent change
            // we asume that on middle of the curve it doesnt have kinks. 
            string[] changesStr = null;
            var changesRadians = new double[DIVBY_TEST];
            var changes = new double[DIVBY_TEST];
            for (int i = 0; i < changes.Length; i++)
            {
                changesRadians[i] = tangents[i]._AngleOfUnitizedVectors(tangents[i + 1]);
                changes[i] = changesRadians[i]._RadianToDegree();
            }
            if (DEBUG)
            {
                changesStr = changes.Select(o => o._ToStringAngle()).ToArray();
                log.temp("AngleChanges = " + String.Join(", ", changesStr));
            }

            List<CurveKinkData> res = null;
            foreach (var end in ends)
            {
                var DIVBY25 = DIVBY_TEST / 4;
                var iFirst = 0;
                var iLast = changes.Length - 1;
                var iO = (end == CurveEnd.Start) ? iFirst : iLast;
                var iA = (end == CurveEnd.Start) ? iFirst : iLast - DIVBY25;
                var iB = (end == CurveEnd.Start) ? iFirst + DIVBY25 : iLast;

                double maxChange = 0; //25% of start
                double summChange = 0;
                int summCount = 0;
                for (int i = iA; i <= iB; i++)
                {
                    if (i == iO) continue; //skip first and last
                    maxChange = Math.Max(maxChange, changes[i]);
                    summChange += changes[i];
                    summCount++;
                }
                var avgChange = summChange / DIVBY25;

                var maxDeviation = maxChange - avgChange;
                var endDeviation = changes[iO] - avgChange;

                var deviationTimes = (int)(endDeviation / maxDeviation._GetNonZeroForDevisionOperation());
                if (deviationTimes >= 10 && endDeviation >= 5) // if end change is really bigger from previous changes - we have kink
                {
                    var iTangent = (end == CurveEnd.Start) ? 0 : DIVBY_TEST;
                    var kink = new CurveKinkData
                    {
                        Crv = curve,
                        CrvEnd = end,
                        DegreeChange = changes[iO],
                        DegreeDeviation_FromAvg_Closest25ofCurve = endDeviation,
                        Closest25ofCurve_DegreeAvgChange = avgChange,
                        Closest25ofCurve_DegreeDeviationMaxFromAvg = maxDeviation,
                        //Point = (end == CurveEnd.Start ? crv.PointAt((tStart1 + tStart2) / 2) : crv.PointAt((tEnd1 + tEnd2) / 2)),
                        Point = crvNormal.PointAt(end),
                        TangentCurrent = tangents[iTangent],
                        TangentExcepted = tangents[iTangent + ((end == CurveEnd.Start) ? 1 : -1)],
                        DegreesChangesStr = String.Join(", ", changes.Select(o => o._ToStringAngle()).ToArray()),
                        DeviationBiggerNTimes = deviationTimes
                    };
                    if (res == null) res = new List<CurveKinkData>(2);
                    res.Add(kink);
                }
            }


            if (res != null && DEBUG)
            {
                if (DEBUG) log.temp("================");
                log.temp("AngleChanges = " + String.Join(", ", changesStr));
                foreach (var kink in res)
                {
                    var problem = String.Format("Kink at {0} by {1}", kink.CrvEnd, kink.DegreeDeviation_FromAvg_Closest25ofCurve._ToStringAngle());
                    log.temp(problem);
                    log.temp("endDeviation/maxDeviation = {0:0.000}", (kink.DegreeDeviation_FromAvg_Closest25ofCurve / kink.Closest25ofCurve_DegreeDeviationMaxFromAvg._GetNonZeroForDevisionOperation()));
                }
                if (DEBUG) log.temp("================");
            }

            return res;
        }

        public static Curve _Kinks_TryRemove(this Curve curve, List<CurveKinkData> kinks, out string failReason, out double deviation, double maxAllowedDeviation = 0.01)
        {
            failReason = "";
            deviation = 0;

            if (curve == null)
            {
                failReason = "crv == null";
                return null;
            }

            var DIVBY_FIX = 20;   // 5% of domain length
            var DIVBY_MIN = 100;
            double MAX_ALLOWED_DEVIATION = 0.1;
            Curve crv = null;

            var failReasons = new List<string>();

            // Try smooth internal method
            crv = _Kinks_TryRemove_Smooth_Iternal(curve, kinks, DIVBY_FIX, DIVBY_MIN, out failReason);
            if (crv != null)
            {
                if (_Kinks_NewCurve_IsValid(curve, crv, kinks, out deviation, out failReason, MAX_ALLOWED_DEVIATION))
                {
                    return crv;
                }
            }
            failReasons.Add(failReason);

            // Try smooth method
            crv = _Kinks_TryRemove_Smooth(curve, kinks, DIVBY_FIX, DIVBY_MIN, out failReason);
            if (crv != null)
            {
                if (_Kinks_NewCurve_IsValid(curve, crv, kinks, out deviation, out failReason, MAX_ALLOWED_DEVIATION))
                {
                    return crv;
                }
            }
            failReasons.Add(failReason);

            // Try simple method
            var crvSimple= _Kinks_TryRemove_Simple(curve, kinks, DIVBY_FIX, DIVBY_MIN, out failReason);
            if (crvSimple != null)
            {
                if (_Kinks_NewCurve_IsValid(curve, crvSimple, kinks, out deviation, out failReason, MAX_ALLOWED_DEVIATION))
                {
                    return crvSimple;
                }
            }
            failReasons.Add(failReason);

            failReason = String.Join(", ", failReasons);

            log.wrong("Failed to fix edge kink: " + failReason);
            return null;
        }
        /// <summary>
        /// Remove kinks if possible.
        /// Works only for 3d curves.
        /// </summary>
        /// <param name="curve">3d curve</param>
        /// <param name="kinks">kinks provided by a method '_FindKinksAtEnds'</param>
        /// <param name="failReason">if a method failed - this string will have fail reason</param>
        /// <returns></returns>
        private static Curve _Kinks_TryRemove_Smooth_Iternal(Curve curve, List<CurveKinkData> kinks, int DIVBY_FIX, int DIVBY_MIN, out string failReason)
        {
            failReason = "";

            Percent cutPercents = 1.0 / DIVBY_FIX;

            //
            // Cut curve at kink ends
            //
            var crvCutted = new CurveNormalized(curve);
            foreach (var kink in kinks)
            {
                switch (kink.CrvEnd)
                {
                    case CurveEnd.Start:
                        crvCutted = crvCutted.Trim(0 + cutPercents, 1, out failReason); // remove outside interval
                        if (crvCutted == null)
                        {
                            return null;
                        }
                        break;
                    case CurveEnd.End:
                        crvCutted = crvCutted.Trim(0, 1 - cutPercents, out failReason);// remove outside interval 
                        if (crvCutted == null)
                        {
                            return null;
                        }
                        break;
                }
            }
            //if(DEBUG) Layers.Debug.AddCurve(crvCutted);

            //
            // Extend cutted crv
            //
            var crvExtended = crvCutted.Crv;
            foreach (var kink in kinks)
            {
                switch (kink.CrvEnd)
                {
                    case CurveEnd.Start:
                        crvExtended = crvExtended.Extend(CurveEnd.Start, CurveExtensionStyle.Arc, curve.PointAtStart);
                        if (crvExtended == null)
                        {
                            failReason = "failed to extend crv";
                            return null;
                        }
                        break;
                    case CurveEnd.End:
                        crvExtended = crvExtended.Extend(CurveEnd.End, CurveExtensionStyle.Arc, curve.PointAtEnd);
                        if (crvExtended == null)
                        {
                            failReason = "failed to extend crv";
                            return null;
                        }
                        break;
                }
            }
            if (DEBUG) Layers.Debug.AddCurve(crvExtended, "Smooth_Iternal", Color.Bisque);

            return crvExtended;
        }

        /// <summary>
        /// Remove kinks if possible.
        /// Works only for 3d curves.
        /// </summary>
        /// <param name="curve">3d curve</param>
        /// <param name="kinks">kinks provided by a method '_FindKinksAtEnds'</param>
        /// <param name="failReason">if a method failed - this string will have fail reason</param>
        /// <returns></returns>
        private static Curve _Kinks_TryRemove_Smooth(Curve curve, List<CurveKinkData> kinks, int DIVBY_FIX, int DIVBY_MIN, out string failReason)
        {
            failReason = "";

            Percent cutPercents = 1.0 / DIVBY_FIX;
            var crv = new CurveNormalized(curve);

            //
            // Cut curve at kink ends
            //
            var crvCutted = new CurveNormalized(curve);
            var cutPointAtStart = Point3d.Origin;
            var cutPointAtEnd = Point3d.Origin;
            foreach (var kink in kinks)
            {
                switch (kink.CrvEnd)
                {
                    case CurveEnd.Start:
                        cutPointAtStart = crv.PointAt(0 + cutPercents); // cut 5% from start
                        crvCutted = crvCutted.Trim(0 + cutPercents, 1, out failReason); // remove outside interval
                        if (crvCutted == null)
                        {
                            return null;
                        }
                        break;
                    case CurveEnd.End:
                        cutPointAtEnd = crv.PointAt(1 - cutPercents); // cut 5% from start
                        crvCutted = crvCutted.Trim(0, 1 - cutPercents, out failReason);// remove outside interval 
                        if (crvCutted == null)
                        {
                            return null;
                        }
                        break;
                }
            }
            //if(DEBUG) Layers.Debug.AddCurve(crvCutted);

            //
            // Extend cutted crv
            //
            var crvExtended = crvCutted.Crv;
            foreach (var kink in kinks)
            {
                switch (kink.CrvEnd)
                {
                    case CurveEnd.Start:
                        crvExtended = crvExtended._ExtendToPoint(CurveEnd.Start, curve.PointAtStart);
                        if (crvExtended == null)
                        {
                            failReason = "failed to extend crv";
                            return null;
                        }
                        break;
                    case CurveEnd.End:
                        crvExtended = crvExtended._ExtendToPoint(CurveEnd.End, curve.PointAtEnd);
                        if (crvExtended == null)
                        {
                            failReason = "failed to extend crv";
                            return null;
                        }
                        break;
                }
            }
            //if (DEBUG) Layers.Debug.AddCurve(crvExtended);


            //
            // Move ends of extended curve
            //
            var divby = crvExtended._GetDivBy(null, 0.01, DIVBY_MIN);
            Point3d[] pointsExtended;
            double[] tsExtended;
            if (!crvExtended._TryDivideByCount(divby, out pointsExtended, out tsExtended, out failReason))
            {
                return null;
            }


            foreach (var kink in kinks)
            {
                var end = kink.CrvEnd;
                var extendToPoint = curve._P(end);
                var direction = extendToPoint - crvExtended._P(end);
                var iStart = (end == CurveEnd.Start)   // will be more close to middle of curve
                    ? -Math.Abs(Array.BinarySearch(tsExtended, crvExtended._T(cutPointAtStart))) // negative value, like '-5'
                    : Math.Abs(Array.BinarySearch(tsExtended, crvExtended._T(cutPointAtEnd)));
                var iEnd = (end == CurveEnd.Start) ? 0 : pointsExtended.Length - 1; // will be at ends of curve

                for (int ii = iStart; ii <= iEnd; ii++)
                {
                    double shiftByPercent = (ii - iStart) / (double)(iEnd - iStart); // from 0 to 1
                    var i = Math.Abs(ii);
                    if (DEBUG) Layers.Debug.AddPoint(pointsExtended[i]);
                    pointsExtended[i] = pointsExtended[i] + direction * shiftByPercent;
                    if (DEBUG) Layers.Debug.AddPoint(pointsExtended[i]);
                }
            }

            //
            // Construct new curve
            //
            var newCuve = Curve.CreateControlPointCurve(pointsExtended, 3);
            if (newCuve == null)
            {
                failReason = "failed to create curve from 3d points";
                return null;
            }
            if (DEBUG) Layers.Debug.AddCurve(newCuve, "Smooth");
            newCuve = newCuve._Simplify(); //simplify crv after constructing if from many points

            return newCuve;
        }

        /// <summary>
        /// Remove kinks if possible.
        /// Works only for 3d curves.
        /// </summary>
        /// <param name="curve">3d curve</param>
        /// <param name="kinks">kinks provided by a method '_FindKinksAtEnds'</param>
        /// <param name="failReason">if a method failed - this string will have fail reason</param>
        /// <returns></returns>
        private static Curve _Kinks_TryRemove_Simple(Curve curve, List<CurveKinkData> kinks, int DIVBY_FIX, int DIVBY_MIN, out string failReason)
        {
            failReason = "";
            Percent cutPercents = 1.0 / DIVBY_FIX;
            var crv = new CurveNormalized(curve);

            // Div crv by small segments
            var divby = curve._GetDivBy(null, 0.01, DIVBY_MIN);

            Point3d[] points;
            double[] ts;
            if (!curve._TryDivideByCount(divby, out points, out ts, out failReason))
            {
                return null;
            }

            // Construct copy indexes (we will remove 5% from start and end, where the kinks found)
            var tMin = crv.Domain.T0 - 1;
            var tMax = crv.Domain.T1 + 1;
            foreach (var kink in kinks)
            {
                switch (kink.CrvEnd)
                {
                    case CurveEnd.Start:
                        tMin = crv.T(0 + cutPercents); // cut 5% from start
                        break;
                    case CurveEnd.End:
                        tMax = crv.T(1 - cutPercents); // cut 5% from end
                        break;
                }
            }


            // Copy points excluding kink diapasons
            var iadded = new List<int>();
            if (kinks.Exists(o => o.CrvEnd == CurveEnd.Start))
            {
                iadded.Add(0); // add start point anyway, since we need it and it will be removed by condition 'tMin <= ts[i]'
            }
            for (int i = 0; i < points.Length; i++)
            {
                if (tMin <= ts[i] && ts[i] <= tMax)
                {
                    iadded.Add(i);
                }
            }
            if (kinks.Exists(o => o.CrvEnd == CurveEnd.End))
            {
                iadded.Add(points.Length - 1); // add end point anyway, since we need it and it will be removed by condition 'ts[i] <= tMax'
            }
            var validPoints = iadded.Select(o => points[o]).ToList();

            //
            // Construct new curve
            //
            var newCuve = Curve.CreateControlPointCurve(validPoints, 3);
            //var newCuve = Curve.CreateInterpolatedCurve(validPoints, 3); - makes zigzag - so we cant use it here

            if (DEBUG)
            {
                Layers.Debug.AddCurve(newCuve);
                for (var i = 0; i < validPoints.Count; i++)
                {
                    var p = validPoints[i];
                    Layers.Debug.AddPoint(p);
                    //                    Layers.Debug.AddTextPoint("" + tsadded[i]._ToStringX(2), p);
                }

            }

            if (newCuve == null)
            {
                failReason = "failed to create curve from 3d points";
                return null;
            }
            newCuve = newCuve._Simplify(); //simplify crv after constructing if from many points
            return newCuve;
        }

        public static bool _Kinks_NewCurve_IsValid(Curve crv, Curve newCuve, List<CurveKinkData> kinks, out double deviation, out string failReason, double maxAllowedDeviation = 0.01)
        {
            failReason = "";
            deviation = 0;
            Percent cutPercents = 1.0 / DIVBY_TEST;
            var divby = newCuve._GetDivBy(null, 0.01, DIVBY_TEST * 5);


            if (newCuve == null)
            {
                failReason = "newCuve == null";
                return false;
            }

            //
            // Validate crv for deviation (crv shouldn't have big distance from original curve in diapason outside kinks)
            //
            Point3d[] points;
            double[] ts;
            if (!crv._TryDivideByCount(divby, out points, out ts, out failReason))
            {
                return false;
            }
            Point3d[] pointsNewCuve;
            if (!newCuve._TryDivideByCount(divby, out pointsNewCuve, out failReason))
            {
                return false;
            }

            // get region outside kinks
            var tMin = crv.Domain.T0 - 1;
            var tMax = crv.Domain.T1 + 1;
            foreach (var kink in kinks)
            {
                switch (kink.CrvEnd)
                {
                    case CurveEnd.Start:
                        tMin = crv._TAtPercent(cutPercents); // cut 5% from start
                        break;
                    case CurveEnd.End:
                        tMax = crv._TAtPercent(1 - cutPercents); // cut 5% from end
                        break;
                }
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (tMin <= ts[i] && ts[i] <= tMax)
                {
                    var oldPoint = points[i];
                    var newPoint = pointsNewCuve[i];
                    deviation = Math.Max(deviation, oldPoint._DistanceTo(newPoint));
                }
            }
            //log.temp("deviation = {0:0.00000}", deviation);

            // max dist must be reasonable - this is they key of fixing: 'correcting do not break'
            if (deviation > maxAllowedDeviation)
            {
                failReason = "deviation {0:0.00000} is max from allowed"._Format(deviation);
                //log.wrong("_Curve._TryRemoveKinks: " + failReason);
                return false;
            }

            //
            // Validate crv for kinks (if we removing kinks - they should really be removed - otherwise fix failed)
            //
            if (DEBUG)
            {
                var oldKinks = crv._Kinks_Find(); // debug
            }
            var newKinks = newCuve._Kinks_Find();
            if (newKinks != null)
            {
                failReason = "fixed curve has {0} kinks"._Format(newKinks.Count);
                //log.wrong("_Curve._TryRemoveKinks: " + failReason); - no need to write worng message here - we have 3 function that tries to fix - if one not succeed, another may - and only if 3 of them fail - we will write wrong message
                return false;
            }

            var zigzags = newCuve._ZigZagDeformationsFind();
            if (zigzags != null)
            {
                failReason = "fixed curve has {0} zigzags"._Format(zigzags.Length);
                //log.wrong("_Curve._TryRemoveKinks: " + failReason);
                return false;
            }


            return true;
        }

    }
}
