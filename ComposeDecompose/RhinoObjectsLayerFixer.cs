using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;

namespace SolidUtils.ComposeDecompose
{
    public static class RhinoObjectsLayerFixer
    {
        public static int Go(RhinoDoc doc)
        {
            if (doc == null || doc.Layers.Count == 0)
            {
                return 0;
            }

            var res = 0;
            res += RemoveEmptyLayers(doc);
            return res;
        }


        private static int RemoveEmptyLayers(RhinoDoc doc)
        {
            var res = 0;
            var deleted = true;
            while (deleted)
            {
                deleted = false;
                foreach (var layer in doc.Layers)
                {
                    if (layer._GetObjsCount(doc, false) == 0)
                    {
                        if (doc.Layers.Delete(layer.LayerIndex, true))
                        {
                            deleted = true;
                            res++;
                        }
                    }
                }
            }
            return res;
        }
    }
}
