using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;

namespace SolidUtils
{
    public class OptionBool : OptionBaseT<bool>
    {
        public OptionBool(string key, bool defaultValue, string caption, Type[] relatedTo, OptionType optionType) 
            : base(key, defaultValue, caption, relatedTo, optionType)
        {
        }

        public override bool Load()
        {            
            bool res_bool;
            if (Settings.TryGetBool(KeyFull, out res_bool))
            {
                _Value = res_bool;
                return true;
            }
            return false;
        }

        public override bool Save()
        {
            Settings.SetBool(KeyFull, _Value);
            return true;
        }

        public static implicit operator bool(OptionBool option)
        {
            return option.Value;
        }
    }
}
