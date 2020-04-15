using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;

namespace SolidUtils
{
    public class OptionStrList : OptionBaseT<string[]>
    {
        public OptionStrList(string key, string[] defaultValue, string caption, Type[] relatedTo, OptionType optionType) 
            : base(key, defaultValue, caption, relatedTo, optionType)
        {
        }

        public override bool  Load()
        {
            var res = Settings.GetStringList(KeyFull, null);
            if (res != null)
            {
                _Value = res;
                return true;
            }
            return false;
        }

        public override bool  Save()
        {
            Settings.SetStringList(KeyFull, _Value);
            return true;
        }
    }
}
