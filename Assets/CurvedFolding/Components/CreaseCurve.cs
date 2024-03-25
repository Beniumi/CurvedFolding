using RuntimeSceneView.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem.Components
{
    public class CreaseCurve : BasicCurve, IControllPointHandler
    {
        [field: SerializeField]
        public bool Symmetrization { get; set; } = false;

        [field: SerializeField, Range(6, 180)]
        public int DivisionCount { get; set; } = 60;

        [field: SerializeField]
        public ControlPointManager ControlPoints { get; private set; }

        private void Start()
        {
            UpdateCurve();
        }

        private void OnValidate()
        {
            UpdateCurve();
        }

        public void UpdateCurve()
        {
            if (!Symmetrization)
            {
                Divided = new BezierCurve(ControlPoints.Positions).Divide(DivisionCount);
            }
            else
            {
                List<Vector3> reverse = ReverseControlPoints();
                Divided = new BezierCurve(reverse).Divide(DivisionCount);
            }
        }

        public List<Vector3> ReverseControlPoints()
        {
            List<Vector3> positions = ControlPoints.Positions;
            List<Vector3> reverse = new List<Vector3>(positions);
            int n = positions.Count;
            for (int i = n - 1; 0 <= i; i--)
            {
                Vector3 pt = positions[i];
                reverse.Add(new Vector3(-pt.x, pt.y, pt.z));
            }
            return reverse;
        }

        #region interface methods

        public void OnControlPointChanged(List<Vector3> positions)
        {
            UpdateCurve();
        }

        #endregion
    }
}
