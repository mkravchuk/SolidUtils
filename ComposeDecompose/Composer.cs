using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using SolidUtils;

namespace SolidUtils.ComposeDecompose
{
    public static class Composer
    {
        public static void Go(RhinoDoc doc, bool afterFixAllIssues)
        {
            int progressCurrentIndex = 0;
            int progressMax = 0;
            int progressShowedPercent = 0;

            for (var tick = 0; tick <= 1; tick++)
            {
                foreach (var layer in doc.Layers)
                {
                    if (Layers.LayerIndexes.IsLayerIgnored(layer)) continue;
                    if (afterFixAllIssues && !layer.Name.StartsWith("OBJ:")) continue;

                    if (tick == 0)
                    {
                        var objsCount = GetBrepsToComposeCount(doc, layer);
                        if (objsCount > 1)
                        {
                            progressMax += objsCount;
                        }
                    }
                    else
                    {
                        Compose(doc, layer,
                                        ref progressCurrentIndex, ref progressShowedPercent, progressMax, afterFixAllIssues);
                    }
                }
            }
            // write new line for progress dots
            if (progressMax > 0)
            {
                log.info(g.SolidNavigator, "");
            }
            ColapseLayers(doc, afterFixAllIssues);
        }

        public static bool IsActive(RhinoDoc doc)
        {
            foreach (var layer in doc.Layers)
            {
                if (Layers.LayerIndexes.IsLayerIgnored(layer)) continue;

                if (GetBrepsToComposeCount(doc, layer) > 1)
                {
                    return true;
                }
            }
            return false;
        }

        private static int GetBrepsToComposeCount(RhinoDoc doc, Layer layer)
        {
            // select breps (valid only)            

            // v1 slow
            //return doc.Objects.FindByLayer(layer).Count(o => o.ObjectType == ObjectType.Brep
            //    //&& o.Geometry.IsValid - we cant check this in this function since we spend a lot of time for this additional check
            //    );

            // v2 fast
            return layer._GetObjsCount(doc, false, ObjectType.Brep);
        }

        private static RhinoObject[] GetBrepsToCompose(RhinoDoc doc, Layer layer, bool CheckValidity)
        {
            // select breps (valid only)
            if (CheckValidity)
            {
                return doc.Objects.FindByLayer(layer)
                    .Where(o => o.ObjectType == ObjectType.Brep && o.Geometry.IsValid).ToArray();
            }
            else
            {
                return doc.Objects.FindByLayer(layer)
                    .Where(o => o.ObjectType == ObjectType.Brep).ToArray();
            }
            
        }

        private static void ColapseLayers(RhinoDoc doc, bool afterFixAllIssues)
        {
            while (ColapseSomeLayer(doc, afterFixAllIssues))
            {
                // continue until all layers collapsed
            }
        }
        private static bool ColapseSomeLayer(RhinoDoc doc, bool afterFixAllIssues)
        {
            var blackList = new List<string>();
            for(int i = 0; i < doc.Layers.Count; i++)
            {
                var layer = doc.Layers[i];
                var layerName = afterFixAllIssues ? layer.Name.Replace("OBJ:", "") : layer.Name;
                var objsCountInLayer = layer._GetObjsCount(doc);
                if (objsCountInLayer != 1) continue; // layer has to have only one object

                var objs = GetBrepsToCompose(doc, layer, false);
                var objsLength = objs.Length;
                var layerChilds = layer.GetChildren();
                var layerHasNoChilds =  layerChilds == null || layerChilds.Length == 0;
                
                if (layerHasNoChilds // has no parent layers
                    && objsLength == 1                   // has only one object
                    && layer.ParentLayerId != Guid.Empty // is not root layer
                    && objs[0].Name == layerName        // obj name is same as layer
                    && !blackList.Contains(layer.Name) // is not banned in prev iteraction
                    && !Layers.LayerIndexes.IsLayerIgnored(layer)
                    )
                {
                    var parentLayerIndex = doc.Layers.Find(layer.ParentLayerId, true);
                    if (parentLayerIndex != -1)
                    {
                        var obj = doc.Objects.Find(objs[0].Id);
                        if (obj != null)
                        {
                            obj.Attributes.LayerIndex = parentLayerIndex;
                            obj.CommitChanges();
                            if (doc.Layers.Delete(layer.LayerIndex, true))
                            {
                                return true;
                            }
                        }
                    }
                    blackList.Add(layer.Name);
                }
            }
            return false;
        }

        private static void Compose(RhinoDoc doc, Layer layer, ref int progressCurrentIndex, ref int progressShowedPercent, int progressMax, bool afterFixAllIssues)
        {
            // select breps (valid only)
            var objs = GetBrepsToCompose(doc, layer, true);
            if (objs.Length <= 1)
            {
                return;
            }

            var breps = objs.Select(o => o.Geometry as Brep).ToArray();
            //foreach (var b in breps)
            //{
            //    string invalidLogMessage;
            //    if (!b.IsValidWithLog(out invalidLogMessage))
            //    {
            //        log.error(g.SolidNavigator, "Brep is invalid: {0}", invalidLogMessage);
            //    }
            //}

            // join breps
            var joinedBreps = Brep.JoinBreps(breps, 0.1);

            // add joined breps, and name same as layer name
            var index = 0;
            foreach (var joinedObj in joinedBreps)
            {
                var layerName = afterFixAllIssues ? layer.Name.Replace("OBJ:", "") : layer.Name;
                index++;
                var sub = "";
                if (joinedBreps.Length > 1)
                {
                    sub = index.ToString();
                }
                string invalidLogMessage;
                if (!joinedObj.IsValidWithLog(out invalidLogMessage))
                {
                    log.error(g.SolidNavigator, "Can't join  {0} breps into layer {1}: {2}", breps.Length, layer.Name, invalidLogMessage);
                    return;
                }
                var id = doc.Objects.AddBrep(joinedObj, new ObjectAttributes()
                {
                    LayerIndex = layer.LayerIndex,
                    Visible = true,
                    Name = layerName + sub
                });
                if (id == Guid.Empty)
                {
                    log.error(g.SolidNavigator, "Empty brep is returned by Brep.JoinBreps after joining {0} breps into layer", breps.Length, layer.Name);
                    return;
                }
            }

            // Deleting old objs
            var progressCurrentIndexNext = progressCurrentIndex + objs.Length;
            foreach (var obj in objs)
            {
                var deleted = obj._Delete();

                progressCurrentIndex++;
                var progressCurrentPercent = 100 - ((progressMax - progressCurrentIndex) * 100 / progressMax);
                if (progressCurrentPercent - progressShowedPercent >= 1)
                {
                    progressShowedPercent = progressCurrentPercent;
                    log.rawText(g.SolidNavigator, ".");
                }
            }
        }
    }
}
