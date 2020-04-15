using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    public class FailedToFixIssue : Exception
    {
        public FailedToFixIssue(string failedReason)
            : base (failedReason)
        {

        }
    }
}
