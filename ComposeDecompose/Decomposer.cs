using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using SolidUtils;

namespace SolidUtils.ComposeDecompose
{
    public static class Decomposer
    {
        public static int Go(RhinoDoc doc, bool beforeFixAllIssues)
        {
            int progressCurrentIndex = 0;
            int progressMax = 0;
            int progressShowedPercent = 0;
            var decomposedCount = 0;

            for (var tick = 0; tick <= 1; tick++)
            {
                foreach (var layer in doc.Layers)
                {
                    if (Layers.LayerIndexes.IsLayerIgnored(layer)) continue;

                    DecomposeLayer(doc, layer
                        , ref decomposedCount, tick
                        , ref progressCurrentIndex, ref progressShowedPercent, ref progressMax, beforeFixAllIssues);
                }
            }

            // write new line for progress dots
            if (progressMax > 0)
            {
                log.info(g.SolidNavigator, "");
            }
            return decomposedCount;
        }

        public static bool IsActive(RhinoDoc doc)
        {
            foreach (var layer in doc.Layers)
            {
                if (Layers.LayerIndexes.IsLayerIgnored(layer)) continue;

                //var has_slow = doc.Objects.FindByLayer(layer)
                //    .FirstOrDefault(o => o.ObjectType == ObjectType.Brep
                //        //&& o.Geometry.IsValid - we cant check this in this function since we spend a lot of time for this additional check
                //                         && (o.Geometry as Brep).Faces.Count > 1
                //    ) != null;

                var has = doc.Objects.GetObjectList(new ObjectEnumeratorSettings()
                {
                    LayerIndexFilter = layer.LayerIndex,
                    ObjectTypeFilter = ObjectType.Brep
                }).FirstOrDefault(o => (o.Geometry is Brep) && ((o.Geometry as Brep).Faces.Count > 1)) != null;
                if (has)
                {
                    return true;
                }
            }
            return false;
        }

        public static void DecomposeLayer(RhinoDoc doc, Layer layer, ref int decomposedCount, int tick, ref int progressCurrentIndex, ref int progressShowedPercent, ref int progressMax, bool beforeFixAllIssues)
        {
            var objs = doc.Objects.FindByLayer(layer);
            foreach (var obj in objs)
            {
                if (obj.ObjectType == ObjectType.Brep)
                {
                    var brep = obj.Geometry as Brep;
                    if (brep != null && brep.Faces.Count > 1)
                    {
                        if (tick == 0)
                        {
                            progressMax += brep.Faces.Count;
                            //if (obj.IsValid)
                            //{
                            //    progressMax += brep.Faces.Count;
                            //}
                            //else
                            //{
                            //    log.warn(g.SolidNavigator, "        Cannot decompose object '{0}' in layer '{1}' since it has some invalid face, what will be lost by uncomposing.", obj.Name, layer.Name);
                            //}
                        }
                        else
                        {
                            //string  invalidobjlog;
                            //brep.IsValidWithLog(out invalidobjlog);
                            if (obj.IsValid)
                            {
                                DecomposeObj(doc, layer, obj, brep,
                                    ref progressCurrentIndex, ref progressShowedPercent, progressMax, beforeFixAllIssues, objs.Length);
                                decomposedCount++;
                            }
                            else
                            {
                                DecomposeObj_Invalid(doc, layer, obj, brep,
                                    ref progressCurrentIndex, ref progressShowedPercent, progressMax, beforeFixAllIssues, objs.Length);
                                decomposedCount++;
                            }
                        }
                    }
                }
            }
        }

        public static void DecomposeObj_Invalid(RhinoDoc doc, Layer layer, RhinoObject obj, Brep brep, ref int progressCurrentIndex, ref int progressShowedPercent, int progressMax, bool beforeFixAllIssues, int objsInLayerCount = -1)
        {
            var newLayerIndex = GetNewLayer(doc, layer, obj, beforeFixAllIssues);
            var newlayer = doc.Layers[newLayerIndex];
            //var savedCurrentIndex = doc.Layers.CurrentLayerIndex;
            //doc.Layers.SetCurrentLayerIndex(newLayerIndex, true);
            try
            {
                //var objsbefore = doc.Objects.FindByLayer(layer);
                //var map = new Dictionary<uint, bool>();
                //foreach (var o in objsbefore)
                //{
                //    map[o.RuntimeSerialNumber] = true;
                //}
                // move obj to new layer
                obj.Attributes.LayerIndex = newLayerIndex;
                obj.CommitChanges();
                // explode it
                doc.Objects.UnselectAll();
                doc.Objects.Select(obj.Id);
                if (RhinoApp.RunScript("_Explode", false))
                {
                    var objsnow = doc.Objects.FindByLayer(newlayer);
                    objsInLayerCount = objsnow.Length;
                    int index = 0;
                    int nameLength = 1;
                    if (objsInLayerCount >= 10) nameLength = 2;
                    if (objsInLayerCount >= 100) nameLength = 3;
                    if (objsInLayerCount >= 1000) nameLength = 4;
                    if (objsInLayerCount >= 10000) nameLength = 5;
                    foreach (var newobj in objsnow)
                    {
                        index++;
                        newobj.Attributes.Name = index.ToString("D" + nameLength);
                        newobj.CommitChanges();
                    }
                    Shared.SharedCommands.Execute(SharedCommandsEnum.ST_UpdateGeomNames);
                }
                progressCurrentIndex += brep.Faces.Count;
            }
            finally
            {
                doc.Objects.UnselectAll();
                //doc.Layers.SetCurrentLayerIndex(savedCurrentIndex, true);
            }
        }

        private static int GetNewLayer(RhinoDoc doc, Layer layer, RhinoObject obj, bool beforeFixAllIssues)
        {
            var newLayerIndex = layer.LayerIndex;
            //if (layer.Name != obj.Name
            //    || objsInLayerCount > 1
            //    || beforeFixAllIssues)
            //{
            var layername = beforeFixAllIssues ? "OBJ:" + obj.Name : obj.Name;
            var index = 1;
            var layername_wanted = layername;
            while (doc.Layers.Find(layername, true) != -1) 
            {
                index++;
                layername = layername_wanted + "_"+index.ToString();
            }
            newLayerIndex = doc.Layers.Add(new Layer()
            {
                ParentLayerId = layer.Id,
                Name = layername,
            });
            //}
            return newLayerIndex;
        }

        public static void DecomposeObj(RhinoDoc doc, Layer layer, RhinoObject obj, Brep brep, ref int progressCurrentIndex, ref int progressShowedPercent, int progressMax, bool beforeFixAllIssues, int objsInLayerCount = -1)
        {
            //if (objsInLayerCount == -1)
            //{
            //    objsInLayerCount = layer._GetObjsCount(doc, false, ObjectType.Brep);
            //}

            var newLayerIndex = GetNewLayer(doc, layer, obj, beforeFixAllIssues);
            int index = 0;
            int nameLength = 1;
            if (brep.Faces.Count >= 10) nameLength = 2;
            if (brep.Faces.Count >= 100) nameLength = 3;
            if (brep.Faces.Count >= 1000) nameLength = 4;
            if (brep.Faces.Count >= 10000) nameLength = 5;

            foreach (BrepFace face in brep.Faces_ThreadSafe())
            {
                index++;

                // Duplicate brep
                var face_copy = face.DuplicateFace(true);
                var id = doc.Objects.AddBrep(face_copy, new ObjectAttributes()
                {
                    LayerIndex = newLayerIndex,
                    Visible = true,
                    Name = index.ToString("D" + nameLength),
                });

                // Add to group
                if (id != Guid.Empty && obj.GroupCount > 0)
                {
                    foreach (var groupId in obj.GetGroupList())
                    {
                        doc.Groups.AddToGroup(groupId, id);
                    }
                }

                // Show progress
                progressCurrentIndex++;
                var progressCurrentPercent = 100 - ((progressMax - progressCurrentIndex) * 100 / progressMax);
                if (progressCurrentPercent - progressShowedPercent >= 1)
                {
                    progressShowedPercent = progressCurrentPercent;
                    log.rawText(g.SolidNavigator, ".");
                }
            }

            obj._Delete();
        }


    }
}
