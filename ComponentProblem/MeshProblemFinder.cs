using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace SolidUtils
{
    public static class MeshProblemFinder
    {
        public static void Find(ref ComponentProblems res, Mesh mesh, int maxAllowedDisjointMeshCount = 0)
        {
            //
            // Empty mesh
            //
            if (mesh == null || mesh.Faces.Count == 0)
            {
                var problem = "Mesh is empty";
                ComponentProblems.Add(ref res, mesh, problem, ComponentProblemTypes.MeshIsEmpty);
            }

            if (mesh == null) return;


            if (mesh.DisjointMeshCount > maxAllowedDisjointMeshCount)
            {
                var problem = "Found {0} disjoint mesh faces"._Format(mesh.DisjointMeshCount);
                ComponentProblems.Add(ref res, mesh, problem, ComponentProblemTypes.MeshHasDisjoints);
            }


            //
            // Invalid mesh
            //
            string invalidLog;
            if (mesh.Faces.Count != 0   // this is already checked in ComponentProblemTypes.MeshIsEmpty
                && !mesh.IsValidWithLog(out invalidLog))
            {
                var problem = "Mesh is invalid: {0}"._Format(invalidLog);
                ComponentProblems.Add(ref res, mesh, problem, ComponentProblemTypes.MeshIsEmpty);
            }

            //
            // Invalid faces
            //
            var vertexesCount = mesh.Vertices.Count;
            var invalidFacesCount = 0;
            foreach (var f in mesh.Faces)
            {
                if (!f.IsValid(vertexesCount)
                    || (!f.IsTriangle && !f.IsQuad)
                    ||  (f.IsTriangle && f.IsQuad))
                {
                    invalidFacesCount++;
                    continue;
                }

                var count = 0;
                if (f.IsTriangle)
                {
                    count = 3;
                }
                else if (f.IsQuad)
                {
                    count = 4;
                }
                for (int i = 0; i < count; i++)
                {
                    var vertexIndex = f[i];
                    if (vertexIndex < 0 || vertexIndex >= vertexesCount)
                    {
                        invalidFacesCount++;
                        continue;
                    }
                }
            }
            if (invalidFacesCount > 0)
            {
                var problem = "Mesh has {0} invalid faces."._Format(invalidFacesCount);
                ComponentProblems.Add(ref res, mesh, problem, ComponentProblemTypes.MeshHasInvalidFaces);
            }


            //
            // Unattached vertices
            //
            var vertexesUsed = GetVertexesUsed(mesh);
            var unattachedVerticesCount = vertexesUsed.Count(o => !o);
            if (unattachedVerticesCount > 0)
            {
                var problem = "Mesh has {0} unattached vertices."._Format(unattachedVerticesCount);
                ComponentProblems.Add(ref res, mesh, problem, ComponentProblemTypes.MeshHasUnattachedVertices);
            }

             //
            // Flipped faces
            //
            //TODO: implement
        }

        public static bool[] GetVertexesUsed(Mesh mesh)
        {
            if (mesh == null || mesh.Vertices.Count == 0)
            {
                return new bool[0];
            }

            var vertexesCount = mesh.Vertices.Count;
            var vertexesUsed = new bool[vertexesCount];
            for (int i = 0; i < vertexesUsed.Length; i++) vertexesUsed[i] = false;
            foreach (var f in mesh.Faces)
            {
                var count = 0;
                if (f.IsTriangle)
                {
                    count = 3;
                }
                else if (f.IsQuad)
                {
                    count = 4;
                }
                for (int i = 0; i < count; i++)
                {
                    var vertexIndex = f[i];
                    if (0 <= vertexIndex && vertexIndex < vertexesCount)
                    {
                        vertexesUsed[vertexIndex] = true;
                    }
                }
            }
            return vertexesUsed;
        }

        public static bool Find_UnattachedVertices(Mesh mesh, out List<Point3d> unattachedVertices)
        {
            unattachedVertices = null;
            if (mesh == null)
            {
                return false;
            }
            var vertexesUsed = GetVertexesUsed(mesh);
            var unattachedVerticesCount = vertexesUsed.Count(o => !o);
            if (unattachedVerticesCount == 0)
            {
                return false;
            }

            unattachedVertices = new List<Point3d>();
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                if (!vertexesUsed[i])
                {
                    unattachedVertices.Add(mesh.Vertices[i]);
                }
            }
            return true;
        }
    }
}
