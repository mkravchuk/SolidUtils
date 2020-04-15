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
    public static class _RhinoObject
    {
        public static bool _Delete(this RhinoObject obj)
        {
            if (obj != null && obj.Document != null)
            {
                return obj.Document.Objects._Delete(obj);
            }
            return false;
        }

        public static bool _Purge(this RhinoObject obj)
        {
            if (obj != null && obj.Document != null)
            {
                return obj.Document.Objects._Purge(obj);
            }
            return false;
        }


        public static bool _IsAvailable(this RhinoObject obj)
        {
            return obj._IsAvailable(RhinoDoc.ActiveDoc);
        }

        public static bool _IsAvailable(this RhinoObject obj, RhinoDoc doc)
        {
            if (obj == null) return false;
            if (doc == null) return false;
            var res = (doc.Objects.Find(obj.RuntimeSerialNumber) != null && !obj.IsDeleted);
            return res;
        }


        public static void _CreateMesh(this RhinoObject obj, MeshType meshType = MeshType.Render, MeshingParameterStyle meshQuaility = MeshingParameterStyle.None)
        {
            if (obj == null) return;
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return;

            if (meshQuaility == MeshingParameterStyle.None)
            {
                meshQuaility = doc.MeshingParameterStyle;
            }
            var res = obj.CreateMeshes(meshType, doc.GetMeshingParameters(meshQuaility), false);
        }
    }
}
