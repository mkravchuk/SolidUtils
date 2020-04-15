using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;

namespace SolidUtils
{
    public class OptionStr : OptionBaseT<string>
    {
        public OptionStr(string key, string defaultValue, string caption, Type[] relatedTo, OptionType optionType) 
            : base(key, defaultValue, caption, relatedTo, optionType)
        {
        }

        public override bool Load()
        {
            var res = Settings.GetString(KeyFull, "DEF_VALUE");
            if (res != "DEF_VALUE")
            {
                _Value = res;
                return true;
            }
            return false;
        }

        public override bool  Save()
        {
            Settings.SetString(KeyFull, _Value);
            return true;
        }

        public static implicit operator string(OptionStr option)
        {
            return option.Value;
        }

        //public override string ToString()
        //{
        //    return Value;
        //}
    }
}
