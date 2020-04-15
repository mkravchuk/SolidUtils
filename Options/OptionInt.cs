using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;

namespace SolidUtils
{
    public class OptionInt : OptionBaseT<int>
    {
        public OptionInt(string key, int defaultValue, string caption, Type[] relatedTo, OptionType optionType) 
            : base(key, defaultValue, caption, relatedTo, optionType)
        {
        }

        public override bool Load()
        {            
            int res;
            if (Settings.TryGetInteger(KeyFull, out res))
            {
                _Value = res;
                return true;
            }
            return false;
        }

        public override bool Save()
        {
            Settings.SetInteger(KeyFull, _Value);
            return true;
        }

        public static implicit operator int(OptionInt option)
        {
            return option.Value;
        }
    }
}
