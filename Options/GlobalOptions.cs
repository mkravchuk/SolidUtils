using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    public static class GlobalOptions
    {
        public static List<OptionBase> Options { get; set; }
        public static Rhino.PersistentSettings Settings;

        static GlobalOptions()
        {
            Options = new List<OptionBase>();
        }


        private static bool IsLoaded;
        public static void Load()
        {
            for (int i = 0;i < Options.Count; i++)
            {
                var o = Options[i];
                Shared.TryCatchAction(() => o.Load(), g.SolidFix, "Failed to load option '{0}'"._Format(o.Caption));
            }


            //foreach (var o in Options)
            //{
            //    o.CallOnLoadEvent();
            //}
            for (int i = 0; i < Options.Count; i++)
            {
                var o = Options[i];
                Shared.TryCatchAction(o.CallOnLoadEvent, g.SolidFix, "Failed to call OnLoad event on option '{0}'"._Format(o.Caption));
            }
            IsLoaded = true;
        }

        public static void Save()
        {
            //foreach (var o in Options)
            //{
            //    o.Save(settings);
            //}

            for (int i = 0; i < Options.Count; i++)
            {
                var o = Options[i];
                Shared.TryCatchAction(() => o.Save(), g.SolidFix, "Failed to call OnLoad event on option '{0}'"._Format(o.Caption));
            }
        }

        public static void Add(OptionBase option)
        {
            if (IsLoaded)
            {
                //log.temp("Options '{0}' is added after GlobalOptions was loaded", option.KeyShort);
            }
            Options.Add(option);
        }



    }
}
