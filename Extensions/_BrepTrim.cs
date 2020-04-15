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
    public static class _BrepTrim
    {
        public static bool _NeedFixClosedCurve(this BrepTrim trim)
        {
            return ((Curve)trim)._NeedFixClosedCurve()
                   || (trim.Edge != null && trim.Edge._NeedFixClosedCurve());
        }


        public static string _GUIEdgeName(this BrepTrim trim)
        {
            return "Edge " + trim._GUIEdgeNum();
        }
        public static string _GUIEdgeNum(this BrepTrim trim)
        {
            var num = Shared.GUIComponentNum(trim.TrimIndex); // in GUI we show starting from 1 or from 0
            return num._ToStringFastSharp();
        }



        /// <summary>
        /// Get index of trim in loop to which it belongs.
        /// Dont use TrimIndex, since it is global index: index of trim in all trims from Brep.
        /// </summary>
        /// <param name="trim"></param>
        /// <param name="loopTrims"></param>
        /// <returns></returns>
        public static int _IndexInLoop(this BrepTrim trim, BrepTrimList loopTrims = null)
        {
            var trims = loopTrims ?? trim.Loop.Trims_ThreadSafe();
            for (int i = 0; i < trims.Count; i++)
            {
                if (trims[i].TrimIndex == trim.TrimIndex)
                {
                    return i;
                }
            }
            throw new Exception("Cannot find trimindex in mehod: _BrepTrim._IndexInLoop(trim)");
        }

        /// <summary>
        /// Find prev and next trim in same loop.
        /// 
        /// </summary>
        /// <param name="trim"></param>
        /// <param name="prevTrim"></param>
        /// <param name="nextTrim"></param>
        /// <param name="loopTrims">Provide this parameter is possible. Speed optimization. If it is not provided - will be calculcated, what takes some time.</param>
        public static void _GetPrevNextTrims(this BrepTrim trim, out BrepTrim prevTrim, out BrepTrim nextTrim, BrepTrimList loopTrims = null)
        {
            var trims = loopTrims ?? trim.Loop.Trims_ThreadSafe();
            for (int i = 0; i < trims.Count; i++)
            {
                if (trims[i].TrimIndex == trim.TrimIndex)
                {
                    var iPrev =  (i != 0) ? i - 1 : trims.Count - 1;
                    var iNext =  (i != trims.Count - 1) ? i + 1 : 0;
                    prevTrim = trims[iPrev];
                    nextTrim = trims[iNext];
                    return;
                }
            }
            throw new Exception("Cannot find trimindex in mehod: _BrepTrim._GetPrevNextTrims(trim, out prevTrim, out nextTrim)");
        }

        /// <summary>
        /// Fix control points of trim
        /// </summary>
        /// <param name="trim"></param>
        /// <param name="singulars"></param>
        /// <param name="roundStart">Round value of singular axis (if surface is singular on V  - then round first value of V)</param>
        /// <param name="roundEnd">Round value of singular axis (if surface is singular on V  - then round last value of V)</param>
        /// <returns></returns>
        public static NurbsCurve _FixContorlPoints(this BrepTrim trim, SurfaceSingulars singulars = null, bool roundStart = false, bool roundEnd = false)
        {
            var srf = trim._Srf();
            var trimNurb = trim._ToNurbsCurve();
            // v1 - full
            var res =  trimNurb._Fix2dContorlPoints(srf, singulars, trim.Edge, roundStart, roundEnd);
            var newtrim = res._ZigZagDeformations_TryRemove(srf);
            //if (newtrim != null)
            //{
            //    res = newtrim;
            //}

            // v2 - only zigzag
            //var res = trimNurb;
            //var newtrim = trimNurb._ZigZagDeformations_TryRemove(srf);
            //if (newtrim != null)
            //{
            //    res = newtrim;
            //}
            //res =  res._Fix2dContorlPoints(srf, singulars, trim.Edge, roundStart, roundEnd);

            return res;
        }

        public static Surface _Srf(this BrepTrim trim)
        {
            return trim.Brep.Surfaces[trim.Face.SurfaceIndex];
        }

        public static NurbsCurve _Simplify(this BrepTrim trim)
        {
            return trim._Simplify(trim._Srf());
        }


        public static bool _IsSameDirectionToEdge(this BrepTrim trim)
        {
            if (trim.TrimType == BrepTrimType.Singular) return false;
            var srf = trim._Srf();
            var trimPointAtStart = trim.PointAtStart;
            var trimPointAtEnd = trim.PointAtEnd;
            var trimT0srf = srf.PointAt(trimPointAtStart.X, trimPointAtStart.Y);
            var trimT1srf = srf.PointAt(trimPointAtEnd.X, trimPointAtEnd.Y);
            var Crv3d = trim.Edge;
            var Domain = Crv3d.Domain;
            var edgeT03d = Crv3d.PointAt(Domain.T0);
            var edgeT13d = Crv3d.PointAt(Domain.T1);
            return trim._IsSameDirectionToEdge(srf, trimT0srf, trimT1srf, edgeT03d, edgeT13d, false);
        }

        public static bool _IsSameDirectionToEdge(this BrepTrim trim, Surface srf,
            Point3d trimT03d, Point3d trimT13d,
            Point3d edgeT03d, Point3d edgeT13d, bool validateTrimType = true)
        {
            if (validateTrimType) // speed optimization - avoiding call to UnsafeNativeMethods.ON_BrepTrim_Type
            {
                if (trim.TrimType == BrepTrimType.Singular) return false;
            }
            return _Vector3d._IsSameDirection(trimT03d, trimT13d, edgeT03d, edgeT13d);

            //            ON_BrepTrim& 
            //ON_Brep::NewTrim( ON_BrepEdge& edge, ON_BOOL32 bRev3d, int c2i )
            //{
            //  m_is_solid = 0;
            //  ON_BrepTrim& trim = NewTrim( c2i );
            //  trim.m_ei = edge.m_edge_index;
            //  edge.m_ti.Append(trim.m_trim_index);
            //  trim.m_vi[0] = edge.m_vi[bRev3d?1:0];
            //  trim.m_vi[1] = edge.m_vi[bRev3d?0:1];
            //  trim.m_bRev3d = bRev3d?true:false;
            //  return trim;
            //}
        }

        public static int _StartVertexIndex(this BrepTrim trim, double tol = 10, bool throwExceptionIfNotFound = true)
        {
            if (trim.Edge != null)
            {
                return trim.Edge._GetStartVertex().VertexIndex;
            }
            var point = trim.Face.PointAt(trim.PointAtStart.X, trim.PointAtStart.Y);
            return point._GetVertexIndex(trim.Brep.Vertices, tol, throwExceptionIfNotFound);
        }

        public static int _EndVertexIndex(this BrepTrim trim, double tol = 10, bool throwExceptionIfNotFound = true)
        {
            if (trim.Edge != null)
            {
                return trim.Edge._GetEndVertex().VertexIndex;
            }
            var point = trim.Face.PointAt(trim.PointAtEnd.X, trim.PointAtEnd.Y);
            return point._GetVertexIndex(trim.Brep.Vertices, tol, throwExceptionIfNotFound);
        }

        #region OLD CODE
        /* OLD CODE
        public static void _RecreateCurves(this BrepTrim trim, Surface srf, SurfaceSingulars ssingulars, bool reverseCurveDirection, bool fixDeformedEdges, double tol_tol_fixDeformedEdges, bool recreate3dCurves, out NurbsCurve crv3d, out NurbsCurve crv2d)
        {
            if (recreate3dCurves)
            {
                crv3d = trim._PullEdgeToSurface(srf, ssingulars, reverseCurveDirection, fixDeformedEdges, tol_tol_fixDeformedEdges);

                ////DEBUG
                //foreach (var l in crv3d.Points)
                //{
                //    debugIndexEdge2++;
                //    string text = "#" + debugIndexEdge2;
                //    AddDebugPoint(Doc, text, l.Location, Color.Gold); //debug
                //}

                //
                // Create 2d curve (trim) from new edge (that is already projected on surface)
                // High interpolation of 500 points makes mistakes almost unvisible
                //                
                crv2d = srf._Convert3dCurveTo2d(crv3d, ssingulars, trim);
            }
            else
            {
                crv3d = trim.Edge._ToNurbsCurve();
                crv2d = trim._ToNurbsCurve();

                if (reverseCurveDirection)
                {
                    crv3d.Reverse();
                }

                // Reverse 2d crv if needed
                var trimStartPoint = srf.PointAt(trim.PointAtStart.X, trim.PointAtStart.Y);
                var trimEndPoint = srf.PointAt(trim.PointAtEnd.X, trim.PointAtEnd.Y);
                if (crv3d.PointAtStart.DistanceTo(trimStartPoint) > crv3d.PointAtStart.DistanceTo(trimEndPoint))
                {
                    crv2d.Reverse();
                }
            }
        }

        public static NurbsCurve _PullEdgeToSurface(this BrepTrim trim, Surface srf, SurfaceSingulars ssingulars, bool reverseCurveDirection, bool fixDeformedEdges, double tol_tol_fixDeformedEdges)
        {
            //
            // Project Edge to surface, and create new Edge from control points (edge devided by 100 points)
            //
            Curve edge = trim.Edge;
            NurbsCurve crv3d = null;

            if (fixDeformedEdges)
            {
                NurbsCurve newedge;
                if (edge._ZigZagDeformations_TryRemove_old(srf, tol_tol_fixDeformedEdges, out newedge))
                {
                    //log.temp("trim #{0} had zigzag ", trimIndex+1);
                    edge = newedge;
                }
            }
            int DIVIDE_BY_COUNT_I = Math.Min(Math.Max(Convert.ToInt32(edge._GetLength_ThreadSafe() / 0.01), 10), 100);
            if (edge.Degree == 1)
            {
                DIVIDE_BY_COUNT_I = 10;
            }

            Point3d[] edgeDevidedPoints;
            edge.DivideByCount(DIVIDE_BY_COUNT_I, true, out edgeDevidedPoints);
            if (edgeDevidedPoints != null)
            {
                if (reverseCurveDirection)
                {
                    edgeDevidedPoints = edgeDevidedPoints.Reverse().ToArray();
                }
                var edgeDevidedUV = srf._ClosestPoints(edgeDevidedPoints);


                //DEBUG
                //Logger.log("+++");
                //foreach (var p in edgeDevidedUV)
                //{
                //    debugIndexEdge++;
                //    //bool inside = trim.Brep.IsPointInside(point, 0.000001,false);
                //    if (debugIndexEdge < debugIndexMAX)
                //    {
                //        string text = "#" + debugIndexEdge + ":   " + String.Format("{0:0.00}", p.u) + " - " + String.Format("{0:0.00}", p.v);
                //        //string text = "#" + debugIndexEdge;
                //        AddDebugPoint(Doc, text, srf.PointAt(p.u, p.v), Color.Red); //debug
                //        Logger.log(text);
                //    }
                //}

                //
                // Fix U or V in singularities (in case where values are indentical at start of surface (like Point3d(u, v) == Point3d(u, v+100)));
                //
                var fixedSrf = srf._FixSurfacePoints(ref edgeDevidedUV, true, ssingulars, trim);

                //
                // Contruct 3d curve on surface
                //
                var points2d = edgeDevidedUV.Select(o => new Point2d(o.u, o.v)).ToList();
                crv3d = srf.InterpolatedCurveOnSurfaceUV(points2d, 0.0000001);

                if (crv3d == null)
                {
                    var point3d = edgeDevidedUV.Select(o => srf.PointAt(o.u, o.v)).ToList();
                    crv3d = srf.InterpolatedCurveOnSurface(point3d, 0.0001);
                    //crv3d = crv3d;
                    //var doc = RhinoDoc.ActiveDoc;
                    //foreach (var p in point3d)
                    //{
                    //    debugIndexEdge2++;
                    //    string text = "#" + debugIndexEdge2;
                    //    AddDebugPoint(doc, text, p, Color.Gold); //debug
                    //}
                }
            }



            if (crv3d == null)
            {
                //log.error(g.IssueFixer, "Can't build crv3d from 2D points");
                throw new Exception("Can't rebuild " + trim._GUIEdgeName());
            }

            return crv3d;
        }

        private static void AddDebugPoint(RhinoDoc doc, string text, Point3d point, Color color = default(Color))
        {
            // return;

            color = (color == default(Color))
                ? Color.Aqua
                : color;

            //doc.Objects.AddPoint(point, new ObjectAttributes()
            //{
            //    DisplayOrder = 12,
            //    LayerIndex = 0,
            //    Name = "temp",
            //    //Mode = ObjectMode.Locked,
            //    ColorSource = ObjectColorSource.ColorFromObject,
            //    ObjectColor = Color.Red,
            //    Visible = true
            //    //ObjectDecoration = ObjectDecoration.BothArrowhead,                    
            //});
            Guid id = doc.Objects.AddTextDot(text, point, new ObjectAttributes()
            {
                DisplayOrder = 5,
                LayerIndex = 0,
                //Name = "DebugPoint " + text,
                Name = text,
                //Mode = ObjectMode.Locked,
                ColorSource = ObjectColorSource.ColorFromObject,
                ObjectColor = color,
                Visible = true
            });


        }

        */

        #endregion

        //


        /// <summary>
        /// Recalculated iso status.
        /// Works always compare to internal Rhino method 'srf.IsIsoparametric(trim)'
        /// </summary>
        /// <param name="trim"></param>
        /// <param name="ssingulars">provide it for speed optimization. otherwise it will be calculated automatically.</param>
        /// <returns></returns>
        public static IsoStatus _IsoStatus(this BrepTrim trim, SurfaceSingulars ssingulars = null)
        {
            // COMMENTED - just to show what is a internal Rhino method to detect isoStatus
            //return srf.IsIsoparametric(trim); - doesnt work always properly!!!

            // COMMENTED - we want to be very sure and lets recalculate isostaus always
            // Calculate ony if status is invalid
            //var trimIsoStatus = trim.IsoStatus;
            //var isValidIso = trimIsoStatus == IsoStatus.East
            //                 || trimIsoStatus == IsoStatus.North
            //                 || trimIsoStatus == IsoStatus.South
            //                 || trimIsoStatus == IsoStatus.West;
            //if (isValidIso)
            //{
            //    return trimIsoStatus;
            //}
            

            if (ssingulars == null)
            {
                var srf = trim._Srf();
                ssingulars = new SurfaceSingulars(srf);
            }

            var T0 = trim.PointAtStart;
            var T1 = trim.PointAtEnd;
            var T0IsoStatus = ssingulars.GetIsoStatus(T0.X, T0.Y);
            var T1IsoStatus = ssingulars.GetIsoStatus(T1.X, T1.Y);
            if (T0IsoStatus == T1IsoStatus)
            {
                return T0IsoStatus;
            }

            //  show warnig message only both iso statuses are not 'None' - when we substitute surface with no singulars we will have such situation when all iso statuses became 'None'            
            if ((T0IsoStatus != IsoStatus.None || T1IsoStatus != IsoStatus.None))
            {
                if (ssingulars.Srf.IsAtSingularity(T0.X, T0.Y, true) || ssingulars.Srf.IsAtSingularity(T1.X, T1.Y, true))
                {
                    log.wrong("_BrepTrim._IsoStatus()    Cannot detect proper IsoStatus:  T0:{0}-T1:{1}  T0=[{2:0.000},{3:0.000}]   T1=[{4:0.000},{5:0.000}]", T0IsoStatus, T1IsoStatus, T0.X, T0.Y, T1.X, T1.Y);
                }else
                {
                    var temp = 0;
                }
                }
            return IsoStatus.None;
        }
    }
}
