using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    public class IssueWeight
    {
        /// <summary>
        /// Allows or disallows issue to be fixed in automated mode.
        /// Ovverides issue 'IsFixableManualOnly' property: if IsAutomateFixAllowed==false - 'IsFixableManualOnly' will be always true.
        /// </summary>
        public bool IsAutomateFixAllowed; 

        /// <summary>
        /// How much time it can take to fix this issue (or how difficult to fix this issue). 
        /// 15 - have problem hard to find and hard to fix 
        /// 10 - have problem hard to find but in final needs to rebuild surface 
        /// 7   - have problem easy to find but in final needs to rebuild surface 
        /// 5   - have some problem that not easy to fix
        /// 3   - have some trivial problem
        /// </summary>
        public int Complexity;

        /// <summary>
        /// How important to fix this issue
        /// </summary>
        public IssueSeverityType Severity;

        public int ComplexityLevel
        {
            get
            {
                if (Complexity <= 3) return 1;
                if (Complexity <= 5) return 2;
                if (Complexity <= 10) return 3;
                if (Complexity <= 20) return 4;
                return 5;
            }
        }
    }
}
