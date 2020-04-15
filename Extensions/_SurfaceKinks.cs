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
    public class SurfaceKinkData
    {
        public IsoStatus SrfEnd { get; set; }
        public double AngleDeviation { get; set; }

        public Surface SrfFixed { get; set; }
    }

    public static class _SurfaceKinks
    {
        private const double EXTEND_BY_PERCENT = 0.50; // 50%
        private const int POINTS_COUNT_ON_CURVE = 100;
        private const int MIN_CONTORL_POINTS_COUNT = 3;
        private const bool DEBUG = false;

        public static List<SurfaceKinkData> _Kinks_Find(this Surface srf)
        {
            if (srf.IsClosed(0) || srf.IsClosed(1)) return null;
            if (srf.IsPeriodic(0) || srf.IsPeriodic(1)) return null;
            if (srf.IsCone()) return null;
            if (srf.IsCylinder()) return null;
            if (srf.IsSphere()) return null;
            if (srf.IsTorus()) return null;

            var ss = new SurfaceSeams(srf);
            if (ss.HasSeams) return null;
            var sss = new SurfaceSingulars(srf);
            if (sss.HasSingulars) return null;

            var srfNurb = srf._IncreaseSurfaceDensity().ToNurbsSurface();
            if (srfNurb.Points.CountU < MIN_CONTORL_POINTS_COUNT
                && srfNurb.Points.CountV < MIN_CONTORL_POINTS_COUNT) return null;

            //var srfBound = srf._GetMinMaxUV();
            //var faceBound = face._GetMinMaxUV();

            var uv0 = 0;
            var uv1 = 1;
            if (srfNurb.Points.CountU < MIN_CONTORL_POINTS_COUNT) uv0 = 1; // exclude checking on u if not enought control points
            if (srfNurb.Points.CountV < MIN_CONTORL_POINTS_COUNT) uv1 = 0; // exclude checking on v if not enought control points


            string  failReason = "";
            double deviation = 0;
            List<SurfaceKinkData> res = null;
            for (int uv = uv0; uv <= uv1; uv++)
            {
                var crv = srf._TryGetIsoCurve_Mid(uv, true, out failReason, out deviation);
                if (crv == null)
                {
                    log.wrong("srf._TryGetIsoCurve_Mid failed: " + failReason);
                    continue;
                }
                if (DEBUG)
                {
                    Layers.Debug.AddTangent(crv, "" + uv, Color.Red);
                    RhinoDoc.ActiveDoc.Objects.AddCurve(crv);
                }
                var crvEnds = new[] { CurveEnd.Start, CurveEnd.End };
                var srfEnds = (uv == 0)
                    ? new[] { IsoStatus.West, IsoStatus.East }
                    : new[] { IsoStatus.South, IsoStatus.North };

                for (int istartend = 0; istartend <= 1; istartend++)
                {
                    //
                    // Angle 
                    //
                    var angle = GetAngle(srf, crv, uv, istartend);
                    if (Math.Abs(90 - angle) > 3)
                    {
                        log.wrong("_SurfaceKinks._Kinks_Find:   Math.Abs(90 - angle) > 10");
                        continue;
                    }


                    //
                    // Angle Extended
                    //
                    var crvEnd = crvEnds[istartend];
                    var srfEnd = srfEnds[istartend];
                    var srfExtended = srf._ExtendSurfaceByPercent(srfEnd, 100 * EXTEND_BY_PERCENT);
                    var crvExtended = srfExtended._TryGetIsoCurve_Mid(uv, true, out failReason, out deviation);
                    if (crvExtended == null)
                    {
                        log.wrong("srfExtended._TryGetIsoCurve_Mid failed: " + failReason);
                        continue;
                    }
                    var angleExtended = GetAngle(srfExtended, crvExtended, uv, istartend);
                    if (Math.Abs(90 - angleExtended) > 3)
                    {
                        log.wrong("_SurfaceKinks._Kinks_Find:   Math.Abs(90 - angleExtended) > 10");
                        continue;
                    }

                    //
                    // Angle between 'extended IsoCurve1' to 'IsoCurve2'
                    //
                    var crvIso1Extended = crv.Extend(crvEnd, crv._GetLength_ThreadSafe() * EXTEND_BY_PERCENT, CurveExtensionStyle.Arc);
                    if (crvIso1Extended == null)
                    {
                        log.wrong("crv.Extend failed");
                        continue;
                    }
                    var angleCrvs = Vector3d.VectorAngle(crvExtended._Tangent(crvEnd), crvIso1Extended._Tangent(crvEnd))._RadianToDegree();
                    if (angleCrvs > 10)
                    {
                        var kink = new SurfaceKinkData
                        {
                            AngleDeviation = angleCrvs,
                            SrfEnd = srfEnd,
                        };
                        if (res == null) res = new List<SurfaceKinkData>(2);
                        res.Add(kink);                        
                    }
                    //if (DEBUG)
                    //{
                    //    //RhinoDoc.ActiveDoc.Objects.AddSurface(srfExtended);
                    //    RhinoDoc.ActiveDoc.Objects.AddCurve(crvExtended);
                    //}
                }
            }
            return res;
        }

        private static double GetAngle(Surface srf, Curve crv, int uv, int istartend)
        {
            var crvEnds = new[] { CurveEnd.Start, CurveEnd.End };
            var srfEnds = (uv == 0)
                ? new[] { IsoStatus.West, IsoStatus.East }
                : new[] { IsoStatus.South, IsoStatus.North };

            var crvEnd = crvEnds[istartend];
            var crvTangent = crv._Tangent(crvEnd);
            var crvPoint = crv._P(crvEnd);

            var srfEnd = srfEnds[istartend];
            var domainT0T1 = new[] {srf.Domain(uv).T0, srf.Domain(uv).T1};
            var domainOpositeMid = srf.Domain(uv ^ 1).Mid;
            var u = (uv == 0) ? domainT0T1[istartend] : domainOpositeMid;
            var v = (uv == 1) ? domainT0T1[istartend] : domainOpositeMid;
            var srfNormal = srf.NormalAt(u, v);
            var srfPoint = srf.PointAt(u, v);

            var angle = Vector3d.VectorAngle(crvTangent, srfNormal)._RadianToDegree();

            if (DEBUG && angle > 3)
            {
                var dist = srfPoint._DistanceTo(crvPoint);
                Layers.Debug.AddNormal(srfPoint, srfNormal, Color.Red, srfEnd.ToString());
                log.temp("angle = {0},  dist = {1:0.00000},  srfEnd = {2}, u={3:0.00}, v={4:0.00}", angle._ToStringAngle(), dist, srfEnd, u, v);
            }

            return Math.Abs(angle);
        }


        public static Surface _Kinks_TryRemove(this Surface srf, List<SurfaceKinkData> kinks, out string failReason, out double deviation, double maxAllowedDeviation = 0.001)
        {
            failReason = "";
            deviation = 0;
            //return srf.Duplicate() as Surface;
            failReason = Shared.AUTOFIX_NOT_IMPLEMENTED;
            return null;
        }

        public static Curve _TryGetIsoCurve_Mid(this Surface srf, int uv, bool callInternalMethod, out string failReason, out double deviation, double desiredDeviation = 0.001)
        {
            var domainOposite = srf.Domain(uv ^ 1);
            return srf._TryGetIsoCurve(uv, domainOposite.Mid, callInternalMethod, out failReason, out deviation, desiredDeviation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srf"></param>
        /// <param name="uv"></param>
        /// <param name="uvOppositeT"></param>
        /// <param name="callInternalMethod">use false if you will use curve in GUI - this approach return curve that sometimes not visible in viewport</param>
        /// <param name="failReason"></param>
        /// <param name="deviation"></param>
        /// <param name="desiredDeviation"></param>
        /// <returns></returns>
        public static Curve _TryGetIsoCurve(this Surface srf, int uv, double uvOppositeT, bool callInternalMethod, out string failReason, out double deviation, double desiredDeviation = 0.001)
        {
            deviation = 0;
            failReason = "";

            var domain = srf.Domain(uv);

            double minDistance;
            Curve crv = null;

            // DOESNT WORK PROPERLY: problems in GUI - sometimes curve is not visible. 
            // probably because it copied from some special structure and is not updated proprly by rhino algorithms.
            // this is Rhino bu-g. So lets skipp this method.
            // bad works in F:\Katya_3d\Data\issues\FaceInvalid\FaceHasKinks\q1q1_2.3dm when extending surface - one of the extended isocurves not visible
            // Try 1 
            // try first internal method
            if (callInternalMethod)
            {
                crv = srf.IsoCurve(uv, uvOppositeT);
                if (crv._TryDistanceToSrf(srf, out minDistance, out deviation, 50, desiredDeviation)) // 50 points enought to check if curve is enought close to surface
                {
                    var res = crv.DuplicateCurve();
                    res.EnsurePrivateCopy();
                    return res;
                }
            }



            // Try 2..n
            // if precision if not enought - lets try our method
            var COUNT = POINTS_COUNT_ON_CURVE;
            while (COUNT < 2000)
            {
                var points = new Point3d[COUNT];
                for (int i = 0; i < COUNT; i++)
                {
                    var nextT = domain.Min + domain.Length * (i / ((double)(COUNT - 1)));
                    points[i] = (uv == 0)
                        ? srf.PointAt(nextT, uvOppositeT)
                        : srf.PointAt(uvOppositeT, nextT);
                }

                crv = Curve.CreateControlPointCurve(points, 3);

                if (crv == null)
                {
                    failReason = "failed Curve.CreateControlPointCurve";
                    return null;
                }
                if (crv._TryDistanceToSrf(srf, out minDistance, out deviation, 50, desiredDeviation)) // 50 points enought to check if curve is enought close to surface
                {
                    // if deviation is acceptible - return immidiate. otherwise try increase precision.
                    if (deviation < desiredDeviation)
                    {
                        return crv;
                    }
                }
                COUNT *= 2; // increase curve control points
            }

            //failReason = "deviation is to big: {0:0.000}, max allowed: {1:0.000}"._Format(deviation, desiredDeviation);
            return crv;
        }
    }
}