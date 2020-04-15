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
    public static class _RhinoDoc
    {
        public static void _ClearDocumentChanges(this RhinoDoc doc)
        {
            if (doc != null)
            {
                doc.Modified = false;
                doc.ClearUndoRecords(true);
                doc.ClearRedoRecords();
                doc.Modified = false;
            }
        }

        public static void _OpenFileWithoutSaveConfirmationDialog(this RhinoDoc doc, string filename)
        {
            _ClearDocumentChanges(doc);
            RhinoDoc.OpenFile(filename);
        }

        public static void _CloseDocument(this RhinoDoc doc)
        {
            if (doc != null)
            {
                doc.Layers._RemoveAllLayersAndObjectsExceptDefaultLayer();
                doc.Notes = "";
            }
        }
    }

    public static class RhinoDocSafeEvents
    {
        public static bool DEBUG = false;  
        /// <summary> 
        /// If user has changed selection - we need to clear highlight layer.  
        /// If selection is made by application - developer must to reset this flag after he finished all selection and highlights.
        /// RedrawSuppressor class already reseting this flag, so everething that is selected or highlighted inside RedrawSuppressor scope will remain.
        /// </summary>
        internal static bool OnIdleClearHighlightLayer;

        private static bool OnIdleEnabled;
        private static Stopwatch stopwatch_Idle10timesOnSecond;
        private static bool IsAppInitialized;

        /// <summary>
        /// If your code depends on selection - use this flag in event OnIdle.
        /// </summary>
        public static bool IsSelectionMayHaveChangedSinceLastOnIdle;
        /// <summary>
        /// If your code depends on selection - use this flag in event OnIdle10timesOnSecond.
        /// </summary>
        public static bool IsSelectionMayHaveChangedSinceLastOnIdle10timesOnSecond;

        public static event EventHandler<DocumentOpenEventArgs> BeginOpenDocument;
        public static event EventHandler<DocumentOpenEventArgs> EndOpenDocument;
        public static event EventHandler<DocumentOpenEventArgs> EndOpenDocumentInitialiViewUpdate;
        public static event EventHandler<DocumentEventArgs> CloseDocument;
        public static event EventHandler<RhinoModifyObjectAttributesEventArgs> ModifyObjectAttributes;
        public static event EventHandler<RhinoObjectEventArgs> AddRhinoObject;
        public static event EventHandler<RhinoObjectEventArgs> DeleteRhinoObject;
        public static event EventHandler<RhinoObjectEventArgs> UndeleteRhinoObject;
        public static event EventHandler<RhinoObjectSelectionEventArgs> SelectObjects;
        public static event EventHandler<RhinoObjectSelectionEventArgs> DeselectObjects;
        public static event EventHandler<RhinoDeselectAllObjectsEventArgs> DeselectAllObjects;
        public static event EventHandler Idle;
        public static event EventHandler Initialized;
        public static event EventHandler Idle10timesOnSecond;

        static RhinoDocSafeEvents()
        {
            RhinoDoc.BeginOpenDocument += On_BeginOpenDocument;
            RhinoDoc.EndOpenDocument += On_EndOpenDocument;
            RhinoDoc.EndOpenDocumentInitialiViewUpdate += On_EndOpenDocumentInitialiViewUpdate;
            RhinoDoc.CloseDocument += On_CloseDocument;
            RhinoDoc.ModifyObjectAttributes += On_ModifyObjectAttributes;
            RhinoDoc.AddRhinoObject += On_AddRhinoObject;
            RhinoDoc.DeleteRhinoObject += On_DeleteRhinoObject;
            RhinoDoc.UndeleteRhinoObject += On_UndeleteRhinoObject;
            RhinoDoc.SelectObjects += On_SelectObjects;
            RhinoDoc.DeselectObjects += On_DeselectObjects;
            RhinoDoc.DeselectAllObjects += On_DeselectAllObjects;
            RhinoApp.Idle += On_Idle;
            RhinoApp.Initialized += On_Initialized;
            OnIdleEnabled = false; // when Rhino starts - no document is loaded - so disable OnIdle.
            IsAppInitialized = false;
            stopwatch_Idle10timesOnSecond = new Stopwatch();
            stopwatch_Idle10timesOnSecond.Start();
        }

        private static void On_SelectObjects(object sender, RhinoObjectSelectionEventArgs e)
        {
            //if (!IsAppInitialized) return;
            if (DEBUG) log.debug(g._RhinoDoc, "On_SelectObjects");
            if (Viewport.DEBUG)
            {
                log.temp("!!! REDRAW:   On_SelectObjects");
            }
            IsSelectionMayHaveChangedSinceLastOnIdle = true;
            IsSelectionMayHaveChangedSinceLastOnIdle10timesOnSecond = true;
            OnIdleClearHighlightLayer = true;

            if (SelectObjects == null) return;
            Shared.TryCatchAction(() => SelectObjects(sender, e), g._RhinoDoc, "exception in On_SelectObjects event");
        }

        private static void On_DeselectObjects(object sender, RhinoObjectSelectionEventArgs e)
        {
            if (DEBUG) log.debug(g._RhinoDoc, "On_DeselectObjects");
            if (Viewport.DEBUG)
            {
                log.temp("!!! REDRAW:   On_DeselectObjects");
            }
            IsSelectionMayHaveChangedSinceLastOnIdle = true;
            IsSelectionMayHaveChangedSinceLastOnIdle10timesOnSecond = true;
            OnIdleClearHighlightLayer = true;

            if (DeselectObjects == null) return;
            Shared.TryCatchAction(() => DeselectObjects(sender, e), g._RhinoDoc, "exception in On_DeselectObjects event");
        }

        private static void On_DeselectAllObjects(object sender, RhinoDeselectAllObjectsEventArgs e)
        {
            if (DEBUG) log.debug(g._RhinoDoc, "On_DeselectAllObjects");
            if (Viewport.DEBUG)
            {
                log.temp("!!! REDRAW:   On_DeselectAllObjects");
            }
            IsSelectionMayHaveChangedSinceLastOnIdle = true;
            IsSelectionMayHaveChangedSinceLastOnIdle10timesOnSecond = true;
            OnIdleClearHighlightLayer = true;

            if (DeselectAllObjects == null) return;
            Shared.TryCatchAction(() => DeselectAllObjects(sender, e), g._RhinoDoc, "exception in On_DeselectAllObjects event");
        }

        private static void On_BeginOpenDocument(object sender, DocumentOpenEventArgs e)
        {
            if (DEBUG) log.debug(g._RhinoDoc, "On_BeginOpenDocument");
            IsSelectionMayHaveChangedSinceLastOnIdle = true;
            IsSelectionMayHaveChangedSinceLastOnIdle10timesOnSecond = true;
            OnIdleEnabled = false;
            if (BeginOpenDocument == null) return;
            Shared.TryCatchAction(() => BeginOpenDocument(sender, e), g._RhinoDoc, "exception in On_BeginOpenDocument event");
        }

        private static void On_EndOpenDocument(object sender, DocumentOpenEventArgs e)
        {
            if (DEBUG) log.debug(g._RhinoDoc, "On_EndOpenDocument");
            IsSelectionMayHaveChangedSinceLastOnIdle = true;
            IsSelectionMayHaveChangedSinceLastOnIdle10timesOnSecond = true;
            if (EndOpenDocument == null) return;
            Shared.TryCatchAction(() => EndOpenDocument(sender, e), g._RhinoDoc, "exception in EndOpenDocument event");
        }

        private static void On_EndOpenDocumentInitialiViewUpdate(object sender, DocumentOpenEventArgs e)
        {
            if (DEBUG) log.debug(g._RhinoDoc, "On_EndOpenDocumentInitialiViewUpdate");
            OnIdleEnabled = true;
            stopwatch_Idle10timesOnSecond.Restart();
            if (EndOpenDocumentInitialiViewUpdate == null) return;
            Shared.TryCatchAction(() => EndOpenDocumentInitialiViewUpdate(sender, e), g._RhinoDoc, "exception in EndOpenDocumentInitialiViewUpdate event");
        }

        private static void On_CloseDocument(object sender, DocumentEventArgs e)
        {
            if (DEBUG) log.debug(g._RhinoDoc, "On_CloseDocument");
            IsSelectionMayHaveChangedSinceLastOnIdle = true;
            IsSelectionMayHaveChangedSinceLastOnIdle10timesOnSecond = true;
            OnIdleEnabled = false;
            if (CloseDocument == null) return;
            Shared.TryCatchAction(() => CloseDocument(sender, e), g._RhinoDoc, "exception in CloseDocument event");
        }

        private static void On_ModifyObjectAttributes(object sender, RhinoModifyObjectAttributesEventArgs e)
        {
            if (ModifyObjectAttributes == null) return;
            Shared.TryCatchAction(() => ModifyObjectAttributes(sender, e), g._RhinoDoc, "exception in ModifyObjectAttributes event");
        }

        private static void On_AddRhinoObject(object sender, RhinoObjectEventArgs e)
        {
            if (AddRhinoObject == null) return;
            Shared.TryCatchAction(() => AddRhinoObject(sender, e), g._RhinoDoc, "exception in AddRhinoObject event");
        }

        private static void On_DeleteRhinoObject(object sender, RhinoObjectEventArgs e)
        {
            if (DeleteRhinoObject == null) return;
            Shared.TryCatchAction(() => DeleteRhinoObject(sender, e), g._RhinoDoc, "exception in DeleteRhinoObject event");
        }

        private static void On_UndeleteRhinoObject(object sender, RhinoObjectEventArgs e)
        {
            if (UndeleteRhinoObject == null) return;
            Shared.TryCatchAction(() => UndeleteRhinoObject(sender, e), g._RhinoDoc, "exception in UndeleteRhinoObject event");
        }


        private static void On_Initialized(object sender, EventArgs e)
        {
            IsAppInitialized = true;
            RhinoApp.SetFocusToMainWindow();
            if (DEBUG) log.debug(g._RhinoDoc, "On_Initialized");
            if (Initialized == null) return;
            Shared.TryCatchAction(() => Initialized(sender, e), g._RhinoDoc, "exception in Initialized event");
        }


        private static void On_Idle(object sender, EventArgs e)
        {
            if (DEBUG) log.debug(g._RhinoDoc, "On_Idle");
            if (!IsAppInitialized) return;

            if (Shared.IsForeachParallelInProgress)
            {
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                log.wrong("On_Idle called when IsForeachParallelInProgress");
                return;
            }


            // OnIndle enabled only when document is loaded, and disabled between events On_BeginOpenDocument and EndOpenDocumentInitialiViewUpdate
            if (!OnIdleEnabled) return;

            // if it is not from main thread - lets wait for call from main thread
            if (!Shared.IsExecutingInMainThread) return;
            if (RhinoApp.MainApplicationWindow.InvokeRequired) return;


            if (OnIdleClearHighlightLayer)
            {
                if (Viewport.DEBUG)
                {
                    log.temp("User has changed selection - clearing HighlightLayer");
                }
                OnIdleClearHighlightLayer = false;
                Layers.HighlightLayer.Clear();
            }

            if (Idle != null)
            {
                Shared.TryCatchAction(() => Idle(sender, e), g._RhinoDoc, "exception in Idle event");
                IsSelectionMayHaveChangedSinceLastOnIdle = false;
            }

            if (Idle10timesOnSecond != null)
            {
                // update only 10 times per second to avoid CPU load
                if (stopwatch_Idle10timesOnSecond.ElapsedMilliseconds > 100)
                {
                    if (DEBUG) log.debug(g._RhinoDoc, "On_Idle10timesOnSecond     ElapsedMilliseconds=" + stopwatch_Idle10timesOnSecond.ElapsedMilliseconds);
                    Shared.TryCatchAction(() => Idle10timesOnSecond(sender, e), g._RhinoDoc, "exception in OnIdle10timesOnSecond event");
                    IsSelectionMayHaveChangedSinceLastOnIdle10timesOnSecond = false;
                }
            }

        }
    }
}
