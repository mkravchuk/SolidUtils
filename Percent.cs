using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    /// <summary>
    /// Value from 0 to 1
    /// </summary>
    public struct Percent
    {
        public double Value;

        public Percent(double p, bool valueSupposeToBeInScope01 = false)
        {
            Value = p;
            if (valueSupposeToBeInScope01 && (p < 0 || p > 1))
            {
                log.wrong("Class 'Percent' assume that value is in range [0..1]. Please check your argorithm.");
            }
        }

        public static implicit operator double(Percent p)
        {
            return p.Value;
        }

        public static implicit operator Percent(double p)
        {
            return new Percent { Value = p };
        }

        public override string ToString()
        {
            return (Value*100)._ToStringX(2) + "%";
        }

        public void MustBeInScope01()
        {
            if (Value < 0 || Value > 1)
            {
                log.wrong("Class 'Percent' assume that value is in range [0..1]. Current value {0}. Please check your argorithm.", ToString());
                if (Value < 0) Value = 0;
                if (Value > 1) Value = 1;
            }
        }

        public bool is0percent()
        {
            return Math.Abs(Value) < 1E-08;
        }
        public bool is100percent()
        {
            return Math.Abs(1 - Value) < 1E-08;
        }
    }
}
