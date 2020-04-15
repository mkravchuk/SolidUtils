using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace SolidUtils
{
    public class ComponentProblem
    {
        public ComponentIndex Component { get; set; } // what is a real component (Trim or Edge) 
        internal ComponentIndex ComponentGUI { get; set; } // what user will see in Issues list (Edge.EdgeIndex is seen as Trim.TrimIndex)
        public string Problem { get; set; }
        public object Data { get; set; }
        public ComponentProblemTypes Type { get; set; }
        public bool? IsFixable { get; set; } // by default this is undefined. But it is possible to say that this problem has no fix by setting this parameter to 'false'
        public string FixableFailReason { get; set; }

        public override string ToString()
        {
            return "<ComponentProblem> : " + Info;
        }

        public string Info
        {
            get
            {
                if (ComponentGUI.ComponentIndexType == ComponentIndexType.InvalidType)
                {
                    return Problem;
                }
                var componentType = "";
                switch (ComponentGUI.ComponentIndexType)
                {
                    case ComponentIndexType.BrepFace:
                        componentType = "Face";
                        break;
                    case ComponentIndexType.BrepEdge:
                        componentType = "Edge";
                        break;
                    case ComponentIndexType.BrepLoop:
                        componentType = "Loop";
                        break;
                    case ComponentIndexType.BrepTrim:
                        componentType = "Trim";
                        break;
                    case ComponentIndexType.BrepVertex:
                        componentType = "Vertex";
                        break;
                    default:
                        componentType = ComponentGUI.ComponentIndexType.ToString().Replace("Brep", "");
                        break;
                }
                
                var num = Shared.GUIComponentNum(ComponentGUI.Index); // in GUI we show starting from 1 or from 0
                var res = componentType + " " + num + ": " + Problem;
                return res;
            }
        }

        public void Close()
        {
            // do not close Data - it is important to have access after Geom is closed
        }
    }

    public class ComponentProblems
    {
        public List<ComponentProblem> Problems { get; set; }
        internal bool IsProblemsSorted { get; set; }

        public ComponentProblems()
        {
            Problems = new List<ComponentProblem>();
        }

        private FixWhat fixWhat;
        public FixWhat FixWhat
        {
            get
            {
                if (fixWhat == null) fixWhat = new FixWhat(this);
                return fixWhat;
            }
        }

        public static void AddRange(ref ComponentProblems problems, ComponentProblems newProblems)
        {
            if (newProblems == null) return;
            if (problems == null)
            {
                problems = new ComponentProblems();
            }
            problems.Problems.AddRange(newProblems.Problems);
            problems.IsProblemsSorted = false;
            problems.fixWhat = null;
        }


        public static ComponentProblem Add(ref ComponentProblems problems, string problem, ComponentProblemTypes type, object data = null)
        {
            return Add(ref problems, ComponentIndex.Unset, ComponentIndex.Unset, problem, type, data);
        }

        public static ComponentProblem Add(ref ComponentProblems problems, BrepFace face, string problem, ComponentProblemTypes type, object data = null)
        {
            var componentReal = new ComponentIndex(ComponentIndexType.BrepFace, face.FaceIndex);
            return Add(ref problems, componentReal, componentReal, problem, type, data);
        }

        public static ComponentProblem Add(ref ComponentProblems problems, Mesh mesh, string problem, ComponentProblemTypes type, object data = null)
        {
            //var componentReal = new ComponentIndex(ComponentIndexType.MeshFace, 0);
            
            var componentReal = mesh != null ? mesh.ComponentIndex() : new ComponentIndex(ComponentIndexType.MeshFace, 0);
            return Add(ref problems, componentReal, componentReal, problem, type, data);
        }

        public static ComponentProblem Add(ref ComponentProblems problems, BrepVertex vertex, string problem, ComponentProblemTypes type, object data = null)
        {
            var componentReal = new ComponentIndex(ComponentIndexType.BrepVertex, vertex.VertexIndex);
            return Add(ref problems, componentReal, componentReal, problem, type, data);
        }

        public static ComponentProblem Add(ref ComponentProblems problems, BrepTrim trim, string problem, ComponentProblemTypes type, object data = null)
        {
            var componentReal = new ComponentIndex(ComponentIndexType.BrepTrim, trim.TrimIndex);
            return Add(ref problems, componentReal, componentReal, problem, type, data);
        }

        public static ComponentProblem Add(ref ComponentProblems problems, BrepEdge edge, BrepTrim trim, string problem, ComponentProblemTypes type, object data = null)
        {
            var componentReal = new ComponentIndex(ComponentIndexType.BrepEdge, edge.EdgeIndex);
            var componentGUI = new ComponentIndex(ComponentIndexType.BrepEdge, trim.TrimIndex);
            return Add(ref problems, componentReal, componentGUI, problem, type, data);
        }

        private static ComponentProblem Add(ref ComponentProblems problems, ComponentIndex componentReal, ComponentIndex componentGUI, string problem, ComponentProblemTypes type, object data = null)
        {
            if (problems == null) problems = new ComponentProblems();
            var res = problems.AddProblem(componentReal, componentGUI, problem, type, data);
            return res;
        }

        

        private ComponentProblem AddProblem(ComponentIndex componentReal, ComponentIndex componentGUI, string problem, ComponentProblemTypes type, object data = null)
        {
            var p = new ComponentProblem
            {
                Component = componentReal,
                ComponentGUI = componentGUI,
                Problem = problem,
                Type = type,
                Data = data
            };
            Problems.Add(p);
            IsProblemsSorted = false;
            fixWhat = null;
            return p;
        }


        //public void AddProblem(BrepEdge edge, BrepTrim trim, string problem, ComponentProblemTypes type, object data = null)
        //{

        //}

        public override string ToString()
        {
            return "<ComponentProblems> : " + Info;
        }

        public string Info
        {
            get
            {
                if (Problems.Count == 0)
                {
                    return "";
                }
                // speed optimization - dont allocate list if we have only 1 info
                if (Problems.Count == 1)
                {
                    return Problems[0].Info;
                }
                SortByPriority();

                var pss = new List<string>(Problems.Count);
                foreach (var p in Problems)
                {
                    var s = p.Info;
                    pss.Add(s);
                }
                return String.Join(";  ", pss);
            }
        }

        private void SortByPriority()
        {
            if (IsProblemsSorted || Problems.Count <= 1)
            {
                return;
            }
            IsProblemsSorted = true;
            Problems.Sort((a, b) => ComponentProblemTypesManager.Infos[b.Type].Priority - ComponentProblemTypesManager.Infos[a.Type].Priority);
        }

        public void Close()
        {
            foreach (var p in Problems)
            {
                p.Close();
            }
            Problems.Clear();
            FixWhat.Close();
        }

        public ComponentProblems Select(ComponentProblemTypes type)
        {
            return Select(o => o.Type == type);
        }

        public ComponentProblems Select(Func<ComponentProblem, bool> filter)
        {
            var ps = Problems.Where(filter).ToList();
            if (ps.Count == 0) return null;
            var res = new ComponentProblems();
            res.Problems.AddRange(ps);
            return res;
        }

        public void RemoveAll(Func<ComponentProblem, bool> filter)
        {
            var ps = Problems.Where(filter).ToList();
            foreach (var p in ps)
            {
                Problems.Remove(p);
            }
        }

        public void RemoveAll(ComponentProblemTypes filter)
        {
            var filteredProblems = Select(filter);
            foreach (var p in filteredProblems.Problems)
            {
                Problems.Remove(p);
            }
        }

        public bool Exists(ComponentProblemTypes filterType, ref string issueDescription)
        {
            var filteredProblems = Select(filterType);
            if (filteredProblems == null) return false;
            issueDescription = filteredProblems.Info;
            return true;
        }

        public bool Exists(ComponentProblemTypes filterType)
        {
            var filteredProblems = Select(filterType);
            return filteredProblems != null;
        }

        public bool ContainsProblem(ComponentProblem problem)
        {
            foreach (var p in Problems)
            {
                if (p.Type == problem.Type
                    && p.Info == problem.Info)
                {
                    return true;
                }
            }
            return false;
        }

        public ComponentProblems Duplicate()
        {
            return Select(o => true);
        }

        
    }
}
