using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

namespace CurvedFoldingSystem
{
    public class BezierCurve : ParametricCurve
    {
        class Bernstein
        {
            List<int>[] combinationsTable;

            public Bernstein(int controlPointCount)
            {
                SetTable(controlPointCount);
            }

            #region private

            void SetTable(int controlPointCount)
            {
                combinationsTable = new List<int>[controlPointCount];
                for (int di = 0; di < controlPointCount; di++)
                    combinationsTable[di] = Combinations(di);
            }

            static List<int> Combinations(int n)
            {
                List<int> combinations = new List<int>();
                List<int> factorials = new List<int>();
                for (int i = 0; i <= n; i++)
                    factorials.Add(Factorial(i));

                for (int i = 0; i <= n; i++)
                    combinations.Add(factorials[n] / (factorials[i] * factorials[n - i]));
                return combinations;
            }

            static int Factorial(int n)
            {
                if (n <= 1)
                    return 1;
                return n * Factorial(n - 1);
            }

            #endregion

            public void Update(int controlPointCount)
            {
                if (controlPointCount != combinationsTable.Length)
                    SetTable(controlPointCount);
            }

            public float Basis(int n, int i, float t)
            {
                return combinationsTable[n][i] * Mathf.Pow(t, i) * Mathf.Pow(1 - t, n - i);
            }

            public float Derivative(int degree, int cpi, float t, int order)
            {
                if (cpi < 0 || degree < cpi)
                    return 0;

                if (order == 0)
                    return Basis(degree, cpi, t);

                return degree * (Derivative(degree - 1, cpi - 1, t, order - 1) - Derivative(degree - 1, cpi, t, order - 1));
            }
        }

        readonly Bernstein bernstein;

        List<Vector3> controlPoints = new List<Vector3>();

        public List<Vector3> ControlPoints
        {
            get
            {
                return controlPoints;
            }

            set
            {
                controlPoints = value;
                bernstein.Update(controlPoints.Count);
            }
        }

        public BezierCurve(List<Vector3> controlPoints)
        {
            bernstein = new Bernstein(ControlPoints.Count);
            ControlPoints = controlPoints;
        }

        public override Vector3 Position(float t)
        {
            int degree = ControlPoints.Count - 1;
            Vector3 position = Vector3.zero;
            for (int i = 0; i < ControlPoints.Count; i++)
                position += bernstein.Basis(degree, i, t) * ControlPoints[i];
            return position;
        }

        public override Vector3 Differential(float t, int order)
        {
            int degree = ControlPoints.Count - 1;
            Vector3 vector = Vector3.zero;
            for (int i = 0; i < ControlPoints.Count; i++)
                vector += bernstein.Derivative(degree, i, t, order) * ControlPoints[i];
            return vector;
        }

        public override DividedCurve Divide(int divisionCount)
        {
            List<float> t = new List<float>();
            t.Add(0f);
            int localDivisionCount = divisionCount;
            float sectionSize = 0.5f;
            int sectionCount = 0;
            for (int i = 0; i < divisionCount; i++)
            {
                for (int j = 1; j <= localDivisionCount; j++)
                    t.Add((float)i / divisionCount + (float)j / localDivisionCount / divisionCount);

                if (++sectionCount >= sectionSize)
                {
                    sectionCount = 0;
                    sectionSize *= 2.0f;
                    localDivisionCount = Mathf.CeilToInt(localDivisionCount / 2.0f);
                }
            }

            List<float> length = new List<float>();
            length.Add(0f);
            for (int i = 0; i < t.Count - 1; i++)
                length.Add(length[i] + LengthByRungeKuttaMethod(t.GetRange(i, 2)));
            float dx = length[length.Count - 1] / divisionCount;

            List<float> parameter = new List<float>();
            parameter.Add(0f);
            int searchIndex = 1;
            for (int i = 1; i < divisionCount; i++)
            {
                float goal = i * dx;
                while (length[searchIndex] < goal)
                    searchIndex++;
                parameter.Add(ParameterFromLength(length[searchIndex - 1], t[searchIndex - 1], t[searchIndex], goal));
            }
            parameter.Add(1f);

            List<float> arcLegths = new List<float>();
            for (int i = 0; i < parameter.Count; i++)
            {
                arcLegths.Add(dx);
            }
            DividedCurve curve = new DividedCurve(this, parameter, arcLegths);
            return curve;
        }

        float ParameterFromLength(float length, float t0, float t1, float goal)
        {
            float tmid = (t0 + t1) / 2.0f;
            float lmid = length + LengthByRungeKuttaMethod(new List<float>() { t0, tmid });
            if (Mathf.Abs(lmid - goal) < MARGIN_OF_ERROR)
                return tmid;
            if (goal < lmid)
                return ParameterFromLength(length, t0, tmid, goal);
            else
                return ParameterFromLength(lmid, tmid, t1, goal);
        }
    }
}
