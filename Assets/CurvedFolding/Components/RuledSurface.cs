using RuntimeSceneView;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem.Components
{
    [ExecuteAlways]
    public class RuledSurface : MonoBehaviour, ITransformChangedReceiver
    {
        private static class Loader
        {
            public static readonly Material lineMaterial;
            public static readonly Material faceMaterial;

            static Loader()
            {
                lineMaterial = Resources.Load<Material>("Line");
                faceMaterial = Resources.Load<Material>("Paper");
            }
        }

        public class SurfaceMesh
        {
            public Mesh Ruling { get; private set; }
            public Mesh Outline { get; private set; }
            public Mesh Strip { get; private set; }
            public Mesh Back { get; private set; }

            public List<Vector3> curvePositions = new List<Vector3>();
            public List<Vector3> rulingDirections = new List<Vector3>();
            public List<List<float>> rulingLengths = new List<List<float>>();

            public class IndexManager
            {
                public List<List<int>> RulingToVertexIndex { get; private set; } = new List<List<int>>();

                public int VertexCount { get; private set; } = 0;

                public int RulingCount => RulingToVertexIndex.Count;

                public void AddRuling(int rulingVertexCount)
                {
                    List<int> newIndices = new List<int>();
                    for (int i = 0; i < rulingVertexCount; i++)
                    {
                        newIndices.Add(VertexCount++);
                    }
                    RulingToVertexIndex.Add(newIndices);
                }

                public void AddRuling(List<int> newIndices, int vertexCount)
                {
                    VertexCount += vertexCount;
                    RulingToVertexIndex.Add(newIndices);
                }

                public List<T> RulingToVertexList<T>(List<T> rulingList)
                {
                    List<T> vertexList = new List<T>();
                    int rulingCount = RulingCount;
                    for (int i = 0; i < rulingCount; i++)
                    {
                        int vertexCount = RulingToVertexIndex[i].Count;
                        for (int j = 0; j < vertexCount; j++)
                        {
                            vertexList.Add(rulingList[i]);
                        }
                    }
                    return vertexList;
                }

                public List<T> VertexToRulingList<T>(List<T> vertexList)
                {
                    List<T> rulingList = new List<T>();
                    int rulingCount = RulingCount;
                    int vertexIndex = 0;
                    Debug.Log(rulingCount + " " + vertexList.Count);
                    for (int i = 0; i < rulingCount; i++)
                    {
                        rulingList.Add(vertexList[vertexIndex]);
                        vertexIndex += RulingToVertexIndex[i].Count;
                    }
                    return rulingList;
                }
            }
            private IndexManager manager;

            public SurfaceMesh() { }

            public void SetSurfaceMesh(DevelopableSurface surface)
            {
                SetSurfaceMesh(surface.Curve.Positions, surface.Rulings, surface.Lengths);
            }

            public void SetSurfaceMesh(List<Vector3> curvePositions, List<Vector3> rulings)
            {
                List<Vector3> rulingDirections = new List<Vector3>();
                List<float> lengths = new List<float>();
                foreach (Vector3 r in rulings)
                {
                    float l = r.magnitude;
                    lengths.Add(l);
                    Vector3 dir = Mathf.Approximately(l, 0) ? Vector3.zero : r / l;
                    rulingDirections.Add(dir);
                }
                SetSurfaceMesh(curvePositions, rulingDirections, lengths);
            }

            public void SetSurfaceMesh(List<Vector3> curvePositions, List<Vector3> rulingDirections, List<float> rulingLengths)
            {
                List<List<float>> containGapRulingLengths = new List<List<float>>();
                foreach (float l in rulingLengths)
                {
                    containGapRulingLengths.Add(new List<float>() { 0f, l });
                }
                SetSurfaceMesh(curvePositions, rulingDirections, containGapRulingLengths);
            }

            public void SetSurfaceMesh(List<Vector3> curvePositions, List<Vector3> rulingDirections, List<List<float>> rulingLengths)
            {
                this.curvePositions = new List<Vector3>(curvePositions);
                this.rulingDirections = new List<Vector3>(rulingDirections);
                this.rulingLengths = new List<List<float>>(rulingLengths);
            }

            public void Clear()
            {
                Ruling.Clear(false);
                Outline.Clear(false);
                Strip.Clear(false);
                Back.Clear(false);
            }

            public void CreateMesh()
            {
                //setup
                if(manager == null)
                {
                    Ruling = new Mesh();
                    Outline = new Mesh();
                    Strip = new Mesh();
                    Back = new Mesh();
                }
                else
                {
                    Clear();
                }

                //Entry Vetex index to manager
                manager = new IndexManager();
                EntryVertices(out List<Vector3> vertices);

                //Set Mesh
                SetRulingMesh(vertices);
                SetOutlineMesh(vertices);
                SetBothSideStripMesh(vertices);
            }

            private void EntryVertices(out List<Vector3> vertices)
            {
                vertices = new List<Vector3>();
                for (int i = 0; i < rulingLengths.Count; i++)
                {
                    List<float> ls = rulingLengths[i];
                    foreach (float l in ls)
                        vertices.Add(curvePositions[i] + rulingDirections[i] * l);
                    manager.AddRuling(ls.Count);
                }
            }

            private void SetRulingMesh(List<Vector3> vertices)
            {
                int n = manager.VertexCount;
                int[] indices = new int[n];
                for (int i = 0; i < n; i++)
                    indices[i] = i;

                Ruling.SetVertices(vertices);
                Ruling.SetIndices(indices, MeshTopology.Lines, 0);
            }

            private void SetOutlineMesh(List<Vector3> vertices)
            {
                if (manager.VertexCount < 4)
                    return;

                List<Vector3> innerVertices = new List<Vector3>();
                List<Vector3> outlineVertices = new List<Vector3>();
                int n = manager.RulingCount;
                for (int i = 0; i < n; i++)
                {
                    List<int> currentIndices = manager.RulingToVertexIndex[i];
                    if (currentIndices.Count > 0)
                    {
                        innerVertices.Add(vertices[currentIndices[0]]);
                        outlineVertices.Add(vertices[currentIndices[currentIndices.Count - 1]]);
                    }
                }
                innerVertices.Reverse();
                outlineVertices.AddRange(innerVertices);
                outlineVertices.Add(outlineVertices[0]);
                List<int> indices = new List<int>();
                List<Color> colors = new List<Color>();
                for (int i = 0; i < outlineVertices.Count; i++)
                {
                    indices.Add(i);
                    colors.Add(Color.black);
                }
                Outline.SetVertices(outlineVertices);
                Outline.SetIndices(indices, MeshTopology.LineStrip, 0);
                Outline.SetColors(colors);
            }

            private void SetBothSideStripMesh(List<Vector3> vertices)
            {
                List<int> stripTriangles = new List<int>();
                for (int i = 1; i < manager.RulingCount; i++)
                {
                    List<int> triangles = StripTriangles(manager.RulingToVertexIndex[i - 1], manager.RulingToVertexIndex[i], vertices);
                    stripTriangles.AddRange(triangles);
                }

                Strip.SetVertices(vertices);
                Strip.SetTriangles(stripTriangles, 0);
                Strip.RecalculateNormals();

                Back.SetVertices(vertices);
                stripTriangles.Reverse();
                Back.SetTriangles(stripTriangles, 0);
                Vector3[] backNormals = new Vector3[Strip.normals.Length];
                for(int i = 0; i < Strip.normals.Length; i++)
                    backNormals[i] = -Strip.normals[i];
                Back.normals = backNormals;
            }

            public static List<int> StripTriangles(List<int> vn0, List<int> vn1, List<Vector3> vertices)
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
                    triangles.Add(vn0[i - 1]);
                    triangles.Add(vn0[i]);
                    triangles.Add(vn1[i]);
                    triangles.Add(vn1[i]);
                    triangles.Add(vn1[i - 1]);
                    triangles.Add(vn0[i - 1]);
                }
                return triangles;
            }

            public void SetRulingColor(Color color)
            {
                List<Color> colors = new List<Color>();
                for(int i = 0; i < rulingDirections.Count; i++)
                    colors.Add(color);
                SetRulingColors(colors);
            }

            public void SetRulingColors(List<Color> colors)
            {
                if(Ruling != null)
                    Ruling.SetColors(manager.RulingToVertexList(colors));
            }

            public void SetStripColor(Color color)
            {
                List<Color> colors = new List<Color>();
                for (int i = 0; i < rulingDirections.Count; i++)
                    colors.Add(color);
                SetStripColors(colors);
            }

            public void SetStripColors(List<Color> colors)
            {
                if(Strip != null)
                {
                    List<Color> vertexColors = manager.RulingToVertexList(colors);
                    Strip.SetColors(vertexColors);
                    Back.SetColors(vertexColors);
                }
            }

            public List<bool> GetCrossed()
            {
                if (Ruling == null)
                    return new List<bool>();

                List<bool> isCrossed = new List<bool>() { false };
                Vector3[] vertices = Ruling.vertices;
                for (int i = 1; i < manager.RulingCount; i++)
                {
                    List<int> prevIndices = manager.RulingToVertexIndex[i - 1];
                    List<int> currentIndices = manager.RulingToVertexIndex[i];

                    if (prevIndices.Count < 1 || currentIndices.Count < 1)
                    {
                        isCrossed.Add(false);
                    }
                    else
                    {
                        Vector3 X0 = vertices[prevIndices[0]];
                        Vector3 Y0 = vertices[prevIndices[^1]];
                        Vector3 X1 = vertices[currentIndices[0]];
                        Vector3 Y1 = vertices[currentIndices[^1]];
                        bool cross = IsCrossing(X0, Y0, X1, Y1);
                        if (cross)
                            isCrossed[^1] = true;
                        isCrossed.Add(cross);
                    }
                }
                return isCrossed;
            }

            static bool IsCrossing(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
            {
                double s1 = Vector3.Cross(B - A, C - A).y;
                double t1 = Vector3.Cross(B - A, D - A).y;
                double s2 = Vector3.Cross(D - C, A - C).y;
                double t2 = Vector3.Cross(D - C, B - C).y;
                if (s1 * t1 < 0 && s2 * t2 < 0)
                    return true;
                return false;
            }

            public List<float> GetFlatness()
            {
                if (Ruling == null)
                    return new List<float>();

                List<float> flatness = new List<float>();
                Vector3[] vertices = Ruling.vertices;
                for (int i = 1; i < manager.RulingCount; i++)
                {
                    List<int> prevIndices = manager.RulingToVertexIndex[i - 1];
                    List<int> currentIndices = manager.RulingToVertexIndex[i];

                    if (prevIndices.Count > 0 && currentIndices.Count > 0)
                    {
                        Vector3 X0 = vertices[prevIndices[0]];
                        Vector3 Y0 = vertices[prevIndices[^1]];
                        Vector3 X1 = vertices[currentIndices[0]];
                        Vector3 Y1 = vertices[currentIndices[^1]];
                        flatness.Add(Flatness(X0, Y0, X1, Y1));
                    }
                    else
                    {
                        flatness.Add(0f);
                    }
                }
                flatness.Add(0f);
                return flatness;
            }

            static float Flatness(Vector3 X0, Vector3 Y0, Vector3 X1, Vector3 Y1)
            {
                float lavg = ((Y1 - X0).magnitude + (Y0 - X1).magnitude) / 2;
                float d = Math.Abs(Vector3.Dot(Vector3.Cross(Y1 - X0, Y0 - X1), X1 - X0) / Vector3.Cross(Y1 - X0, Y0 - X1).magnitude);
                float f = d / lavg;

                return f;
            }

            public List<float> GetDevelopabilities(List<Vector3> foldDirections)
            {
                if (curvePositions.Count < 3)
                    return new List<float>(new float[curvePositions.Count]);

                List<float> developabilities = new List<float>() { 0f };
                for (int i = 1; i < curvePositions.Count - 1; i++)
                {
                    developabilities.Add(Developability(curvePositions[i - 1], curvePositions[i], curvePositions[i + 1], rulingDirections[i], foldDirections[i]) - 2.0f * Mathf.PI);
                }
                developabilities.Add(0f);
                return developabilities;
            }

            static float Developability(Vector3 X0, Vector3 X1, Vector3 X2, Vector3 rL, Vector3 rR)
            {
                float developability = 0;
                Vector3 C0 = X0 - X1;
                Vector3 C1 = X2 - X1;
                developability += Vector3.Angle(C0, rL);
                developability += Vector3.Angle(C1, rL);
                developability += Vector3.Angle(C0, rR);
                developability += Vector3.Angle(C1, rR);

                return developability * Mathf.Deg2Rad;
            }

            public List<float> GetLengths()
            {
                List<float> lengths = new List<float>();
                if (Ruling == null)
                    return lengths;
                for (int i = 0; i < manager.RulingCount; i++)
                {
                    if (rulingLengths[i].Count > 0)
                        lengths.Add(rulingLengths[i][^1]);
                    else
                        lengths.Add(0);
                }
                return lengths;
            }

            public static void CombinedMesh(Mesh result, SurfaceMesh s0, SurfaceMesh s1)
            {
                List<Vector3> vertices = new List<Vector3>();
                int n = s0.manager.RulingCount;
                for (int i = 0; i < n; i++)
                {
                    int s0CreaseVi = s0.manager.RulingToVertexIndex[i][0];
                    int s1CreaseVi = s1.manager.RulingToVertexIndex[i][0];
                    List<Vector3> viPositions = new List<Vector3>();
                    /*
                    foreach(int vi in s0vi)
                    {
                        viPositions.Add(s0.Strip.vertices[vi]);
                    }
                    if (s0.Strip.vertices[s0vi[0]] == s1.Strip.vertices[s1vi[0]])
                    {
                        ls.RemoveAt();
                    }
                    */
                }

            }
        }


        [field: SerializeField]
        public BasicCurve Curve { get; private set; }
        [field: SerializeField]
        public bool Surface { get; set; } = true;
        [field: SerializeField]
        public bool Rulings { get; set; } = true;
        [field: SerializeField]
        public bool Outline { get; set; } = true;

        public SurfaceMesh SurfMesh { get; private set; } = new SurfaceMesh();

        private DevelopableSurface surf = new DevelopableSurface();

        public void SetRulings(BasicCurve handle)
        {
            surf = new DevelopableSurface(Curve.WorldDivided, handle.WorldDivided);
            SurfMesh.SetSurfaceMesh(surf);
        }
        public void SetFoldRulings(RuledSurface ruledSurface)
        {
            List<Vector3> foldRulings = new List<Vector3>();
            for(int i = 0; i < ruledSurface.surf.Rulings.Count; i++)
            {
                Vector3 fr = ruledSurface.surf.GetFoldRuling(i);
                fr *= ruledSurface.surf.Lengths[i];
                foldRulings.Add(fr);
            }
            surf = new DevelopableSurface(Curve.WorldDivided, foldRulings);
            SurfMesh.SetSurfaceMesh(surf);
        }

        public void DevelopSurface(RuledSurface surface3d)
        {
            surf = DevelopableSurface.Develop(surface3d.surf);
            SurfMesh.SetSurfaceMesh(surf);
            Curve.Divided = surf.Curve;
        }

        public void TestDevelopSurface(RuledSurface surface3d)
        {
            surf = DevelopableSurface.TestDevelop(surface3d.surf);
            SurfMesh.SetSurfaceMesh(surf);
            Curve.Divided = surf.Curve;
        }

        public void OutDevelopedCurve(BasicCurve curve)
        {
            DividedCurve developedCurve = surf.GetDevelopedCurve();
            curve.Divided = developedCurve;
        }

        public void OutBihandleCurve(BasicCurve bihandle)
        {
            DividedCurve bihandleCurve = surf.GetBihandleCurve(out bool[] isModified);
            bihandle.Divided = bihandleCurve;
        }

        public void OutBihandleCurve(BasicCurve bihandle, out bool[] isModified)
        {
            DividedCurve bihandleCurve = surf.GetBihandleCurve(out isModified);
            bihandle.WorldDivided = bihandleCurve;
        }

        public void Trimming(CreasePatternEdge edge)
        {
            List<List<float>> allLengths = new List<List<float>>();
            List<Vector3> pts = SurfMesh.curvePositions;
            List<Vector3> r = SurfMesh.rulingDirections;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                List<float> lengths = new List<float>();
                List<Vector3> intersections = edge.Paper.GetIntersections(pts[i], r[i]);
                foreach (Vector3 cross in intersections)
                    lengths.Add(Vector3.Distance(pts[i], cross));
                lengths.Sort();
                if (intersections.Count % 2 != 0)
                    lengths.Insert(0, 0f);
                allLengths.Add(lengths);
            }
            SurfMesh.SetSurfaceMesh(pts, r, allLengths);
        }

        public void SetTrimSize(RuledSurface from)
        {
            SurfMesh.SetSurfaceMesh(SurfMesh.curvePositions, SurfMesh.rulingDirections, from.SurfMesh.rulingLengths);
        }

        public void ResetTrimming()
        {
            SurfMesh.SetSurfaceMesh(surf);
        }

        public void CreateMesh()
        {
            SurfMesh.CreateMesh();
            SurfMesh.SetRulingColor(Color.gray);
            SurfMesh.SetStripColor(Color.white);
        }

        /*
        public void OutBihandleRulingLength(SngleCurvedFold bihandleRoot)
        {
            int ri = bihandleRoot.BaseRulingIndex;
            Vector3 crease = Curve.WorldDivided.Positions[ri];
            Vector3 ruling = surf.GetFoldRuling(ri);
            Vector3 creaseToRoot = bihandleRoot.transform.position - crease;
            bihandleRoot.BaseRulingLength = Vector3.Dot(ruling, creaseToRoot);
        }

        public void OutBihandleCurveWithDevelopableCrease(SngleCurvedFold bihandle)
        {
            int ri = bihandle.BaseRulingIndex;
            if (ri >= Curve.WorldDivided.Positions.Count) ri = 0;
            Vector3 basePos = Curve.WorldDivided.Positions[ri];
            Vector3 baseDir = surf.GetFoldRuling(ri);
            float baseLength = bihandle.AutoLengthSet ? surf.Lengths[ri] : bihandle.BaseRulingLength;
            Vector3 rp = basePos + baseDir * baseLength;
            //bihandle.WorldDivided = surf.GetBihandleCurveWithDevelopableCrease(ri, rp, out DividedCurve crease);
            //Curve.WorldDivided = crease;
        }
        */

        void Update()
        {
            Matrix4x4 m = Matrix4x4.identity;
            if (Surface)
            {
                Graphics.DrawMesh(SurfMesh.Strip, m, Loader.faceMaterial, gameObject.layer);
                Graphics.DrawMesh(SurfMesh.Back, m, Loader.faceMaterial, gameObject.layer);
            }
            if (Rulings)
            {
                Graphics.DrawMesh(SurfMesh.Ruling, m, Loader.lineMaterial, gameObject.layer);
            }
            if (Outline)
            {
                Graphics.DrawMesh(SurfMesh.Outline, m, Loader.lineMaterial, gameObject.layer);
            }
        }

        public void OnTransformChanged()
        {
            surf = new DevelopableSurface(Curve.WorldDivided, surf.Lengths, surf.Alpha, surf.Beta);
            SurfMesh.SetSurfaceMesh(surf);
        }
    }
}
