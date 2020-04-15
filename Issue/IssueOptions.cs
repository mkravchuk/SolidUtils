using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    public static class IssueOptions
    {
        public static Type[] RelatedTo = {typeof (IssueOptions)};
        public static double MaxDeformationAllowed = 0.01;
        public static double VerySmallLength = 0.001; // constant value
        public static OptionEnum<double> SmallEdgesLength { get; set; }
        private static OptionBool DebugShowFailedFixesOption { get; set; }
        public static bool DebugShowFailedFixes {
            get
            {
                if (!Debugger.IsAttached) return false;
                return DebugShowFailedFixesOption.Value;
            }
        }

        public static void InitSettings()
        {
            SmallEdgesLength = new OptionEnum<double>("SmallEdgesLength", "Small edge length", RelatedTo, OptionType.IssueGlobalOption)
                .InitAsValues(0.1, new[] { 0.001, 0.003, 0.005, 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08, 0.09, 0.1, 0.2, 0.3, 0.5, 0.7, 1, 2, 3, 5, 9 });
            DebugShowFailedFixesOption = new OptionBool("DebugShowFailedFixes", false, null, RelatedTo, OptionType.Debug);
        }
    }
}
