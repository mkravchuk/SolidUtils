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
    /// <summary>
    /// Allows to get point based on percent instead of relative T parameter.
    /// Thread safe.
    /// </summary>
    public class CurveNormalized
    {
        /// <summary>
        /// Private - to avoid user to access this Curve and its domain or other function that can make a mistakes
        /// This class provide clear, errorless methods that correct in accessing 3d Curve domain parameters
        /// </summary>
        public Curve Crv { get; private set; }
        private double? _length;
        private object _lengthLockObject = new object();
        public double Length
        {
            get
            {
                if (!_length.HasValue)
                {
                    lock (_lengthLockObject)
                    {
                        if (!_length.HasValue)
                        {
                            _length = Crv._GetLength_ThreadSafe();
                        }
                    }
                }
                return _length.Value;
            }
        }
        public int Degree
        {
            get { return Crv.Degree; }
        }
        public int Dimension
        {
            get { return Crv.Dimension; }
        }

        private Interval? _domain;
        public Interval Domain
        {
            get
            {
                if (!_domain.HasValue)
                {
                    _domain = Crv.Domain;
                }
                return _domain.Value;
            }
        }

        public CurveNormalized(Curve crv, double? length = null)
        {
            Crv = crv;
            _length = length;
            _domain = null;
        }

        public double T(Percent p)
        {
            p.MustBeInScope01();

            double t;
            if (Crv.NormalizedLengthParameter(p, out t, 1E-08))
            {
                return t;
            }

            // very small curves are really hard to get some middle point - so dont bother - show a warning only for normal curves
            var len = Crv.PointAtStart._DistanceTo(Crv.PointAtEnd);
            if (len > 0.0001) 
            {                 
                log.wrong("CurveNormalized.T(Percent p) cannot get value from method 'NormalizedLengthParameter'");
            }
            // at least we can return approximated value
            return Crv.Domain.T0 + Crv.Domain.Length * p;
        }

        // same as 'T' but use sinlge method call to optimize perfromance
        public double[] T(Percent[] ps)
        {
            foreach(var p in ps) p.MustBeInScope01();

            double[] s = ps.Select(o => (double) o).ToArray();
            double[] t = Crv.NormalizedLengthParameters(s, 1E-08); // single method call to Rhinocommon
            if (t!=null)
            {
                return t;
            }
            return ps.Select(o => T(o)).ToArray();
        }


        public double T(CurveEnd end)
        {
            return (end == CurveEnd.Start) ? Domain.T0 : Domain.T1;
        }

        public Point3d PointAtStart
        {
            get { return Crv.PointAtStart; }
        }
        public Point3d PointAtEnd
        {
            get { return Crv.PointAtEnd; }
        }
        public Point3d PointAtMid
        {
            get { return PointAt(0.5); }
        }

        public Point3d PointAt(Percent p)
        {
            return Crv.PointAt(T(p));
        }
        public Point3d[] PointsAt(Percent[] ps)
        {
            double[] ts = T(ps);
            return ts.Select(o=>Crv.PointAt(o)).ToArray();
        }

        public Point3d PointAt(CurveEnd end)
        {
            return Crv.PointAt(T(end));
        }

        public Vector3d TangentAt(Percent p)
        {
            return Crv.TangentAt(T(p));
        }

        /// <summary>
        /// Needs to calculate Length of the curve. If you have it - create this class passing 'length' parameter.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Percent PercentAt(double t)
        {
            var subLength = Crv.GetLength(new Interval(Domain.T0, t));
            Percent p = subLength / Length;
            return p;
        }

        /// <summary>
        /// Needs to calculate Length of the curve. If you have it - create this class passing 'length' parameter.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Percent PercentAt(Point3d point)
        {
            double t;
            if (!Crv.ClosestPoint(point, out t))
            {
                throw new Exception("CurveNormalized.P(Point3d point)  failed to get T from 3d point!");
            }
            return PercentAt(t);
        }

        /// <summary>
        /// Remove portions of the curve outside the specified interval.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="failReason"></param>
        /// <returns></returns>
        public CurveNormalized Trim(Percent p0, Percent p1, out string failReason)
        {
            failReason = "";
            Curve res = null;
            if (p0.is0percent() && p1.is100percent())
            {
                res = Crv;
            }
            else
            {
                var t0 = T(p0);
                var t1 = T(p1);
                res = Crv.DuplicateCurve().Trim(t0, t1);
            }
            if (res == null)
            {
                failReason = "failed to trim crv";
                return null;
            }
            return new CurveNormalized(res);
        }
    }
}
