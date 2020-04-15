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
    public static class _Double
    {
        public const double ZERO = 0.00000000001;


        public static string  _ToQualityStr(this double quality)
        {
            if (quality._IsSame(100))
            {
                return "PERFECT";
            }
            return quality._ToStringX(3) + "%";
        }
        public static bool _IsSame(this double o, double otherValue)
        {
            return Math.Abs(o - otherValue) < ZERO;
        }
        public static bool _IsSame(this double o, double otherValue, double tol)
        {
            return Math.Abs(o - otherValue) < tol;
        }
        public static bool _IsZero(this double o)
        {
            return o < ZERO;
        }
        public static double _GetNonZeroForDevisionOperation(this double o)
        {
            return o < ZERO ? ZERO : o;
        }
        public static string _ToStringX(this double o, int length)
        {
            if (double.IsNaN(o)) return "NAN";
            return string.Format("{0:N" + length + "}", o);
        }
        public static string _ToStringAngle(this double o)
        {
            return o._ToStringX(0) + "°";
            return o._ToStringX(0);
        }
        
        public static double _RoundToValue(this double value, double alternativeValue, double tol)
        {
            if (Math.Abs(value - alternativeValue) < tol)
            {
                return alternativeValue;
            }
            return value;
        }

        public static double _RoundToDomainMinMax(this double value, Interval domain, double tolInPercent = 0.01)
        {
            var tolAbsolute = tolInPercent * domain.Length;
            value = value._RoundToValue(domain.T0, tolAbsolute);
            value = value._RoundToValue(domain.T1, tolAbsolute);
            return value;
        }

        public static double _Limit(this double value, double min, double max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }
        public static double _Limit(this double value, double min, double max, out bool changed)
        {
            changed = false;
            if (value < min)
            {
                changed = true;
                value = min;
            }
            if (value > max)
            {
                changed = true;
                value = max;
            }
            return value;
        }

        public static double _Normalize(this double value, double min, double max)
        {
            double norm = Math.Abs(value - min) / (max - min);
            if (norm < 0) norm = 0;
            if (norm > 1) norm = 1;
            return norm;
        }

        public static double _DegreeToRadian(this double angle)
        {
            return  angle * (Math.PI / 180.0);
        }

        public static double _DegreeToCos(this double angle)
        {
            return Math.Cos(_DegreeToRadian(angle));
        }

        public static double _RadianToDegree(this double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        /// <summary>
        /// Difference between two values in percents: from 0 to 1.
        /// The less percent - the biggest difference between values.
        /// Example: 
        /// [2, 4] ==> 0.5
        /// [4, 2] ==> 0.5
        /// [1, 4] ==> 0.25
        /// [10, 1] ==> 0.1
        /// </summary>
        /// <param name="value"></param>
        /// <param name="compareToValue"></param>
        /// <returns></returns>
        public static Percent _DiffInPercent(this double value, double compareToValue)
        {
            // ver 1
            //var min = Math.Min(value, compareToValue);
            //var max = Math.Max(value, compareToValue);
            //var res = min / max._GetNonZeroForDevisionOperation();
            //return res;

            //ver 2 - speed optimized
            if (value < compareToValue)
            {
                return value / compareToValue._GetNonZeroForDevisionOperation();
            }
            else
            {
                return compareToValue / value._GetNonZeroForDevisionOperation();
            }
        }

        public static int _CompareSortValue(this double a, double b)
        {
            if (a._IsSame(b)) return 0;
            return a < b ? -1 : 1;
        }
    }
}
