using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem.Components
{
    public class ScalableCurve : BasicCurve
    {
        [SerializeField, Range(0.01f, 1.99f)]
        float scale = 1.0f;

        public float Scale
        {
            get { return scale; }
            set
            {
                scale = value;
                transform.localScale = new Vector3(scale, scale, scale);
            }
        }

        private void OnValidate()
        {
            Scale = scale;
        }
    }
}
