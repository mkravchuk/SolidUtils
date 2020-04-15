using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public enum SFLineType
    {
        Default,
        Hidden, Dashed, DashDot, Center, Border, Dots, // Rhino default line types
        myDash, myDashSmall,
    }

    public static class Viewport
    {
        public static bool DEBUG = false;


        #region Redraw

        [DllImport("user32")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;

        internal static void RedrawWhenPossiblePlease()
        {
            Interlocked.Increment(ref Redraw_WhenRedrawSuppresed_CallsCount);
        }

        internal static long Redraw_WhenRedrawSuppresed_CallsCount;
        private static long RedrawSuppressor_StackCount;
        private static bool Redraw_IsSuppresed
        {
            get { return Interlocked.Read(ref RedrawSuppressor_StackCount) > 0; }
        }


        public class RedrawSuppressorRhinoMainWindow : IDisposable
        {
            private static long StackCount;
            public RedrawSuppressorRhinoMainWindow()
            {
                Interlocked.Increment(ref StackCount);
                if (Interlocked.Read(ref StackCount) == 1)
                {
                    SendMessage(RhinoApp.MainApplicationWindow.Handle, WM_SETREDRAW, false, 0);
                }
            }

            public void Dispose()
            {
                Interlocked.Decrement(ref StackCount);
                if (Interlocked.Read(ref StackCount) == 0)
                {
                    SendMessage(RhinoApp.MainApplicationWindow.Handle, WM_SETREDRAW, true, 0);
                }
            }
        }

        public class RedrawSuppressor : IDisposable
        {
            private readonly string operationName;
            private readonly bool redrawAnyway;
            private bool? saveRedrawEnabled;

            public RedrawSuppressor(RhinoDoc doc, string operationName, bool clearHighlights, bool clearSelections) //, bool callRedrawAfterIfCallsWasMade = true
            {
                this.operationName = operationName;
                this.redrawAnyway = redrawAnyway;
                Interlocked.Increment(ref RedrawSuppressor_StackCount);
                if (Interlocked.Read(ref RedrawSuppressor_StackCount) == 1)
                {
                    if (Viewport.DEBUG)
                    {
                        log.temp("!!! REDRAW:   RedrawSuppressor+    " + operationName);
                    }
                    Redraw_WhenRedrawSuppresed_CallsCount = 0;
                    if (doc == null)
                    {
                        doc = RhinoDoc.ActiveDoc;
                    }
                    if (doc != null)
                    {
                        saveRedrawEnabled = doc.Views.RedrawEnabled;
                        doc.Views.RedrawEnabled = false;
                        if (doc.Views.ActiveView != null)
                        {
                            //IDEA: show text in view port what is action in progress
                        }
                        if (clearSelections)
                        {
                            doc.Objects.UnselectAll();
                        }
                        if (clearHighlights)
                        {
                            Layers.Debug.Clear();
                            Layers.HighlightLayer.Clear();
                        }
                    }
                }

            }

            public void Dispose()
            {
                Interlocked.Decrement(ref RedrawSuppressor_StackCount);
                if (Interlocked.Read(ref RedrawSuppressor_StackCount) == 0)
                {
                    if (Viewport.DEBUG)
                    {
                        log.temp("!!! REDRAW:   RedrawSuppressor-     " + operationName);
                    }

                    var doc = RhinoDoc.ActiveDoc;
                    if (doc != null)
                    {
                        if (saveRedrawEnabled.HasValue)
                        {
                            doc.Views.RedrawEnabled = saveRedrawEnabled.Value;
                            saveRedrawEnabled = null;
                        }
                        if (redrawAnyway || Redraw_WhenRedrawSuppresed_CallsCount > 0)
                        {
                            var saveRedrawViewPort_WhenRedrawSuppresed_CallsCount = Redraw_WhenRedrawSuppresed_CallsCount;
                            Redraw_WhenRedrawSuppresed_CallsCount = 0;
                            Redraw(doc, "RedrawSuppressor.Dispose:  {0} (calls to redraw {1})"._Format(operationName, saveRedrawViewPort_WhenRedrawSuppresed_CallsCount));
                        }
                    }

                    // we have to clear flag because inside of RedrawSuppressor we select and unselect objects and add highlights for them
                    // so we want selection and highlight to be persist after we finish our redraw
                    RhinoDocSafeEvents.OnIdleClearHighlightLayer = false;
                }
            }
        }

        /// <summary>
        /// Call as mutch times as you want.
        /// To get maximum performance - all calls to this method should be wrapped in clause 'using(new RedrawSuppressor()){...}'
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="operationName"></param>
        /// <param name="redrawEvenIfRedrawIsSuppressed"></param>
        public static void Redraw(RhinoDoc doc, string operationName, bool redrawEvenIfRedrawIsSuppressed = false)
        {
            if (Shared.FILEGROUPOPERATIONS_CONVERTINGFILES) return;
            if (doc == null) return;
            //doc.Views.ActiveView.MainViewport.WorldAxesVisible = false;

            if ((!Redraw_IsSuppresed) || redrawEvenIfRedrawIsSuppressed)
            {
                var saveRedrawEnabled = doc.Views.RedrawEnabled;
                //var Watch = Stopwatch.StartNew();
                //Watch.Start();
                if (doc.Views.RedrawEnabled == false)
                {
                    //log.file("RedrawViewPort - setting RedrawEnabled to true");
                    doc.Views.RedrawEnabled = true;
                }
                try
                {

                    // here speed optimization - redraw only current view - all other views will be synchronized shomehow
                    // additional condition 'doc.Views.ActiveView.Maximized' doesnt required - anyway views will be synchronized
                    // speed increased from 250ms to 15ms - why so huge difference i dont know (probably there are some repaint issues in Rhino)
                    if (doc.Views.ActiveView != null) // && doc.Views.ActiveView.Maximized
                    {
                        doc.Views.ActiveView.Redraw(); // 5-10 times faster from full redraw
                    }
                    else
                    {
                        doc.Views.Redraw(); // full redraw - slow
                    }

                    if (Viewport.DEBUG)
                    {
                        log.temp("!!! REDRAW:   RedrawViewPort   -   " + operationName);
                        //Watch.Stop();
                        //TimeSpan ts = Watch.Elapsed;
                        //string elapsedTime = String.Format("{0:0.000}", ts.TotalMilliseconds);
                        //log.temp("!!! REDRAW:   time taken:  " + elapsedTime);
                    }
                    //log.temp("!!! REDRAW:   RedrawViewPort-    " + operationName);
                    //log.temp("!!!!!! RedrawViewPort");
                }
                finally
                {
                    if (doc.Views.RedrawEnabled != saveRedrawEnabled) doc.Views.RedrawEnabled = saveRedrawEnabled;
                }
            }
            else if (Redraw_IsSuppresed)
            {
                RedrawWhenPossiblePlease();
            }
        }

        #endregion


        public static class LineTypes
        {
            public static int GetLineTypeIndex(RhinoDoc doc, SFLineType type)
            {
                Linetype line = null;
                switch (type)
                {
                    case SFLineType.Default:
                        return -1;
                    case SFLineType.Hidden:
                    case SFLineType.Dashed:
                    case SFLineType.DashDot:
                    case SFLineType.Center:
                    case SFLineType.Border:
                    case SFLineType.Dots:
                        line = doc.Linetypes.FirstOrDefault(o => o.Name == type.ToString());
                        break;
                    case SFLineType.myDash:
                    case SFLineType.myDashSmall:
                        var myTypeName = "SF_LineType_" + type.ToString();
                        var l = doc.Linetypes.FirstOrDefault(o => o.Name == myTypeName);
                        if (l == null)
                        {
                            var lineType = new Linetype();
                            lineType.Name = myTypeName;
                            switch (type)
                            {
                                case SFLineType.myDash:
                                    lineType.AppendSegment(0.1, true);
                                    lineType.AppendSegment(0.05, false);
                                    break;
                                case SFLineType.myDashSmall:
                                    lineType.AppendSegment(0.05, true);
                                    lineType.AppendSegment(0.05, false);
                                    break;
                            }
                            var index = doc.Linetypes.Add(lineType);
                            line = doc.Linetypes[index];
                        }
                        else
                        {
                            line = doc.Linetypes[l.LinetypeIndex];

                        }
                        break;
                }

                // return our line index
                if (line != null)
                {
                    return line.LinetypeIndex;
                }

                // default line index - continuous line
                return -1;
            }
        }


        public static void Select(RhinoDoc doc, IEnumerable<RhinoObject> objects, double fitFactor = 3, bool resetCameraPosition = false)
        {
            foreach (var o in objects)
            {
                o.Select(true);
            }
            Zoom(doc, objects, fitFactor, resetCameraPosition);
        }

        public static void Zoom(RhinoDoc doc, IEnumerable<RhinoObject> objects, double fitFactor = 3, bool resetCameraPosition = false)
        {
            Zoom(doc, objects.Select(o => o.Geometry), fitFactor, resetCameraPosition);
        }

        public static void Zoom(RhinoDoc doc, IEnumerable<GeometryBase> objects, double fitFactor = 3, bool resetCameraPosition = false)
        {
            Zoom(doc, objects.Select(o => o.GetBoundingBox(true)), fitFactor, resetCameraPosition);
        }

        public static void Zoom(RhinoDoc doc, IEnumerable<BoundingBox> boxes, double fitFactor = 3, bool resetCameraPosition = false)
        {
            BoundingBox box = BoundingBox.Empty;
            bool isBoxEmpty = true;
            foreach (var b in boxes)
            {
                if (isBoxEmpty)
                {
                    box = b;
                    isBoxEmpty = false;
                }
                else
                {
                    box.Union(b);
                }
            }

            if (!isBoxEmpty)
            {
                Zoom(doc, box, fitFactor, resetCameraPosition);
            }
        }

        public static void Zoom(RhinoDoc doc, BoundingBox box, double fitFactor = 3, bool resetCameraPosition = false)
        {
            if (doc.Views.ActiveView == null) return;
            if (!box.IsValid) return;

            bool needRedraw = false;

            if (resetCameraPosition)
            {
                ResetCameraPosition(doc, box, false);
                needRedraw = true;
            }


            // if (Shared.FILEBROWSER_FILEGROUPOPERATIONS_ENABLED) return;

            //var view = doc.Views.ActiveView;
            //if (view != null)
            //{
            //    var xMiddle = view.ScreenRectangle.Width/2;
            //    var yMiddle = view.ScreenRectangle.Height/2;
            //    var screenMiddle = new Point3d(xMiddle, yMiddle, 0);
            //    //var pointMiddle = view.ScreenToClient(new Point2d(xMiddle, yMiddle));

            //    Transform screen_to_world = view.ActiveViewport.GetTransform(CoordinateSystem.Screen,
            //        CoordinateSystem.World);
            //    screenMiddle.Transform(screen_to_world);

            //    Transform world_to_camera = view.ActiveViewport.GetTransform(CoordinateSystem.World,
            //        CoordinateSystem.Camera);
            //    doc.Objects.AddBrep(box.ToBrep());

            //    //l.Transform(world_to_camera);
            //    box.GetCorners();
            //}

            //return;
            if (!fitFactor._IsSame(0))
            {
                //const double pad = 0.05;    // A little padding...
                //double dx = (box.Max.X - box.Min.X) * pad;
                //double dy = (box.Max.Y - box.Min.Y) * pad;
                //double dz = (box.Max.Z - box.Min.Z) * pad;
                fitFactor = fitFactor * 0.1;
                double dx = Math.Abs(box.Max.X - box.Min.X) * fitFactor;
                double dy = Math.Abs(box.Max.Y - box.Min.Y) * fitFactor;
                double dz = Math.Abs(box.Max.Z - box.Min.Z) * fitFactor;
                box.Inflate(dx, dy, dz);

                doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(box);
                needRedraw = true;
            }

            if (needRedraw)
            {
                Redraw(doc, "Viewport.Zoom");
            }
        }

        public static void ResetCameraPosition(RhinoDoc doc, BoundingBox box, bool redraw = true)
        {
            if (doc.Views.ActiveView == null) return;
            doc.Views.ActiveView.ActiveViewport.SetCameraLocation(Point3d.Origin, true);
            doc.Views.ActiveView.ActiveViewport.SetCameraDirection(Point3d.Origin - box.Center, true);
            if (redraw)
            {
                Redraw(doc, "Viewport.ResetCameraPosition");
            }
        }

        public static void SetCameraPosition(RhinoDoc doc, Vector3d direction, bool redraw = true)
        {
            if (doc.Views.ActiveView == null) return;
            if (doc.Views.ActiveView.ActiveViewport == null) return;
            doc.Views.ActiveView.ActiveViewport.SetCameraDirection(direction, true);
            if (redraw)
            {
                Redraw(doc, "Viewport.SetCameraPosition");
            }
        }

        public static void SetCameraPositionIfDifferent(RhinoDoc doc, Vector3d direction, double maxDifferenceInAngle, bool redraw = true)
        {
            if (doc.Views.ActiveView == null) return;
            if (doc.Views.ActiveView.ActiveViewport == null) return;

            var v = doc.Views.ActiveView.ActiveViewport;
            var angle = v.CameraDirection._AngleInRadians(direction)._RadianToDegree();
            if (angle < maxDifferenceInAngle)
            {
                return;
            }

            v.SetCameraDirection(direction, true);
            if (redraw)
            {
                Redraw(doc, "Viewport.SetCameraPosition");
            }
        }


        public static void ZoomAll(RhinoDoc doc, double fitFactor = 3, bool resetCameraPosition = false)
        {
            if (doc.Views.ActiveView == null) return;
            Zoom(doc, doc.Objects, fitFactor, resetCameraPosition);
        }

        public static void Zoom100(RhinoDoc doc)
        {
            if (doc.Views.ActiveView == null) return;
            //foreach (var v in doc.Views)
            //{
            //    v.ActiveViewport.ZoomExtents();
            //}
            doc.Views.ActiveView.ActiveViewport.ZoomExtents();
            Redraw(doc, "Viewport.Zoom100");
        }

        public static void UnselectAll()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc != null)
            {
                doc.Objects.UnselectAll();
            }
        }

        public static void CreateRenderMeshes(bool showProgress, List<RhinoObject> objs = null, RhinoDoc doc = null)
        {
            if (doc == null)
            {
                doc = RhinoDoc.ActiveDoc;
            }
            if (doc == null) return;

            CreateRenderMeshes(showProgress, doc.MeshingParameterStyle, objs, doc);
        }

        public static void CreateRenderMeshes(bool showProgress, MeshingParameterStyle meshingStyle, List<RhinoObject> objs = null, RhinoDoc doc = null)
        {
            if (doc == null)
            {
                doc = RhinoDoc.ActiveDoc;
            }
            if (doc == null) return;

            if (objs == null)
            {
                objs = doc.Objects.Where(o => o.GetMeshes(MeshType.Render).Length == 0).ToList();
            }
            else
            {
                objs = objs.Where(o => o.GetMeshes(MeshType.Render).Length == 0).ToList();
            }
            
            if (objs.Count == 0) return;

            using (var meshingParametersU = doc.GetMeshingParameters(meshingStyle))
            {
                var meshingParameters = meshingParametersU;
                var caption = String.Format("Creating meshes for {0} objects ...", objs.Count);
                objs._ForeachParallel_WithOrWithoutProgressWindow(showProgress, caption, obj => obj.CreateMeshes(MeshType.Render, meshingParameters, false));
            }
        }
    }
}
