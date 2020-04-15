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
    public static class _NurbsCurve
    {
        public static Point3d[] _Locations(this NurbsCurve crv)
        {
            return crv.Points._Locations();
        }

        public static Point3d[] _Locations3d(this NurbsCurve crv, Surface srf = null)
        {
            var points = crv.Points._Locations();
            if (crv.Dimension == 2 && srf != null)
            {
                points = points.Select(o => srf.PointAt(o.X, o.Y)).ToArray();
            }
            return points;
        }

        public static bool _Is2dControlPointsOutOfFaceDomain(this NurbsCurve crv2d, Surface srf, out Percent maxOutsideDistPercent, out int outOfDomainCount)
        {
            Percent domainTolPercent = 0.01;
            var domainU = srf.Domain(0);
            var domainV = srf.Domain(1);
            var domainUTol = domainU.Length * domainTolPercent; //1% of domain length
            var domainVTol = domainV.Length * domainTolPercent;//1% of domain length
            outOfDomainCount = 0;
            var minU = domainU.T0 - domainUTol;
            var maxU = domainU.T1 + domainUTol;
            var minV = domainV.T0 - domainVTol;
            var maxV = domainV.T1 + domainVTol;
            maxOutsideDistPercent = 0;
            foreach (var p in crv2d.Points)
            {
                var u = p.Location.X;
                var v = p.Location.Y;
                if (u < minU || u > maxU || v < minV || v > maxV)
                {
                    double dist = 0;

                    if (u < minU)
                    {
                        dist = (domainU.T0 - u) / domainU.Length._GetNonZeroForDevisionOperation();
                        maxOutsideDistPercent = Math.Max(maxOutsideDistPercent, dist);
                    }
                    if (u > maxU)
                    {
                        dist = (u - domainU.T1) / domainU.Length._GetNonZeroForDevisionOperation();
                        maxOutsideDistPercent = Math.Max(maxOutsideDistPercent, dist);
                    }
                    if (v < minV)
                    {
                        dist = (domainV.T0 - v) / domainV.Length._GetNonZeroForDevisionOperation();
                        maxOutsideDistPercent = Math.Max(maxOutsideDistPercent, dist);
                    }
                    if (v > maxV)
                    {
                        dist = (v - domainV.T1) / domainV.Length._GetNonZeroForDevisionOperation();
                        maxOutsideDistPercent = Math.Max(maxOutsideDistPercent, dist);
                    }
                    outOfDomainCount++;
                }
            }
            return outOfDomainCount > 0;
        }
    }
}
