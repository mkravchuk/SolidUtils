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
    public static class _ObjectTable
    {
        public static ObjectEnumeratorSettings _SelectedFilter
        {
            get
            {
                return new ObjectEnumeratorSettings()
                {
                    IncludePhantoms = true,
                    SelectedObjectsFilter = true,
                    ReferenceObjects = true,
                };
            }
        }

        public static ObjectEnumeratorSettings _HiddenFilter
        {
            get
            {
                return new ObjectEnumeratorSettings()
                {
                    IncludePhantoms = true,
                    HiddenObjects = true,
                    ReferenceObjects = true,
                };
            }
        }

        public static int _SelectedCount(this ObjectTable objects)
        {
            var count = objects.ObjectCount(_SelectedFilter);
            return count;
        }

        public static int _HiddenCount(this ObjectTable objects)
        {
            var filter_all_breps = new ObjectEnumeratorSettings()
            {
                IncludePhantoms = true,
                HiddenObjects = true,
                ReferenceObjects = true,
                ObjectTypeFilter = ObjectType.Brep
            };
            var filter_only_visible_breps = new ObjectEnumeratorSettings()
            {
                IncludePhantoms = true,
                HiddenObjects = true,
                ReferenceObjects = true,
                VisibleFilter = true,
                ObjectTypeFilter = ObjectType.Brep
            };
            var count = objects.ObjectCount(filter_all_breps) - objects.ObjectCount(filter_only_visible_breps);
            //var obs1 = objects.GetObjectList(filter_all_breps);
            //var obs2 = objects.GetObjectList(filter_only_visible_breps);
            //var obs = objects.GetObjectList(_HiddenFilter);
            return count;
        }

        /// <summary>
        /// Return selected object or null (no selected objects or more than 1 selected object)
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static RhinoObject _SelectedObject(this ObjectTable objects)
        {
            var count = objects._SelectedCount();
            if (count == 1)
            {
                //return objects.GetSelectedObjects(false, false).FirstOrDefault();
                return objects.GetObjectList(_SelectedFilter).FirstOrDefault();
            }
            return null;
        }

        /// <summary>
        /// Return selected breps
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static Brep[] _SelectedBreps(this ObjectTable objects)
        {
            return objects.GetObjectList(new ObjectEnumeratorSettings()
            {
                ObjectTypeFilter = ObjectType.Brep,
                SelectedObjectsFilter = true,
                ReferenceObjects = true,
            }).Where(o => o.Geometry is Brep).Select(o=>(Brep)o.Geometry).ToArray();
        }

        public static bool _Delete(this ObjectTable objects, RhinoObject obj)
        {
            if (obj == null || objects == null) return false;

            // for successful removal object must be visible
            if (obj.Attributes.Visible == false)
            {
                obj.Attributes.Visible = true;
                obj.CommitChanges();
            }
            var deleted = objects.Delete(obj, true);

            // if we can't delete - at least hide it
            if (!deleted)
            {
                obj.Attributes.Visible = false;
                obj.CommitChanges();
                log.wrong("Failed to delete object SN={0}", obj.RuntimeSerialNumber);
            }
            return deleted;
        }

        public static bool _Purge(this ObjectTable objects, RhinoObject obj)
        {
            if (obj == null || objects == null) return false;

            // for successful removal object must be visible
            if (obj.Attributes.Visible == false)
            {
                obj.Attributes.Visible = true;
                obj.CommitChanges();
            }
            bool removed = objects.Purge(obj);

            // if we can't remove - at least hide it
            if (!removed)
            {
                //var IsDocumentControlled = obj.Attributes.IsDocumentControlled;
                //var IsInstanceDefinitionObject = obj.Attributes.IsInstanceDefinitionObject;
                //var mode = obj.Attributes.Mode;
                //var name = obj.Attributes.Name;
                //var GroupCount = obj.Attributes.GroupCount;
                //removed = objects.Purge(obj);
                //var deleted = objects.Delete(obj.Id, true);               
                //var selectable = obj.IsSelectable();
                //obj.Attributes.RemoveDisplayModeOverride();
                //obj.CommitChanges();
                //obj.Attributes.Mode = ObjectMode.Hidden;
                //obj.Attributes.RemoveFromAllGroups();
                obj.Attributes.Visible = false;
                obj.CommitChanges();
            }
            
            return removed;
        }

    }
}
