using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;

namespace SolidUtils.ComposeDecompose
{
    public static class RhinoObjectsGroupFixer
    {
        public static int Go(RhinoDoc doc)
        {
            if (doc == null || doc.Groups.Count == 0)
            {
                return 0;
            }

            var res = 0;
            res += Remove01Groups(doc);
            return res;
        }


        private static int Remove01Groups(RhinoDoc doc)
        {
            var res = 0;
            var groupNames = doc.Groups.GroupNames(true);
            if (groupNames == null) return 0;
            foreach (var groupName in groupNames)
            {
                if (!groupName.StartsWith("Group")) continue;
                var groupId = doc.Groups.Find(groupName, true);
                var objsInGroupdCount = doc.Groups.GroupObjectCount(groupId);
                if (objsInGroupdCount == 1)
                {
                    var objs = doc.Groups.GroupMembers(groupId);
                    if (objs != null && objs.Length == 1)
                    {
                        var obj = objs[0];
                        // dont remove group from polyfaces
                        if (obj.ObjectType == Rhino.DocObjects.ObjectType.Brep)
                        {
                            var brep = obj.Geometry as Brep;
                            if (brep != null && brep.Faces != null && brep.Faces.Count > 1)
                            {
                                continue;
                            }
                        }
                        obj.Attributes.RemoveFromGroup(groupId);
                        obj.CommitChanges();
                        res++;
                        doc.Groups.Delete(groupId);
                    }
                }
                else if (objsInGroupdCount == 0)
                {
                    res++;
                    doc.Groups.Delete(groupId);
                }
            }
            return res;
        }
    }
}
