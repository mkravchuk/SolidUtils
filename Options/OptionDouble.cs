using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;

namespace SolidUtils
{
    public class OptionDouble : OptionBaseT<double>
    {
        public OptionDouble(string key, double defaultValue, string caption, Type[] relatedTo, OptionType optionType) 
            : base(key, defaultValue, caption, relatedTo, optionType)
        {
        }

        public override bool Load()
        {            
            double res;
            if (Settings.TryGetDouble(KeyFull, out res))
            {
                _Value = res;
                return true;
            }
            return false;
        }

        public override bool Save()
        {
            Settings.SetDouble(KeyFull, _Value);
            return true;
        }

        public static implicit operator double(OptionDouble option)
        {
            return option.Value;
        }
    }
}
