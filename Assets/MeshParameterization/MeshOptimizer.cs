using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NLoptNet;

namespace MeshDataStructures.Parameterization
{
    [ExecuteAlways]
    public class MeshOptimizer : MonoBehaviour
    {
        public enum Optimization
        {
            EdgeBase,
            AngleBase
        }
        [SerializeField]
        Optimization method = Optimization.EdgeBase;
        [SerializeField]
        NLoptAlgorithm algorithm = NLoptAlgorithm.LN_COBYLA;
        [SerializeField]
        int maxCount = 100;
        [SerializeField]
        bool optimizeAlways = false;

        [SerializeField]
        Material material;

        private Mesh inputMesh;

        public Mesh InputMesh
        {
            get { return inputMesh; }
            set
            {
                inputMesh = value;
                if (optimizeAlways)
                    Optimize();
            }
        }

        [field:SerializeField]
        public Mesh Result { get; private set; }

        private Mesh resultReverse;

        [SerializeField]
        uint debugEdgeIndex;

        private void OnValidate()
        {
            DebugColor();
        }

        public void DebugColor()
        {
            if (Result == null || resultReverse == null) return;
            Color[] colors = new Color[Result.vertexCount];
            for (int i = 0; i < Result.vertexCount; i++)
            {
                colors[i] = Color.white;
            }

            HalfEdgeDataStructure data = new HalfEdgeDataStructure(Result, true);
            if (data.edges.Count <= debugEdgeIndex) return;
            Edge e = data.edges[(int)debugEdgeIndex];
            Vertex verr1 = e.start;
            Vertex verr2 = e.end;
            for (int i = 0; i < data.verts.Count; i++)
            {
                if (data.verts[i] == verr1) colors[i] = Color.red;
                if (data.verts[i] == verr2) colors[i] = Color.blue;
            }
            Result.colors = colors;
            resultReverse.colors = colors;
        }

        [ContextMenu("optimize")]
        private void Optimize()
        {
            if(Result == null)
            {
                Result = new Mesh();
            }
            else
            {
                Result.Clear(true);
            }
            HalfEdgeDataStructure data = new HalfEdgeDataStructure(inputMesh, true);

            switch (method)
            {
                case Optimization.EdgeBase:
                    {
                        var parametrization = new EdgeBasedParameterization(data);
                        Result.vertices = parametrization.OptimizePositions(algorithm, maxCount);
                    }
                    break;
                case Optimization.AngleBase:
                    {
                        var parametrization = new AngleBasedParameterization(data);
                        Result.vertices = parametrization.OptimizePositions(algorithm, maxCount);
                    }
                    break;
            }

            Result.triangles = inputMesh.triangles;
            Result.RecalculateNormals();
            resultReverse = new Mesh();
            resultReverse.vertices = Result.vertices;
            List<int> triangles = new List<int>(inputMesh.triangles);
            triangles.Reverse();
            resultReverse.triangles = triangles.ToArray();
            resultReverse.RecalculateNormals();
        }

        // Update is called once per frame
        void Update()
        {
            if (Result != null) Graphics.DrawMesh(Result, transform.localToWorldMatrix, material, 0);
            if (resultReverse != null) Graphics.DrawMesh(resultReverse, transform.localToWorldMatrix, material, 0);
        }
    }

}