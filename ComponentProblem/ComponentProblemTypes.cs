using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;

namespace SolidUtils
{
    public enum ComponentProblemTypes
    {
        BrepIsInvalid,
        BrepIsEmpty,

        FaceWithNoArea,
        FaceIsIncomplete,
        FaceIsDuplicated,
        FaceDomainLengthIsVerySmall,
        FaceUnfixableTrimDefinition,
        FaceRebuildUV,
        FaceRedundantSeam,
        FaceRedundantSingularity,
        FaceHasKinks,
        FaceTrimEdgeMismatch,

        //LoopIncorrectOrientation,    // will be done later - very uncommon issue

        TrimDoubleIsoStatuses,
        TrimInvalidIsoStatus,
        TrimEndBeginMismatch,
        TrimControlPointsNotCorrectInSeam,
        TrimControlPointsNotCorrectInSingularity,
        TrimZigZagControlpoints,
        TrimUVOutOfFaceDomain,
        TrimControlPointsCanBeSimplified,

        EdgeStartAndEndVertexesSame,
        EdgeIsClosedButNoUniquInLoop,
        EdgeIsClosedButEndAndStartPointsAreFar,
        EdgeVerySmall,
        EdgeVerySmallUnatached,
        EdgeInvalidDomainLength,
        EdgeCrossIntersection,
        EdgeHasKinks,
        EdgeZigZagControlpoints,
        EdgeControlPointsCanBeSimplified,
        EdgeNeedJoin,

        VertexFarFromEdges,
        VertexFarFromTrims, // currently ignored. hard problem - it is for now ignored since visual problems are not often happend, and i tried to rebuild srf and increase crv CP's - results are random. So lets ignore this small issue for now.

        MeshIsEmpty,
        MeshHasDisjoints,
        MeshHasUnattachedVertices,
        MeshHasInvalidFaces,
        MeshHasFlippedFaces,
    }


    public static class ComponentProblemTypesManager
    {
        static public EnumInfo<ComponentProblemTypes> Infos = new EnumInfo<ComponentProblemTypes>();

        static ComponentProblemTypesManager()
        {
            Infos.Add(ComponentProblemTypes.BrepIsInvalid, true, "Brep is invalid ", null).SetWeight(IssueSeverityType.Error, 20, false);
            Infos.Add(ComponentProblemTypes.BrepIsEmpty, true, "Brep is empty ", null).SetWeight(IssueSeverityType.Error, 2, true);

            //Infos.Add(ComponentProblemTypes.LoopIncorrectOrientation, true, "Loop trims has incorrect orientation").SetWeight(IssueSeverityType.Error, 10, true);


            Infos.Add(ComponentProblemTypes.FaceWithNoArea, true, "Face with no area").SetWeight(IssueSeverityType.Error, 15, true);
            Infos.Add(ComponentProblemTypes.FaceIsIncomplete, true, "Face is incomplete").SetWeight(IssueSeverityType.Error, 15, false);
            Infos.Add(ComponentProblemTypes.FaceIsDuplicated, true, "Face is duplicated").SetWeight(IssueSeverityType.Error, 5, false);
            Infos.Add(ComponentProblemTypes.FaceDomainLengthIsVerySmall, true, "Face domain dimension is dangerously small or big", null, ComponentProblemTypes_Options.FaceDomainLengthIsVerySmall.InitSettings).SetWeight(IssueSeverityType.Warning, 5, true);
            Infos.Add(ComponentProblemTypes.FaceUnfixableTrimDefinition, true, "Face unfixable trim definitions").SetWeight(IssueSeverityType.Error, 7, false);
            Infos.Add(ComponentProblemTypes.FaceRebuildUV, true, "Face UV should be rebuilded").SetWeight(IssueSeverityType.Suggestion, 3, true); //- by now this issue is works improperly
            Infos.Add(ComponentProblemTypes.FaceRedundantSeam, true, "Face has redundant seam").SetWeight(IssueSeverityType.Warning, 7, true);
            Infos.Add(ComponentProblemTypes.FaceRedundantSingularity, true, "Face has redundant singularity").SetWeight(IssueSeverityType.Warning, 10, true);
            Infos.Add(ComponentProblemTypes.FaceHasKinks, false, "Face have kink").SetWeight(IssueSeverityType.Warning, 7, true);
            Infos.Add(ComponentProblemTypes.FaceTrimEdgeMismatch, true, "Faces has Mismatch in edges and trims", null, ComponentProblemTypes_Options.FaceTrimEdgeMismatch.InitSettings).SetWeight(IssueSeverityType.Warning, 10, false);

            Infos.Add(ComponentProblemTypes.TrimDoubleIsoStatuses, true, "Trim has redundant iso definition", 2022).SetWeight(IssueSeverityType.Error, 7, true);
            Infos.Add(ComponentProblemTypes.TrimInvalidIsoStatus, true, "Trim invalid iso definition", 2021).SetWeight(IssueSeverityType.Error, 7, true);
            Infos.Add(ComponentProblemTypes.TrimEndBeginMismatch, true, "Trim begin and end points mismatch", 2020).SetWeight(IssueSeverityType.Error, 7, true);
            Infos.Add(ComponentProblemTypes.TrimControlPointsNotCorrectInSeam, true, "Trim ControlPoints not correct in Seam", 2019).SetWeight(IssueSeverityType.Error, 10, true);
            Infos.Add(ComponentProblemTypes.TrimControlPointsNotCorrectInSingularity, true, "Trim ControlPoints not correct in Singularity", 2018).SetWeight(IssueSeverityType.Warning, 7, true);
            Infos.Add(ComponentProblemTypes.TrimZigZagControlpoints, true, "Trim deformed", 2017).SetWeight(IssueSeverityType.Warning, 15, true);
            Infos.Add(ComponentProblemTypes.TrimUVOutOfFaceDomain, true, "Trim outside of face domain", 2016).SetWeight(IssueSeverityType.Hint, 5, true);
            Infos.Add(ComponentProblemTypes.TrimControlPointsCanBeSimplified, true, "Trim can by simplified", null, ComponentProblemTypes_Options.ControlPointsCanBeSimplified.TrimSimplificationInitSettings).SetWeight(IssueSeverityType.Hint, 1, true);

            Infos.Add(ComponentProblemTypes.EdgeStartAndEndVertexesSame, true, "Edge start and end vertexes are same").SetWeight(IssueSeverityType.Error, 10, true);
            Infos.Add(ComponentProblemTypes.EdgeIsClosedButNoUniquInLoop, true, "Edge is wrongly closed").SetWeight(IssueSeverityType.Error, 10, true);
            Infos.Add(ComponentProblemTypes.EdgeIsClosedButEndAndStartPointsAreFar, true, "Edge is invalidly closed").SetWeight(IssueSeverityType.Error, 10, true);
            Infos.Add(ComponentProblemTypes.EdgeVerySmall, true, "Edge is very small").SetWeight(IssueSeverityType.Warning, 7, true);
            Infos.Add(ComponentProblemTypes.EdgeVerySmallUnatached, true, "Edge is very small and unattached").SetWeight(IssueSeverityType.Error, 15, true);
            Infos.Add(ComponentProblemTypes.EdgeInvalidDomainLength, true, "Edge invalid domain length").SetWeight(IssueSeverityType.Error, 10, true);
            Infos.Add(ComponentProblemTypes.EdgeCrossIntersection, true, "Edges cross-intersection", 1018).SetWeight(IssueSeverityType.Error, 10, true);
            Infos.Add(ComponentProblemTypes.EdgeHasKinks, true, "Edge have kink", 1017).SetWeight(IssueSeverityType.Warning, 10, true);
            Infos.Add(ComponentProblemTypes.EdgeZigZagControlpoints, true, "Edge deformed", 1016).SetWeight(IssueSeverityType.Warning, 15, true);
            Infos.Add(ComponentProblemTypes.EdgeControlPointsCanBeSimplified, false, "Edge can by simplified", 1015, ComponentProblemTypes_Options.ControlPointsCanBeSimplified.EdgeSimplificationInitSettings).SetWeight(IssueSeverityType.Hint, 1, true);
            Infos.Add(ComponentProblemTypes.EdgeNeedJoin, true, "Edges should be joined", 1014, ComponentProblemTypes_Options.EdgeNeedJoin.InitSettings).SetWeight(IssueSeverityType.Hint, 5, true);

            Infos.Add(ComponentProblemTypes.VertexFarFromEdges, true, "Vertex very far from edges", 1010).SetWeight(IssueSeverityType.Warning, 10, true);
            Infos.Add(ComponentProblemTypes.VertexFarFromTrims, true, "Vertex very far from trims").SetWeight(IssueSeverityType.Warning, 10, true);

            Infos.Add(ComponentProblemTypes.MeshIsEmpty, true, "Mesh is empty").SetWeight(IssueSeverityType.Error, 10, true);
            Infos.Add(ComponentProblemTypes.MeshHasInvalidFaces, true, "Mesh has invalid faces").SetWeight(IssueSeverityType.Error, 10, true);
            Infos.Add(ComponentProblemTypes.MeshHasDisjoints, true, "Mesh has disjoints").SetWeight(IssueSeverityType.Warning, 10, true);
            Infos.Add(ComponentProblemTypes.MeshHasUnattachedVertices, true, "Mesh has unattached vertices").SetWeight(IssueSeverityType.Warning, 10, true);
            Infos.Add(ComponentProblemTypes.MeshHasFlippedFaces, true, "Mesh has flipped faces").SetWeight(IssueSeverityType.Error, 10, true);

            Infos.ValidateEnumFullness();
        }

        public static bool IsEnabled(ComponentProblemTypes type)
        {
            return Infos[type].IsChecked;
        }
    }

    public static class ComponentProblemTypes_Options
    {
        public static class EdgeNeedJoin
        {
            public enum JoinFlexibilityType
            {
                /// <summary>
                /// Exact continiuty must be between curves (lowest flexibility)
                /// </summary>
                Low,

                /// <summary>
                /// Small difference in connection allowed (medium flexibility)
                /// </summary>
                Medium,

                /// <summary>
                /// Any valid joins allowed (highest flexibility)
                /// </summary>
                High
            }

            public static OptionEnum<JoinFlexibilityType> JoinFlexibility { get; set; }

            public static void InitSettings()
            {
                JoinFlexibility = new OptionEnum<JoinFlexibilityType>("EdgeNeedJoin_Flexibility", "Joint flexibility", new[] { typeof(ComponentProblemTypes_Options) }, OptionType.IssueOption)
                    .InitAsEnum(JoinFlexibilityType.Medium, new[]
                    {
                        "Low        (Exact continiuty must be between curves)",
                        "Medium   (Small difference in connection allowed)",
                        "High        (Any valid joins allowed)"
                    });
            }
        }

        public static class ControlPointsCanBeSimplified
        {
            public enum AnalyzeLevelType
            {
                /// <summary>
                /// Only huge simplification will be checked (contorl points above 300 and decrease for 5 times)
                /// </summary>
                Fast,

                /// <summary>
                /// Avarage simplification will be checked (contorl points above 130 and decrease for 3 times)
                /// </summary>
                Balanced,

                /// <summary>
                /// Small simplification will be checked (contorl points above 50 and decrease for 3 times)
                /// </summary>
                Detailed,

                /// <summary>
                /// Any simplification will be checked (contorl points above 20 and decrease for 2 times)
                /// </summary>
                Maximum
            }

            private static string[] Captions = new[]
            {
                "Fast             Only huge simplification will be checked (control points above 300 and decrease for 5 times)",
                "Balanced      Avarage simplification will be checked (control points above 130 and decrease for 3 times)",
                "Detailed      Any small trim simplification will be checked (control points above 50 and decrease for 3 times)",
                "Maximum    Any simplification will be checked (contorl points above 20 and decrease for 2 times)"
            };

            public static OptionEnum<AnalyzeLevelType> TrimSimplificationAnalyzeLevel { get; set; }

            public static void TrimSimplificationInitSettings()
            {
                TrimSimplificationAnalyzeLevel = new OptionEnum<AnalyzeLevelType>("ControlPointsCanBeSimplified_TrimSimplificationAnalyzeLevel", "Simplification level", new[] { typeof(ComponentProblemTypes_Options) }, OptionType.IssueOption)
                    .InitAsEnum(AnalyzeLevelType.Fast, Captions);
            }

            public static OptionEnum<AnalyzeLevelType> EdgeSimplificationAnalyzeLevel { get; set; }

            public static void EdgeSimplificationInitSettings()
            {
                EdgeSimplificationAnalyzeLevel = new OptionEnum<AnalyzeLevelType>("ControlPointsCanBeSimplified_EdgeSimplificationAnalyzeLevel", "Simplification level", new[] { typeof(ComponentProblemTypes_Options) }, OptionType.IssueOption)
                    .InitAsEnum(AnalyzeLevelType.Balanced, Captions);
            }

            public static int AnalyzeLevelType_CPCountToTest(AnalyzeLevelType type)
            {
                switch (type)
                {
                    case AnalyzeLevelType.Fast:
                        return 300;
                    case AnalyzeLevelType.Balanced:
                        return 130;
                    case AnalyzeLevelType.Detailed:
                        return 50;
                    case AnalyzeLevelType.Maximum:
                        return 20;
                }
                return 100;
            }

            public static int AnalyzeLevelType_DecreaseTime(AnalyzeLevelType type)
            {
                switch (type)
                {
                    case AnalyzeLevelType.Fast:
                        return 5;
                    case AnalyzeLevelType.Balanced:
                        return 3;
                    case AnalyzeLevelType.Detailed:
                        return 3;
                    case AnalyzeLevelType.Maximum:
                        return 2;
                }
                return 100;
            }

        }

        public static class FaceDomainLengthIsVerySmall
        {
            public enum SensetivityEnum
            {
                Exact,
                Less001, Less01, Less02, Less05, Less1,
                Greater1, Greater2, Greater5, Greater10, Greater100,
                Less_01_Greater_10, Less_02_Greater_20
            }

            public static class SensetivityEnumManager
            {
                public static string[] Names = new[]
        {
            "Must be exact [0..1]",
            "Length < 0.01", "Length < 0.1", "Length < 0.2", "Length < 0.5", "Length < 1",
            "Length > 1", "Length > 2", "Length > 5", "Length > 10", "Length > 100",
            "Length < 0.1  or  Length > 10", "Length < 0.2  or  Length > 20"
        };

            }

            public static OptionEnum<SensetivityEnum> Sensetivity { get; set; }

            public static void InitSettings()
            {
                Sensetivity = new OptionEnum<SensetivityEnum>("FaceDomainLengthIsVerySmall_Sensetivity", "Face domain dimension is dangerous when", new[] { typeof(ComponentProblemTypes_Options) }, OptionType.IssueOption)
                   .InitAsEnum(SensetivityEnum.Less_01_Greater_10, SensetivityEnumManager.Names);
            }
        }

        public class FaceTrimEdgeMismatch
        {
            public static OptionEnum<double> MAX_DIFF_RELATIVE_ALLOWED { get; set; }
            public static OptionEnum<double> MAX_DIST_ABSOLUTE_ALLOWED { get; set; }

            public static void InitSettings()
            {
                MAX_DIFF_RELATIVE_ALLOWED = new OptionEnum<double>("FaceTrimEdgeMismatch_MAX_DIFF_ALLOWED", "Maximum allowed relative distance between edge and trim (distance relative  to edge length)", IssueOptions.RelatedTo, OptionType.IssueOption)
    .InitAsValues(0.01, new[] { 0.001, 0.003, 0.005, 0.01, 0.03, 0.05, 0.07, 0.1, 0.2, 0.3 }, new[] { "0.1%", "0.3%", "0.5%", "1%", "3%", "5%", "7%", "10%", "20%", "30%" });
                MAX_DIST_ABSOLUTE_ALLOWED = new OptionEnum<double>("FaceTrimEdgeMismatch_MAX_DIST_ALLOWED", "Maximum allowed absolute distance between edge and trim", IssueOptions.RelatedTo, OptionType.IssueOption)
    .InitAsValues(0.01, new[] { 0.0001, 0.001, 0.005, 0.07, 0.01, 0.03, 0.05, 0.1, 0.3, 0.5 });
            }
        }
    }
}
