using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeSceneView
{
    public abstract class LineDrawer : MonoBehaviour
    {
        public enum LineType
        {
            LINES,
            STRIP,
        }

        [SerializeField]
        protected bool useWorldSpace = false;
        [SerializeField]
        protected LineType mode = LineType.STRIP;
        [SerializeField]
        protected List<Vector3> positions = new List<Vector3>();
        [SerializeField]
        protected Material material;
        [field: SerializeField]
        public Mesh Mesh { get; private set; }

        public List<Vector3> Positions
        {
            get { return positions; }
            set
            {
                positions = value;
                PrepareLine();
            }
        }

        protected void Awake()
        {
            Mesh = new Mesh();
        }

        public abstract void PrepareLine();
    }

}