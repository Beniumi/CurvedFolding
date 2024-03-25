using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeSceneView
{
    [ExecuteAlways]
    public class SimpleLine : LineDrawer
    {
        [SerializeField]
        Color color = Color.white;

        private Material drawMaterial;

        private void OnValidate()
        {
            if (material != null)
                drawMaterial = new Material(material) { color = color };
            PrepareLine();
        }

        private void Start()
        {
            if (material != null)
                drawMaterial = new Material(material) { color = color };
        }

        public override void PrepareLine()
        {
            if(Mesh == null) return;

            Mesh.Clear(false);
            Mesh.SetVertices(Positions);
            List<int> indices = new List<int>();
            for (int i = 0; i < Positions.Count; i++)
                indices.Add(i);

            switch (mode)
            {
                case LineType.LINES:
                    Mesh.SetIndices(indices, MeshTopology.Lines, 0);
                    break;
                case LineType.STRIP:
                    Mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
                    break;
            }
        }

        void Update()
        {
            if (useWorldSpace)
                Graphics.DrawMesh(Mesh, Matrix4x4.identity, drawMaterial, gameObject.layer);
            else
                Graphics.DrawMesh(Mesh, transform.localToWorldMatrix, drawMaterial, gameObject.layer);
        }
    }
}
