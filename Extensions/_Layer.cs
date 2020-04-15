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
    public static class _Layer
    {
        public static string _GetObjNewName(this Layer layer, RhinoDoc doc)
        {
            var objs = doc.Objects.FindByLayer(layer);

            // detect what is the biggest index in name of all objects in layer
            bool IsZeroBased = false;
            var maxIndex = 0;
            foreach (var obj in objs)
            {
                var name = obj.Name;
                if (name.StartsWith("0"))
                {
                    IsZeroBased = true;
                }
                int index;
                if (Int32.TryParse(name, out index))
                {
                    if (index > maxIndex) maxIndex = index;
                }
            }
            maxIndex++;

            // return result
            if (IsZeroBased)
            {
                int nameLength = 1;
                if (objs.Length >= 10) nameLength = 2;
                if (objs.Length >= 100) nameLength = 3;
                if (objs.Length >= 1000) nameLength = 4;
                if (objs.Length >= 10000) nameLength = 5;
                var res = maxIndex.ToString("D" + nameLength);
                return res;
            }
            else
            {
                return maxIndex.ToString();
            }
        }

        public static int _GetObjsCount(this Layer layer, RhinoDoc doc, bool includeChildsLayers = false, ObjectType objectTypeFilter = ObjectType.AnyObject)
        {
            var settings = new ObjectEnumeratorSettings
            {
                LayerIndexFilter = layer.LayerIndex,
                HiddenObjects = true,
                
            };
            if (objectTypeFilter != ObjectType.AnyObject)
            {
                settings.ObjectTypeFilter = objectTypeFilter;
            }
            var res = doc.Objects.ObjectCount(settings);
            //var res2 = doc.Objects.FindByLayer(layer).Count();
            //if (res != res2)
            //{
            //    res = res2;
            //}
            if (includeChildsLayers) 
            {
                var childs = layer.GetChildren();
                if (childs != null)
                {
                    res += childs.Sum(childLayer => childLayer._GetObjsCount(doc, includeChildsLayers, objectTypeFilter));
                }
            }

            return res;
        }


        public static List<RhinoObject> _GetObjects(this Layer layer, RhinoDoc doc, bool includeChildsLayers = false, ObjectType objectTypeFilter = ObjectType.AnyObject)
        {
            var res = new List<RhinoObject>(10000);


            var objs = doc.Objects.FindByLayer(layer);
            if (objs != null)
            {
                res.AddRange(objs);
            }


            if (includeChildsLayers)
            {
                var childs = layer.GetChildren();
                if (childs != null)
                {
                    foreach (var childLayer in childs)
                    {
                        res.AddRange(childLayer._GetObjects(doc, includeChildsLayers, objectTypeFilter));
                    }
                }
            }
            return res;
        }
    }

    public static class _LayerTable
    {
        public static void _RemoveAllLayersAndObjectsExceptDefaultLayer(this LayerTable docLayers)
        {
            if (docLayers == null) return;
            var doc = docLayers.Document;
            if (doc == null) return;

            //DEBUG
            //var layers1 =  doc.Layers.Where(o => o.ParentLayerId == Guid.Empty).ToList();
            //var objs1 =  doc.Objects.ToList();

            //
            // unselect all object to be able to delete them and layer that hold this selected object
            //
            Layers.HighlightLayer.UnselectAll();

            //
            // delete all objects
            //
            foreach (var o in doc.Objects)
            {
                if (!doc.Objects._Purge(o))
                {
                    //DEBUG
                    //RhinoApp.WriteLine("DEBUG:  RhinoFile.Open: Unable to delete object " + o.Name);
                }
            }

            // clear garabage collector - to ensure we will not have issues with memory
            GC.Collect();
            GC.WaitForPendingFinalizers();

            //
            // Set current layer to default to be able to delete others layer that sometimes are default even named different than 'Default'            
            //

            var defaultLayerIndex = -1;
            // lets create unique 'Default' layer
            foreach (var defLayerUniqueName in new[] { "Default", "DefaultLayer", "DefaultLayer0", "DefaultLayer00", "DefaultLayer000" })
            {
                defaultLayerIndex = doc.Layers.FindByFullPath(defLayerUniqueName, true);
                if (defaultLayerIndex == -1)
                {
                    doc.Layers.Add(new Layer() {Name = defLayerUniqueName});
                    defaultLayerIndex = doc.Layers.FindByFullPath(defLayerUniqueName, true);
                }
                
                // if 'Default' layer is not in root - dont use it - better lets create a new layer in next loop-cycle
                if (doc.Layers[defaultLayerIndex].ParentLayerId != Guid.Empty)
                {                    
                    defaultLayerIndex = -1;
                }

                // if we found our  'Default' layer - lets break our loop
                if (defaultLayerIndex != -1)
                {
                    break;
                }
            }

            // take first layer as default (in case we cannot have 'Default' layer as default) ( this is almost impossible but anyway lets keep this condition)
            if (defaultLayerIndex == -1 && doc.Layers.Count != 0)
            {
                var layersThatCanBeDefault = doc.Layers.Where(o => o.ParentLayerId == Guid.Empty && !o.IsDeleted && !Layers.LayerIndexes.IsLayerIgnored(o)).ToList();
                if (layersThatCanBeDefault.Count != 0)
                {
                    defaultLayerIndex = layersThatCanBeDefault[0].LayerIndex;
                }
            }

            // if we found default layer - set it!
            if (defaultLayerIndex != -1)
            {
                doc.Layers.SetCurrentLayerIndex(defaultLayerIndex, true);                
            }

            // set 'Default' layer to zero index - very important
            //if (defaultLayerIndex != 0)
            //{
            //    var zeroLayer = doc.Layers.FirstOrDefault(o => o.LayerIndex == 0);
            //    if (zeroLayer != null)
            //    {                    
            //        zeroLayer.LayerIndex = defaultLayerIndex;
            //        zeroLayer.CommitChanges();
            //    }
            //    var defLayer = doc.Layers[defaultLayerIndex];
            //    defLayer.LayerIndex = 0;
            //    defLayer.CommitChanges();
            //    defaultLayerIndex = defLayer.LayerIndex;
            //}

            //
            // Try to delete all layers except default
            //
            foreach (var o in doc.Layers)
            {
                // dont delete default layer
                if (o.LayerIndex == defaultLayerIndex)
                {
                    continue;
                }
                //o.Name = o.Name + o.LayerIndex; //debug
                o.IsVisible = true;
                o.IsLocked = false;
                o.CommitChanges();
            }

            foreach (var o in doc.Layers)
            {
                // dont delete default layer
                if (o.LayerIndex == defaultLayerIndex)
                {
                    continue;
                }
                
                // try to delete layer
                if (!doc.Layers.Purge(o.LayerIndex, true))
                {
                    //DEBUG
                    //RhinoApp.WriteLine("DEBUG:  RhinoFile.Open: Unable to delete layer " + o.Name);
                }
            }

            //DEBUG
            //var layers2 =  doc.Layers.Where(o => o.ParentLayerId == Guid.Empty).ToList();
            //var objs2 =  doc.Objects.ToList();

            //
            // Clear undo records after we had deleted all object and layers
            //
            doc.ClearRedoRecords();
            doc.ClearUndoRecords(true);

            //var layers3 =  doc.Layers.Where(o => o.ParentLayerId == Guid.Empty).ToList();
            //var objs4 =  doc.Objects.ToList();
        }
    }
}
