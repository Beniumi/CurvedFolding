using MeshDataStructures;
using NLoptNet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleBasedParameterization
{
    public class Wheel
    {
        public List<HalfEdge> outlineEdges = new List<HalfEdge>();
        public List<HalfEdge> toCenterEdges = new List<HalfEdge>();
        public List<HalfEdge> fromCenterEdges = new List<HalfEdge>();

        public Wheel(Vertex innerVert)
        {
            var fromCenterEdges = HalfEdgeDataStructure.AdjacentEdges(innerVert);
            foreach (HalfEdge e in fromCenterEdges)
            {
                toCenterEdges.Add(e.pair);
                outlineEdges.Add(e.next);
            }
        }
    }

    private HalfEdgeDataStructure data;
    private double[] radians;
    private double[] initialRadians;
    private List<Wheel> wheels;
    private Dictionary<HalfEdge, int> halfedgeRadindex;
    private int Nintv, Nr, Nf;


    private double Planarity(Wheel wheel)
    {
        double sum = 0;
        foreach(HalfEdge e in wheel.outlineEdges)
        {
            sum += radians[halfedgeRadindex[e]];
        }
        return sum - 2.0d * Math.PI;
    }

    private double Reconstruction(Wheel wheel)
    {
        double product1 = 1;
        foreach (HalfEdge e in wheel.fromCenterEdges)
        {
            double rad = radians[halfedgeRadindex[e]];
            product1 *= Math.Sin(rad);
        }
        double product2 = 1;
        foreach (HalfEdge e in wheel.toCenterEdges)
        {
            double rad = radians[halfedgeRadindex[e]];
            product2 *= Math.Sin(rad);
        }
        return product1 - product2;
    }

    private double Trianglevalidity(Face f)
    {
        HalfEdge e2 = f.halfEdge;
        HalfEdge e1 = e2.prev;
        HalfEdge e3 = e2.next;
        double sum = 0;
        sum += radians[halfedgeRadindex[e1]];
        sum += radians[halfedgeRadindex[e2]];
        sum += radians[halfedgeRadindex[e3]];
        return sum - Math.PI;
    }

    private double Energy()
    {
        double sum = 0;
        for(int i = 0; i < Nr; i++)
        {
            double diff = radians[i] - initialRadians[i];
            sum += diff * diff / (initialRadians[i] * initialRadians[i]);
        }
        return sum;
    }

    public AngleBasedParameterization(HalfEdgeDataStructure data)
    {
        this.data = data;
        Nf = data.faces.Count;

        wheels = new List<Wheel>();
        for (int i = 0; i < data.verts.Count; i++)
        {
            Vertex v = data.verts[i];
            if (!HalfEdgeDataStructure.IsBorder(v))
                wheels.Add(new Wheel(v));
        }
        Nintv = wheels.Count;

        halfedgeRadindex = new Dictionary<HalfEdge, int>();
        Nr = data.halfEdges.Count;
        radians = new double[Nr];
        initialRadians = new double[Nr];
        for (int i = 0; i < Nr; i++)
        {
            HalfEdge e = data.halfEdges[i];
            halfedgeRadindex.Add(e, i);
            Vector3 toStartVertex = e.vert.pos - e.prev.vert.pos;
            Vector3 toEndVertex = e.next.vert.pos - e.prev.vert.pos;
            initialRadians[i] = Vector3.Angle(toStartVertex, toEndVertex) * Mathf.Deg2Rad;
        }
    }

    public Vector3[] OptimizePositions(NLoptAlgorithm algorithm, int maxCount)
    {
        if (maxCount == 0)
        {
            Debug.Log("Not optimize.");
            initialRadians.CopyTo(radians, 0);
            return Get2DVertexPositions();
        }
        initialRadians.CopyTo(radians, 0);

        using (var solver = new NLoptSolver(algorithm, (uint)Nr, 0.0001, maxCount))
        {
            int count = 0;
            //Bounds
            solver.SetLowerBounds(new double[Nr]);

            //Objective
            string objective = "objective\n";
            solver.SetMinObjective(var =>
            {
                count++;
                //Debug
                string varText = count + ":var\n";
                for (int i = 0; i < var.Length; i++)
                {
                    varText += var[i] + "\n";
                }
                Debug.Log(varText);

                //Update

                //Function
                double E = Energy();

                objective += E.ToString("F6") + "\n";
                return E;
            });
            //CTri
            for (int i = 0; i < Nf; i++)
            {
                Face f = data.faces[i];
                solver.AddEqualZeroConstraint(var =>
                {
                    return Trianglevalidity(f);
                });
            }
            //CPlan and CLen
            for (int i = 0; i < Nintv; i++)
            {
                Wheel wheel = wheels[i];
                solver.AddEqualZeroConstraint(var =>
                {
                    return Planarity(wheel);
                });
                solver.AddEqualZeroConstraint(var =>
                {
                    return Reconstruction(wheel);
                });
            }

            double? finalScore;
            var result = solver.Optimize(radians, out finalScore);
            Debug.Log("åãâ \t" + result + "\tåJÇËï‘ÇµâÒêî\t" + count + "\tñ⁄ìIä÷êî\t" + finalScore?.ToString("F8"));
            Debug.Log(objective);
        }

        return Get2DVertexPositions();
    }

    private Vector3[] Get2DVertexPositions()
    {
        HalfEdge e0 = data.halfEdges[0];
        Vertex v0 = e0.vert;
        Vertex v1 = e0.next.vert;
        float l0 = Vector3.Distance(v0.pos, v1.pos);
        Dictionary<Vertex, Vector3> vertPositions = new Dictionary<Vertex, Vector3>
        {
            { v0, Vector3.zero },
            { v1, Vector3.zero + l0 * Vector3.right }
        };
        HashSet<Face> isSearched = new HashSet<Face>();
        Get2DVertexPositionsRecursive(e0, vertPositions, isSearched);
        if (e0.pair != null) Get2DVertexPositionsRecursive(e0.pair, vertPositions, isSearched);

        Vector3[] positions2D = new Vector3[data.verts.Count];
        for (int i = 0; i < data.verts.Count; i++)
        {
            Vertex v = data.verts[i];
            positions2D[i] = vertPositions[v];
        }
        return positions2D;
    }

    private void Get2DVertexPositionsRecursive(HalfEdge e, Dictionary<Vertex, Vector3> vertPositions, HashSet<Face> isSearched)
    {
        Face f = e.face;
        if (isSearched.Contains(f)) return;
        isSearched.Add(f);

        Vector3 start = vertPositions[e.vert];
        Vector3 end = vertPositions[e.next.vert];
        Vector3 eDir = Vector3.Normalize(end - start);
        float degreeStart = (float)radians[halfedgeRadindex[e.next]] * Mathf.Rad2Deg;
        Vector3 fromStart = Quaternion.AngleAxis(degreeStart, Vector3.up) * eDir;
        float degreeEnd = (float)radians[halfedgeRadindex[e.prev]] * Mathf.Rad2Deg;
        Vector3 fromEnd = Quaternion.AngleAxis(degreeEnd, -Vector3.up) * -eDir;

        if (IsIntersectXZ(start, fromStart, end, fromEnd, out Vector3 vnPos))
        {
            Vertex vn = e.prev.vert;
            vertPositions[vn] = vnPos;
            if (e.prev.pair != null) Get2DVertexPositionsRecursive(e.prev.pair, vertPositions, isSearched);
            if (e.next.pair != null) Get2DVertexPositionsRecursive(e.next.pair, vertPositions, isSearched);
        }
        else
        {
            Debug.Log("Not Intersect " + halfedgeRadindex[e] + ".");
        }

    }

    public static bool IsIntersectXZ(Vector3 p0, Vector3 v0, Vector3 p1, Vector3 v1, out Vector3 cross)
    {
        float d = v0.x * -v1.z + v1.x * v0.z;
        Vector3 p = p1 - p0;
        float t0 = (-v1.z * p.x + v1.x * p.z) / d;
        float t1 = (-v0.z * p.x + v0.x * p.z) / d;
        cross = p0 + v0 * t0;
        if (Mathf.Abs(d) < 1E-05f || t0 < 0f || t1 < 0f)
            return false;
        return true;
    }
}
