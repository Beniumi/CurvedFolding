using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshDataStructures
{
    public class Vertex
    {
        public Vector3 pos;
        public HalfEdge halfEdge;

        public Vertex(Vector3 pos)
        {
            this.pos = pos;
        }
    }

    public class HalfEdge
    {
        public Vertex vert;
        public Face face;
        public HalfEdge next;
        public HalfEdge prev;
        public HalfEdge pair;
        public Edge edge;

        public HalfEdge(Vertex vert, Face face)
        {
            this.vert = vert;
            this.face = face;
            vert.halfEdge = this;
            face.halfEdge = this;
        }

        public static void SetConnection(HalfEdge prev, HalfEdge next)
        {
            prev.next = next;
            next.prev = prev;
        }

        public static void SetPairs(HalfEdge e1, HalfEdge e2)
        {
            e1.pair = e2;
            e2.pair = e1;
        }
    }

    public class Face
    {
        public HalfEdge halfEdge;
    }

    public class Edge
    {
        public HalfEdge left;
        public HalfEdge right;

        public Edge(HalfEdge left)
        {
            Set(left);
        }

        public Edge(HalfEdge left, HalfEdge right)
        {
            Set(left, right);
        }

        public Vertex start => left.vert;

        public Vertex end => left.next.vert;

        public void Set(HalfEdge left)
        {
            this.left = left;
            left.edge = this;
        }

        public void Set(HalfEdge left, HalfEdge right)
        {
            this.left = left;
            left.edge = this;
            this.right = right;
            right.edge = this;
        }
    }

    public class HalfEdgeDataStructure
    {
        public readonly List<Vertex> verts;
        public readonly List<HalfEdge> halfEdges;
        public readonly List<Face> faces;
        public readonly List<Edge> edges;

        public HalfEdgeDataStructure(Mesh mesh, bool createWholeEdge = false) : this(mesh.vertices, mesh.triangles, createWholeEdge) { }

        public HalfEdgeDataStructure(Vector3[] vertices, int[] triangles, bool createWholeEdge = false)
        {
            verts = new List<Vertex>();
            for (int i = 0; i < vertices.Length; i++)
            {
                verts.Add(new Vertex(vertices[i]));
            }

            halfEdges = new List<HalfEdge>();
            faces = new List<Face>();
            HalfEdge[,] A = new HalfEdge[vertices.Length, vertices.Length];
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Face f = new Face();
                faces.Add(f);
                int v1i = triangles[i];
                int v2i = triangles[i + 1];
                int v3i = triangles[i + 2];
                HalfEdge e12 = new HalfEdge(verts[v1i], f);
                HalfEdge e23 = new HalfEdge(verts[v2i], f);
                HalfEdge e31 = new HalfEdge(verts[v3i], f);
                halfEdges.Add(e12);
                halfEdges.Add(e23);
                halfEdges.Add(e31);
                HalfEdge.SetConnection(e12, e23);
                HalfEdge.SetConnection(e23, e31);
                HalfEdge.SetConnection(e31, e12);
                A[v1i, v2i] = e12;
                A[v2i, v3i] = e23;
                A[v3i, v1i] = e31;
                if (A[v2i, v1i] != null) HalfEdge.SetPairs(A[v2i, v1i], e12);
                if (A[v3i, v2i] != null) HalfEdge.SetPairs(A[v3i, v2i], e23);
                if (A[v1i, v3i] != null) HalfEdge.SetPairs(A[v1i, v3i], e31);
            }

            if (createWholeEdge)
            {
                edges = new List<Edge>();
                HashSet<HalfEdge> hasEdge = new HashSet<HalfEdge>();
                foreach (HalfEdge he in halfEdges)
                {
                    if (he.pair == null)
                    {
                        edges.Add(new Edge(he));
                        //NULLならペアが存在しないのでHashSetに登録する必要はない。
                    }
                    else if (!hasEdge.Contains(he))
                    {
                        Edge edge = new Edge(he, he.pair);
                        edges.Add(edge);
                        hasEdge.Add(he.pair);
                    }
                }
            }
        }
        
        public void RemoveEdge(Edge e)
        {
            RemoveHalfEdge(e.left);
        }

        public void RemoveHalfEdge(HalfEdge he)
        {
            //Safety
            List<HalfEdge> startAdjacentEdges = AdjacentEdges(he.vert);
            List<Vertex> adjacentVerts = new List<Vertex>();
            foreach (HalfEdge ahe in startAdjacentEdges)
            {
                adjacentVerts.Add(ahe.next.vert);
            }
            List<HalfEdge> endAdjacentEdges = AdjacentEdges(he.next.vert);
            int adjacentCount = 0;
            foreach (HalfEdge ahe in endAdjacentEdges)
            {
                if (adjacentVerts.Contains(ahe.next.vert))
                {
                    adjacentCount++;
                    if(adjacentCount == 3)
                    {
                        Debug.LogError("This Half Edge cannot be deleted.");
                        return;
                    }
                }
            }

            he.next.vert.pos = Vector3.Lerp(he.vert.pos, he.next.vert.pos, 0.5f);

            HalfEdge heLT = he.prev.pair;
            HalfEdge heRT = he.next.pair;
            he.next.vert.halfEdge = heLT;
            he.prev.vert.halfEdge = heRT;
            HalfEdge.SetPairs(heLT, heRT);
            HalfEdge heLB, heRB;
            if(he.pair != null)
            {
                heLB = he.pair.next.pair;
                heRB = he.pair.prev.pair;
                he.pair.prev.vert.halfEdge = heLB;
                HalfEdge.SetPairs(heLB, heRB);
            }
            foreach (HalfEdge ahe in startAdjacentEdges)
            {
                ahe.vert = he.next.vert;
            }



            verts.Remove(he.vert);
            faces.Remove(he.face);




            if(he.edge != null) edges.Remove(he.edge);


        }

        public static bool IsBorder(Vertex v)
        {
            HalfEdge start = v.halfEdge;
            HalfEdge current = start;
            do
            {
                if (current.pair == null) return true;
                current = current.pair.next;
            } while (current != start);
            return false;
        }

        public static List<HalfEdge> AdjacentEdges(Vertex v)
        {
            List<HalfEdge> edges = new List<HalfEdge>();
            HalfEdge start = v.halfEdge;
            HalfEdge current = start;
            bool shouldReverse = false;
            do
            {
                edges.Add(current);
                if (current.pair == null)
                {
                    shouldReverse = true;
                    break;
                }
                current = current.pair.next;
            } while (current != start);

            if (shouldReverse)
            {
                current = start.prev.pair;
                while (current != null)
                {
                    edges.Insert(0, current);
                    current = current.prev.pair;
                }
            }
            return edges;
        }

    }

}
