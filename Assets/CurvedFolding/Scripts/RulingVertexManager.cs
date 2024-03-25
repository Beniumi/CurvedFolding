using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem
{
    public class RulingVertexManager
    {
        public readonly List<List<List<int>>> surface_ruling_vertex = new List<List<List<int>>>();
        public readonly List<Vector3> vertices = new List<Vector3>();

        public int SurfaceCount => surface_ruling_vertex.Count;

        public int RulingCount(int surface) => surface_ruling_vertex[surface].Count;

        public void AddNewSurface()
        {
            List<List<int>> newSurface = new List<List<int>>();
            surface_ruling_vertex.Add(newSurface);
        }

        public void AddRuling(Vector3 pos, Vector3 dir, List<float> length)
        {
            List<List<int>> currentSurface = surface_ruling_vertex[^1];
            List<int> newRuling = new List<int>();

            for(int i = 1; i < length.Count - 1; i++)
            {
                Vector3 newVert = pos + dir * length[i];
                newRuling.Add(vertices.Count);
                vertices.Add(newVert);
            }

            if (length.Count > 1)
            {
                Vector3 newVert0 = pos + dir * length[0];
                Vector3 newVertN = pos + dir * length[^1];
                bool shouldAddNewVert0 = true;
                bool shouldAddNewVertN = true;
                //前のrulingが存在 かつ 前のrulingの頂点が存在
                if (currentSurface.Count > 0 && currentSurface[^1].Count > 1)
                {
                    List<int> prevRuling = currentSurface[^1];
                    if (newVert0 == vertices[prevRuling[0]])
                    {
                        newRuling.Insert(0, prevRuling[0]);
                        shouldAddNewVert0 = false;
                    }
                    if (newVertN == vertices[prevRuling[^1]])
                    {
                        newRuling.Add(prevRuling[^1]);
                        shouldAddNewVertN = false;
                    }
                }

                int newRulingIndex = currentSurface.Count;
                foreach(var surface in surface_ruling_vertex)
                {
                    //同じindexのrulingが存在 かつ 同じindexのrulingの頂点が存在
                    if (newRulingIndex < surface.Count && surface[newRulingIndex].Count > 1)
                    {
                        List<int> sameIndexRuling = surface[newRulingIndex];
                        if (newVert0 == vertices[sameIndexRuling[0]])
                        {
                            newRuling.Insert(0, sameIndexRuling[0]);
                            shouldAddNewVert0 = false;
                        }
                    }
                }

                if (shouldAddNewVert0)
                {
                    newRuling.Insert(0, vertices.Count);
                    vertices.Add(newVert0);
                }
                if (shouldAddNewVertN)
                {
                    newRuling.Add(vertices.Count);
                    vertices.Add(newVertN);
                }
            }
            currentSurface.Add(newRuling);
        }

        public bool TryGetStartVertex(int surface, int ruling, out int vertex)
        {
            List<int> rulingVertices = surface_ruling_vertex[surface][ruling];
            if(rulingVertices.Count > 0)
            {
                vertex = rulingVertices[0];
                return true;
            }
            vertex = default;
            return false;
        }

        public bool TryGetEndVertex(int surface, int ruling, out int vertex)
        {
            List<int> rulingVertices = surface_ruling_vertex[surface][ruling];
            if (rulingVertices.Count > 0)
            {
                vertex = rulingVertices[^1];
                return true;
            }
            vertex = default;
            return false;
        }

        public List<int> GetTriangles(List<bool> reverseSurface)
        {
            List<int> meshTriangles = new List<int>();
            for(int j = 0 ; j < surface_ruling_vertex.Count; j++)
            {
                List<List<int>> surface = surface_ruling_vertex[j];
                for (int i = 1; i < surface.Count; i++)
                {
                    List<int> triangles = StripTriangles(surface[i - 1], surface[i], vertices);
                    if (reverseSurface[j]) triangles.Reverse();
                    meshTriangles.AddRange(triangles);
                }
            }
            return meshTriangles;
        }

        #region private methods

        private static List<int> StripTriangles(List<int> vn0, List<int> vn1, List<Vector3> vertices)
        {
            switch (vn0.Count.CompareTo(vn1.Count))
            {
                case 1:
                    return MakeTriangles(MatchVertices(vn1, vn0, vertices), vn1);
                case -1:
                    return MakeTriangles(vn0, MatchVertices(vn0, vn1, vertices));
                default:
                    return MakeTriangles(vn0, vn1);
            }
        }

        private static List<int> MatchVertices(List<int> goal, List<int> target, List<Vector3> vertices)
        {
            List<int> prod = new List<int>();
            foreach (int g in goal)
            {
                float minSqrDistance = Mathf.Infinity;
                int pair = 0;
                foreach (int t in target)
                {
                    float sqrDistance = Vector3.SqrMagnitude(vertices[g] - vertices[t]);
                    if (sqrDistance > minSqrDistance)
                        break;
                    minSqrDistance = sqrDistance;
                    pair = t;
                }
                prod.Add(pair);
            }
            return prod;
        }

        private static List<int> MakeTriangles(List<int> vn0, List<int> vn1)
        {
            List<int> triangles = new List<int>();
            for (int i = 1; i < vn0.Count; i += 2)
            {
                if (vn0[i] == vn1[i])
                {
                    triangles.Add(vn1[i]);
                    triangles.Add(vn1[i - 1]);
                    triangles.Add(vn0[i - 1]);
                }
                else
                {
                    triangles.Add(vn0[i - 1]);
                    triangles.Add(vn0[i]);
                    triangles.Add(vn1[i]);
                    triangles.Add(vn1[i]);
                    triangles.Add(vn1[i - 1]);
                    triangles.Add(vn0[i - 1]);
                }
            }
            return triangles;
        }

        #endregion
    }

}