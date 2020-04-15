using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class _UserControl
    {
        public static bool _IsInRuntimeMode(this UserControl c)
        {
            // c.DesignMode - doesnt wotk!!!
            return LicenseManager.UsageMode != LicenseUsageMode.Designtime;
        }

        public static void _InvokeIfRequired<T>(this T c, Action<T> action) where T : ISynchronizeInvoke
        {
            if (c.InvokeRequired)
            {
                //log.file("Invoke+");
                c.Invoke(action, new object[] { c });
                //log.file("Invoke-");
            }
            else
            {
                action(c);
            }
        }
    }
}
