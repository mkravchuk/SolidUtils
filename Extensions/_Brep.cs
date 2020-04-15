using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class _Brep
    {
        #region OLD CODE
        /* 
         * 
         * RecreateTrimsFromEdges
         * 
         * 
        public static Brep _RecreateTrimsFromEdges(this Brep brep, int faceIndex, bool fixDeformedEdges, double tol_fixDeformedEdges)
        {
            var newBrep = new Brep();
            var srf = (Surface) brep.Surfaces[brep.Faces_ThreadSafe()[faceIndex].SurfaceIndex].Duplicate();
            var ssingulars = new SurfaceSingulars(srf);
            //newBrep = brep.Faces[faceIndex].DuplicateFace(false);
            //newBrep.Faces[0].RebuildEdges(10, true, false);
            //return newBrep;

            //srf = srf._IncreaseSurfaceDensity(10);
            //srf = srf.Reverse(0);
            var newface = newBrep.Faces_ThreadSafe().Add(newBrep.AddSurface(srf));

            foreach (var loop in brep.Faces_ThreadSafe()[faceIndex].Loops_ThreadSafe())
            {
                var newLoop = newBrep.Loops_ThreadSafe().Add(loop.LoopType, newface);
                bool prevVertexDefined = false;
                Point3d ps_start_of_loop = Point3d.Unset;
                Point3d ps_prev = Point3d.Unset;
                Point3d pe_prev = Point3d.Unset;
                const double TOLERANCE = 0.001;
                //const double TOLERANCE = 1;                

                //
                // Sort trims - so next trim will continue previous
                //
                List<NurbsCurve> curves2d;
                List<int> singulars;
                List<BrepTrim> trimsSorted;
                var curves3d = _PullTrimEdgesToSurface(srf, brep.Faces_ThreadSafe()[faceIndex], ssingulars, loop.LoopType, loop.Trims_ThreadSafe(), out curves2d, out singulars, out trimsSorted, fixDeformedEdges, tol_fixDeformedEdges);
                //var p2after = new List<SurfacePoint>();
                //foreach (var t in curves2d)
                //{
                //    p2after.Add(new SurfacePoint(t.PointAtStart));
                //    p2after.Add(new SurfacePoint(t.PointAtEnd));
                //}


                //
                // Iterate throught each trim and recreate it
                //
                for (int trimIndex = 0; trimIndex < trimsSorted.Count; trimIndex++)
                {
                    //var trim = trimsSorted[trimIndex];

                    var edge = curves3d[trimIndex];

                    //
                    // Adding curves (prepare)
                    //

                    //b.Edges[edge_index].Tolerance = TOLERANCE;


                    var ps = edge.PointAtStart;
                    var pe = edge.PointAtEnd;
                    var reverseEdge = false;
                    if (prevVertexDefined)
                    {
                        var psDistance = pe_prev.DistanceTo(ps);
                        var peDistance = pe_prev.DistanceTo(pe);

                        // reverse edge if needed
                        if (psDistance > peDistance)
                        {
                            reverseEdge = true;
                            if (!edge.Reverse())
                            {
                                throw new Exception("Exception:  FixTrim - Failed to reverse edge");
                            }

                            ps = edge.PointAtStart;
                            pe = edge.PointAtEnd;
                            psDistance = pe_prev.DistanceTo(ps);
                            peDistance = pe_prev.DistanceTo(pe);
                        }
                        if (psDistance > TOLERANCE)
                        {
                            throw new Exception("Exception:  FixTrim - peDistance > TOLERANCE! Trim index =  " +
                                                trimIndex);
                        }
                        ps = pe_prev;

                        if (trimIndex == trimsSorted.Count - 1)
                        {
                            psDistance = pe.DistanceTo(ps_start_of_loop);
                            if (psDistance > TOLERANCE)
                            {
                                throw new Exception("Exception:  FixTrim - psDistance > TOLERANCE! Trim index =  end");
                            }
                            pe = ps_start_of_loop;
                        }
                    }
                    else
                    {
                        ps_start_of_loop = ps;
                    }

                    //
                    // Adding curves
                    //

                    BrepVertex vs = newBrep.Vertices.SingleOrDefault(o => o.Location == ps);
                    if (vs == null) vs = newBrep.Vertices.Add(ps, TOLERANCE);

                    BrepVertex ve = newBrep.Vertices.SingleOrDefault(o => o.Location == pe);
                    if (ve == null) ve = newBrep.Vertices.Add(pe, TOLERANCE);

                    ps_prev = ps;
                    pe_prev = pe;
                    prevVertexDefined = true;

                    int edge_curve_index = newBrep.AddEdgeCurve(edge);
                    var newEdge = newBrep.Edges.Add(vs, ve, edge_curve_index, edge.Domain, TOLERANCE);

                    //
                    // Add TrimCurve
                    //
                    var trimCurve = curves2d[trimIndex];
                    var trim_curve_index = newBrep.AddTrimCurve(trimCurve);
                    var newTrim = newBrep.Trims.Add(newEdge, false, newLoop, trim_curve_index);
                    newTrim.TrimType = BrepTrimType.Boundary;
                    newTrim.IsoStatus = srf.IsIsoparametric(newTrim);
                    // we must here recalculate IsoStatus since we have changed u and v to match prev and next trims
                    newTrim.SetTolerances(TOLERANCE, TOLERANCE);


                    //
                    // Add Singularity
                    //

                    if (singulars.Contains(trimIndex))
                    {
                        var singIndexStart = trimIndex;
                        var singIndexEnd = trimIndex + 1;
                        if (singIndexEnd >= trimsSorted.Count)
                        {
                            singIndexEnd = 0;
                        }
                        var singPointStart = curves2d[singIndexStart].Points[curves2d[singIndexStart].Points.Count - 1].Location;
                        var singPointEnd = curves2d[singIndexEnd].Points[0].Location;

                        var point2d = new Point2d[] {new Point2d(singPointStart), new Point2d(singPointEnd)};
                        var crv2d = new LineCurve(point2d[0], point2d[1])._ToNurbsCurve();
                        //newBrep.Trims[2].SetEndPoint(new Point3d(point2d[0].X, point2d[0].Y, 0));
                        //newBrep.Trims[0].SetStartPoint(new Point3d(point2d[1].X, point2d[1].Y, 0));
                        var curve2d_index = newBrep.Curves2D.Add(crv2d);
                        var singVertexIndex = srf.PointAt(singPointStart.X, singPointStart.Y)._GetVertexIndex(newBrep.Vertices, 100); //tol is very high coz we must find vertex anyway from the list
                        var newTrim2 = newBrep.Trims.AddSingularTrim(newBrep.Vertices[singVertexIndex], newLoop, IsoStatus.None, curve2d_index);
                        var iso = srf.IsIsoparametric(newTrim2);
                        newTrim2.IsoStatus = iso;
                    }

                }
            }

            //
            // DEBUG
            //
            //var point2d = new Point2d[] { new Point2d(1, 0), new Point2d(1, 1) };
            //var crv2d = new LineCurve(point2d[0], point2d[1]);
            //newBrep.Trims[2].SetEndPoint(new Point3d(point2d[0].X, point2d[0].Y, 0));
            //newBrep.Trims[0].SetStartPoint(new Point3d(point2d[1].X, point2d[1].Y, 0));
            //var curve2d_index = newBrep.Curves2D.Add(crv2d);
            //var newTrim2 = newBrep.Trims.AddSingularTrim(newBrep.Vertices[0], newBrep.Loops[0], IsoStatus.None, curve2d_index);
            //var iso = srf.IsIsoparametric(newTrim2);
            //newTrim2.IsoStatus = iso;

            //newBrep.Curves2D[2].SetEndPoint(new Point3d(point2d[0].X, point2d[0].Y, 0));
            //newBrep.Curves2D[0].SetStartPoint(new Point3d(point2d[1].X, point2d[1].Y, 0));


            //var p2after2dCurves = new List<SurfacePoint>();
            //foreach (var t in newBrep.Curves2D)
            //{
            //    p2after2dCurves.Add(new SurfacePoint(t.PointAtStart));
            //    p2after2dCurves.Add(new SurfacePoint(t.PointAtEnd));
            //}
            //var p2after2dTrims = new List<SurfacePoint>();
            //foreach (var t in newBrep.Trims)
            //{
            //    p2after2dTrims.Add(new SurfacePoint(t.PointAtStart));
            //    p2after2dTrims.Add(new SurfacePoint(t.PointAtEnd));
            //}
            //var p2after3dCurves = new List<Point3d>();
            //foreach (var c in newBrep.Curves3D)
            //{
            //    p2after3dCurves.Add(c.PointAtStart);
            //    p2after3dCurves.Add(c.PointAtEnd);
            //}

            newBrep.Compact();
            return newBrep;
        }

        private static List<NurbsCurve> _PullTrimEdgesToSurface(Surface srf, BrepFace face, SurfaceSingulars ssingulars, BrepLoopType loopType, BrepTrimList srfTrims, out List<NurbsCurve> curves2d, out List<int> singulars, out List<BrepTrim> trimsSorted, bool fixDeformedEdges, double tol_tol_fixDeformedEdges)
        {
            List<bool> trimsSortedRaw_ReversedFlag;
            bool reverseCurveDirection;
            var trimsSortedRaw = srfTrims._SortByCrvs3d(out trimsSortedRaw_ReversedFlag, out reverseCurveDirection);
            if (face.OrientationIsReversed)
            {
                //reverseCurveDirection = !reverseCurveDirection;
            }
            var curves3d = new List<NurbsCurve>();
            curves2d = new List<NurbsCurve>();


            int debugIndexEdge = 0;
            int debugIndexEdge2 = 0;
            int debugIndexTrim = 0;
            int debugIndexMAX = 90;

            //1.2 Trimmed NURBS
            //(not yet implemented)
            //The trimming curve specifies a NURBS-curve that limits the NURBS surface in order to create NURBS surfaces that contain holes or have smooth boundaries. Trimming curves are curves in the parametric space of the surface. An implementation approach can be based on the OpenGL trimming definition:
            //A trimming region is defined by a set of closed trimming loops in the parameter space of a surface. When a loop is oriented counter-clockwise, the area within the loop is retained, and the part outside is discarded. When the loop is oriented clockwise, the area within the loop is discarded, and the rest is retained. Loops may be nested, but a nested loop must be oriented oppositely from the loop that contains it. The outermost loop must be oriented counter-clockwise.
            //A trimming loop consists of a connected sequence of NURBS curves and piece wise linear curves. The last point of every curve in the sequence must be the same as the first point of the next curve, and the last point of the last curve must be the same as the first point of the first curve. Self intersecting curves are not allowed.
            //The following Nodes sketch a trimmed NURBS surface extension. 
            var trimIndexes = new List<int>();
            if (reverseCurveDirection)
            {
                for (var i = trimsSortedRaw.Count - 1; i >= 0; i--) trimIndexes.Add(i);
            }
            else
            {
                for (var i = 0; i < trimsSortedRaw.Count; i++) trimIndexes.Add(i);
            }

            trimsSorted = new List<BrepTrim>();
            foreach (var trimIndex in trimIndexes)
            {
                var trim = trimsSortedRaw[trimIndex];
                trimsSorted.Add(trim);

                var reverseCurveDirectionLocal = reverseCurveDirection;
                if (trimsSortedRaw_ReversedFlag[trimIndex])
                {
                    reverseCurveDirectionLocal = !reverseCurveDirectionLocal;
                }

                NurbsCurve crv3d = null;
                NurbsCurve crv2d = null;
                trim._RecreateCurves(srf, ssingulars, reverseCurveDirectionLocal, fixDeformedEdges, tol_tol_fixDeformedEdges,
                    true, out crv3d, out crv2d);
                curves3d.Add(crv3d);
                curves2d.Add(crv2d);


                //DEBUG
                if (curves3d.Count >= 2)
                {
                    var i1 = curves3d.Count - 2;
                    var i2 = curves3d.Count - 1;
                    var i1Reversed = trimsSortedRaw_ReversedFlag[trimIndexes[i1]];
                    var i2Reversed = trimsSortedRaw_ReversedFlag[trimIndexes[i2]];
                    var trim1 = trimsSortedRaw[trimIndexes[i1]];
                    var trim2 = trimsSortedRaw[trimIndexes[i2]];
                    var trim1edgeEndPoint = !i1Reversed ? trim1.Edge.PointAtEnd : trim1.Edge.PointAtStart;
                    var trim2edgeStartPoint = !i2Reversed ? trim2.Edge.PointAtStart : trim2.Edge.PointAtEnd;
                    var distTrim = trim1edgeEndPoint.DistanceTo(trim2edgeStartPoint);
                    log.debug(g.IssueFixer_FaceProblems, "Distance:  {0}-{1} = {2}", trim1._GUIEdgeNum(), trim2._GUIEdgeNum(), distTrim._ToStringX(5));
                }
            }

            //
            // Fix trim Start and End Points
            //           
            srf._SnapCurves2d(curves2d, out singulars);
            SortCurvesAlongSingularity(ref curves3d, ref curves2d, ref singulars, ref trimsSorted);

            //Logger.log("===========" + reverseCurveDirection);
            //var tempindex = 0;
            //foreach (var c in curves2d)
            //{
            //    tempindex++;
            //    log.temp("-- #" + tempindex);
            //    foreach (var p in c.Points)
            //    {
            //        log.temp("{0:0.00}, {1:0.00}", p.Location.X, p.Location.Y);
            //    }
            //}

            // enshure orientation of curves are clockwise - old - we use new method at the begining of this method
            //var jC = Curve.JoinCurves(curves3d, 100);// we must tell tollerance to be independed from document tolerance
            //if (jC.Length == 1)
            //{
            //    var orientation = jC[0].ClosedCurveOrientation(face._GetNormal());
            //    var needOrientation = (loopType == BrepLoopType.Outer)
            //        ? CurveOrientation.Clockwise
            //        : CurveOrientation.CounterClockwise;
            //    if (orientation != needOrientation
            //        && reverseCurveDirection == false // to avoid recursive self call
            //        )
            //    {

            //        // return self recurs result with reversed curve directions
            //        return _PullTrimEdgesToSurface(srf, face, loopType, srfTrims, out curves2d, out singulars, out trimsSorted, true, fixDeformedEdges, tol_tol_fixDeformedEdges);
            //    }
            //}

            return curves3d;
        }

        private static void SortCurvesAlongSingularity(ref List<NurbsCurve> curves3D, ref List<NurbsCurve> curves2D,
            ref List<int> singulars, ref List<BrepTrim> trimsSorted)
        {
            if (singulars.Count != 1)
            {
                return;
            }

            var singIndex = singulars[0];
            var lastIndex = curves3D.Count - 1;
            var singIndexShouldBe = lastIndex;
            while (singIndex < singIndexShouldBe)
            {
                curves3D.Insert(0, curves3D[lastIndex]);
                curves3D.RemoveAt(lastIndex + 1);
                curves2D.Insert(0, curves2D[lastIndex]);
                curves2D.RemoveAt(lastIndex + 1);
                trimsSorted.Insert(0, trimsSorted[lastIndex]);
                trimsSorted.RemoveAt(lastIndex + 1);
                singIndex++;
            }
            singulars[0] = singIndexShouldBe;
        }

        */
        #endregion

        public static string _GetFaceName(this Brep brep, string objectName, BrepFace face, bool shortNameIfPossible = true)
        {
            return face._GetName(objectName, brep, shortNameIfPossible);
        }

        public static bool _IsValidWithLog(this Brep brep, out string logtext)
        {
            logtext = "";
            return true;
            //new ComponentIndex().ComponentIndexType
            //class ComponentIndex
        }


        /// <summary>
        /// Returns same face but with better trimming curves
        /// Sometimes after reversing trims we have opposite side and crvs2d and crv3d doesnt really follow face contours
        /// </summary>
        /// <param name="newBrep"></param>
        /// <param name="debuginfo">just for debuging issues that hardly happend in this method</param>
        /// <param name="opposite">Choise oppossite surface</param>
        /// <returns></returns>
        public static Brep _TryGetReversedBrepWithBetterTrims(this Brep newBrep, string debuginfo, bool opposite = false)
        {
            Brep res = null;
            try
            {
                //log.info(g.IssueFixer, "_TryGetReversedBrepWithBetterTrims    " + debuginfo);
                // v0 - simple
                //res = newBrep._GetReversedBrepWithBetterTrims(0.000001, opposite);
                // v1 - wait for some time and fail if this action take to much time
                if (!ForeachParallel.RunActionWithTimeout(4000, "GetReversedBrepWithBetterTrims", () => res = newBrep._GetReversedBrepWithBetterTrims(0.000001, opposite)))
                {
                    if (!ForeachParallel.RunActionWithTimeout(1000, "GetReversedBrepWithBetterTrims", () => res = newBrep._GetReversedBrepWithBetterTrims(0.001, opposite)))
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                // nothing                
                res = null;
            }
            return res;
        }

        [HandleProcessCorruptedStateExceptions]
        public static Brep _GetReversedBrepWithBetterTrims(this Brep newBrep, double tol = 0.000001, bool opposite = false)  // tolerance is important to set high - to make edges correct (check FaceInvalid\_Mixed\IKEA_PYRO_3321_122.3dm)
        {
            if (newBrep == null) return null;
            if (newBrep.Faces[0].Loops.Count != 1) return null;

            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return null;


            //
            // Get all curves from trim
            //
            var crvs1 = newBrep.Loops_ThreadSafe()[0]._GetJoinedTrims3d();
            if (crvs1 != null && crvs1.Length == 1 && newBrep.Faces.Count == 1)
            {
                var b = newBrep.Surfaces[0]._Split_ThreadSafe(crvs1, tol);
                if (b != null
                    && b.Faces.Count == 2 // brep must be devided by 2 faces after split
                    && b.Faces_ThreadSafe()[0].Loops.Count == 1 // we work only with 1 loop faces
                    && b.Faces_ThreadSafe()[1].Loops.Count == 1 // we work only with 1 loop faces
                    )
                {
                    //DEBUG
                    //doc.Objects.AddBrep(b); 

                    // we have 2 good faces!
                    // now lets choise 1
                    var b1 = b.Faces_ThreadSafe()[0].DuplicateFace(true);
                    var b2 = b.Faces_ThreadSafe()[1].DuplicateFace(true);

                    //
                    //ver 1 - closest centroid
                    //
                    //var meshBN = Mesh.CreateFromBrep(newBrep);
                    //var centroidBN = AreaMassProperties.Compute(meshBN).Centroid;
                    //var meshB1 = Mesh.CreateFromBrep(b1);
                    //var centroidB1 = AreaMassProperties.Compute(meshB1).Centroid;
                    //var meshB2 = Mesh.CreateFromBrep(b2);
                    //var centroidB2 = AreaMassProperties.Compute(meshB2).Centroid;
                    //var dist1 = centroidBN.DistanceTo(centroidB1);
                    //var dist2 = centroidBN.DistanceTo(centroidB2);
                    //var loopLength = newBrep.Faces_ThreadSafe()[0].Loops_ThreadSafe()[0]._GetLength3d();
                    //var loopLength1 = b1.Loops_ThreadSafe()[0]._GetLength3d();
                    //var loopLength2 = b2.Loops_ThreadSafe()[0]._GetLength3d();

                    //// choise face what has better mutch by mesh centroid
                    //var d1isCloser = dist1 < dist2;
                    //if (opposite) d1isCloser = !d1isCloser;

                    //var distBest = d1isCloser ? dist1 : dist2;
                    //var bbest = d1isCloser ? b1 : b2;
                    //var loopLengthBest = d1isCloser ? loopLength1 : loopLength2;
                    //if (distBest < 1 // if mesh is in same position as for original face
                    //   &&  loopLengthBest < loopLength // new loop length should be smaller from original
                    //    && (loopLength - loopLengthBest) / loopLength > 0.1 // decrease of loop length should be at least 10%
                    //    )             
                    //{
                    //    return bbest;
                    //}

                    //
                    // ver 2 - smallest area
                    //
                    var meshB1 = Mesh.CreateFromBrep(b1);
                    var areaB1 = AreaMassProperties.Compute(meshB1).Area;
                    var meshB2 = Mesh.CreateFromBrep(b2);
                    var areaB2 = AreaMassProperties.Compute(meshB2).Area;

                    var smallerBrep = (areaB1 < areaB2)
                        ? b1
                        : b2;
                    //smallerBrep.Faces[0].OrientationIsReversed = newBrep.Faces[0].OrientationIsReversed;
                    return smallerBrep;

                }
            }
            return null;
        }
    }
}
