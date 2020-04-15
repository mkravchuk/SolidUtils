using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class _File
    {
        public static bool _DeleteSave(string filename)
        {
            if (!File.Exists(filename)) return true;
            try
            {
                File.Delete(filename);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
