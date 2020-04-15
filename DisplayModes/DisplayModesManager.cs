using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;

namespace SolidUtils.DisplayModes
{
    public enum DisplayModeType
    {
        Other, Ghosted, Topology, TopologyColoredSurfaces, Mesh
    }

    public class DisplayModesManager : IDisposable
    {
        public static DisplayModeType CurrentDisplayMode;
        public static Dictionary<DisplayModeType, DisplayModeDescription> DisplayModes;

        public static void Register()
        {
            DisplayModes = new Dictionary<DisplayModeType, DisplayModeDescription>();
            foreach (DisplayModeType modeType in Enum.GetValues(typeof(DisplayModeType)))
            {
                if (modeType == DisplayModeType.Other) continue;
                using (var m = new DisplayModesManager())
                {
                    m.GetMode(modeType);
                }
            }
            
            RhinoDocSafeEvents.Idle10timesOnSecond += On_Idle;
        }

        public static void ObjectAttributes_SetDisplayModeOverride(ObjectAttributes attr, DisplayModeType modeType)
        {
            using (var m = new DisplayModesManager())
            {
                var mode = m.GetMode(modeType);
                if (mode != null)
                {
                    attr.SetDisplayModeOverride(mode);
                }
            }
        }

        public DisplayModeDescription GetMode(DisplayModeType modeType)
        {
            if (modeType == DisplayModeType.Other) return null;
            if (DisplayModes.ContainsKey(modeType))
            {
                return DisplayModes[modeType]; // return from cache
            }

            var modeName = "SolidFix_" + modeType;
            var mode = DisplayModeDescription.FindByName(modeName);
            if (mode == null)
            {
                log.info(g.SolidFix, "\n * Cannot find SolidFix Display Mode '{0}'", modeName);
                var path = Path.Combine(Utils.AssemblyDirectory, @"DisplayModes\");
                log.info(g.SolidFix, "* Please go to 'Options->View->Display Modes' \n  and import SolidFix display mode \n  from location:  '{0}'\n", path);
                return null;
            }
            else
            {
                DisplayModes[modeType] = mode; // cache
            }
            return mode;
        }

        public static bool Choise(DisplayModeType modeType)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return false;

            using (new Viewport.RedrawSuppressor(doc, "DisplayModesManager.Choise", false, false))
            {
                using (var m = new DisplayModesManager())
                {
                    var mode = m.GetMode(modeType);
                    if (mode != null)
                    {
                        if (doc.Views.ActiveView != null
                            && doc.Views.ActiveView.ActiveViewport != null)
                        {
                            CurrentDisplayMode = modeType;
                            doc.Views.ActiveView.ActiveViewport.DisplayMode = mode;
                            UpdateTopoLayerVisibility();
                            Viewport.Redraw(doc, "DisplayModesManager.Choise");
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        private static void On_Idle(object sender, EventArgs e)
        {
            UpdateTopoLayerVisibility();
        }

        [HandleProcessCorruptedStateExceptions]
        public bool IsDisplayModeAvailable(RhinoViewport viewport)
        {
            if (viewport == null) return false;
            try
            {
                var mode = viewport.DisplayMode;
                var res = (mode != null);
                if (mode != null) mode.Dispose();
                return res;
            }
            catch (Exception ex)
            {
                log.temp("DisplayMode is unavailable: " + ex.Message);
                return false;
            }
        }

        private static void UpdateTopoLayerVisibility()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return;
            if (doc.Views.ActiveView == null) return;
            if (doc.Views.ActiveView.ActiveViewport == null) return;

            Update_CurrentDisplayMode(doc);

            int layerIndex = Layers.LayerIndexes.TopoLayerIndex;
            if (layerIndex == -1) return;
            var topoLayer = doc.Layers[layerIndex];

            var IsTopoLayerShouldBeVisible = (CurrentDisplayMode == DisplayModeType.Topology);
            if (topoLayer.IsVisible != IsTopoLayerShouldBeVisible)
            {
                topoLayer.IsVisible = IsTopoLayerShouldBeVisible;
                topoLayer.CommitChanges();
                Viewport.Redraw(doc, "DisplayModesManager.UpdateTopoLayerVisibility");
            }
        }

        private static void Update_CurrentDisplayMode(RhinoDoc doc)
        {
            var modeName = Get_CurrentDisplayMode_EnglishName(doc);
            CurrentDisplayMode = DisplayModeType.Other;
            if (modeName == "SolidFix_Ghosted") CurrentDisplayMode = DisplayModeType.Ghosted;
            if (modeName == "SolidFix_Topology") CurrentDisplayMode = DisplayModeType.Topology;
            if (modeName == "SolidFix_Mesh") CurrentDisplayMode = DisplayModeType.Mesh;
            if (modeName == "SolidFix_TopologyColoredSurfaces") CurrentDisplayMode = DisplayModeType.TopologyColoredSurfaces;
        }

        [HandleProcessCorruptedStateExceptions]
        public static string Get_CurrentDisplayMode_EnglishName(RhinoDoc doc)
        {
            using (var m = new DisplayModesManager())
            {
                try
                {
                    if (doc == null
                        || doc.Views.ActiveView == null
                        || doc.Views.ActiveView.ActiveViewport == null)
                    {
                        return "";
                    }

                    var mode = doc.Views.ActiveView.ActiveViewport.DisplayMode;
                    if (mode == null) return "";
                    var res = mode.EnglishName;
                    mode.Dispose();
                    return res;
                }
                catch (Exception ex)
                {
                    log.temp("Get_CurrentDisplayMode_EnglishName:   DisplayMode is unavailable: " + ex.Message);
                    return "";
                }
            }
        }

        public void Dispose()
        {
            // clear garabage collector - to ensure that all Displaymodes will be disposed
            //GC.Collect(2, GCCollectionMode.Forced);
            //GC.WaitForPendingFinalizers();
        }
    }
}
