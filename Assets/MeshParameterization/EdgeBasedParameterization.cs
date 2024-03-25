using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NLoptNet;
using System;
using CurvedFoldingSystem;
using System.Windows.Forms.VisualStyles;

namespace MeshDataStructures.Parameterization
{
    public class EdgeBasedParameterization
    {
        public class EdgeTriangle
        {
            public class InnerAngle
            {
                public readonly float val;
                public readonly float partialDerivative1;
                public readonly float partialDerivative2;
                public readonly float partialDerivative3;

                /// <summary>
                /// <param name="l1"></param>
                /// <param name="l2"></param>
                /// <param name="l3">opposite edge length.</param>
                /// </summary>
                public InnerAngle(float l1, float l2, float l3)
                {
                    bool isZero1 = l1 < 1E-06;
                    bool isZero2 = l2 < 1E-06;
                    bool isZero3 = l3 < 1E-06;

                    float t;
                    if (isZero1 || isZero2)
                    {
                        t = 0f;
                    }
                    else
                    {
                        t = (l1 * l1 + l2 * l2 - l3 * l3) / (2 * l1 * l2);
                        t = Mathf.Clamp(t, -0.999f, 0.999f);
                    }
                    val = Mathf.Acos(t);

                    if (isZero1)
                    {
                        if(isZero2) partialDerivative1 = 0f;
                        else partialDerivative1 = - 1.0f / (2.0f * l2);
                        partialDerivative2 = 0f;
                        partialDerivative3 = 0f;
                        Debug.Log("is zero1");
                    }
                    else if (isZero2)
                    {
                        partialDerivative1 = 0f;
                        partialDerivative2 = -1.0f / (2.0f * l1);
                        partialDerivative3 = 0f;
                        Debug.Log("is zero2");
                    }
                    else if (isZero3)
                    {
                        partialDerivative1 = 0f;
                        partialDerivative2 = 0f;
                        partialDerivative3 = Mathf.Sqrt(2.0f) / l1;
                        Debug.Log("is zero3");
                    }
                    else
                    {
                        float tPartialDerivativel1 = 1.0f / (2.0f * l2) - l2 / (2.0f * l1 * l1) + l3 * l3 / (2.0f * l2 * l1 * l1);
                        float tPartialDerivativel2 = -l1 / (2.0f * l2 * l2) + 1.0f / (2.0f * l1) + l3 * l3 / (2.0f * l1 * l2 * l2);
                        float tPartialDerivativel3 = -l3 / (l1 * l2);
                        float derivativeArcCos = -1.0f / Mathf.Sqrt(1.0f - t * t);
                        partialDerivative1 = derivativeArcCos * tPartialDerivativel1;
                        partialDerivative2 = derivativeArcCos * tPartialDerivativel2;
                        partialDerivative3 = derivativeArcCos * tPartialDerivativel3;
                    }

                    if (float.IsInfinity(partialDerivative1) || float.IsNaN(partialDerivative1))
                    {
                        Debug.Log("pd1 is Infinity or NaN" + "\nt\t" + t + "\nl1\t" + l1 + "\nl2\t" + l2 + "\nl3\t" + l3 + "\nbool\n" + isZero1 + "\t" + isZero2 + "\t" + isZero3);
                    }
                    /*
                    if (float.IsInfinity(partialDerivative2) || float.IsNaN(partialDerivative2))
                    {
                        Debug.Log("pd2 is Infinity or NaN" + "\nt\t" + t + "\nl1\t" + l1 + "\nl2\t" + l2 + "\nl3\t" + l3 + "\nbool\n" + isZero1 + "\t" + isZero2 + "\t" + isZero3);
                    }
                    if (float.IsInfinity(partialDerivative3) || float.IsNaN(partialDerivative3))
                    {
                        Debug.Log("pd3 is Infinity or NaN" + "\nt\t" + t + "\nl1\t" + l1 + "\nl2\t" + l2 + "\nl3\t" + l3 + "\nbool\n" + isZero1 + "\t" + isZero2 + "\t" + isZero3);
                    }
                    */
                }
            }

            public readonly Face f;
            public readonly Edge e1;
            public readonly Edge e2;
            public readonly Edge e3;
            public readonly Vertex p1;
            public readonly Vertex p2;
            public readonly Vertex p3;
            private Dictionary<Vertex, float> v_angle;
            private Dictionary<Vertex, Dictionary<Edge, float>> v_angle_PD_e;

            public EdgeTriangle(Face f)
            {
                this.f = f;
                HalfEdge he1 = f.halfEdge;
                HalfEdge he2 = he1.next;
                HalfEdge he3 = he2.next;
                p1 = he1.prev.vert;
                p2 = he2.prev.vert;
                p3 = he3.prev.vert;
                e1 = he1.edge;
                e2 = he2.edge;
                e3 = he3.edge;
            }

            public void SetInnerAngles(float l1, float l2, float l3)
            {
                InnerAngle a1 = new InnerAngle(l2, l3, l1);
                InnerAngle a2 = new InnerAngle(l3, l1, l2);
                InnerAngle a3 = new InnerAngle(l1, l2, l3);
                v_angle = new Dictionary<Vertex, float>();
                v_angle[p1] = a1.val;
                v_angle[p2] = a2.val;
                v_angle[p3] = a3.val;
                v_angle_PD_e = new Dictionary<Vertex, Dictionary<Edge, float>>();
                v_angle_PD_e[p1] = new Dictionary<Edge, float>();
                v_angle_PD_e[p1][e1] = a1.partialDerivative3;
                v_angle_PD_e[p1][e2] = a1.partialDerivative1;
                v_angle_PD_e[p1][e3] = a1.partialDerivative2;
                v_angle_PD_e[p2] = new Dictionary<Edge, float>();
                v_angle_PD_e[p2][e1] = a2.partialDerivative2;
                v_angle_PD_e[p2][e2] = a2.partialDerivative3;
                v_angle_PD_e[p2][e3] = a2.partialDerivative1;
                v_angle_PD_e[p3] = new Dictionary<Edge, float>();
                v_angle_PD_e[p3][e1] = a3.partialDerivative1;
                v_angle_PD_e[p3][e2] = a3.partialDerivative2;
                v_angle_PD_e[p3][e3] = a3.partialDerivative3;
            }

            public float GetAngle(Vertex v)
            {
                return v_angle[v];
            }

            public float GetPartialDerivativeAngle(Vertex v, Edge e)
            {
                return v_angle_PD_e[v][e];
            }
        }

        private HalfEdgeDataStructure data;
        private uint Nintv, Ne;
        private Dictionary<Vertex, List<HalfEdge>> innerVertEdges;
        private Dictionary<Vertex, int> innerVertIndex;
        private Dictionary<Edge, int> edgeIndex;
        private Dictionary<Face, EdgeTriangle> faceTriangle;
        private double[] initialLength;
        double[] length;

        public EdgeBasedParameterization(HalfEdgeDataStructure data)
        {
            this.data = data;

            //Inner Vertices Setting
            innerVertEdges = new Dictionary<Vertex, List<HalfEdge>>();
            innerVertIndex = new Dictionary<Vertex, int>();
            for (int i = 0; i < data.verts.Count; i++)
            {
                Vertex v = data.verts[i];
                
                if (!HalfEdgeDataStructure.IsBorder(v))
                {
                    innerVertIndex.Add(v, innerVertEdges.Count);
                    innerVertEdges.Add(v, HalfEdgeDataStructure.AdjacentEdges(v));
                }
                else
                {
                    innerVertIndex.Add(v, -1);
                }
            }
            Nintv = (uint)innerVertEdges.Count;

            //Edge Setting
            edgeIndex = new Dictionary<Edge, int>();
            Ne = (uint)data.edges.Count;
            initialLength = new double[Ne];
            for (int i = 0; i < Ne; i++)
            {
                Edge e = data.edges[i];
                edgeIndex.Add(e, i);
                float l = Vector3.Distance(e.start.pos, e.end.pos);
                initialLength[i] = l;
            }

            //Face (Inner Angle) Setting
            faceTriangle = new Dictionary<Face, EdgeTriangle>();
            for (int i = 0; i < data.faces.Count; i++)
            {
                Face f = data.faces[i];
                EdgeTriangle t = new EdgeTriangle(f);
                faceTriangle.Add(f, t);
            }
        }

        private void RecalcEdgeTriangle(float[] length)
        {
            foreach (EdgeTriangle t in faceTriangle.Values)
            {
                float l1 = length[edgeIndex[t.e1]];
                float l2 = length[edgeIndex[t.e2]];
                float l3 = length[edgeIndex[t.e3]];
                t.SetInnerAngles(l1, l2, l3);
            }
        }

        public Vector3[] OptimizePositions(NLoptAlgorithm algorithm, int maxCount)
        {
            length = new double[Ne];
            if (maxCount == 0)
            {
                Debug.Log("Not optimize.");
                for (int i = 0; i < Ne; i++)
                    length[i] = initialLength[i];

                Vector3[] vs = new Vector3[data.verts.Count];
                for (int i = 0; i < data.verts.Count; i++ )
                {
                    vs[i] = data.verts[i].pos;
                }
                return vs;
            }

            double[] omega = new double[Ne];
            for (int i = 0; i < Ne; i++)
            {
                omega[i] = 1.0d / (initialLength[i] * initialLength[i]);
            }

            Debug.Log("Ne:" + Ne + "\tNintv:" + Nintv);
            initialLength.CopyTo(length, 0);

            //Solver
            using (var solver = new NLoptSolver(algorithm, Ne, 0.0001, maxCount))
            {
                int count = 0;


                //Bounds
                //solver.SetLowerBounds(new double[Ne]);

                //Objective
                string objective = "objective\n";
                double[] prevLength = new double[Ne];
                length.CopyTo(prevLength, 0);
                solver.SetMinObjective((var, grad) =>
                {
                    count++;

                    //Condition
                    bool[] illegal = new bool[Ne];
                    for (int i = 0; i < var.Length; i++)
                    {
                        if (var[i] <= 0d) illegal[i] = true;
                    }
                    foreach (Face f in data.faces)
                    {
                        EdgeTriangle t = faceTriangle[f];
                        int e1i = edgeIndex[t.e1];
                        int e2i = edgeIndex[t.e2];
                        int e3i = edgeIndex[t.e3];
                        if (var[e1i] <= Math.Abs(var[e2i] - var[e3i])) illegal[e1i] = true;
                        if (var[e2i] <= Math.Abs(var[e3i] - var[e1i])) illegal[e2i] = true;
                        if (var[e3i] <= Math.Abs(var[e1i] - var[e2i])) illegal[e3i] = true;
                    }
                    bool existIllegal = false;
                    for (int i = 0; i < var.Length; i++)
                    {
                        if (illegal[i])
                        {
                            existIllegal = true;
                            omega[i] *= 1.01d;
                        }
                    }
                    if (existIllegal)
                    {
                        Debug.Log("count " + count + " var is illigal.");
                        for (int i = 0; i < var.Length; i++)
                        {
                            var[i] = prevLength[i];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < var.Length; i++)
                        {
                            prevLength[i] = var[i];
                        }
                    }


                    //Debug
                    string varText = count + ":var\n";
                    for (int i = 0; i < var.Length; i++)
                    {
                        varText += var[i] + "\n";
                    }
                    //Debug.Log(varText);


                    //Update
                    float[] tempLength = new float[Ne];
                    for (int i = 0; i < var.Length; i++)
                    {
                        tempLength[i] = (float)var[i];
                    }
                    RecalcEdgeTriangle(tempLength);


                    //Function
                    double E = 0d;
                    for (int i = 0; i < Ne; i++)
                    {
                        double diff = var[i] - initialLength[i];
                        E += omega[i] * diff * diff;
                    }
                    if (grad != null)
                    {
                        for (int i = 0; i < Ne; i++)
                        {
                            double diff = var[i] - initialLength[i];
                            grad[i] = 2 * omega[i] * diff;
                        }
                    }
                    objective += E.ToString("F6") + "\n";
                    return E;
                });

                //constraint
                foreach (var keyValue in innerVertEdges)
                {
                    Vertex v = keyValue.Key;
                    List<HalfEdge> edges = keyValue.Value;
                    solver.AddEqualZeroConstraint((var, grad) =>
                    {
                        float innerAngle = 0f;
                        foreach (HalfEdge e in edges)
                        {
                            EdgeTriangle t = faceTriangle[e.face];
                            innerAngle += t.GetAngle(v);
                        }
                        if(grad != null)
                        {
                            for (int i = 0; i < Ne; i++) 
                                grad[i] = 0;

                            foreach (HalfEdge e in edges)
                            {
                                EdgeTriangle t = faceTriangle[e.face];
                                int e1i = edgeIndex[t.e1];
                                int e2i = edgeIndex[t.e2];
                                int e3i = edgeIndex[t.e3];
                                grad[e1i] += t.GetPartialDerivativeAngle(v, t.e1);
                                grad[e2i] += t.GetPartialDerivativeAngle(v, t.e2);
                                grad[e3i] += t.GetPartialDerivativeAngle(v, t.e3);
                            }
                        }
                        return innerAngle - 2.0f * Mathf.PI;
                    }, 8.7E-04);
                }

                double? finalScore;
                var result = solver.Optimize(length, out finalScore);
                Debug.Log("Œ‹‰Ê\t" + result + "\tŒJ‚è•Ô‚µ‰ñ”\t" + count + "\t–Ú“IŠÖ”\t" + finalScore?.ToString("F8"));
                Debug.Log(objective);
            }

            string angleCheck = "innerAngle\n";
            foreach (var keyValue in innerVertEdges)
            {
                Vertex v = keyValue.Key;
                List<HalfEdge> edges = keyValue.Value;
                float innerAngle = 0f;
                foreach (HalfEdge e in edges)
                {

                    EdgeTriangle t = faceTriangle[e.face];
                    innerAngle += t.GetAngle(v);
                }
                angleCheck += (innerAngle - 2.0f * Mathf.PI).ToString("E3") + "\n";
            }
            Debug.Log(angleCheck);

            string edgeCheck = "edgeLength\n";
            double[] lengthDiff = new double[Ne];
            for(int i = 0; i < Ne; i++)
            {
                lengthDiff[i] = Math.Abs((length[i] - initialLength[i]) / initialLength[i]);
                edgeCheck += lengthDiff[i].ToString("E3") + "\n";
            }
            Debug.Log(edgeCheck);
            double max = lengthDiff[0];
            double min = lengthDiff[0];
            double sum = lengthDiff[0];
            for (int i = 1; i < Ne; i++)
            {
                if(max < lengthDiff[i]) max = lengthDiff[i];
                if(min > lengthDiff[i]) min = lengthDiff[i];
                sum += lengthDiff[i];
            }
            Debug.Log("sum\t" + sum + "\tave\t" + sum / Ne + "\nmax\t" + max +"\tmin\t" + min);

            return Get2DVertexPositions();
        }

        private Vector3[] Get2DVertexPositions()
        {
            HalfEdge e0 = data.halfEdges[0];
            float l0 = (float)length[edgeIndex[e0.edge]];
            Vertex v0 = e0.vert;
            Vertex v1 = e0.next.vert;
            Dictionary<Vertex, Vector3> vertPositions = new Dictionary<Vertex, Vector3>
            {
                { v0, Vector3.zero },
                { v1, Vector3.zero + l0 * Vector3.right }
            };
            HashSet<Face> isSearched = new HashSet<Face>();
            Get2DVertexPositionsRecursive(e0, vertPositions, isSearched);
            if(e0.pair != null) Get2DVertexPositionsRecursive(e0.pair, vertPositions, isSearched);

            Vector3[] positions2D = new Vector3[data.verts.Count];
            for (int i = 0; i < data.verts.Count; i++)
            {
                Vertex v = data.verts[i];
                positions2D[i] = vertPositions[v];
            }
            return positions2D;
        }

        private void Get2DVertexPositionsRecursive(HalfEdge he, Dictionary<Vertex, Vector3> vertexPositions, HashSet<Face> isSearched)
        {
            Face f = he.face;
            if (isSearched.Contains(f)) return;
            isSearched.Add(f);

            Vector3 center1 = vertexPositions[he.vert];
            Vector3 center2 = vertexPositions[he.next.vert];
            float radius1 = (float)length[edgeIndex[he.prev.edge]];
            float radius2 = (float)length[edgeIndex[he.next.edge]];
            Vector3 p = IntersectionOfCirclesXZ(center1, radius1, center2, radius2);
            vertexPositions[he.prev.vert] = p;

            if (he.prev.pair != null)
            {
                Get2DVertexPositionsRecursive(he.prev.pair, vertexPositions, isSearched);
            }
            if(he.next.pair != null)
            {
                Get2DVertexPositionsRecursive(he.next.pair, vertexPositions, isSearched);
            }
        }

        public Vector3 IntersectionOfCirclesXZ(Vector3 center1, float radius1, Vector3 center2, float radius2)
        {
            float a = 2 * (center2.x - center1.x);
            float b = 2 * (center2.z - center1.z);
            float c = (center1.x + center2.x) * (center1.x - center2.x) + (center1.z + center2.z) * (center1.z - center2.z) + (radius2 + radius1) * (radius2 - radius1);
            float D = -(a * center1.x + b * center1.z  + c);
            float aabb = a * a + b * b;
            float rootExp = Mathf.Sqrt(aabb * radius1 * radius1 - D * D);
            Vector3 A = new Vector3((a * D - b * rootExp) / aabb + center1.x, 0, (b * D + a * rootExp) / aabb + center1.z);
            //Debug.Log("radius1, distance1A\t" + radius1 + ", " + Vector3.Distance(center1, A));
            //Debug.Log("radius2, distance2A\t" + radius2 + ", " + Vector3.Distance(center2, A));
            if (Vector3.SignedAngle(center2 - center1, A - center1, Vector3.up) > 0)
            {
                return A;
            }
            else
            {
                Vector3 B = new Vector3((a * D + b * rootExp) / aabb + center1.x, 0, (b * D - a * rootExp) / aabb + center1.z);
                return B;
            }
        }
    }
}
