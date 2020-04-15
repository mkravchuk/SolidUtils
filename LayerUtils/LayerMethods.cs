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
    public static partial class Layers
    {
        public static class LayerMethods
        {
            public static bool DEFAULT_LOCK_STATE = true;
            //private static HistoryRecord historyRecord;
            //private static HistoryRecord HistoryRecord
            //{
            //    get
            //    {
            //        if (historyRecord == null)
            //        {
            //            historyRecord = new HistoryRecord(new HighlightLayerCommand(), 0);
            //        }
            //        return historyRecord;
            //    }
            //}
            public static bool IsLayerLocked(RhinoDoc doc, string LAYER_NAME)
            {
                if (doc == null)
                {
                    log.wrong("LayersMethods.IsLayerLocked() doc is null");
                    return false;
                }

                int index = doc.Layers.Find(LAYER_NAME, true);
                if (index != -1)
                {
                    return doc.Layers[index].IsLocked;
                }
                return DEFAULT_LOCK_STATE; // return default lock status when for new layers
            }

            public static void SetLayerLocked(RhinoDoc doc, string LAYER_NAME, bool locked)
            {
                if (doc == null)
                {
                    log.wrong("LayersMethods.SetLayerLocked()  doc is null");
                    return ;
                }


                int index = doc.Layers.Find(LAYER_NAME, true);
                if (index != -1)
                {
                    var topolayer = doc.Layers[index];
                    topolayer.IsLocked = locked;
                    topolayer.CommitChanges();
                    Viewport.Redraw(doc, "LayerMethods.SetLayerLocked");
                }
            }

            public static Layer GetLayer(RhinoDoc doc, string LAYER_NAME)
            {
                if (doc == null)
                {
                    throw new FailedToFixIssue("LayersMethods.GetLayer() - doc is null");
                }

                var index = EnsureIsCreated(doc, LAYER_NAME);
                return doc.Layers[index];
            }

            public static int EnsureIsCreated(RhinoDoc doc, string LAYER_NAME)
            {
                if (doc == null)
                {
                    throw new FailedToFixIssue("LayersMethods.EnsureIsCreated() - doc is null");
                }

                //log.temp("EnsureIsCreated  " + LAYER_NAME);
                int index = doc.Layers.Find(LAYER_NAME, true);
                if (index == -1)
                {
                    // ensure we  have at least 1 layer before adding our utils layer
                    if (doc.Layers.Count == 0)
                    {
                        doc.Layers.Add(new Layer
                        {
                            Name = "Default",
                            IsLocked = false,
                            IsVisible = true
                        });
                    }
                    // add our utils layer (it will be not saved into file, since we need it temporaly)
                    index = doc.Layers.AddReferenceLayer(new Layer
                    {
                        Name = LAYER_NAME,
                        Color = Color.Navy,
                        IsLocked = DEFAULT_LOCK_STATE,
                        IsVisible = true
                    });
                }
                return index;
            }

            public static void DeleteLayer(RhinoDoc doc, string LAYER_NAME)
            {
                if (doc == null)
                {
                    log.wrong("LayersMethods.DeleteLayer()  doc is null");
                    return;
                }

                Clear(doc, LAYER_NAME);
                int layerIndex = doc.Layers.Find(LAYER_NAME, true);
                doc.Layers.Delete(layerIndex, true);
                Viewport.Redraw(doc, "LayerMethods.DeleteLayer");
            }

            public static int Clear(RhinoDoc doc, string LAYER_NAME)
            {
                if (doc == null)
                {
                    log.wrong("LayersMethods.Clear()  doc is null");
                    return 0;
                }

                var removedCount = 0;
                int index = doc.Layers.Find(LAYER_NAME, true);
                if (index != -1)
                {
                    var objs = doc.Objects.FindByLayer(LAYER_NAME);
                    foreach (var o in objs)
                    {
                        o._Purge();
                        removedCount++;
                    }
                }
                if (removedCount > 0)
                {
                    if (Viewport.DEBUG) 
                    {
                        log.temp("LayerMethods.Clear - cleared {0} objects"._Format(removedCount));
                    }
                    Viewport.Redraw(doc, "LayerMethods.Clear");
                }
                return removedCount;
            }

            public static void Zoom(RhinoDoc doc, string LAYER_NAME, int fitFactor)
            {
                if (doc == null)
                {
                    log.wrong("LayersMethods.Zoom()  doc is null");
                    return;
                }

                var objs = doc.Objects.FindByLayer(LAYER_NAME);
                Viewport.Zoom(doc, objs, fitFactor: fitFactor);
                Viewport.Redraw(doc, "LayerMethods.Zoom");
            }

            public static void UnselectAll(RhinoDoc doc, string LAYER_NAME)
            {
                if (doc == null)
                {
                    log.wrong("LayersMethods.UnselectAll()  doc is null");
                    return;
                }
                if (Viewport.DEBUG)
                {
                    log.temp("LayerMethods.UnselectAll on layer " + LAYER_NAME);
                }

                Viewport.UnselectAll();
                Clear(doc, LAYER_NAME);
                Viewport.Redraw(doc, "LayerMethods.UnselectAll");
            }
        }
    }
}
