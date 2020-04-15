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
    public class SurfaceDomains
    {
        public Interval u { get; private set; }
        public Interval v { get; private set; }
        public Interval[] uv;
        public SurfaceDomains(Interval u, Interval v)
        {
            this.u = u;
            this.v = v;
            uv = new[] { u, v };
        }

        public SurfaceDomains(double minU, double maxU, double minV, double maxV)
            : this(new Interval(minU, maxU), new Interval(minV, maxV))
        {
        }

        public void SetUV(Interval value, int iUV)
        {
            uv[iUV] = value;
            if ((iUV) == 0)
                u = value;
            else
                v = value;
        }

        public void SetU(Interval value)
        {
            SetUV(value, 0);
        }

        public void SetV(Interval value)
        {
            SetUV(value, 1);
        }

        public Surface TrimSurface(Surface srf)
        {
            return srf.Trim(u, v);
        }
    }

    //[DebuggerDisplay("({u}, {v})")]
    public class SurfacePoint2d3dSrf : SurfacePoint
    {
        public Point3d LocationCrv;
        public Point3d LocationSrf;
        public double Deviation { get { return LocationCrv._DistanceTo(LocationSrf); } }

        public SurfacePoint2d3dSrf(Point3d locationCrv, Point3d locationSrf, double u, double v)
            : base(u, v)
        {
            this.LocationCrv = locationCrv;
            this.LocationSrf = locationSrf;
        }

        public override string ToString()
        {
            return base.ToString() + ", Deviation = " + Deviation._ToStringX(8);
        }
    }

    //[DebuggerDisplay("({u}, {v})")]
    public class SurfacePoint2d3d : SurfacePoint
    {
        public Point3d Location;

        public SurfacePoint2d3d(Point3d location, double u, double v)
            : base(u, v)
        {
            this.Location = location;
        }
    }

    //[DebuggerDisplay("({u}, {v})")]
    public class SurfacePoint
    {
        public double u { get; private set; }
        public double v { get; private set; }
        // used ony in Surface._FixSurfacePoints
        public double[] uv;

        public SurfacePoint(double u, double v)
        {
            this.u = u;
            this.v = v;
            uv = new[] { u, v };
        }

        public SurfacePoint(Point3d point)
            : this(point.X, point.Y)
        {
        }
        public SurfacePoint(Point2d point)
            : this(point.X, point.Y)
        {
        }
        public override string ToString()
        {
            return u._ToStringX(5) + "," + v._ToStringX(5);
        }

        public static bool operator ==(SurfacePoint point1, SurfacePoint point2)
        {
            return point1.u._IsSame(point2.u)
                && point1.v._IsSame(point2.v);
        }

        public static bool operator !=(SurfacePoint point1, SurfacePoint point2)
        {
            return !point1.u._IsSame(point2.u)
                || !point1.v._IsSame(point2.v);
        }

        public static SurfacePoint operator +(SurfacePoint point1, SurfacePoint point2)
        {
            return new SurfacePoint(point1.u + point2.u, point1.v + point2.v);
        }

        public static SurfacePoint operator /(SurfacePoint point, double divby)
        {
            divby = divby._GetNonZeroForDevisionOperation();
            return new SurfacePoint(point.u / divby, point.v / divby);
        }

        public void SetUV(double value, int iUV)
        {
            uv[iUV] = value;
            if ((iUV) == 0)
                u = value;
            else
                v = value;
        }

        public void SetU(double value)
        {
            SetUV(value, 0);
        }

        public void SetV(double value)
        {
            SetUV(value, 1);
        }

        public Point3d ToPoint3d()
        {
            return new Point3d(u, v, 0);
        }

        public Point2d ToPoint2d()
        {
            return new Point2d(u, v);
        }
    }

    public static class _Surface
    {
        public static Point3d _PointAt(this Surface srf, Point3d point)
        {
            return srf.PointAt(point.X, point.Y);
        }
        public static Point3d _PointAt(this Surface srf, Point2d point)
        {
            return srf.PointAt(point.X, point.Y);
        }

        public static Vector3d _NormalAt(this Surface srf, Point3d p)
        {
            double u;
            double v;
            srf.ClosestPoint(p, out u, out v);
            var normal = srf.NormalAt(u, v);
            return normal;
        }

        public static void _Get2dNormalizedMultipliyers(this Surface srf, out double uM, out double vM)
        {
            uM = 1 / srf._GetDomain3dLength(0);
            vM = 1 / srf._GetDomain3dLength(1);
        }

        public static double _GetDomain3dLength(this Surface srf, int domain, int divby = 0)
        {
            if (srf == null) return 0;

            // detect proper count
            if (divby == 0)
            {
                divby = 3;
                var len3 = srf._GetDomain3dLength(domain, divby);
                divby = Convert.ToInt32(Math.Min(10000, len3 / 0.1));
                if (divby <= 3) return len3;
            }

            var du = srf.Domain(0);
            var dv = srf.Domain(1);
            double u = (domain == 0) ? du.Min : du.Mid;
            var duinc = du.Length / divby;
            double v = (domain == 1) ? dv.Min : dv.Mid;
            var dvinc = dv.Length / divby;

            double len = 0;
            var pointPrev = srf.PointAt(u, v);
            for (int i = 1; i < divby; i++)
            {
                if (domain == 0) u += duinc;
                if (domain == 1) v += dvinc;
                var point = srf.PointAt(u, v);
                len += pointPrev._DistanceTo(point);
                pointPrev = point;
            }
            return len;
        }

        public static Curve _GetUCurve(this Surface srf, double v)
        {
            int POINTS_COUNT_ON_CURVE = 100;
            var domainU = srf.Domain(0);
            var points = new List<Point3d>();
            for (int x = 0; x <= POINTS_COUNT_ON_CURVE; x++)
            {
                var u = domainU.Min + x * (domainU.Length / POINTS_COUNT_ON_CURVE);
                var p = srf.PointAt(u, v);
                points.Add(p);
            }
            var curve = Curve.CreateControlPointCurve(points, 3);
            return curve;
        }
        public static Curve _GetVCurve(this Surface srf, double u)
        {
            int POINTS_COUNT_ON_CURVE = 100;
            var domainV = srf.Domain(1);
            var points = new List<Point3d>();
            for (int y = 0; y <= POINTS_COUNT_ON_CURVE; y++)
            {
                var v = domainV.Min + y * (domainV.Length / POINTS_COUNT_ON_CURVE);
                var p = srf.PointAt(u, v);
                points.Add(p);
            }
            var curve = Curve.CreateControlPointCurve(points, 3);
            return curve;
        }

        /// <summary>
        /// Extend surface by 50% in multiple sides.
        /// After extension doesnt require to recalculate Trims
        /// </summary>
        /// <param name="srf"></param>
        /// <param name="extSides"></param>
        /// <param name="extentByPercent"></param>
        /// <returns></returns>
        public static Surface _ExtendSurface(this Surface srf, List<IsoStatus> extSides, double extentByPercent)
        {
            var res = srf;
            var x = 0;
            var y = 0;
            double extentByPercent_1 = extentByPercent;
            double extentByPercent_2 = (double)100 / ((100 + extentByPercent) / extentByPercent);
            if (extSides != null)
            {
                //DEBUG
                //extSides.Add(IsoStatus.South);

                foreach (var srfSide in extSides)
                {
                    var xi = (srfSide == IsoStatus.West || srfSide == IsoStatus.East) ? 1 : 0;
                    var yi = (srfSide == IsoStatus.South || srfSide == IsoStatus.North) ? 1 : 0;
                    x += xi;
                    y += yi;
                    var extentByPercent_I = extentByPercent_1;
                    if (xi == 1 && x == 2) extentByPercent_I = extentByPercent_2;
                    if (yi == 1 && y == 2) extentByPercent_I = extentByPercent_2;
                    var srfExtI = res._ExtendSurfaceByPercent(srfSide, extentByPercent_I);
                    if (srfExtI != null)
                    {
                        res = srfExtI;
                    }

                    //DEBUG
                    //var srfSS = srf._GetSurfaceSingulars();
                    //var srfExtSS = srfExt._GetSurfaceSingulars();
                }
            }
            return res;
        }

        /// <summary>
        /// Extend surface by some percent in some direction.
        /// to extend by 50% pass parameter 'percent=50'
        /// </summary>
        /// <param name="srf"></param>
        /// <param name="extensionDirection"></param>
        /// <param name="percent">value '50' will be 50%</param>
        /// <returns></returns>
        public static Surface _ExtendSurfaceByPercent(this Surface srf, IsoStatus extensionDirection, double percent)
        {
            return ExtendSurfaceByPercent_Recurs(srf, extensionDirection, percent);
        }
        private static Surface ExtendSurfaceByPercent_Recurs(Surface srf, IsoStatus extensionDirection, double percent, bool recurs = true)
        {
            var srfExt = srf.Extend(extensionDirection, percent, true); // true - same as CurveExtensionStyle.Arc, false - same as CurveExtensionStyle.Smooth
            if (srfExt == null)
            {
                return srf;
            }
            var domainSrf= new Interval();
            var domainSrfExt = new Interval();
            switch (extensionDirection)
            {
                case IsoStatus.West:
                case IsoStatus.East:
                case IsoStatus.X:
                    domainSrf = srf.Domain(0);
                    domainSrfExt = srfExt.Domain(0);
                    break;
                case IsoStatus.North:
                case IsoStatus.South:
                case IsoStatus.Y:
                    domainSrf = srf.Domain(1);
                    domainSrfExt = srfExt.Domain(1);
                    break;
            }

            var increased = ((domainSrfExt.Length - domainSrf.Length) / domainSrf.Length) * 100;
            if (increased > 0.00000001 && recurs)
            {
                return ExtendSurfaceByPercent_Recurs(srf, extensionDirection, (percent * percent) / increased, false);
            }

            return srfExt;
        }

        public static IsoStatus _FindSingular(this Surface srf, Point3d p)
        {
            double u, v;
            if (srf.ClosestPoint(p, out u, out v))
            {
                return srf._FindSingular(u, v);
            }
            return IsoStatus.None;
        }
        public static IsoStatus _FindSingular(this Surface srf, double u, double v)
        {
            var uDomain = srf.Domain(0);
            var vDomain = srf.Domain(1);

            var uSnap = u._RoundToDomainMinMax(uDomain);
            var vSnap = v._RoundToDomainMinMax(vDomain);

            if (!srf.IsAtSingularity(uSnap, vSnap, false))
            {
                return IsoStatus.None;
            }

            var tolU = 0.00001 * uDomain.Length;
            var uT0T1Dist = srf.PointAt(uDomain.T0, vSnap)._DistanceTo(srf.PointAt(uDomain.T1, vSnap));
            if (uT0T1Dist < tolU)
            {
                return Math.Abs(vDomain.T0 - vSnap) < Math.Abs(vDomain.T1 - vSnap)
                    ? IsoStatus.South : IsoStatus.North;
                //return IsoStatus.X;
            }

            var tolV = 0.00001 * vDomain.Length;
            var vT0T1Dist = srf.PointAt(uSnap, vDomain.T0)._DistanceTo(srf.PointAt(uSnap, vDomain.T1));
            if (vT0T1Dist < tolV)
            {
                return Math.Abs(uDomain.T0 - uSnap) < Math.Abs(uDomain.T1 - uSnap)
                    ? IsoStatus.West : IsoStatus.East;
                //return IsoStatus.Y;
            }
            return IsoStatus.None;
        }


        public static string _UVToString(this Surface srf, double u, double v, IsoStatus singularOn = IsoStatus.None)
        {
            var us = u._ToStringX(2);
            var vs = v._ToStringX(2);
            if (singularOn == IsoStatus.X
                    || singularOn == IsoStatus.South
                    || singularOn == IsoStatus.North)
            {
                us = String.Format("{0:0.00}..{1:0.00}", srf.Domain(0).T0, srf.Domain(0).T1);
            }
            if (singularOn == IsoStatus.Y
                     || singularOn == IsoStatus.West
                     || singularOn == IsoStatus.East)
            {
                vs = String.Format("{0:0.00}..{1:0.00}", srf.Domain(1).T0, srf.Domain(1).T1);
            }
            var text = us + " * " + vs;
            text = text.Replace(",00", "").Replace(".00", "");
            return text;
        }

        public static Surface _IncreaseSurfaceDensity(this Surface srf, int increaseTimes = 7)
        {
            var uCount = srf.ToNurbsSurface().Points.CountU * increaseTimes;
            var vCount = srf.ToNurbsSurface().Points.CountV * increaseTimes;
            var newSrf = srf.Rebuild(srf.Degree(0), srf.Degree(1), uCount, vCount);
            if (newSrf != null
                    && newSrf.SetDomain(0, srf.Domain(0))
                    && newSrf.SetDomain(1, srf.Domain(1))
                )
            {
                return newSrf;
            }
            return srf;
        }

        public static SurfaceSingulars _GetSurfaceSingulars(this Surface srf)
        {
            return new SurfaceSingulars(srf);
        }

        /// <summary>
        /// Get 'West,East,North,South' surface side of 3d curve at middle point of curve.
        /// Returns 'None' if crv is not touch any side.
        /// </summary>
        /// <param name="srf"></param>
        /// <param name="crv">3d curve</param>
        /// <returns>surface side (iso status). None if crv is not touch any side.</returns>
        public static IsoStatus _GetCurveIsoStatus(this Surface srf, Curve crv)
        {
            var res = IsoStatus.None;

            var mid = crv._PointAtMid();
            //mid = crv.PointAt(crv.Domain.Max);
            double u, v;
            if (srf.ClosestPoint(mid, out u, out v))
            {
                var s = new SurfaceSingulars(srf);
                if (u._IsSame(s.U.T0)) res = IsoStatus.West;
                else if (u._IsSame(s.U.T1)) res = IsoStatus.East;
                else if (v._IsSame(s.V.T0)) res = IsoStatus.South;
                else if (v._IsSame(s.V.T1)) res = IsoStatus.North;
            }
            return res;
        }


        #region Curves2d

        public static NurbsCurve _Convert3dCurveTo2d(this Surface srf, NurbsCurve crv3d
            , SurfaceSingulars ssingulars = null, Curve trim = null, Curve edge = null, double pullback_tol = 0.001)
        {
            var seam = new SurfaceSeams(srf);
            if (seam.HasSeams)
            {
                log.wrong("Dangerous convertion of 3d curve to 2d in method _Curve._Convert3dCurveTo2d().  Remove seam before use such convertions.");
            }


            // Try internal method first - it is faster, but may be incorrect
            var newCrv2d = srf.Pullback(crv3d, pullback_tol);
            Point3d[] point3d;
            if (newCrv2d != null && newCrv2d._TryDivideByCount(5, out point3d))
            {
                //var sps = newCrv2d.Points._SurfacePoints();
                var sps = point3d.Select(o => new SurfacePoint(o)).ToList();
                var isSPSFixed = srf._FixSurfacePoints(ref sps, false, null, null, crv3d);
                if (!isSPSFixed) // if there is no need in fixing points - we have good 2d curve - lets return result
                {
                    return newCrv2d._ToNurbsCurve();
                }
            }


            // Lets use our method - slow but very precise
            // Rebuild 3d curve for better precision pulling to 2d surface.
            var divby = crv3d._GetDivBy(null, 0.01, 100);
            NurbsCurve Crv3dRebuilded = crv3d.Rebuild(divby, 3, false) ?? crv3d;
            Crv3dRebuilded = Crv3dRebuilded._SetDimension(3);
            newCrv2d = srf._Convert3dCurveTo2d_WithoutRebuildAndSimplify(Crv3dRebuilded, ssingulars, trim, edge);
            var res = newCrv2d._Simplify(srf);
            return res;
        }

        public static NurbsCurve _Convert3dCurveTo2d_WithoutRebuildAndSimplify(this Surface srf, NurbsCurve crv3d
            , SurfaceSingulars ssingulars = null, Curve trim = null, Curve edge = null)
        {
            if (ssingulars == null)
            {
                ssingulars = new SurfaceSingulars(srf);
            }

            if (edge == null)
            {
                edge = crv3d;
            }

            //// version 1 - bad since it uses method Curve.CreateControlPointCurve) and is a little bit incorrect
            //crv3d.DivideByCount(DIVIDE_BY_COUNT_I * 5, true, out edgeDevidedPoints);
            //crv3d.DivideByCount(DIVIDE_BY_COUNT_I, true, out edgeDevidedPoints);
            //var edgeDevidedPointsProjectedUV = new List<Point3d>();
            //foreach (var point in edgeDevidedPoints)
            //{
            //    debugIndexTrim++;
            //    if (debugIndexTrim < debugIndexMAX) AddDebugPoint(Doc, debugIndexTrim.ToString(), point, Color.Red);//debug
            //    double u, v;
            //    if (srf.ClosestPoint(point, out u, out v))
            //    {
            //        if (debugIndexTrim < debugIndexMAX)
            //        {
            //            //string text = debugIndexTrim + " - " + Convert.ToInt32(Math.Round(u)) + " - " + Convert.ToInt32(Math.Round(v));                            
            //            //AddDebugPoint(Doc, text, srf.PointAt(u, v), Color.Purple);//debug
            //            //AddDebugPoint(Doc, debugIndexTrim.ToString(), srf.PointAt(u, v), Color.Purple);//debug
            //            //Logger.log(text);
            //        }
            //        var point2d = new Point3d(u, v, 0);
            //        edgeDevidedPointsProjectedUV.Add(point2d);
            //    }
            //    else
            //    {
            //        int i = 0;
            //    }
            //}

            //var crv2d = Curve.CreateControlPointCurve(edgeDevidedPointsProjectedUV, 2);
            ////crv2d = crv2d.Rebuild(10, crv3dUV.Degree, false);

            //// version 2
            //var crv2d = srf.InterpolatedCurveOnSurfaceUV(edgeDevidedPointsProjectedUV, 0.0000001);

            //// version 3 -  bad since control points can be not enough to keep good precision
            var crv2d = new NurbsCurve(2, crv3d.IsRational, crv3d.Degree + 1, crv3d.Points.Count);
            var crv2dPoints = new List<SurfacePoint>(crv3d.Points.Count);
            var crv2dWeights = new List<double>(crv3d.Points.Count);
            //var points3d = crv3d.Points.Select(o => o.Location).ToArray();

            //log.temp("crv3d.Points.Count = " + crv3d.Points.Count);
            foreach (var point3d in crv3d.Points)
            {
                double u, v;
                if (srf.ClosestPoint(point3d.Location, out u, out v))
                {
                    //AddDebugPoint(Doc, "", srf.PointAt(u, v), Color.Red); //debug
                    crv2dPoints.Add(new SurfacePoint(u, v));
                    crv2dWeights.Add(point3d.Weight);
                }
            }

            var fixedSrf = srf._FixSurfacePoints(ref crv2dPoints, false, ssingulars, trim, edge);
            var index = 0;

            foreach (var p in crv2dPoints)
            {
                crv2d.Points.SetPoint(index, p.u, p.v, 0, crv2dWeights[index]);
                index++;
            }

            index = 0;
            foreach (var knot in crv3d.Knots)
            {
                crv2d.Knots[index] = knot;
                index++;
            }

            //// version 4
            //var crv2d = new NurbsCurve(2, edgeDevidedPointsProjectedUV.Count);
            //var index = 0;
            //foreach (var srfPoint in edgeDevidedPointsProjectedUV)
            //{
            //    crv2d.Points.SetPoint(index, srfPoint.X, srfPoint.Y, 0, 1);
            //    index++;
            //}
            //index = 0;
            //foreach (var knot in crv3d.Knots)
            //{
            //    crv2d.Knots[index] = knot;
            //    index++;
            //}


            return crv2d;
        }

        public static NurbsCurve _Convert2dCurveTo3d(this Surface srf, NurbsCurve crv2d)
        {
            var crv3d = new NurbsCurve(3, crv2d.IsRational, crv2d.Degree + 1, crv2d.Points.Count);
            var index = 0;
            foreach (var point2d in crv2d.Points)
            {
                var p = srf.PointAt(point2d.Location.X, point2d.Location.Y);
                crv3d.Points.SetPoint(index, p.X, p.Y, p.Z, point2d.Weight);
                //AddDebugPoint(Doc, "", p, Color.Red); //debug
                index++;
            }

            index = 0;
            foreach (var knot in crv2d.Knots)
            {
                crv3d.Knots[index] = knot;
                index++;
            }

            return crv3d;
        }

        /// <summary>
        /// Snap End and Start points for each curve in list 'curves2d' (new point is avarage of both)
        /// If found singularity - 
        /// 1) add index of curve2d after wich need to add singular trim in list 'singulars'
        /// 2) snap Start and End points to the corner of surface where Singularity exists
        /// </summary>
        /// <param name="srf"></param>
        /// <param name="curves2d">list of curves in UV projection for sruface 'srf'</param>
        /// <param name="singulars">Will contain index of curve2d after which need to add singularity</param>
        public static void _SnapCurves2d(this Surface srf, List<NurbsCurve> curves2d, out List<int> singulars)
        {
            var domainU = srf.Domain(0);
            var domainV = srf.Domain(1);

            singulars = new List<int>();

            for (int i = 0; i < curves2d.Count; i++)
            {
                int iEnd = i - 1;
                int iStart = i;
                if (iStart == 0)
                {
                    iEnd = curves2d.Count - 1;
                }
                var pointStart = new SurfacePoint(curves2d[iStart].PointAtStart);
                var pointEnd = new SurfacePoint(curves2d[iEnd].PointAtEnd);

                var diffU = Math.Abs(pointStart.u - pointEnd.u);
                var diffV = Math.Abs(pointStart.v - pointEnd.v);
                if (diffU < 0.01 * domainU.Length && diffV < 0.01 * domainV.Length)
                {
                    var avarageU = (pointStart.u + pointEnd.u) / 2;
                    var avarageV = (pointStart.v + pointEnd.v) / 2;
                    var avaragePoint = new Point3d(avarageU, avarageV, 0);
                    curves2d[iStart].SetStartPoint(avaragePoint);
                    curves2d[iEnd].SetEndPoint(avaragePoint);
                    curves2d[iStart].Points.SetPoint(0, avaragePoint);
                    curves2d[iEnd].Points.SetPoint(curves2d[iEnd].Points.Count - 1, avaragePoint);
                }
                else
                {
                    var pointStart3d = srf.PointAt(pointStart.u, pointStart.v);
                    var pointEnd3d = srf.PointAt(pointEnd.u, pointEnd.v);
                    var distance3d = pointEnd3d._DistanceTo(pointStart3d);
                    var isExtremSingularity = distance3d < _Double.ZERO;
                    var isNormalSingularity = (distance3d < 0.001)
                                              && (diffU > domainU.Length / 7 || diffV > domainV.Length / 7);
                    if (isExtremSingularity || isNormalSingularity) // singularity present
                    {
                        // round start and end points to mutch surface corner in singularity
                        var pointAtStart = curves2d[iStart].PointAtStart._RoundToDomain(domainU, domainV);
                        curves2d[iStart].SetStartPoint(pointAtStart);
                        var pointAtEnd = curves2d[iEnd].PointAtEnd._RoundToDomain(domainU, domainV);
                        curves2d[iEnd].SetEndPoint(pointAtEnd);
                        singulars.Add(iEnd);
                    }
                    else
                    {
                        throw new Exception("Can't snap Curves2d at index " + i);
                    }
                }
            }

            singulars.Sort();
        }

        public static List<SurfacePoint> _ClosestPoints(this Surface srf, IEnumerable<Point3d> points3d)
        {
            var UVs = new List<SurfacePoint>(100);
            foreach (var point in points3d)
            {
                double u, v;
                if (srf.ClosestPoint(point, out u, out v))
                {
                    UVs.Add(new SurfacePoint(u, v));
                }
            }
            return UVs;
        }

        public static Point3d _ClosestPoints(this Surface srf, Point3d point3d)
        {
            double u; double v;
            if (srf.ClosestPoint(point3d, out u, out v))
            {
                return srf.PointAt(u, v);
            }
            return point3d;
        }

        public static bool _ClosestPoints(this Surface srf, Point3d point3d, out Point3d pointsOnSurface3d)
        {
            double u; double v;
            if (srf.ClosestPoint(point3d, out u, out v))
            {
                pointsOnSurface3d = srf.PointAt(u, v);
                return true;
            }
            pointsOnSurface3d = Point3d.Origin;
            return false;
        }

        public static double _DistanceTo(this Surface srf, Point3d point3d, double defValue)
        {
            Point3d pointsOnSurface3d;
            if (!srf._ClosestPoints(point3d, out pointsOnSurface3d))
            {
                return defValue;
            }
            return pointsOnSurface3d._DistanceTo(point3d);
        }

        public static bool _DistanceTo(this Surface srf, Point3d point3d, out double distance)
        {
            Point3d pointsOnSurface3d;
            if (!srf._ClosestPoints(point3d, out pointsOnSurface3d))
            {
                distance = 0;
                return false;
            }
            distance = pointsOnSurface3d._DistanceTo(point3d);
            return true;
        }

        #endregion




        /// <summary>
        /// Get centroid of surface base on avarage 3d points devided by paramter 'divby'.
        /// If 'divby' is zero - 'srf.PointAt(uDomain.Mid, vDomain.Mid)' will be returned.
        /// </summary>
        /// <param name="srf"></param>
        /// <param name="divby"></param>
        /// <returns></returns>
        public static Point3d _GetCentroid(this Surface srf, int divby)
        {
            if (divby == 0)
            {
                return srf._GetCentroid();
            }
            var points3d = srf._GetDivby3dPoints(divby);

            var sumPoint = Point3d.Origin;
            for (int i = 0; i < points3d.Length; i++)
            {
                sumPoint += points3d[i];
            }

            var c =  sumPoint / points3d.Length;

            double uclosest, vclosest;
            if (srf.ClosestPoint(c, out uclosest, out vclosest))
            {
                c = srf.PointAt(uclosest, vclosest);
            }
            return c;
        }

        public static Point3d _GetCentroid(this Surface srf)
        {
            return srf.PointAt(srf.Domain(0).Mid, srf.Domain(1).Mid);
        }


        /// <summary>
        /// Get centroid of surface base on avarage 3d points devided by paramter 'divby'.
        /// If 'divby' is zero - 'srf.PointAt(uDomain.Mid, vDomain.Mid)' will be returned.
        /// </summary>
        /// <param name="srf"></param>
        /// <param name="loopEdges">all edges of loop</param>
        /// <param name="accurate">Accurate takes more time but have great precision.</param>
        /// <returns></returns>
        public static Point3d _GetCentroid(this Surface srf, Curve[] loopEdges, bool accurate)
        {
            // Get edges points
            var MAX_EDGE_POINTS = accurate ? 1000 : 500;
            var edgesPoints = new List<Point3d>();
            var prevPointAtStart = Point3d.Origin;
            var prevPointAtEnd = Point3d.Origin;
            var edgesLengths = loopEdges.Select(o => o._GetLength_ThreadSafe()).ToList();
            var divbyTol = accurate ? 0.01 : 0.1;
            var desiredEdgePointsCount = edgesLengths.Sum() / divbyTol;
            if (desiredEdgePointsCount > MAX_EDGE_POINTS)
            {
                divbyTol = edgesLengths.Sum() / MAX_EDGE_POINTS; // limit edge point at approximatelly 1000 points
            }
            for (var i = 0; i < loopEdges.Length; i++)
            {
                var edge = loopEdges[i];
                var divby = edge._GetDivBy(edgesLengths[i], divbyTol, 0);
                Point3d[] points;
                if (edge._TryDivideByCount(divby, out points) && points.Length >= 2)
                {
                    var deleteDuplicatePointIndex = -1;// index of point3d that id duplicated for 2 edges (some connection point)
                    if (edgesPoints.Count != 0)
                    {
                        var distToStart = Math.Min(points.First()._DistanceTo(prevPointAtStart), points.First()._DistanceTo(prevPointAtEnd));
                        var distToEnd = Math.Min(points.Last()._DistanceTo(prevPointAtStart), points.Last()._DistanceTo(prevPointAtEnd));
                        if (distToStart < distToEnd && distToStart < 0.001)
                        {
                            deleteDuplicatePointIndex = edgesPoints.Count;
                        }
                        else if (distToEnd < distToStart && distToEnd < 0.001)
                        {
                            deleteDuplicatePointIndex = edgesPoints.Count + points.Length - 1;
                        }
                    }
                    edgesPoints.AddRange(points);
                    // remove duplicate point if such exists - this will increate accuracy
                    if (deleteDuplicatePointIndex != -1)
                    {
                        edgesPoints.RemoveAt(deleteDuplicatePointIndex);
                    }
                    prevPointAtStart = points.First(); // remember PointAtStart of last edge
                    prevPointAtEnd = points.Last();// remember PointAtStart of last edge
                    //Layers.Debug.AddPoints(points, Color.Black);
                }
            }

            // Limit edge points to 1000 - speed improvement (we can have here 26000 points what take 3 seconds of time - to much)
            int skipp = edgesPoints.Count / MAX_EDGE_POINTS;
            if (skipp > 1)
            {
                var edgesPoints_limited  = new List<Point3d>(MAX_EDGE_POINTS * 2);
                var  i = 0;
                while (i < edgesPoints.Count)
                {
                    edgesPoints_limited.Add(edgesPoints[i]);
                    i += skipp;
                }
                edgesPoints = edgesPoints_limited;
            }

            // detect interval where loop is inside - this will make our calculation more accurate for small loops of big surfaces
            //var edgesPointsOnSrf = srf._ClosestPoints(edgesPoints);
            //var uDomain = new Interval(edgesPointsOnSrf.Select(o => o.u).Min(), edgesPointsOnSrf.Select(o => o.u).Max());
            //var vDomain = new Interval(edgesPointsOnSrf.Select(o => o.v).Min(), edgesPointsOnSrf.Select(o => o.v).Max());

            var uDomain = srf.Domain(0);
            var vDomain = srf.Domain(1);



            var iterationNum = 0;
            var iterationCentroid = Point3d.Origin;
            var domain3dLength = Double.MaxValue;
            var iterationsMax = accurate ? 50 : 10;
            var iterationDomain3dLengthEnought = accurate ? 0.0001 : 0.001;

            while (iterationNum < iterationsMax
                && domain3dLength > iterationDomain3dLengthEnought)
            {
                // Get srf points
                var srfPoints2d = srf._GetDivby2dPoints(4, uDomain, vDomain);
                if (srfPoints2d.Length == 0) break;
                var srfPoints3d = srfPoints2d.Select(o => srf.PointAt(o.u, o.v)).ToArray();
                var bestDist_Pow2 = Double.MaxValue;
                var best2dPoint = new SurfacePoint(0, 0);
                var best3dPoint = Point3d.Origin;
                var bestIndex = -1;
                domain3dLength = Double.MaxValue;
                for (var  i = 0; i < srfPoints2d.Length; i++)
                {
                    var p2d = srfPoints2d[i];
                    var p3d = srfPoints3d[i];
                    //var summ3dDists_Pow2 = edgesPoints.Select(ep => ep._DistanceTo_Pow2(p3d)).Sum();
                    double summ3dDists_Pow2 = 0;
                    for (int k = 0; k < edgesPoints.Count; k++)
                    {
                        summ3dDists_Pow2 += p3d._DistanceTo_Pow2(edgesPoints[k]);
                    }
                    if (summ3dDists_Pow2 < bestDist_Pow2)
                    {
                        bestDist_Pow2 = summ3dDists_Pow2;
                        best2dPoint = p2d;
                        best3dPoint = p3d;
                        bestIndex = i;
                    }
                    //Layers.Debug.AddPoint(p3d, Color.Wheat);
                }
                //Layers.Debug.AddPoint(best3dPoint, Color.Red);
                iterationNum++;
                iterationCentroid = best3dPoint;
                var domainSrfMidPoint = srf.PointAt(uDomain.Mid, vDomain.Mid);
                domain3dLength = Math.Min(domain3dLength, domainSrfMidPoint._DistanceTo(srfPoints3d.First()));
                domain3dLength = Math.Min(domain3dLength, domainSrfMidPoint._DistanceTo(srf.PointAt(uDomain.T0, vDomain.T1)));
                domain3dLength = Math.Min(domain3dLength, domainSrfMidPoint._DistanceTo(srf.PointAt(uDomain.T1, vDomain.T0)));
                domain3dLength = Math.Min(domain3dLength, domainSrfMidPoint._DistanceTo(srfPoints3d.Last()));
                uDomain = new Interval(best2dPoint.u - uDomain.Length / 4, best2dPoint.u + uDomain.Length / 4);
                vDomain = new Interval(best2dPoint.v - vDomain.Length / 4, best2dPoint.v + vDomain.Length / 4);
            }

            //Layers.Debug.AddTextPoint("_BrepLoop._GetCentroid", iterationCentroid, Color.Red);
            return iterationCentroid;
        }


        public static SurfacePoint[] _GetDivby2dPoints(this Surface srf, int divby, Interval? uDomain = null, Interval? vDomain = null)
        {
            if (!uDomain.HasValue) uDomain = srf.Domain(0);
            if (!vDomain.HasValue) vDomain = srf.Domain(1);

            var udom = uDomain.Value;
            var vdom = vDomain.Value;

            if (divby == 0)
            {
                return new[] { new SurfacePoint(udom.Mid, vdom.Mid) };
            }

            var res = new List<SurfacePoint>();
            for (int iu = 0; iu <= divby; iu++)
            {
                for (int iv = 0; iv <= divby; iv++)
                {
                    var u = udom.T0 + udom.Length * iu / divby;
                    var v = vdom.T0 + vdom.Length * iv / divby;
                    res.Add(new SurfacePoint(u, v));
                }
            }
            return res.ToArray();
        }

        public static Point3d[] _GetDivby3dPoints(this Surface srf, int divby, Interval? uDomain = null, Interval? vDomain = null)
        {
            return srf._GetDivby2dPoints(divby, uDomain, vDomain).Select(o => srf.PointAt(o.u, o.v)).ToArray();
        }

        public static Curve[] _GetEdgesWithDirectionsSameAsTrims(this Surface srf, BrepLoopType loopType, Curve[] loopEdges)
        {
            // duplicate edges coz we want to edit curves (reverse some of them)
            var crvs = loopEdges.Select(o => o.DuplicateCurve()).ToArray();

            // connect them together
            for (var i = 1; i < crvs.Length; i++)
            {
                var iPrev = i - 1;
                var crv = crvs[i];
                var crvPrev = crvs[iPrev];

                CurvesConnectionInfo connectionInfo;
                crv._AreConnected(crvPrev, out connectionInfo, 10000);
                if (connectionInfo.Crv1End == connectionInfo.Crv2End)
                {
                    crv.Reverse();
                }
            }

            // Join them into 1 curve
            var jC = Curve.JoinCurves(crvs, 100);// we must tell tollerance to be independed from document tolerance

            // enshure orientation of curves are same as should be for trims: clockwise for Outer loops, and CounterClockwise for Inner loops
            if (jC.Length == 1)
            {
                //Layers.Debug.AddCurve(jC[0], Color.Red, ObjectDecoration.EndArrowhead);
                var loopCentroid = srf._GetCentroid(loopEdges, true);
                var normal = srf._NormalAt(loopCentroid);
                //Layers.Debug.AddNormal(srf.PointAt(u, v), normal, Color.Red, "_GetEdgesWithDirectionsSameAsTrims - loop centroid normal");
                var orientation = jC[0].ClosedCurveOrientation(normal);
                var needOrientation = (loopType == BrepLoopType.Outer)
                    ? CurveOrientation.Clockwise
                    : CurveOrientation.CounterClockwise;
                if (orientation != needOrientation)
                {
                    foreach (var crv in crvs) crv.Reverse();
                }
            }
            else
            {
                log.wrong("_BrepLoop._GetEdgesWithDirectionsSameAsTrims  failed to join {0} curves", loopEdges.Length);
            }

            return crvs;
        }

        public static SurfaceDomains _GetMinMaxUV(this Surface srf)
        {
            var domainU = srf.Domain(0);
            var domainV = srf.Domain(1);
            var bound = new SurfaceDomains(domainU.Min, domainU.Max, domainV.Min, domainV.Max);
            return bound;
        }


        public static SurfaceDomains _GetMinMaxUV(this Surface srf, Curve[] crvs2d, int divbyTrims)
        {
            var minU = srf.Domain(0).Max; // here we have to use max possible value to get properly works Math.Min()
            var maxU = srf.Domain(0).Min;// here we have to use min possible value to get properly works Math.Max()
            var minV = srf.Domain(1).Max; // here we have to use max possible value to get properly works Math.Min()
            var maxV = srf.Domain(1).Min;// here we have to use min possible value to get properly works Math.Max()

            divbyTrims = Math.Max(2, divbyTrims);
            // here we will store points for trim - for speed optimization
            Point3d[] devidedPoints;

            foreach (var crv in crvs2d)
            {
                if (divbyTrims == 2
                    || crv.Degree == 1) // for lines - we dont have to devide 
                {
                    devidedPoints = new[] { crv.PointAtStart, crv.PointAtEnd };
                }
                else
                {
                    crv._DivideByCount_ThreadSafe(divbyTrims - 1, true, out devidedPoints);
                }

                if (devidedPoints == null)
                {
                    continue;
                }

                minU = Math.Min(minU, devidedPoints.Min(o => o.X));
                maxU = Math.Max(maxU, devidedPoints.Max(o => o.X));
                minV = Math.Min(minV, devidedPoints.Min(o => o.Y));
                maxV = Math.Max(maxV, devidedPoints.Max(o => o.Y));
            }
            var bound = new SurfaceDomains(minU, maxU, minV, maxV);
            return bound;
        }

        public static bool _Trim(this Surface srf, Curve[] crvs3d, out Brep newBrep, out string fixFailReason)
        {
            newBrep = null;
            fixFailReason = "";

            var joins = Curve.JoinCurves(crvs3d, 0.1, true);
            if (joins.Length != 1)
            {
                fixFailReason = "Failed to join {0} edges into loop"._Format(crvs3d.Length);
                return false;
            }

            var srfBrep = srf.ToBrep();
            if (srfBrep.Faces.Count != 1)
            {
                fixFailReason = "Failed to convert surface to brep";
                return false;
            }

            var b = srf._Split_ThreadSafe(crvs3d, 0.1);
            if (b == null || b.Faces.Count == 0)
            {
                fixFailReason = "Failed to split surface using 3D curves";
                return false;
            }

            BrepFace bestFace = b.Faces[0];
            double bestMinDist = Double.MaxValue;
            
            if (b.Faces.Count > 1)
            {
                var centroid = srf._GetCentroid(crvs3d, false);
                foreach (var f in b.Faces_ThreadSafe())
                {
                    if (f.Loops.Count == 1 && f.OuterLoop != null)
                    {
                        var centroidNew = f.OuterLoop._GetCentroid(false);
                        var dist = centroid._DistanceTo(centroidNew);
                        if (dist < bestMinDist)
                        {
                            bestMinDist = dist;
                            bestFace = f;
                        }
                    }
                }
            }
            if (bestFace == null)
            {
                fixFailReason = "Failed to extract splited face after splitting 3D curves";
                return false;
            }

            newBrep = bestFace.DuplicateFace(true);
            return true;
        }
    }
}
