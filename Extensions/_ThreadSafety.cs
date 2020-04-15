using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class _ThreadSafety
    {
        public static double[] _DivideByCount_ThreadSafe(this Curve crv, int divby, bool includeEnds, out Point3d[] points)
        {
            // v0 - no need in thread safe for this method
            return crv.DivideByCount(divby, includeEnds, out points);
            //using (var crvDUPLICATE = crv.DuplicateCurve())
            //{
            //    return crvDUPLICATE.DivideByCount(divby, includeEnds, out points);
            //}
        }

        public static double _GetLength_ThreadSafe_Fast(this Curve crv)
        {
            //var gpuLen = GPU.GetLength(crv._ToNurbsCurve());            
            //log.file("" + crv._ToNurbsCurve().Points.Count);
            //log.file("" + gpuLen); 
            //if (Math.Abs(gpuLen - len) > 0.0001)
            //{
            //var error = 1;
            //}
            //var nurb = crv._ToNurbsCurve();
            //if (nurb.Points.Count > 300)
            //{
            //    var temp = 0;
            //}

            return _GetLength_ThreadSafe(crv);
        }

        public static double _GetLength_ThreadSafe(this Curve crv)
        {
            //if (GPU.IsEnabled)
            //{
            //}


            if (crv.Dimension == 3 && crv.Degree == 1)
            {
                return crv.PointAtStart._DistanceTo(crv.PointAtEnd);
            }

            using (var crvDUPLICATE = crv.DuplicateCurve()) // very important to dispose curve
            {
                var len = crvDUPLICATE.GetLength();
                return len;
            }
        }

        // split brep using 3d curves or return null if fail
        public static Brep _Split_ThreadSafe(this Brep brep, Curve[] curves, double tol)
        {
            if (brep.Faces.Count == 0) return null;
            return brep.Faces[0]._Split_ThreadSafe(curves, tol);
        }

        // split face using 3d curves or return null if fail
        public static Brep _Split_ThreadSafe(this BrepFace face, Curve[] curves, double tol)
        {
            using (var faceDUPLICATED = face.DuplicateFace(false)) // must be here to avoid System.AccessViolationException
            {
                if (faceDUPLICATED.Faces.Count == 0) return null;
                var splitedBreps = faceDUPLICATED.Faces[0].Split(curves, tol);
                return splitedBreps;
            }
        }

        // split surface using 3d curves or return null if fail
        public static Brep _Split_ThreadSafe(this Surface srf, Curve[] curves, double tol)
        {
            using (var srfDUPLICATED = srf.Duplicate()) // must be here to avoid System.AccessViolationException
            {
                var brep = ((Surface) srfDUPLICATED).ToBrep();
                if (brep.Faces.Count == 0) return null;
                try
                {
                    var splitedBreps = brep.Faces[0].Split(curves, tol);
                    return splitedBreps;
                }
                catch (ThreadAbortException e)
                {
                    return null;
                }
            }
        }


        public static BrepTrimList _Trims_ThreadSafe(this BrepLoop loop)
        {
            return Trims_ThreadSafe(loop);
        }

        public static BrepTrimList Trims_ThreadSafe(this BrepLoop loop)
        {
            var res = loop.Trims;
            res._InitThreadSafe();
            return res;
        }

        public static BrepLoopList _Loops_ThreadSafe(this BrepFace face)
        {
            return Loops_ThreadSafe(face);
        }

        public static BrepLoopList Loops_ThreadSafe(this BrepFace face)
        {
            var res = face.Loops;
            res._InitThreadSafe();
            return res;
        }

        internal static void _InitThreadSafe(this BrepTrimList trims)
        {
            lock (trims)
            {
                var count = trims.Count;
                if (count > 0)
                {
                    // we take last element and thus init all list - this make our list threadsafe
                    var last = trims[count - 1];
                }
            }
        }
        internal static void _InitThreadSafe(this BrepLoopList loops)
        {
            lock (loops)
            {
                var count = loops.Count;
                if (count > 0)
                {
                    // we take last element and thus init all list - this make our list threadsafe
                    var last = loops[count - 1];
                }
                foreach (var loop in loops)
                {
                    loop.Trims._InitThreadSafe();
                }
            }
        }
        internal static void _InitThreadSafe(this BrepFaceList faces)
        {
            lock (faces)
            {
                var count = faces.Count;
                if (count > 0)
                {
                    // we take last element and thus init all list - this make our list threadsafe
                    var last = faces[count - 1];
                }
                foreach (var face in faces)
                {
                    face.Loops._InitThreadSafe();
                }
            }
        }

        public static BrepTrimList Trims_ThreadSafe(this Brep brep)
        {
            brep._InitThreadSafe();
            return brep.Trims;
        }
        public static BrepFaceList Faces_ThreadSafe(this Brep brep)
        {
            brep._InitThreadSafe();
            return brep.Faces;
        }
        public static BrepLoopList Loops_ThreadSafe(this Brep brep)
        {
            brep._InitThreadSafe();
            return brep.Loops;
        }

        public static void _InitThreadSafe(this Brep brep)
        {
            lock (brep)
            {
                var Curves2DCount = brep.Curves2D.Count;
                if (Curves2DCount > 0)
                {
                    var last = brep.Curves2D[Curves2DCount - 1];
                }

                var Curves3DCount = brep.Curves3D.Count;
                if (Curves3DCount > 0)
                {
                    var last = brep.Curves3D[Curves3DCount - 1];
                }

                var VerticesCount = brep.Vertices.Count;
                if (VerticesCount > 0)
                {
                    var last = brep.Vertices[VerticesCount - 1];
                }

                var SurfacesCount = brep.Surfaces.Count;
                if (SurfacesCount > 0)
                {
                    var last = brep.Surfaces[SurfacesCount - 1];
                }

                var EdgesCount = brep.Edges.Count;
                if (EdgesCount > 0)
                {
                    var last = brep.Edges[EdgesCount - 1];
                }

                brep.Trims._InitThreadSafe();
                brep.Loops._InitThreadSafe();
                brep.Faces._InitThreadSafe();
            }
        }

    }
}
