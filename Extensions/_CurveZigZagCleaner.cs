using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;

namespace SolidUtils
{
    public class ZigZagDiapason
    {
        public int IndexStart;
        public int IndexEnd;
        public double StartIgnoreAtT;
        public double EndIgnoreAtT;
    }

    public static class _CurveZigZagCleaner
    {
        public static bool _ZigZagDeformationExists(this Curve crv, Surface srf = null)
        {
            return crv._ZigZagDeformationsFind(srf) != null;
        }

        public static List<int> _ZigZagDeformationsFind_old(List<Point3d> points)
        {
            List<int> res = null;

            var p1 = points[points.Count - 1];
            var p2 = points[points.Count - 2];
            for (int c = -(points.Count - 3); c < points.Count; c++) // cycle from up-to-down and from down-to-up
            {
                var i = c; if (i < 0) i = -c;
                if (c == 0)
                {
                    p1 = points[0];
                    p2 = points[1];
                    c = i = 2;
                }
                var p3 = points[i];

                var distance12 = p1._DistanceTo_Pow2(p2);
                var distance13 = p1._DistanceTo_Pow2(p3);
                if (distance13 < distance12)
                {
                    // so point p2 is out of normal series range - lets remove p2
                    if (res == null) res = new List<int>();
                    res.Add(Math.Abs(c - 1));
                    p2 = p3;
                }
                else
                {
                    p1 = p2;
                    p2 = p3;
                }
            }
            if (res != null)
            {
                res._Distinct(); // remove duplicates
                res.Sort();
            }
            return res;
        }

        public static int[] _ZigZagDeformationsFind_old2(Point3d[] points)
        {
            List<int> res = null;

            var i1 = points.Length - 1; var p1 = points[i1];
            var i2 = points.Length - 2; var p2 = points[i2];

            for (int c = -(points.Length - 3); c < points.Length; c++) // cycle from up-to-down and from down-to-up
            {
                var i3 = c; if (i3 < 0) i3 = -c;
                if (c == 0)
                {
                    i1 = 0; p1 = points[i1];
                    i2 = 1; p2 = points[i2];
                    i3 = 2;
                    c = 2;
                }
                var p3 = points[i3];

                var distance12 = p1._DistanceTo_Pow2(p2);
                var distance13 = p1._DistanceTo_Pow2(p3);
                var distance23 = p2._DistanceTo_Pow2(p3);
                if (distance13 < distance23 || distance13 < distance12)
                {
                    // so point p2 is out of normal series range - lets remove p2
                    if (res == null) res = new List<int>();
                    res.Add(Math.Abs(i2));
                    i2 = i3;
                    p2 = p3;
                }
                else
                {
                    i1 = i2;
                    i2 = i3;
                    p1 = p2;
                    p2 = p3;
                }
            }
            if (res != null)
            {
                res._Distinct(); // remove duplicates
                res.Sort();
                return res.ToArray();
            }
            return null;
        }

        public static int[] _ZigZagDeformationsFind(Point3d[] points)
        {
            List<int> res = null;

            for (int i2 = 1; i2 < points.Length - 1; i2++) // cycle from up-to-down and from down-to-up
            {
                var i1 = i2 - 1;
                var i3 = i2 + 1;

                var p1 = points[i1];
                var p2 = points[i2];
                var p3 = points[i3];

                var distance13 = p1._DistanceTo_Pow2(p3);
                var distance21 = p2._DistanceTo_Pow2(p1);
                var distance23 = p2._DistanceTo_Pow2(p3);
                if (distance23 > distance13 || distance21 > distance13)
                {
                    // so point p2 is out of normal series range - lets remove p2
                    if (res == null) res = new List<int>();
                    res.Add(Math.Abs(i2));
                }
            }
            if (res != null)
            {
                res._Distinct(); // remove duplicates
                res.Sort();
                return res.ToArray();
            }
            return null;
        }

        public static int[] _ZigZagDeformationsFind(this Curve crv, Surface srf = null)
        {
            var degree = crv.Degree; // speed optimization
            if (degree == 1) return null; // Linear crv's can't have deformations
            if (degree == 2) return null; // not implemented - so lets skipp it

            var crvNurb = crv._ToNurbsCurve();
            if (crvNurb.Points.Count <= 3) return null;
            var points3d = crvNurb._Locations3d(srf);
            return _ZigZagDeformationsFind(points3d);
        }


        private static List<ZigZagDiapason> _ZigZag_GetDiapasons(NurbsCurve crvNurb, int[] zigzagIndexes)
        {
            //
            // Create Diapasons
            //
            var diapasons = new List<ZigZagDiapason>(zigzagIndexes.Length);
            var diapason = new ZigZagDiapason() { IndexStart = zigzagIndexes[0], IndexEnd = zigzagIndexes[0] };
            var crvNurbPointsCount = crvNurb.Points.Count;
            for (int i = 1; i < zigzagIndexes.Length; i++)
            {
                var index = zigzagIndexes[i];
                if (index == 0 || index == crvNurbPointsCount - 1) continue; // dont include last and first indexes

                // lets try to merge diapasons
                if (index == diapason.IndexEnd + 1
                    || index == diapason.IndexEnd + 2   // after this loop we extend Start and  End by 1 - so anyway we have to merge those diapasons
                    )
                {
                    diapason.IndexEnd = index;
                }
                else
                {
                    diapasons.Add(diapason);
                    diapason = new ZigZagDiapason() { IndexStart = index, IndexEnd = index };
                }
            }
            diapasons.Add(diapason);

            //Extends diapasons for 1 CP to make shure we removed all curves and zigzags
            foreach (var d in diapasons)
            {
                if (d.IndexStart >= 1) d.IndexStart--;  // include prev CP
                if (d.IndexEnd <= crvNurbPointsCount - 1 - 1) d.IndexEnd++;   // include next CP
            }

            foreach (var d in diapasons)
            {
                crvNurb.ClosestPoint(crvNurb.Points[d.IndexStart].Location, out d.StartIgnoreAtT);
                crvNurb.ClosestPoint(crvNurb.Points[d.IndexEnd].Location, out d.EndIgnoreAtT);
            }
            return diapasons;
        }

        // lower value - better smoothnes - for lines result will be 0
        private static double _ZigZagDeformations_Get3dCurveSmoothnest(NurbsCurve crv, Surface srf)
        {
            // 2d curve must provide srf
            var crvDimension = crv.Dimension;
            if (crvDimension == 2)
            {
                return 0;
            }

            //get 100 points on curve
            Point3d[] points;
            crv._DivideByCount_ThreadSafe(50, true, out points);
            if (points == null)
            {
                return 0;
            }

            // convert point to 3d if curve is 2d
            if (crvDimension == 2 && srf != null)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = srf.PointAt(points[i].X, points[i].Y);
                }
            }

            // get biggest angle between edges
            double biggestAngle = 0;

            for (int i2 = 1; i2 < points.Length - 1; i2++) // cycle from up-to-down and from down-to-up
            {
                var i1 = i2 - 1;
                var i3 = i2 + 1;

                var p1 = points[i1];
                var p2 = points[i2];
                var p3 = points[i3];

                double angleInDegree;
                if (_Point3d._TryGetAngle(p1, p2, p2, p3, out angleInDegree))
                {
                    biggestAngle = Math.Max(biggestAngle, angleInDegree);
                }
            }

            return biggestAngle;
        }


        public static NurbsCurve _ZigZagDeformations_TryRemove(this Curve crv, Surface srf = null)
        {
            var DEBUG = false;
            var crvNurb = crv._ToNurbsCurve();
            var zigzagIndexes = crvNurb._ZigZagDeformationsFind(srf);
            if (zigzagIndexes == null)
            {
                return null;
            }

            var diapasons = _ZigZag_GetDiapasons(crvNurb, zigzagIndexes);

            //
            // Try to remove zigzag 
            //
            var ress = new List<NurbsCurve>();
            var ressMethod = new List<string>();

            var res1 = _ZigZagDeformations_TryRemove__by_removing_diapasons(crvNurb, srf, zigzagIndexes, diapasons);
            if (res1 != null)
            {
                ress.Add(res1);
                ressMethod.Add("removing_diapasons");
            }

            if (crv.Dimension == 3)
            {
                var res2 = _ZigZagDeformations_TryRemove__by_sorting_controlPoints(crvNurb, srf, zigzagIndexes, diapasons);
                if (res2 != null)
                {
                    ress.Add(res2);
                    ressMethod.Add("sorting_controlPoints");
                }

                var res3 = _ZigZagDeformations_TryRemove__by_sorting_points3d(crvNurb, srf, zigzagIndexes, diapasons);
                if (res3 != null)
                {
                    ress.Add(res3);
                    ressMethod.Add("sorting_points3d");
                }
            }

            //
            // Return best result
            //
            if (ress.Count == 0)
            {
                return null;
            }
            if (ress.Count == 1)
            {
                return ress[0];
            }

            //DEBUG - log original curve
            //ress.Add(crvNurb);
            //ressMethod.Add("input_curve");

            var smoothnests = new List<double>();
            foreach (var fixedCrv in ress)
            {
                smoothnests.Add(_ZigZagDeformations_Get3dCurveSmoothnest(fixedCrv, srf));
            }

            if (DEBUG)log.temp("ZigZagDeformations   poins {0}", crvNurb.Points.Count);
            var bestIndex = 0;
            var bestSmoothnest = smoothnests[0];
            var bestMethod = ressMethod[0];
            for (int i = 0; i < smoothnests.Count; i++)
            {
                if (DEBUG) log.temp("                            Method {0}  -  smoothnests {1} ", ressMethod[i], smoothnests[i]);
                if (smoothnests[i] < bestSmoothnest)
                {
                    bestIndex = i;
                    bestSmoothnest = smoothnests[i];
                    bestMethod = ressMethod[i];
                }
            }

            return ress[bestIndex];
        }

        private static NurbsCurve _ZigZagDeformations_GetCurveFromPoints(NurbsCurve crv, Surface srf, Point3d[] points3d, bool simplifyCurve, List<int> indexes = null)
        {
            var pps = new List<Point3d>();
            if (indexes == null)
            {
                pps.AddRange(points3d);
            }
            else
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    if (indexes[i] != -1)
                    {
                        pps.Add(points3d[indexes[i]]);
                    }
                }
            }
            var c = Curve.CreateControlPointCurve(pps, 3);
            //var res1 = Curve.CreateInterpolatedCurve(pps, 3); - works bad - creates zigzags
            if (c != null)
            {
                if (crv.Dimension == 2)
                {
                    c = c._SetDimension(2);
                }

                // it is incorrect to fix control point here!
                //////////if (crvNurb.Dimension == 2 && srf != null)
                //////////{
                //////////    c = c._Fix2dContorlPoints(srf); // - shall we do fix 2d control points ??? - NO!!!
                //////////}
                if (simplifyCurve)
                {
                    c = c._Simplify(srf, true);
                }

                if (!c._ZigZagDeformationExists(srf))
                {
                    return c.ToNurbsCurve();
                }
            }
            return null;
        }

        private static NurbsCurve _ZigZagDeformations_TryRemove__by_sorting_points3d(NurbsCurve crv, Surface srf, int[] zigzagIndexes, List<ZigZagDiapason> diapasons)
        {
            // work only with 3d  curves
            var crvDimension = crv.Dimension;
            if (crvDimension == 2)
            {
                return null;
            }

            Point3d[] points;
            crv._DivideByCount_ThreadSafe(1000, true, out points);
            if (points == null)
            {
                return null;
            }

            // convert point to 3d (if curve is 2d)
            if (crvDimension == 2 && srf != null)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = srf.PointAt(points[i].X, points[i].Y);
                }
            }

            var indexes = _Point3d._Sort(points.ToList());
            var res =  _ZigZagDeformations_GetCurveFromPoints(crv, srf, points, true, indexes);
            return res;
        }


        private static NurbsCurve _ZigZagDeformations_TryRemove__by_sorting_controlPoints(NurbsCurve crv, Surface srf, int[] zigzagIndexes, List<ZigZagDiapason> diapasons)
        {
            NurbsCurve res = null;
            var points3d = crv._Locations3d();

            // set default all points are valid
            var indexes = new List<int>();
            for (int i = 0; i < points3d.Length; i++)
            {
                indexes.Add(i);
            }

            // clear bad control points
            for (int di = 0; di < diapasons.Count; di++)
            {
                var d = diapasons[di];
                for (int i = d.IndexStart + 1; i < d.IndexEnd; i++)
                {
                    indexes[i] = -1;
                }
            }

            //
            // v1  - just remove - at least we will have some result if v2 fails
            //
            var res1 = _ZigZagDeformations_GetCurveFromPoints(crv, srf, points3d, false, indexes);
            if (res1 != null) res = res1;


            //
            // v2  - sort bad control points - try get better result
            //
            for (int di = 0; di < diapasons.Count; di++)
            {
                var d = diapasons[di];
                var subPoints = new List<Point3d>();
                for (int i = d.IndexStart; i <= d.IndexEnd; i++)
                {
                    subPoints.Add(points3d[i]);
                }
                var subIndexesSorted = _Point3d._Sort(subPoints);
                for (int i = 0; i < d.IndexEnd - d.IndexStart + 1; i++)
                {
                    var index = d.IndexStart + i;
                    var indexSorted = d.IndexStart + subIndexesSorted[i];
                    indexes[index] = indexSorted;
                }
            }
            var res2 = _ZigZagDeformations_GetCurveFromPoints(crv, srf, points3d, false, indexes);
            if (res2 != null) res = res2;



            return res;
        }

        private static NurbsCurve _ZigZagDeformations_TryRemove__by_removing_diapasons(NurbsCurve crv, Surface srf, int[] zigzagIndexes, List<ZigZagDiapason> diapasons)
        {
            //
            // Split curve on segments
            //
            Point3d[] allCrvPoints;
            //Try1
            var divby = crv._GetDivBy(srf);
            var ts = crv._DivideByCount_ThreadSafe(divby, true, out allCrvPoints);
            if (allCrvPoints == null)
            {
                //Try2
                ts = crv._DivideByCount_ThreadSafe(100, true, out allCrvPoints);
                if (allCrvPoints == null)
                {
                    //Try3
                    var length = crv._Get3dLength(srf);
                    if (length < 0.001)
                    {
                        allCrvPoints = new[] { crv.PointAtStart, crv.PointAtEnd };
                        ts = new[] { crv.Domain.T0, crv.Domain.T1 };
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            //
            // Fix curve by extending diapasons until we get fixed curve
            //
            NurbsCurve res = null;

            var diapasonExtensionMax = crv.Domain.Length * 0.05; // 5%
            double diapasonExtension = 0;
            while (diapasonExtension <= diapasonExtensionMax)
            {
                //Layers.Debug.AddPoint(crvNurb._PX3d(diapasons[0].StartIgnoreAtT - diapasonExtension, srf), Color.Red);
                //Layers.Debug.AddPoint(crvNurb._PX3d(diapasons[0].EndIgnoreAtT + diapasonExtension, srf), Color.Red);
                var crvFixed = _ZigZagDeformations_Remove(crv, srf, ts, diapasons, diapasonExtension);
                if (crvFixed == null) break; //break tries - return what we found till now :(
                // if no more zigzags - set result

                if (!crvFixed._ZigZagDeformationExists(srf))
                {
                    res = crvFixed;
                    break;
                }

                // if we here - zigzags still exists after fix - so lets increaese cut length and lets try once again 
                diapasonExtension += crv.Domain.Length * 0.01;  // 1%
            }

            return res;
        }


        private static NurbsCurve _ZigZagDeformations_Remove(this NurbsCurve crvNurb, Surface srf, double[] ts, List<ZigZagDiapason> diapasons, double diapasonExtension)
        {
            //
            // Filter segmens - remove those segments what is close to zigzag control points
            // 
            var filteredCrvPoints = new List<Point3d>();
            filteredCrvPoints.Add(crvNurb.PointAt(ts[0])); // add 1-st point (PointAtStart)
            for (int i = 1; i < ts.Length - 1; i++)
            {
                var t = ts[i];
                var isCloseToZigzag = false;
                foreach (var d in diapasons)
                {
                    var minT = d.StartIgnoreAtT - diapasonExtension;
                    var maxT = d.EndIgnoreAtT + diapasonExtension;
                    if (minT <= t && t <= maxT)
                    {
                        isCloseToZigzag = true;
                        break;
                    }
                }
                if (!isCloseToZigzag)
                {
                    filteredCrvPoints.Add(crvNurb.PointAt(t));
                    //Layers.Debug.AddPoint(crvNurb._PX3d(t, srf), Color.Green);
                }
                else
                {
                    //Layers.Debug.AddPoint(crvNurb._PX3d(t, srf), Color.Red);
                }


                //DEBUG
                //var p3d = crvNurb.PointAt(t);
                //if (crvNurb.Dimension == 2)
                //{
                //    p3d = srf.PointAt(crvNurb.PointAt(t).X, crvNurb.PointAt(t).Y);
                //}
                //Layers.Debug.AddPoint(p3d, isCloseToZigzag ? Color.Red : Color.Blue);
                //ENDDEBUG
            }
            filteredCrvPoints.Add(crvNurb.PointAt(ts[ts.Length - 1]));// add last point (PointAtEnd)


            //DEBUG
            //log.temp("Adding debig point for ZigZag");
            //Viewport.Redraw(RhinoDoc.ActiveDoc, "Layers.Debug.AddPoints(filteredCrvPoints)");
            //foreach (var p in filteredCrvPoints)
            //{
            //    var p3d = p;
            //    if (crvNurb.Dimension == 2)
            //    {
            //        p3d = srf.PointAt(p.X, p.Y);
            //    }
            //    Layers.Debug.AddPoint(p3d, Color.Blue);
            //}
            //ENDDEBUG

            //
            // Approximate and simplfy new crv
            //
            var res = Curve.CreateControlPointCurve(filteredCrvPoints, 3);
            //var res = Curve.CreateInterpolatedCurve(filteredCrvPoints, 3); - works bad - creates zigzags
            if (res != null)
            {
                if (crvNurb.Dimension == 2)
                {
                    res = res._SetDimension(2);
                }

                // it is incorrect to fix control point here!
                //////////if (crvNurb.Dimension == 2 && srf != null)
                //////////{
                //////////    res = res._Fix2dContorlPoints(srf); // - shall we do fix 2d control points ??? - NO!!!
                //////////}


                res = res._Simplify(srf, true);
            }

            return res == null ? null : res._ToNurbsCurve();
        }

        /// <summary>
        /// Remove zig-zags from curve
        /// WARNING: could have problems in Singularity
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="srf"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool _ZigZagDeformations_TryRemove_old(this Curve crv, Surface srf, double tol, out NurbsCurve newCrvNurb)
        {
            newCrvNurb = null;

            NurbsCurve crvNurb = crv._ToNurbsCurve();

            // Linear crv's can't have deformations - so return original curve
            if (crvNurb.Degree == 1)
            {
                return false;
            }

            var zigzagIndexes = crvNurb._ZigZagDeformationsFind();
            if (zigzagIndexes == null)
            {
                return false;
            }

            //
            // Project all points on surface
            //
            List<SurfacePoint2d3dSrf> srfPoints;
            if (!crvNurb._TryProjectOnSrf(srf, tol, out srfPoints))
            {
                return false;
            }
            var savesrfPointsCount = srfPoints.Count;

            //
            // Remove wrong Control Points
            //
            // remove from left to right
            RemoveZigZagIndexes(ref srfPoints, 5);
            // remove from right to left
            srfPoints.Reverse();
            RemoveZigZagIndexes(ref srfPoints, 5);
            // back to original order
            srfPoints.Reverse();

            //
            // Apply fix
            //
            if (srfPoints.Count == savesrfPointsCount)
            {
                //
            }

            // deformation present - recreate curve base on new control points (without deformed ones)
            var newPoints = srfPoints.Select(o => o.LocationSrf);
            var res = srf.InterpolatedCurveOnSurface(newPoints, tol);
            newCrvNurb = res;
            return true;
        }

        private static bool RemoveZigZagIndexes(ref List<SurfacePoint2d3dSrf> srfPoints, int countToRemoveAlongBadIndex)
        {
            var points = srfPoints.Select(o => o.LocationSrf).ToArray();
            var zigZagsIndexes = _ZigZagDeformationsFind(points);
            if (zigZagsIndexes == null) return false;
            for (int i = zigZagsIndexes.Length - 1; i >= 0; i--)
            {
                var index = zigZagsIndexes[i];
                var maxToRemove = Math.Min(countToRemoveAlongBadIndex, srfPoints.Count - index
                    - 1 // dont remove last index
                    );
                srfPoints.RemoveRange(index, maxToRemove);
            }
            return true;
        }
    }
}
