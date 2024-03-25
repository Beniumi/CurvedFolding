using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RuntimeSceneView;
using RuntimeSceneView.Objects;

namespace CurvedFoldingSystem.Components
{
    [ExecuteAlways]
    public class CreasePatternEdge : MonoBehaviour, ITransformChangedReceiver, IControllPointHandler
    {
        private static readonly Rect A4 = new Rect(0f, 0f, 29.7f, 21.0f);
        public static List<Vector3> A4Vertices
        {
            get
            {
                List<Vector3> verts = new List<Vector3>();
                verts.Add(A4.position);
                verts.Add(new Vector3(A4.x + A4.width, 0, A4.y));
                verts.Add(new Vector3(A4.x + A4.width, 0, A4.y + A4.height));
                verts.Add(new Vector3(A4.x, 0, A4.y + A4.height));
                return verts;
            }
        }

        [SerializeField]
        LineDrawer line;
        [SerializeField]
        UnityEvent<CreasePatternEdge> onChanged;
        [field: SerializeField, Range(0.5f, 10.0f)]
        public float Scale { get; set; } = 1.0f;
        [field: SerializeField]
        public ControlPointManager ControlPoints { get; private set; }

        private Paper paper = new Paper();

        public Paper Paper
        {
            get { return paper; }
            private set
            {
                paper = value;
                if (line != null)
                    line.Positions = paper.Vertices;
                onChanged?.Invoke(this);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            transform.localScale = new Vector3(Scale, Scale, Scale);
            UpdateDevelopment();
        }

        private void OnEnable()
        {
            UpdateDevelopment();
        }

        private void OnDisable()
        {
            UpdateDevelopment();
        }

        private List<Vector3> WorldVertices()
        {
            List<Vector3> verts = new List<Vector3>();
            foreach(Vector3 v in ControlPoints.Positions)
                verts.Add(transform.TransformPoint(v));
            return verts;
        }

        public void UpdateDevelopment()
        {
            Paper = new Paper(WorldVertices());
        }

        [ContextMenu("Use A4")]
        public void UseA4Vertices()
        {
            ControlPoints.Positions = A4Vertices;
        }

        #region interface methods

        public void OnTransformChanged()
        {
            UpdateDevelopment();
        }

        public void OnControlPointChanged(List<Vector3> positions)
        {
            UpdateDevelopment();
        }

        #endregion
    }
}
