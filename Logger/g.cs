using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    public enum g
    {
        None,
        Temp,
        ExceptionHandler,
        _RhinoDoc,
        SpeedTest,
        ForeachParallel,
        SolidFix,
        //SolidFix_InvalidGeometry
        RhinoCommand,
        Topo,
        TopoStats,
        IssueFixer,
        IssueFixer_FaceProblems,
        IssueFinder,
        SolidNavigator,
        FileBrowser,
        SolidRhinoTricks,
        Mesher,
    }

    public static class g_enabled
    {
        public static bool IsEnabled(g group)
        {
            switch (group)
            {
                //case g.None:
                //    return false;
                default:
                    return true;
            }
        }
    }
}
