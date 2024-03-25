using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem
{
    public abstract class ParametricCurve
    {

        public abstract Vector3 Position(float t);

        public abstract Vector3 Differential(float t, int order);

        public abstract DividedCurve Divide(int count);

        #region protected member for override

        protected static readonly float MARGIN_OF_ERROR = 0.001f;

        protected float LengthByRungeKuttaMethod(List<float> ts)
        {
            List<float> length = new List<float>();
            length.Add(0f);
            (float t, float dl) prev = (ts[0], Differential(ts[0], 1).magnitude);
            for (int i = 1; i < ts.Count; i++)
            {
                (float t, float dl) next = (ts[i], Differential(ts[i], 1).magnitude);
                float dt = next.t - prev.t;
                float k1 = prev.dl;
                float k2 = Differential((prev.t + next.t) / 2.0f, 1).magnitude;
                float k3 = k2;
                float k4 = next.dl;
                float rungeKutta = dt * (k1 + k2 * 2 + k3 * 2 + k4) / 6.0f;
                length.Add(length[length.Count - 1] + rungeKutta);
            }
            return length[length.Count - 1];
        }

        #endregion
    }
}
