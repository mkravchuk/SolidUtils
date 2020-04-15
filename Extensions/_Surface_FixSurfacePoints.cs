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
    public class SurfaceStrongPoints
    {
        public SurfaceSeams Seam { get; private set; }
        public SurfaceSingulars Sing { get; private set; }
        public Surface Srf { get; private set; }

        public SurfaceStrongPoints(Surface srf)
        {
            Srf = srf;
            Seam = new SurfaceSeams(srf);
            Sing = new SurfaceSingulars(srf);
        }

        public bool IsStrongPoint(double u, double v)
        {
            var isAtSeam = Seam.HasSeams && (Seam.IsAtSeam(u, v) != 0);
            var isAtSing = Sing.HasSingulars && (Sing.U.IsCloseToSingular_HighJump(u) || Sing.V.IsCloseToSingular_HighJump(v));
            return !isAtSeam && !isAtSing;
        }
    }

    public class SurfaceSeams
    {
        public Surface Srf { get; private set; }
        public bool HasSeams { get; private set; }

        public SurfaceSeamsUV U { get; private set; }
        public SurfaceSeamsUV V { get; private set; }
        public SurfaceSeamsUV[] UV { get; private set; }

        public SurfaceSeams(Surface srf, double tolPercent = 1)
        {
            Srf = srf;

            U = new SurfaceSeamsUV(this, 0, tolPercent);
            V = new SurfaceSeamsUV(this, 1, tolPercent);
            UV = new[] { U, V };

            HasSeams = U.HasSeams || V.HasSeams;
        }

        public class SurfaceSeamsUV
        {
            public SurfaceSeams SurfaceSeams { get; private set; }
            public Interval Domain { get; private set; }
            public double T0;
            public double T1;
            public bool IsAtT0 { get; set; }
            public bool IsAtT1 { get; set; }
            public bool HasSeams { get; set; }
            public double Tolerance_IsAtSeam { get; private set; }


            public SurfaceSeamsUV(SurfaceSeams ss, int domainNum, double tolPercent)
            {
                SurfaceSeams = ss;
                Domain = ss.Srf.Domain(domainNum);
                var DomainOposite = ss.Srf.Domain(domainNum ^ 1);
                T0 = Domain.T0;
                T1 = Domain.T1;
                if (domainNum == 0)
                {
                    IsAtT0 = ss.Srf.IsAtSeam(T0, DomainOposite.Mid) > 0;
                    IsAtT1 = ss.Srf.IsAtSeam(T1, DomainOposite.Mid) > 0;
                }
                else
                {
                    IsAtT0 = ss.Srf.IsAtSeam(DomainOposite.Mid, T0) > 0;
                    IsAtT1 = ss.Srf.IsAtSeam(DomainOposite.Mid, T1) > 0;
                }

                HasSeams = IsAtT0 || IsAtT1;

                Tolerance_IsAtSeam = Domain.Length / 1000; //0.1% (like 0.001)
            }

            public bool FixSurfacePoint(ref double t, double anchor, bool allowOutOfDomainBorders, double tolInPercent = 0.2)
            {
                if (Math.Abs(anchor - T0) < Math.Abs(anchor - T1))
                {
                    if (!IsAtT0) return false;
                    if (Math.Abs(T1 - t) < Domain.Length * tolInPercent)
                    {
                        if (allowOutOfDomainBorders)
                        {
                            t = T0 - (T1 - t);
                        }
                        else
                        {
                            t = T0;
                        }
                        return true;
                    }
                }
                else
                {
                    if (!IsAtT1) return false;
                    if (Math.Abs(t - T0) < Domain.Length * tolInPercent)
                    {
                        if (allowOutOfDomainBorders)
                        {
                            t = T1 + (t - T0);
                        }
                        else
                        {
                            t = T1;
                        }
                        return true;
                    }
                }
                return false;
            }


            public bool isCloseToSingT0(double value, double tol)
            {
                if (IsAtT0 && Math.Abs(value - T0) < tol) return true;
                return false;
            }

            public bool isCloseToSingT1(double value, double tol)
            {
                if (IsAtT1 && Math.Abs(T1 - value) < tol) return true;
                return false;
            }

            public bool IsAtSeam(double value)
            {
                if (!HasSeams) return false;
                return isCloseToSingT0(value, Tolerance_IsAtSeam)
                    || isCloseToSingT1(value, Tolerance_IsAtSeam);
            }
        }

        public bool FixSurfacePoint(ref double u, ref double v, Point2d anchor, bool allowOutOfDomainBorders, double tolInPercent = 0.2)
        {
            var res = U.FixSurfacePoint(ref u, anchor.X, allowOutOfDomainBorders, tolInPercent)
                || V.FixSurfacePoint(ref v, anchor.Y, allowOutOfDomainBorders, tolInPercent);
            return res;
        }

        public bool FixSurfacePoint(ref Point2d point, Point2d anchor, bool allowOutOfDomainBorders, double tolInPercent = 0.2)
        {
            double u = point.X;
            double v = point.Y;
            var res = FixSurfacePoint(ref u, ref v, anchor, allowOutOfDomainBorders, tolInPercent);
            if (res)
            {
                point = new Point2d(u, v);
            }
            return res;
        }

        // return 1 for U, 2 for V, and 3 if U and V at same time
        public int IsAtSeam(double u, double v)
        {
            var ubit = U.HasSeams && U.IsAtSeam(u) ? 1 : 0;
            var vbit = V.HasSeams && V.IsAtSeam(v) ? 2 : 0;
            return ubit | vbit;
        }
    }

    public class SurfaceSingulars
    {
        public Surface Srf { get; private set; }
        public bool HasSingulars { get; private set; }

        public bool West { get; private set; }  //u=0
        public bool East { get; private set; }   //u=1
        public bool South { get; private set; } //v=0
        public bool North { get; private set; } //v=1 

        public double WestValue { get; private set; }  //u=0
        public double EastValue { get; private set; }   //u=1
        public double SouthValue { get; private set; } //v=0
        public double NorthValue { get; private set; } //v=1 

        public SurfaceSingularsUV U { get; private set; }
        public SurfaceSingularsUV V { get; private set; }
        public SurfaceSingularsUV[] UV { get; private set; }

        public SurfaceSingulars(Surface srf, double tolPercent = 1)
        {
            Srf = srf;

            West = srf.IsSingular(3);
            East = srf.IsSingular(1);
            South = srf.IsSingular(0);
            North = srf.IsSingular(2);

            HasSingulars = South || East || North || West;

            U = new SurfaceSingularsUV(this, 0, West, East, tolPercent);
            V = new SurfaceSingularsUV(this, 1, South, North, tolPercent);
            UV = new[] { U, V };

            WestValue = U.Domain.T0;
            EastValue = U.Domain.T1;
            SouthValue = V.Domain.T0;
            NorthValue = V.Domain.T1;
        }

        public class SurfaceSingularsUV
        {
            public SurfaceSingulars SurfaceSingulars { get; set; }
            public Interval Domain { get; private set; }
            public double T0;
            public double T1;
            public bool IsSingAtT0 { get; set; }
            public bool IsSingAtT1 { get; set; }
            public bool HasSingulars { get; private set; }
            public double Tolerance_IsCloseToSingular { get; private set; }
            public double Tolerance_IsAlmostSingularity { get; private set; }
            public double Tolerance_IsCloseToSingular_HighJump { get; private set; }
            public double Tolerance_HighJump { get; private set; }

            public SurfaceSingularsUV(SurfaceSingulars surfaceSingulars, int domainNum, bool singT0, bool singT1, double tolPercent)
            {
                SurfaceSingulars = surfaceSingulars;
                Domain = surfaceSingulars.Srf.Domain(domainNum);
                T0 = Domain.T0;
                T1 = Domain.T1;
                IsSingAtT0 = singT0;
                IsSingAtT1 = singT1;
                HasSingulars = IsSingAtT0 || IsSingAtT1;
                Tolerance_IsCloseToSingular = Domain.Length * tolPercent / 100; // 1%
                Tolerance_IsAlmostSingularity = Domain.Length * tolPercent / 1000; // 0.1%.  we hade before 1% and this is to big value
                Tolerance_IsCloseToSingular_HighJump = Domain.Length / 15;//6%
                Tolerance_HighJump = Domain.Length / 5; // 20%
            }

            public bool IsCloseToSingular(double value)
            {
                return isCloseToSingT0(value, Tolerance_IsCloseToSingular) || isCloseToSingT1(value, Tolerance_IsCloseToSingular);
            }

            public bool IsAlmostSingularity(double value)
            {
                return isCloseToSingT0(value, Tolerance_IsAlmostSingularity) || isCloseToSingT1(value, Tolerance_IsAlmostSingularity);
            }

            public bool IsCloseToSingular_HighJump(double value)
            {
                return isCloseToSingT0(value, Tolerance_IsCloseToSingular_HighJump)
                    || isCloseToSingT1(value, Tolerance_IsCloseToSingular_HighJump);
            }

            public bool IsCloseToSingular_HighJumpDouble(double value)
            {
                return isCloseToSingT0(value, Tolerance_IsCloseToSingular_HighJump * 2)
                    || isCloseToSingT1(value, Tolerance_IsCloseToSingular_HighJump * 2);
            }

            public bool IsCloseToSingular_HighJumpQuad(double value)
            {
                return isCloseToSingT0(value, Tolerance_IsCloseToSingular_HighJump * 4)
                    || isCloseToSingT1(value, Tolerance_IsCloseToSingular_HighJump * 4);
            }

            public bool isCloseToSingT0(double value, double tol)
            {
                if (IsSingAtT0 && Math.Abs(value - T0) < tol) return true;
                return false;
            }

            public bool isCloseToSingT1(double value, double tol)
            {
                if (IsSingAtT1 && Math.Abs(T1 - value) < tol) return true;
                return false;
            }

            public double RoundToSingularValue(double value, double tol)
            {
                if (isCloseToSingT0(value, tol))
                {
                    return T0;
                }
                if (isCloseToSingT1(value, tol))
                {
                    return T1;
                }
                return value;
            }
        }

        public List<SurfacePoint> GetSingularPoints()
        {
            var res = new List<SurfacePoint>();

            if (!HasSingulars)
            {
                return res;
            }

            if (U.IsSingAtT0)
            {
                res.Add(new SurfacePoint(U.T0, V.Domain.Mid));
            }
            if (U.IsSingAtT1)
            {
                res.Add(new SurfacePoint(U.T1, V.Domain.Mid));
            }
            if (V.IsSingAtT0)
            {
                res.Add(new SurfacePoint(U.Domain.Mid, V.T0));
            }
            if (V.IsSingAtT1)
            {
                res.Add(new SurfacePoint(U.Domain.Mid, V.T1));
            }
            // DEBUG
            //foreach (var sp in res) Layers.Debug.AddPoint(Srf.PointAt(sp.u, sp.v));

            return res;
        }

        public IsoStatus GetIsoStatus(double u, double v)
        {
            if (!HasSingulars) return IsoStatus.None;

            var utol = U.Tolerance_IsCloseToSingular;
            if (U.isCloseToSingT0(u, utol)) return IsoStatus.West;
            if (U.isCloseToSingT1(u, utol)) return IsoStatus.East;

            var vtol = V.Tolerance_IsCloseToSingular;
            if (V.isCloseToSingT0(v, vtol)) return IsoStatus.South;
            if (V.isCloseToSingT1(v, vtol)) return IsoStatus.North;

            return IsoStatus.None;
        }
    }

    public class SurfacePoint_SingularCorrection : SurfacePoint
    {
        public Point3d PointSrf3d;
        public Point3d PointEdge3d;
        public double DistToEdge;

        public bool isAlmostSingularity;
        public bool isCloseToSingular;
        public bool isCloseToSingular_HighJump;
        public bool isCloseToSingular_HighJumpDouble;
        public bool isCloseToSingular_HighJumpQuad;
        public double distPectentChanges;

        public double newU;
        public double newV;

        public SurfacePoint_SingularCorrection(double u, double v)
            : base(u, v)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ".   DistToEdge = " + DistToEdge._ToStringX(10);
        }
    }

    public static class _Surface_FixSurfacePoints
    {
        //const double MIN_CHANGE = 0.00001;

        /// <summary>
        /// Fix U or V in case where values are indentical at start of surface (like Point3d(u, v) == Point3d(u, v+100));
        /// </summary>
        /// <param name="srf"></param>
        /// <param name="pointsUV">2d Points on Surface</param>
        /// <param name="removeDuplicatedPoints"></param>
        /// <param name="singulars">Singular info of surface</param>
        /// <param name="trim">For 3d curves trim is used to more precision results</param>
        /// <param name="edge">For 2d curves trim is used to more precision results</param>
        /// <param name="roundStart"></param>
        /// <param name="roundEnd"></param>
        /// <param name="fixSurfacePoints_UVOutOfDomain"></param>
        /// <returns>True if fixed, False if no fix required</returns>
        public static bool _FixSurfacePoints(this Surface srf, ref List<SurfacePoint> pointsUV,
            bool removeDuplicatedPoints = false,
            SurfaceSingulars singulars = null, Curve trim = null, Curve edge = null,
            bool roundStart = false, bool roundEnd = false, bool fixSurfacePoints_UVOutOfDomain = true)
        {
            if (pointsUV.Count <= 1) return false;


            if (singulars == null)
            {
                singulars = new SurfaceSingulars(srf);
            }


            // avoid working on dengorously small domains - this can lead to problems - lets first face will be fixed
            if (singulars.U.Domain.Length < 0.0000001
                || singulars.V.Domain.Length < 0.0000001)
            {
                return false;
            }

            //
            // Fix points
            //
            List<SurfacePoint> pointsUVOrigin = null;

            var changeIsNotable = false;

            ////////////////////////////
            ////// Correct Seam //////
            ////////////////////////////
            srf.CorrectSeam(pointsUV, null, (pointsUVpar) =>
            {
                changeIsNotable = true;
                if (pointsUVOrigin == null) pointsUVOrigin = pointsUVpar.Select(o => new SurfacePoint(o.u, o.v)).ToList();
            });

            ////////////////////////////////////
            ////// Correct InSingularity //////
            ////////////////////////////////////
            if (singulars.HasSingulars)
            {
                // OLD approach
                //srf.CorrectSingularityHighJumpBaseOnEdge(pointsUV, singulars, edge, (pointsUVpar) =>
                //{
                //    changeIsNotable = true;
                //    if (pointsUVOrigin == null) pointsUVOrigin = pointsUVpar.Select(o => new SurfacePoint(o.u, o.v)).ToList();
                //});

                // NEW approach
                srf.CorrectSingularityBaseOnEdge(pointsUV, singulars, edge, (pointsUVpar) =>
                {
                    changeIsNotable = true;
                    if (pointsUVOrigin == null) pointsUVOrigin = pointsUVpar.Select(o => new SurfacePoint(o.u, o.v)).ToList();
                });


                srf._FixSurfacePoints_InSingularity(pointsUV, singulars, trim, (pointsUVpar) =>
                {
                    if (pointsUVOrigin == null) pointsUVOrigin = pointsUVpar.Select(o => new SurfacePoint(o.u, o.v)).ToList();
                }, ref changeIsNotable, roundStart, roundEnd);

            }

            ////////////////////////////////////////
            ////// Correct UVOutOfDomain //////
            ////////////////////////////////////////
            if (fixSurfacePoints_UVOutOfDomain)
            {
                srf._FixSurfacePoints_UVOutOfDomain(pointsUV, (pointsUVpar) =>
                {
                    if (pointsUVOrigin == null) pointsUVOrigin = pointsUVpar.Select(o => new SurfacePoint(o.u, o.v)).ToList();
                }, ref changeIsNotable);
            }


            //int countChanged = (pointsUVOrigin == null) ? 0 : pointsUV.Count(o => !pointsUVOrigin.Exists(o2 => o2 == o));
            //var res = countChanged > 0;
            var res = changeIsNotable;


            //
            // Remove duplicate points
            //
            if (res && removeDuplicatedPoints)
            {
                while (pointsUV[pointsUV.Count - 1] == pointsUV[pointsUV.Count - 2])
                {
                    pointsUV.RemoveAt(pointsUV.Count - 1);
                }
                while (pointsUV[0] == pointsUV[1])
                {
                    pointsUV.RemoveAt(0);
                }
            }

            //
            // Validate new points
            //
            if (res)
            {
                // new curve must have less control points - this means that we have corrected control point
                // - otherwise restore original control points
                var tol = 0.0001;// 0.0001 is the maximum precision because some bigger may hang thread for a minutes and we dont have ability to terminate it, so to prevent problems dont increase this value
                var devidedCurveOrigin = srf.InterpolatedCurveOnSurfaceUV(pointsUVOrigin.Select(o => new Point2d(o.u, o.v)).ToList(), tol);
                var devidedCurve = srf.InterpolatedCurveOnSurfaceUV(pointsUV.Select(o => new Point2d(o.u, o.v)).ToList(), tol);

                var originLength = devidedCurveOrigin == null ? 0 : devidedCurveOrigin._GetLength_ThreadSafe();
                var newLength = devidedCurve == null ? 0 : devidedCurve._GetLength_ThreadSafe();

                if (newLength > 10000 * originLength)
                {
                    log.wrong("_FixSurfacePoints: newLength > 10000*originLength  = {0}", newLength);
                }
                else if (newLength > 100 * originLength)
                {
                    log.wrong("_FixSurfacePoints: newLength > 100*originLength  = {0}", newLength);
                }


                if (newLength > originLength
                    && (newLength - originLength) / originLength > 0.1 // increase of length is more than 10%
                    )
                {
                    // restore original control points
                    pointsUV = pointsUVOrigin;
                    res = false;
                }
            }


            return res;
        }








        private static void CorrectSeam(this Surface srf, List<SurfacePoint> points,
            SurfaceSeams ss, Action<List<SurfacePoint>> doBeforeArrayChange)
        {
            if (ss == null)
            {
                ss = new SurfaceSeams(srf);
            }
            if (!ss.HasSeams) return;


            var countStrongUVS = 0;
            for (int c = -(points.Count - 1); c < points.Count; c++) // 2 cycles at once:  count..0 && 0..count
            {
                var i = c;
                if (c < 0) i = -c;
                if (c == 1)
                {
                    countStrongUVS = Math.Min(countStrongUVS, 1); // clear counter if we start new cycle 0..count 
                }


                var p = points[i];
                var seamIndex = ss.IsAtSeam(p.u, p.v);
                if (seamIndex == 0)
                {
                    countStrongUVS++;
                }
                else
                {
                    if (countStrongUVS >= 2 && seamIndex != 3)
                    {
                        var iPrev1 = Math.Abs(c - 1);
                        var pPrev = points[iPrev1];
                        var iUVSeam = (seamIndex - 1);
                        var iUVOk = (seamIndex - 1) ^ 1;

                        var domain = ss.UV[iUVSeam].Domain;
                        var valPrev = pPrev.uv[iUVSeam];
                        var val = p.uv[iUVSeam];
                        var valApprox = valPrev._RoundToDomainMinMax(domain, 0.5);
                        if (Math.Abs(valApprox - val) > domain.Length / 2)
                        {
                            doBeforeArrayChange(points);
                            points[i].SetUV(valApprox, iUVSeam);
                        }
                    }
                    else
                    {
                        countStrongUVS = 0;
                    }
                }
            }
        }


        private static bool CorrectSingularityBaseOnEdge(this Surface srf, List<SurfacePoint> points,
            SurfaceSingulars singulars, Curve edge, Action<List<SurfacePoint>> doBeforeArrayChange)
        {
            if (edge == null) return false;
            if (!singulars.HasSingulars) return false;
            if (!_PointsAreCloseSingularity_HighJump(srf, points, singulars)) return false;
            if (edge._GetLength_ThreadSafe() < 0.01) return false;
            var ss = new SurfaceSeams(srf);

            var cor = new List<SurfacePoint_SingularCorrection>(points.Count);
            var isNeededCorrection = false;
            foreach (var p in points)
            {
                var c = new SurfacePoint_SingularCorrection(p.u, p.v);
                c.PointSrf3d = srf.PointAt(p.u, p.v);
                double t;
                double dist = -1;
                c.PointEdge3d = edge.PointAtStart;
                if (edge.ClosestPoint(c.PointSrf3d, out t))
                {
                    c.PointEdge3d = edge.PointAt(t);
                    dist = c.PointEdge3d._DistanceTo(c.PointSrf3d);
                }
                c.DistToEdge = dist;

                c.isAlmostSingularity = singulars.U.IsAlmostSingularity(p.u)
                                               || singulars.V.IsAlmostSingularity(p.v);
                c.isCloseToSingular = singulars.U.IsCloseToSingular(p.u)
                                            || singulars.V.IsCloseToSingular(p.v);
                c.isCloseToSingular_HighJump = singulars.U.IsCloseToSingular_HighJump(p.u)
                                                           || singulars.V.IsCloseToSingular_HighJump(p.v);
                c.isCloseToSingular_HighJumpDouble = singulars.U.IsCloseToSingular_HighJumpDouble(p.u)
                                                                           || singulars.V.IsCloseToSingular_HighJumpDouble(p.v);
                c.isCloseToSingular_HighJumpQuad = singulars.U.IsCloseToSingular_HighJumpQuad(p.u)
                                                                           || singulars.V.IsCloseToSingular_HighJumpQuad(p.v);

                cor.Add(c);

                if (c.DistToEdge > 0.001 && !c.isAlmostSingularity)
                {
                    isNeededCorrection = true;
                }
            }
            if (!isNeededCorrection) return true;



            //DEBUG
            //var points3dtemp = points.Select(o => srf.PointAt(o.u, o.v)).ToArray();
            //var zigzagstemp = _CurveZigZagCleaner._ZigZagDeformationsFind(points3dtemp);
            //if (zigzagstemp != null)
            //{
            //    zigzagstemp = zigzagstemp;
            //}
            //ENDDEBUG

            // iterate all except first and last
            //var duShiftMin = singulars.U.HasSingulars ? 0 : srf.Domain(0).Length / 100000; // very small for high precision
            //var dvShiftMin = singulars.V.HasSingulars ? 0 : srf.Domain(1).Length / 100000; // very small for high precision
            var duShiftMin = srf.Domain(0).Length / 1000000; // very small for high precision
            var dvShiftMin = srf.Domain(1).Length / 1000000; // very small for high precision
            var duShift100 = srf.Domain(0).Length / 100; // very small for high precision
            var dvShift100 = srf.Domain(1).Length / 100; // very small for high precision
            var uMin = srf.Domain(0).Min;
            var uMax = srf.Domain(0).Max;
            var vMin = srf.Domain(1).Min;
            var vMax = srf.Domain(1).Max;
            for (int i = 0; i < cor.Count; i++)
            {
                var c = cor[i];
                if (c.isAlmostSingularity)
                {
                    continue;
                }
                if (c.DistToEdge < 0) continue; // skip this points for which we couldnt find distance to edge
                if (ss.IsAtSeam(c.u, c.v) > 0) continue; // dont work with seam - hard to implement

                if (c.DistToEdge >= 0.0001)
                {
                    var pointOnEdge3d = c.PointEdge3d;
                    var newU = c.u;
                    var newV = c.v;
                    //var checkDistToSrf = srf.PointAt(c.u, c.v)._DistanceTo(c.PointSrf);
                    //var checkDistToEdge = srf.PointAt(c.u, c.v)._DistanceTo(c.PointEdge);
                    double newDist = 0;

                    // ver 1 - slow - use very small du dv
                    //GetClosestSurfacePointSlow(srf, pointOnEdge3d, duShiftMin, dvShiftMin, uMin, uMax, vMin, vMax, out newDist, ref newU, ref newV);

                    // ver 2 - fast - use different du dv from bigger to smaller
                    var shiftu = duShift100;
                    var shiftv = dvShift100;
                    while (shiftu > duShiftMin)
                    {
                        GetClosestSurfacePointFast(srf, pointOnEdge3d, shiftu, shiftv, uMin, uMax, vMin, vMax, out newDist, ref newU, ref newV);
                        shiftu /= 10;
                        shiftv /= 10;
                    }

                    if (Math.Abs(c.DistToEdge - newDist) > 0.00005)
                    {
                        doBeforeArrayChange(points);
                        points[i].SetUV(newU, 0);
                        points[i].SetUV(newV, 1);
                        //log.temp("doBeforeArrayChange  i={0}  c.DistToEdge={1}   newDist={2}", i, c.DistToEdge._ToStringX(7), newDist._ToStringX(7));
                        //log.temp("  u={0}  v={1}   newU={2}   newV={3}", c.u._ToStringX(7), c.v._ToStringX(7), newU._ToStringX(7), newV._ToStringX(7));
                    }

                    //DEBUG
                    //log.temp("i = {0}   dist={1:0.00000}   newdist={2:0.00000}  triesCount={3} newu={4:0.00000} newv={5:0.00000}", i, c.DistToEdge, newDist, triesCount, newU, newV);
                    //Layers.Debug.AddPoint(srf.PointAt(points[i].u, points[i].v), Color.Red);
                    //Layers.Debug.AddTextPoint("" + i, srf.PointAt(points[i].u, points[i].v), Color.Red);
                    //Layers.Debug.AddTextPoint("" + i, c.PointEdge, Color.Aqua);
                    //ENDDEBUG
                }
            }


            //int removeZigZagsTries = 0;
            //while (removeZigZagsTries < 10)
            //{
            //    removeZigZagsTries++;
            //    var points3d = points.Select(o => srf.PointAt(o.u, o.v)).ToArray();
            //    var zigzags = _CurveZigZagCleaner._ZigZagDeformationsFind(points3d);
            //    if (zigzags == null) break;
            //    var temp = 0;
            //}
            return true;
        }

        private static bool GetClosestSurfacePointSlow(Surface srf, Point3d pointOnEdge3d, double duShift, double dvShift, double uMin, double uMax, double vMin, double vMax, out double newDist, ref double u, ref double v)
        {
            var changed = false;
            var triesCount = 0;
            newDist = pointOnEdge3d._DistanceTo(srf.PointAt(u, v));
            //var newDist = edge._DistanceTo(srf.PointAt(newU, newV));
            var breakTimes = 0;
            while (newDist > 0.000001 && triesCount < 10000)
            {
                triesCount++;
                var du = (triesCount % 2 == 0) ? duShift : 0;
                var dv = (triesCount % 2 == 0) ? 0 : dvShift;
                var pointDirectionForward = srf.PointAt(u + du, v + dv);
                var pointDirectionBackward = srf.PointAt(u - du, v - dv);
                var direction = pointOnEdge3d._DistanceTo(pointDirectionForward) < pointOnEdge3d._DistanceTo(pointDirectionBackward) ? 1 : -1;
                //var direction = edge._DistanceTo(srf.PointAt(newU + du, newV + dv)) < edge._DistanceTo(srf.PointAt(newU - du, newV - dv)) ? 1 : -1;
                var pointNew = srf.PointAt(u, v);
                var pointNextDuDv = direction == 1 ? pointDirectionForward : pointDirectionBackward;

                var distNew = pointOnEdge3d._DistanceTo(pointNew);
                var distNextDuDv = pointOnEdge3d._DistanceTo(pointNextDuDv);
                //var distNew = edge._DistanceTo(pointNew);
                //var distNextDuDv = edge._DistanceTo(pointNextDuDv);
                if (distNextDuDv > distNew)
                {
                    breakTimes++;
                    if (breakTimes > 4) break;
                    continue;
                }
                var distM = distNew - distNextDuDv;
                var ShiftByMax = distNew / distM._GetNonZeroForDevisionOperation();

                var shifted = false;
                foreach (var ShiftBy in new[] { ShiftByMax, 10, 1 })
                {
                    var newU = u + direction * ShiftBy * du;
                    var newV = v + direction * ShiftBy * dv;
                    if (newU < uMin) newU = uMin;
                    if (newU > uMax) newU = uMax;
                    if (newV < vMin) newV = vMin;
                    if (newV > vMax) newV = vMax;
                    var new_newDist = pointOnEdge3d._DistanceTo(srf.PointAt(newU, newV));
                    //var new_newDist = edge._DistanceTo(srf.PointAt(new_newU, new_newV));
                    if (new_newDist < newDist)
                    {
                        u = newU;
                        v = newV;
                        changed = true;
                        newDist = new_newDist;
                        shifted = true;
                        break;
                    }
                }
                if (!shifted)
                {
                    breakTimes++;
                    if (breakTimes > 4) break;
                    continue;
                }
                breakTimes = 0;
            }
            return changed;
        }

        private static bool GetClosestSurfacePointFast(Surface srf, Point3d pointOnEdge3d, double duShift, double dvShift, double uMin, double uMax, double vMin, double vMax, out double newDist, ref double u, ref double v)
        {
            var changed = false;
            var triesCount = 0;
            var pointUV = srf.PointAt(u, v);
            newDist = pointOnEdge3d._DistanceTo(pointUV);
            //var newDist = edge._DistanceTo(srf.PointAt(newU, newV));
            var breakTimes = 0;
            while (newDist > 0.000001 && triesCount < 10000)
            {
                triesCount++;
                var du = (triesCount % 2 == 0) ? duShift : 0;
                var dv = (triesCount % 2 == 0) ? 0 : dvShift;
                var pointDirectionForward = srf.PointAt(u + du, v + dv);
                var pointDirectionBackward = srf.PointAt(u - du, v - dv);
                var distForward = pointOnEdge3d._DistanceTo(pointDirectionForward);
                var distBackward = pointOnEdge3d._DistanceTo(pointDirectionBackward);
                var direction =  distForward < distBackward ? 1 : -1;
                var pointNextDuDv = direction == 1 ? pointDirectionForward : pointDirectionBackward;

                var distUV = pointOnEdge3d._DistanceTo(pointUV);
                var distNextDuDv = direction == 1 ? distForward : distBackward;

                if (distNextDuDv > distUV)
                {
                    breakTimes++;
                    if (breakTimes > 4) break;
                    continue;
                }

                var shifted = false;
                var newU = u + direction * du;
                var newV = v + direction * dv;
                var newPoint = pointNextDuDv;
                var newnewDist = distNextDuDv;
                if (newU < uMin || newU > uMax || newV < vMin || newV > vMax)
                {
                    if (newU < uMin) newU = uMin;
                    if (newU > uMax) newU = uMax;
                    if (newV < vMin) newV = vMin;
                    if (newV > vMax) newV = vMax;
                    newPoint = srf.PointAt(newU, newV);
                    newnewDist = pointOnEdge3d._DistanceTo(newPoint);
                }
                //var new_newDist = edge._DistanceTo(srf.PointAt(new_newU, new_newV));
                if (newnewDist < newDist)
                {
                    pointUV = newPoint;
                    u = newU;
                    v = newV;
                    changed = true;
                    newDist = newnewDist;
                    shifted = true;
                }
                if (!shifted)
                {
                    breakTimes++;
                    if (breakTimes > 4) break;
                    continue;
                }
                breakTimes = 0;
            }
            return changed;
        }



        public static bool _PointsAreCloseSingularity_HighJump(this Surface srf, List<SurfacePoint> points,
            SurfaceSingulars singulars)
        {
            if (!singulars.HasSingulars) return false;
            for (var iUV = 0; iUV <= 1; iUV++)
            {
                var s = singulars.UV[iUV];
                if (!s.HasSingulars) continue;
                for (int i = 0; i < points.Count; i++)
                {
                    if (s.IsCloseToSingular_HighJump(points[i].uv[iUV]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void CorrectSingularityHighJumpBaseOnEdge(this Surface srf, List<SurfacePoint> points,
            SurfaceSingulars singulars, Curve edge, Action<List<SurfacePoint>> doBeforeArrayChange)
        {
            if (!singulars.HasSingulars) return;
            if (edge == null) return;
            var ss = new SurfaceSeams(srf);

            bool foundisCloseToBounds = false;
            for (var iUV = 0; iUV <= 1; iUV++)
            {
                var s = singulars.UV[iUV];
                if (!s.HasSingulars) continue;
                var nextiEndIsSingular_0 = s.IsCloseToSingular(points[0].uv[iUV]);
                var nextiEndIsSingular_Count = s.IsCloseToSingular(points[points.Count - 1].uv[iUV]);
                if (nextiEndIsSingular_0 || nextiEndIsSingular_Count)
                {
                    foundisCloseToBounds = true;
                }
            }
            if (!foundisCloseToBounds) return;

            var cor = new List<SurfacePoint_SingularCorrection>(points.Count);
            var isNeededCorrection = false;
            foreach (var p in points)
            {
                var psrf = srf.PointAt(p.u, p.v);
                double t;
                double dist = -1;
                Point3d pAtEdge = Point3d.Origin;
                if (edge.ClosestPoint(psrf, out t))
                {
                    pAtEdge = edge.PointAt(t);
                    dist = pAtEdge._DistanceTo(psrf);
                }
                var ps = new SurfacePoint_SingularCorrection(p.u, p.v);
                ps.DistToEdge = dist;

                ps.isAlmostSingularity = singulars.U.IsAlmostSingularity(p.u)
                                               || singulars.V.IsAlmostSingularity(p.v);
                ps.isCloseToSingular = singulars.U.IsCloseToSingular(p.u)
                                            || singulars.V.IsCloseToSingular(p.v);
                ps.isCloseToSingular_HighJump = singulars.U.IsCloseToSingular_HighJump(p.u)
                                                           || singulars.V.IsCloseToSingular_HighJump(p.v);
                ps.isCloseToSingular_HighJumpDouble = singulars.U.IsCloseToSingular_HighJumpDouble(p.u)
                                                                           || singulars.V.IsCloseToSingular_HighJumpDouble(p.v);
                ps.isCloseToSingular_HighJumpQuad = singulars.U.IsCloseToSingular_HighJumpQuad(p.u)
                                                                           || singulars.V.IsCloseToSingular_HighJumpQuad(p.v);
                if (ps.isCloseToSingular_HighJump)
                {
                    double newU, newV;
                    if (dist > 0 && srf.ClosestPoint(pAtEdge, out newU, out newV))
                    {
                        ps.newU = newU;
                        ps.newV = newV;
                        var singU = singulars.U.RoundToSingularValue(newU, singulars.U.Tolerance_IsCloseToSingular_HighJump);
                        var singV = singulars.V.RoundToSingularValue(newV, singulars.U.Tolerance_IsCloseToSingular_HighJump);
                        var distFromSingToEdge = srf.PointAt(singU, singV)._DistanceTo(pAtEdge);
                        var distFromSingToCP = srf.PointAt(singU, singV)._DistanceTo(psrf);
                        var distPectentChanges = Math.Abs(distFromSingToEdge - distFromSingToCP) / distFromSingToEdge;
                        ps.distPectentChanges = distPectentChanges;
                    }
                }
                //ps.isAtSeam = srf.IsAtSeam(p.u, p.v) > 0;
                cor.Add(ps);

                if (ps.DistToEdge > 0.001 && !ps.isAlmostSingularity)
                {
                    isNeededCorrection = true;
                }
            }

            double distNormal = 0;
            double distSingHighJump = 0;
            double distSingHighJumpMax = 0;
            int distNormalCount = 0;
            int distSingHighJumpCount = 0;
            double distSingHighJumpMaxNewU = 0, distSingHighJumpMaxNewV = 0;
            double distPectentChangesMax = 0;
            foreach (var c in cor)
            {
                if (c.DistToEdge < 0) continue; // skip this points for which we couldnt find distance to edge
                if (c.isCloseToSingular) continue; // exclude singularity - it is another algorithm care
                if (c.isCloseToSingular_HighJump)
                {
                    distSingHighJump += c.DistToEdge;
                    distSingHighJumpCount++;
                    if (c.DistToEdge > distSingHighJumpMax)
                    {
                        distSingHighJumpMax = c.DistToEdge;
                        distSingHighJumpMaxNewU = Math.Abs(c.newU - c.u);
                        distSingHighJumpMaxNewV = Math.Abs(c.newV - c.v);

                    }
                    if (c.distPectentChanges > distPectentChangesMax)
                    {
                        distPectentChangesMax = c.distPectentChanges;
                    }
                }
                else
                {
                    distNormal += c.DistToEdge;
                    distNormalCount++;
                }
            }

            if (distNormalCount == 0) distNormalCount = 1;
            if (distSingHighJumpCount == 0) distSingHighJumpCount = 1;
            var distNormalAVG = distNormal / distNormalCount;
            var distSingHighJumpAVG = distSingHighJump / distSingHighJumpCount;

            if (distNormalAVG * 2 < distSingHighJumpAVG) // avarage dist is also higher at least 2*times)
                //if (distNormal < distSingHighJump // total dist to edge in singularity is higher from normal area
                if (distPectentChangesMax > 0.04) // max 4% of difference between 'dist from edge to singular' and 'dist from trim to singular'
                {
                    // here we know that we should do correction
                    var minI = -1;
                    var maxI = -1;
                    var minEdgeT = Double.MaxValue;
                    var maxEdgeT = Double.MinValue;

                    // iterate all except first and last
                    for (int i = 1; i < cor.Count - 1; i++)
                    {
                        var c = cor[i];
                        if (c.isCloseToSingular) continue;
                        if (ss.IsAtSeam(c.u, c.v) > 0) continue; // dont work with seam - hard to implement
                        // srf.IsClosed(1)

                        if (c.isCloseToSingular_HighJumpQuad && c.DistToEdge >= 0.001)
                        {
                            var psrf = srf.PointAt(c.u, c.v);
                            double t;
                            if (edge.ClosestPoint(psrf, out t))
                            {
                                if (minI == -1)
                                {
                                    minI = i;
                                    minEdgeT = t;
                                }
                                if (maxI < i)
                                {
                                    maxI = i;
                                    maxEdgeT = t;
                                }

                                double newU, newV;
                                if (srf.ClosestPoint(edge.PointAt(t), out newU, out newV))
                                {
                                    var newDist = srf.PointAt(newU, newV)._DistanceTo(srf.PointAt(c.u, c.v));
                                    if (c.DistToEdge / newDist._GetNonZeroForDevisionOperation() > 3)// we have decreased dist
                                    {
                                        doBeforeArrayChange(points);
                                        //if (singulars.U.IsCloseToSingular_HighJump(c.u)) points[i].SetUV(newV, 1); // here we change for singulars.U valud of v - it is correct !
                                        //if (singulars.V.IsCloseToSingular_HighJump(c.v)) points[i].SetUV(newU, 0); // here we change for singulars.V valud of u - it is correct !
                                        points[i].SetUV(newU, 0);
                                        points[i].SetUV(newV, 1);
                                    }
                                }
                            }
                        }
                    }

                    // lets do better correction - split controls points by equal length on Edge - so every control point will be equaly far from another
                    // (here we suppose that lines comes throught 1 singularity)
                    //if (minI != -1 && maxI != -1)
                    //{
                    //    var tIncr = (maxEdgeT - minEdgeT) / (maxI - minI + 1);
                    //    var t = minEdgeT;
                    //    for (int i = minI; i <= maxI; i++)
                    //    {
                    //        double newU, newV;
                    //        if (srf.ClosestPoint(edge.PointAt(t), out newU, out newV))
                    //        {
                    //            //var newDist = srf.PointAt(newU, newV)._DistanceTo(srf.PointAt(c.u, c.v));
                    //            doBeforeArrayChange(points);
                    //            points[i].SetUV(newU, 0);
                    //            points[i].SetUV(newV, 1);
                    //        }
                    //        t += tIncr;
                    //    }
                    //}
                }
        }









        private static void _FixSurfacePoints_InSingularity(this Surface srf, List<SurfacePoint> points,
            SurfaceSingulars singulars, Curve trim,
            Action<List<SurfacePoint>> doBeforeArrayChange, ref bool isChangeNotable,
            bool roundStart = false, bool roundEnd = false)
        {

            if (!singulars.HasSingulars) return;
            for (var iUV = 0; iUV <= 1; iUV++)
            {
                var iUVOpposite = iUV ^ 1;
                var s = singulars.UV[iUV];
                var sOpposite = singulars.UV[iUVOpposite];
                if (!s.HasSingulars) continue;

                double MIN_CHANGE = 0.00001 * s.Domain.Length;
                double NOTABLE_CHANGE = 0.005 * s.Domain.Length;
                double MIN_CHANGE_OPPOSITE = 0.00001 * sOpposite.Domain.Length;
                double NOTABLE_CHANGE_OPPOSITE = 0.005 * sOpposite.Domain.Length;

                // if strong numbers have at least 20% - we can rely on them to fix singulars values in CloseToBounds_HighJump area
                var tolFixIn_CloseToBounds_HighJump = singulars.UV[iUV].Domain.Length / 5;


                //
                // Fix seeams
                // Fix circular points that equal even in non singularity (srf.PointAt(T0) == srf.PointAt(T1))
                // This can happend for spheres and torus
                //
                if (trim != null)
                {
                    var tol3d = 0.0001 * singulars.UV[iUV ^ 1].Domain.Length;
                    var tol2d = 0.1 * singulars.UV[iUV ^ 1].Domain.Length;
                    var tol2dbig = 0.6 * singulars.UV[iUV ^ 1].Domain.Length;
                    for (int i = 0; i < points.Count; i++)
                    {
                        // Project point on trim
                        double t;
                        var u = points[i].u;
                        var v = points[i].v;
                        if (!trim.ClosestPoint(new Point3d(u, v, 0), out t)) continue;
                        var pointOnTrim2d = new SurfacePoint(trim.PointAt(t));
                        var pointOnTrim3d = srf.PointAt(pointOnTrim2d.u, pointOnTrim2d.v);

                        // Project point on srf
                        var pointOnSrf = srf.PointAt(u, v);

                        // check if points3d in 3d are very close, but U or V has 20% difference
                        var dist3d = pointOnSrf._DistanceTo(pointOnTrim3d);
                        var uvOnTrim = pointOnTrim2d.uv[iUV ^ 1];
                        var uvDiffFromOriginalTrim = Math.Abs(points[i].uv[iUV ^ 1] - uvOnTrim);
                        if (dist3d < tol3d && uvDiffFromOriginalTrim > tol2d
                            || (uvDiffFromOriginalTrim > tol2dbig))
                        {
                            isChangeNotable = true;
                            doBeforeArrayChange(points);
                            points[i].SetUV(uvOnTrim, iUV ^ 1);
                        }
                    }
                }

                //
                // Approximate points close to singularity
                //
                var countStrongUVS = 0;
                var nextiEndIsSingular_0 = s.IsAlmostSingularity(points[0].uv[iUV]);
                var nextiEndIsSingular_Count = s.IsAlmostSingularity(points[points.Count - 1].uv[iUV]);
                for (int c = -(points.Count - 1); c < points.Count; c++) // 2 cycles at once:  count..0 && 0..count
                {
                    var i = c; if (c < 0) i = -c;
                    if (c == 1)
                    {
                        countStrongUVS = Math.Min(countStrongUVS, 1); // clear counter if we start new cycle 0..count 
                    }
                    var testValue = points[i].uv[iUV];

                    var isCloseToBounds_HighJump = s.IsCloseToSingular_HighJump(testValue);
                    var isAlmostSingT0 = s.isCloseToSingT0(testValue, s.Tolerance_IsAlmostSingularity);
                    var isAlmostSingT1 = s.isCloseToSingT1(testValue, s.Tolerance_IsAlmostSingularity);
                    var nextiEndIsSingular = c < 0 ? nextiEndIsSingular_0 : nextiEndIsSingular_Count;

                    //
                    // Correct near to singular if we have long history before
                    //
                    if (isCloseToBounds_HighJump // we are close to singularity just (in 6% of singularity center)
                        && countStrongUVS >= 3 // we have enough control points to predict next value
                        && nextiEndIsSingular // do this only if curve ends in singularity, otherwise it may do harm - sometimes crvs are close to singularity but Mis it

                        )
                    {
                        var iPrevFirst = Math.Abs(c - countStrongUVS);
                        var iPrev1 = Math.Abs(c - 1);
                        var valDiffNonSingularPrev = points[iPrevFirst].uv[iUV] - points[iPrev1].uv[iUV];
                        if (!valDiffNonSingularPrev._IsSame(0)
                            && Math.Abs(valDiffNonSingularPrev) > tolFixIn_CloseToBounds_HighJump // we have 20% of domain length values to predict values in CloseToBounds_HighJum singularity area
                            )
                        {
                            var valDiffSingularPrev = points[iPrevFirst].uv[iUV ^ 1] - points[iPrev1].uv[iUV ^ 1];
                            var maxChangeSpeed_OfValSingular = valDiffSingularPrev / valDiffNonSingularPrev; // how singular can vary per 1 unit of non singular value
                            var valDiffNonSingularCurrent = points[iPrev1].uv[iUV] - points[i].uv[iUV];
                            //var expectedDiffSingular = (valDiffNonSingularCurrent / valDiffNonSingularPrev) * valDiffSingularPrev;  // same as line after (just for better developer understanding)
                            var expectedDiffSingular = maxChangeSpeed_OfValSingular * valDiffNonSingularCurrent;   // same as line before (just for better developer understanding)

                            var valPrev = points[iPrev1].uv[iUV ^ 1];
                            var valCurrMax = Math.Max(valPrev + expectedDiffSingular, valPrev - expectedDiffSingular);
                            var valCurrMin = Math.Min(valPrev + expectedDiffSingular, valPrev - expectedDiffSingular);
                            var valCurr = points[i].uv[iUV ^ 1];
                            if (valCurr > valCurrMax && valCurr - valCurrMax > MIN_CHANGE_OPPOSITE)
                            {
                                if (valCurr - valCurrMax > NOTABLE_CHANGE_OPPOSITE) isChangeNotable = true;
                                doBeforeArrayChange(points);
                                points[i].SetUV(valCurrMax, iUV ^ 1);
                            }
                            if (valCurr < valCurrMin && valCurrMin - valCurr > MIN_CHANGE_OPPOSITE)
                            {
                                if (valCurrMin - valCurr > NOTABLE_CHANGE_OPPOSITE) isChangeNotable = true;
                                doBeforeArrayChange(points);
                                points[i].SetUV(valCurrMin, iUV ^ 1);
                            }
                            if ((roundStart && i == 0)
                                || (roundEnd && i == points.Count - 1))
                            {
                                var newVal = isAlmostSingT0 ? s.T0 : s.T1;
                                if (!points[i].uv[iUV].Equals(newVal))
                                {
                                    isChangeNotable = true;
                                    doBeforeArrayChange(points);
                                    points[i].SetUV(newVal, iUV);
                                }
                            }
                            countStrongUVS++;
                            continue;
                        }
                    }



                    //
                    // Detect high jump
                    //
                    //bool isHighJump = false;
                    //if (countStrongUVS >= 2 && isCloseToBounds_HighJump)
                    //{
                    //    var iPrev1 = Math.Abs(c - 1);
                    //    var iPrev2 = Math.Abs(c - 2);
                    //    var valCurr = points[i].uv[iUVOpposite];
                    //    var valPrev1 = points[iPrev1].uv[iUVOpposite];
                    //    var valPrev2 = points[iPrev2].uv[iUVOpposite];
                    //    var valIncrPrev1 = Math.Abs(valCurr - valPrev1);
                    //    var valIncrPrev12 = Math.Abs(valPrev1 - valPrev2);
                    //    if (Math.Abs(valIncrPrev1 - valIncrPrev12) > sOpposite.Tolerance_HighJump)
                    //    {
                    //        isHighJump = true;
                    //    }
                    //}

                    //
                    // Correct exactly in singulararity
                    //
                    if (isAlmostSingT0 || isAlmostSingT1) // || isHighJump
                    //if (isCloseToBounds_HighJump || isHighJump)
                    {
                        if (countStrongUVS >= 2) // prev 2 points are good and can be used to approximate our value
                        {
                            var iPrev1 = Math.Abs(c - 1);
                            var iPrev2 = Math.Abs(c - 2);
                            var valPrev1 = points[iPrev1].uv[iUV ^ 1];
                            var valPrev2 = points[iPrev2].uv[iUV ^ 1];
                            var valIncr = valPrev1 - valPrev2;
                            if (countStrongUVS >= 3)
                            {
                                var iPrev3 = Math.Abs(c - 3);
                                var valPrev3 = points[iPrev3].uv[iUV ^ 1];
                                var valIncr2 = valPrev2 - valPrev3;
                                valIncr = (valIncr + valIncr2) / 2;
                            }
                            var valApprox = valPrev1 + valIncr;
                            if (valApprox > sOpposite.T1) valApprox = sOpposite.T1;
                            if (valApprox < sOpposite.T0) valApprox = sOpposite.T0;
                            var valUncertain = points[i].uv[iUV ^ 1];

                            //valApprox = valPrev1;
                            var changeDiff = Math.Abs(valUncertain - valApprox);
                            if (changeDiff > MIN_CHANGE_OPPOSITE)
                            {
                                if (changeDiff > NOTABLE_CHANGE_OPPOSITE) isChangeNotable = true;
                                doBeforeArrayChange(points);
                                points[i].SetUV(valApprox, iUV ^ 1);
                            }
                            countStrongUVS++;
                        }
                        else if (countStrongUVS == 1 && points.Count == 2) // line detected in singular
                        {
                            var valUncertain = points[i].uv[iUV ^ 1];
                            var iPrev1 = Math.Abs(c - 1);
                            var valPrev1 = points[iPrev1].uv[iUV ^ 1];
                            if (Math.Abs(valUncertain - valPrev1) > MIN_CHANGE_OPPOSITE)
                            {
                                isChangeNotable = true;
                                doBeforeArrayChange(points);
                                points[i].SetUV(valPrev1, iUV ^ 1);
                            }
                            countStrongUVS++;
                        }
                        else
                        {
                            countStrongUVS = 0;
                        }


                        if ((roundStart && i == 0)
                            || roundEnd && i == points.Count - 1)
                        {
                            var newVal = isAlmostSingT0 ? s.T0 : s.T1;
                            if (!points[i].uv[iUV].Equals(newVal))
                            {
                                isChangeNotable = true;
                                doBeforeArrayChange(points);
                                points[i].SetUV(newVal, iUV);
                            }
                        }
                    }
                    else
                    {
                        countStrongUVS++;
                    }
                }
            }
        }


        private static void _FixSurfacePoints_UVOutOfDomain(this Surface srf, List<SurfacePoint> points,
            Action<List<SurfacePoint>> doBeforeArrayChange, ref bool isChangeNotable)
        {
            var uMin = srf.Domain(0).T0;
            var uMax = srf.Domain(0).T1;
            var vMin = srf.Domain(1).T0;
            var vMax = srf.Domain(1).T1;
            foreach (var p in points)
            {
                if (p.u < uMin)
                {
                    isChangeNotable = true;
                    doBeforeArrayChange(points);
                    p.SetU(uMin);
                }
                if (p.u > uMax)
                {
                    isChangeNotable = true;
                    doBeforeArrayChange(points);
                    p.SetU(uMax);
                }
                if (p.v < vMin)
                {
                    isChangeNotable = true;
                    doBeforeArrayChange(points);
                    p.SetV(vMin);
                }
                if (p.v > vMax)
                {
                    isChangeNotable = true;
                    doBeforeArrayChange(points);
                    p.SetV(vMax);
                }
            }
        }

    }
}
