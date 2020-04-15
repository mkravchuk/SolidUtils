using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using SolidUtils;

namespace SolidUtils
{
    public class FixWhat_FaceRebuildUV
    {
        public double MinUCount;
        public double MinVCount;
        public int IncreaseTimes { get; set; }
        public NurbsSurface NewSrf { get; set; }
        public int NewUCount { get; set; }
        public int NewVCount { get; set; }
    }

    public class FixWhat
    {
        public ComponentProblems Problems { get; set; }

        public string FixableFailReasons;
        public bool HasNotFixableProblem;
        public bool HasFixableProblem;

        public bool VertexRebuild;                            // Fix only vertexes
        public bool TrimSimplifyControlPoints;
        public bool TrimFixSeamControlPoints;               
        public bool TrimFixSingularity;               // Fix trim singularity
        public bool TrimFixDoubleSingularity;
        public bool TrimEndBeginMismatch;
        public bool TrimUVOutOfDomain;       // Just fix trim UV to be in Face.Domain scope
        public bool TrimRecreateFromEdge;     // recreate Trims from Edges
        public bool TrimZigZag;                     // Fix zigzag points for Crv2d
        public bool EdgeRecreateFromTrim;     // recreate Edges from Trims
        public bool EdgeRemoveKinks;                  // remove kinks at ends
        public bool EdgeRemoveZigZags;                     // Fix zigzag points for Crv3d
        public bool EdgeSimplifyControlPoints;
        public bool FaceDomainSet01;                    // just fix Face domain to be U*V = [0..1*0..1]
        public bool FaceRebuildUV;                 // rebuild surface to increase UV control points - used to remove deformation for big differences edges from trim
        public bool FaceRedundantSeam;         // trim underlying surface to remove seam problems
        public bool FaceRedundantSingularity;
        public bool IsExecuting_Fix_FaceRedundantSingularity;
        public bool FaceRebuildSurface;                    // rebuild surface to remove kinks
        public bool EdgeRemoveSmallUnatached;                   // Remove small edges
        public bool RemoveClosed;                 // Remove small edges
        public bool EdgeNeedJoin;

        public FixWhat(ComponentProblems problems = null)
        {
            Problems = problems ?? new ComponentProblems();

            foreach (var p in Problems.Problems)
            {
                // some problems cannot be fixed, but to norify user they must be present in GUI
                if (p.IsFixable.HasValue && !p.IsFixable.Value)
                {
                    HasNotFixableProblem = true;
                    if (!String.IsNullOrEmpty(FixableFailReasons)) FixableFailReasons += "; ";
                    FixableFailReasons += p.FixableFailReason;
                    continue;
                }

                bool found = true;
                switch (p.Type)
                {
                    //
                    //Vertex
                    //
                    case ComponentProblemTypes.VertexFarFromEdges:
                        VertexRebuild = true;
                        break;
                    //case ComponentProblemTypes.VertexFarFromTrims:
                    //    is filtered out in method 'ComponentProblemFinder.PostProcess_VertexFarFromTrims
                    //    this issue is transformed into FaceRebuildUV or eliminated
                    //    break;

                    //
                    //Face
                    //
                    case ComponentProblemTypes.FaceDomainLengthIsVerySmall:
                        FaceDomainSet01 = true;
                        break;
                    case ComponentProblemTypes.FaceRebuildUV:
                        FaceRebuildUV = true;
                        break;
                    case ComponentProblemTypes.FaceRedundantSeam:
                        FaceRedundantSeam = true;
                        break;
                    case ComponentProblemTypes.FaceRedundantSingularity:
                        FaceRedundantSingularity = true;
                        break;
                    case ComponentProblemTypes.FaceHasKinks:
                        FaceRebuildSurface = true;
                        break;

                    //
                    //Trim
                    //
                    case ComponentProblemTypes.TrimInvalidIsoStatus:
                        TrimFixSingularity = true;
                        break;
                    case ComponentProblemTypes.TrimDoubleIsoStatuses:
                        TrimFixDoubleSingularity = true;
                        break;
                    case ComponentProblemTypes.TrimEndBeginMismatch:
                        TrimEndBeginMismatch = true;
                        break;
                    case ComponentProblemTypes.TrimUVOutOfFaceDomain:
                        TrimUVOutOfDomain = true;
                        break;
                    case ComponentProblemTypes.TrimControlPointsNotCorrectInSeam:
                        TrimFixSeamControlPoints = true;
                        break;
                    case ComponentProblemTypes.TrimControlPointsNotCorrectInSingularity:
                        TrimFixSingularity = true;
                        break;
                    case ComponentProblemTypes.TrimControlPointsCanBeSimplified:
                        TrimSimplifyControlPoints = true;
                        break;
                    case ComponentProblemTypes.TrimZigZagControlpoints:
                        TrimZigZag = true;
                        break;

                    //
                    // Edge
                    //
                    case ComponentProblemTypes.EdgeStartAndEndVertexesSame:
                        VertexRebuild = true;
                        break;
                    case ComponentProblemTypes.EdgeIsClosedButNoUniquInLoop:
                    case ComponentProblemTypes.EdgeIsClosedButEndAndStartPointsAreFar:
                        RemoveClosed = true;
                        break;
                    case ComponentProblemTypes.EdgeVerySmall:
                        // problem is not fixable - it comes from ComponentProblemFinder which dont know about topology, but this problem can be solved only knowing topology
                        found = false;
                        break;
                    case ComponentProblemTypes.EdgeVerySmallUnatached:
                        // problem is fixable - it comes from GeomFaceProblemFinder which already know about topology
                        EdgeRemoveSmallUnatached = true;
                        break;
                    case ComponentProblemTypes.EdgeInvalidDomainLength:
                        EdgeRecreateFromTrim = true;
                        break;
                    case ComponentProblemTypes.EdgeCrossIntersection:
                        EdgeRecreateFromTrim = true;
                        break;
                    case ComponentProblemTypes.EdgeHasKinks:
                        EdgeRemoveKinks = true;
                        break;
                    case ComponentProblemTypes.EdgeZigZagControlpoints:
                        EdgeRemoveZigZags = true;
                        break;
                    case ComponentProblemTypes.EdgeControlPointsCanBeSimplified:
                        EdgeSimplifyControlPoints = true;
                        break;
                    case ComponentProblemTypes.EdgeNeedJoin:
                        EdgeNeedJoin = true;
                        break;

                    default:
                        found = false;
                        break;
                }

                if (found)
                {
                    HasFixableProblem = true;
                }
                else
                {
                    HasNotFixableProblem = true;
                    FixableFailReasons = Shared.AUTOFIX_NOT_IMPLEMENTED;
                }
            }

            // one of these options must be disabled - lets give priority to Edges - they usually more correct
            if (EdgeRecreateFromTrim && TrimRecreateFromEdge)
            {
                EdgeRecreateFromTrim = false;
                TrimRecreateFromEdge = true;
            }
        }

        public void Close()
        {
            Problems = null;
        }
    }

    
}
