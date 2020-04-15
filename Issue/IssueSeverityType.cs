using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    //Severity: A severity classification of a software error is based on the degree of the error impact on the operation of the system. The severity classification is as follows: 
    //public enum SeverityType
    //{
    //    Low,        // The issue is an aesthetic, is an enhancement or is a result of non-conformance to a standard. 
    //    Medium,  // The issue does not cause a failure, does not impair usability, and does not interfere in the fluent work of the system and programs. 
    //    High,       // The issue does not cause a failure, but causes the system to produce incorrect, incomplete, inconsistent results or impairs the system usability. 
    //    Critical,    // The issue causes a failure of the complete software system, subsystem or a program within the system. 
    //}


    //public enum IssueWeightType
    //{
    //    Complex,             // The issue complexify component to much (surface, edge, trim, join edges, join surfaces)
    //    Inconsistent,       // The issue connects faces in inconsistent way, so meshing or texturing can have artifacts
    //    Deformed,           // The issue deforms face or curve (zigzags, kinks, edge-trim mismatch)
    //    Invalid,               // The issue makes brep invalid
    //}

    public enum IssueSeverityType
    {
        /// <summary>
        /// The issue complexify component to much (surface, edge, trim, join edges, join surfaces)
        /// </summary>
        Hint,
        /// <summary>
        /// The issue connects faces in inconsistent way, so meshing or texturing can have artifacts
        /// </summary>
        Suggestion,
        /// <summary>
        /// The issue deforms face or curve (zigzags, kinks, edge-trim mismatch)
        /// </summary>
        Warning,
        /// <summary>
        /// The issue makes brep invalid
        /// </summary>
        Error,
    }

    public static class IssueSeverityTypeManager
    {
        public static string GetDescription(IssueSeverityType severity)
        {
            switch (severity)
            {
                case IssueSeverityType.Hint:
                    return "The issue complexify component to much (surface, edge, trim, join edges, join surfaces)";
                case IssueSeverityType.Suggestion:
                    return "The issue connects faces in inconsistent way, so meshing or texturing can have artifacts";
                case IssueSeverityType.Warning:
                    return "The issue deforms face or curve (zigzags, kinks, edge-trim mismatch)";
                case IssueSeverityType.Error:
                    return "The issue makes brep invalid";
            }
            return "IssueSeverityTypeManager.GetCaption";
        }

        public static string[] GetCaptions()
        {
            return new[]
            {
                "Hint",
                "Suggestion",
                "Warning",
                "Error",
            };
        }
    }
}
