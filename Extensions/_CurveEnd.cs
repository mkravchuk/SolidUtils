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
    public static class _CurveEnd
    {
        public static CurveEnd _Reverse(this CurveEnd end)
        {
            return end == CurveEnd.Start ? CurveEnd.End : CurveEnd.Start;
        }
    }
}
