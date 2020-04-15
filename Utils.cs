using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class Utils
    {
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static T GetPrivateField<T>(object obj, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic
                | BindingFlags.Public | BindingFlags.GetField | BindingFlags.ExactBinding;
            Type type = obj.GetType();
            FieldInfo field = type.GetField(name, flags);
            var val1 = field.GetValue(null);
            return (T)field.GetValue(obj);
        }

        public static T GetPrivateProperty<T>(object obj, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty;
            Type type = obj.GetType();
            PropertyInfo field = type.GetProperty(name, flags);
            return (T)field.GetValue(obj, null);
        }


        public static bool DirectoryHasWritePerMission(string path)
        {
            var writeAllow = false;
            var writeDeny = false;
            var accessControlList = Directory.GetAccessControl(path);
            if (accessControlList == null)
                return false;
            var accessRules = accessControlList.GetAccessRules(true, true,
                                        typeof(System.Security.Principal.SecurityIdentifier));
            if (accessRules == null)
                return false;

            foreach (FileSystemAccessRule rule in accessRules)
            {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                    continue;

                if (rule.AccessControlType == AccessControlType.Allow)
                    writeAllow = true;
                else if (rule.AccessControlType == AccessControlType.Deny)
                    writeDeny = true;
            }

            return writeAllow && !writeDeny;
        }

        public static bool FileHasWritePerMission(string filename)
        {
            return DirectoryHasWritePerMission(Path.GetDirectoryName(filename));
        }

        public static void Resize<T>(this List<T> list, int size, T c = default(T))
        {
            int curSize = list.Count;
            if (size < curSize)
                list.RemoveRange(size, curSize - size);
            else if (size > curSize)
                list.AddRange(Enumerable.Repeat(c, size - curSize));
        }
    }
}
