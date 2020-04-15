using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static partial class Layers
    {
        public static class LayerIndexes
        {
            public const string LAYER_NAME_DebugLayer = "SolidFix:DebugLayer";
            public const string LAYER_NAME_HighlightLayer = "SolidFix:HighlightLayer";
            public const string LAYER_NAME_Topology = "SolidFix:Topology";

            private static bool IsEventSubsribed;
            public static int DebugLayerIndex { get; private set; }
            public static int HighlighLayerIndex { get; private set; }
            public static int TopoLayerIndex { get; private set; }
            public static void SubscribeLayerIndexes()
            {
                if (!IsEventSubsribed)
                {
                    IsEventSubsribed = true;
                    DebugLayerIndex = -1;
                    HighlighLayerIndex = -1;
                    TopoLayerIndex = -1;
                    RhinoDoc.LayerTableEvent += On_RhinoDoc_LayerTableEvent;
                    RhinoDoc.BeginOpenDocument += On_BeginOpenDocument;
                }
            }

            static LayerIndexes()
            {
                DebugLayerIndex = -1;
                HighlighLayerIndex = -1;
                TopoLayerIndex = -1;
            }

            private static void UpdateIndexes(RhinoDoc doc)
            {
                var layers = doc.Layers;
                DebugLayerIndex = layers.Find(LAYER_NAME_DebugLayer, true);
                HighlighLayerIndex = layers.Find(LAYER_NAME_HighlightLayer, true);
                TopoLayerIndex = layers.Find(LAYER_NAME_Topology, true);
            }

            private static void On_BeginOpenDocument(object sender, DocumentOpenEventArgs e)
            {
                UpdateIndexes(e.Document);
            }

            private static void On_RhinoDoc_LayerTableEvent(object sender, LayerTableEventArgs e)
            {
                switch (e.EventType)
                {
                    case LayerTableEventType.Added:
                    case LayerTableEventType.Deleted:
                    case LayerTableEventType.Undeleted:
                        UpdateIndexes(e.Document);
                        break;
                }
            }

            public static bool IsObjectIgnored(RhinoObject obj)
            {
                if (obj == null) return true;
                return IsLayerIgnored(obj.Attributes.LayerIndex);
            }

            public static bool IsLayerIgnored(Layer layer)
            {
                if (layer.IsDeleted) return true;
                return IsLayerIgnored(layer.LayerIndex);
            }

            public static bool IsLayerIgnored(int layerIndex)
            {
                return layerIndex == DebugLayerIndex
                    || layerIndex == HighlighLayerIndex
                    || layerIndex == TopoLayerIndex;
            }
        }
    }
}
