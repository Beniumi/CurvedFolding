using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem
{
    [Serializable]
    public class FloatListInfo
    {
        public static Color normal = Color.green;
        public static Color error = Color.red;

        public static List<Color> EvaluationsToColors(List<float> evaluations)
        {
            List<Color> colors = new List<Color>();
            foreach (float e in evaluations)
                colors.Add(Color.Lerp(normal, error, e));
            return colors;
        }

        [field: SerializeField]
        public float Max { get; private set; }
        [field: SerializeField]
        public float Min { get; private set; }
        [field: SerializeField]
        public float Average { get; private set; }
        [field: SerializeField]
        public List<float> Values { get; private set; }

        public FloatListInfo(List<float> values)
        {
            Max = Mathf.NegativeInfinity;
            Min = Mathf.Infinity;
            Average = 0f;
            foreach (float v in values)
            {
                if (v > Max) Max = v;
                if (v < Min) Min = v;
                Average += v;
            }
            Average /= values.Count;
            Values = values;
        }

        public static FloatListInfo AbsInfo(List<float> values)
        {
            List<float> absValues = new List<float>();
            foreach (float v in values)
                absValues.Add(Mathf.Abs(v));
            return new FloatListInfo(absValues);
        }

        public List<float> Evaluate(float targetValue)
        {
            List<float> evaluations = new List<float>();
            foreach (float v in Values)
            {
                float t = Mathf.InverseLerp(0f, targetValue, v);
                evaluations.Add(t);
            }
            return evaluations;
        }
    }
}
