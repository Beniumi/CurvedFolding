using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UntwistMethod;

namespace CurvedFoldingSystem
{
    public class DevelopableSurface
    {
        public DividedCurve Curve { get; private set; } = new DividedCurve();

        public List<Vector3> Rulings { get; private set; } = new List<Vector3>();

        public List<float> Lengths { get; private set; } = new List<float>();

        public List<Vector2> Alpha { get; private set; } = new List<Vector2>();

        public List<Vector2> Beta { get; private set; } = new List<Vector2>();

        public DevelopableSurface() { }

        public DevelopableSurface(DevelopableSurface clone) : this(clone.Curve, clone.Rulings, clone.Lengths, clone.Alpha, clone.Beta) { }

        public DevelopableSurface(DividedCurve curve, List<Vector3> rulings, List<float> lengths, List<Vector2> alpha, List<Vector2> beta)
        {
            Curve = new DividedCurve(curve);
            Rulings.AddRange(rulings);
            Lengths.AddRange(lengths);
            Alpha.AddRange(alpha);
            Beta.AddRange(beta);
        }

        public DevelopableSurface(DividedCurve curve, List<float> lengths, List<Vector2> alpha, List<Vector2> beta)
        {
            Curve = curve;
            Lengths.AddRange(lengths);
            Alpha.AddRange(alpha);
            Beta.AddRange(beta);
            for (int i = 0; i < curve.Curvatures.Count; i++)
                Rulings.Add(Ruling(curve.Frames[i], alpha[i], beta[i]));
        }

        public DevelopableSurface(DividedCurve curve, List<Vector2> alpha, List<Vector2> beta) : this(curve, InitializeLengths(alpha.Count), alpha, beta) { }

        public DevelopableSurface(DividedCurve curve, List<Vector3> rulings)
        {
            Curve = curve;
            int n = curve.Positions.Count;
            for (int i = 0; i < n; i++)
            {
                //Length
                float length = rulings[i].magnitude;
                Lengths.Add(length);
                //Ruling
                Vector3 ruling = Mathf.Approximately(length, 0) ? Vector3.zero : rulings[i] / length;
                Rulings.Add(ruling);
                var frame = curve.Frames[i];
                Vector3 rulingCrossT = Vector3.Cross(ruling, frame.tangent);
                //Beta
                float sinB = rulingCrossT.magnitude;
                float cosB = Vector3.Dot(ruling, frame.tangent);
                Vector2 b = new Vector2(cosB, sinB);
                Beta.Add(b);
                //Alpha
                rulingCrossT = rulingCrossT.normalized;
                float sinA = Vector3.Dot(rulingCrossT, frame.normal);
                float cosA = Vector3.Dot(rulingCrossT, -frame.binormal);
                Vector2 a = new Vector2(cosA, sinA);
                Alpha.Add(a);
            }
        }

        public DevelopableSurface(DividedCurve curve, DividedCurve handle) : this(curve, GetRulings(curve.Positions, handle.Positions)) { }

        public DevelopableSurface(DividedCurve curve, List<float> lengths, List<float> radians, List<float> diffRads)
        {
            Curve = curve;
            Lengths.AddRange(lengths);
            for (int i = 0; i < curve.Positions.Count; i++)
            {
                //Alpha
                float sinAlpha = Mathf.Sin(radians[i]);
                float cosAlpha = Mathf.Cos(radians[i]);
                Vector2 a = new Vector2(cosAlpha, sinAlpha);
                Alpha.Add(a);
                //Beta
                float torsion = curve.Torsions[i];
                float curvature = curve.Curvatures[i];
                float cotBeta = (diffRads[i] + torsion) / (curvature * sinAlpha);
                Vector2 b = CotToSinCos(cotBeta);
                Beta.Add(b);
                //Ruling
                Rulings.Add(Ruling(curve.Frames[i], a, b));
            }
        }

        public DevelopableSurface(DividedCurve curve, List<float> radians, List<float> diffRads) : this(curve, InitializeLengths(radians.Count), radians, diffRads) { }

        public DevelopableSurface(DividedCurve curve, List<float> radians) : this(curve, radians, DiffRads(radians)) { }

        public static Vector3 Ruling(FrenetFrame frame, Vector2 Alpha, Vector2 Beta) => Beta.x * frame.tangent + Beta.y * (Alpha.x * frame.normal + Alpha.y * frame.binormal);

        static List<float> InitializeLengths(int count)
        {
            List<float> lengths = new List<float>();
            for (int i = 0; i < count; i++)
                lengths.Add(1.0f);
            return lengths;
        }

        public static List<Vector3> GetRulings(List<Vector3> curve1, List<Vector3> curve2)
        {
            List<Vector3> rulings = new List<Vector3>();
            for (int i = 0; i < curve1.Count; i++)
                rulings.Add(curve2[i] - curve1[i]);
            return rulings;
        }

        static List<float> DiffRads(List<float> radians)
        {
            List<float> diffRads = new List<float>() { (radians[1] - radians[0]) };
            for (int i = 1; i < radians.Count - 1; i++)
            {
                diffRads.Add((radians[i + 1] - radians[i - 1]) / 2);
            }
            diffRads.Add(radians[radians.Count - 1] - radians[radians.Count - 2]);
            return diffRads;
        }

        static Vector2 CotToSinCos(float cot)
        {
            if (float.IsNaN(cot))
                return new Vector2(0f, 1.0f);
            float denominator = Mathf.Sqrt(1.0f + Mathf.Pow(cot, 2));
            float sin = 1.0f / denominator;
            float cos = cot / denominator;
            return new Vector2(cos, sin);
        }

        #region fold

        public Vector2 FoldAlpha(int i) => new Vector2(-Alpha[i].x, Alpha[i].y);

        public Vector2 FoldBeta(int i)
        {
            float cotBeta = Beta[i].x / Beta[i].y;
            float foldCotBeta = 2.0f * Curve.Torsions[i] / Curve.Curvatures[i] / Alpha[i].y - cotBeta;
            return CotToSinCos(foldCotBeta);
        }

        public Vector3 GetFoldRuling(int i) => Ruling(Curve.Frames[i], FoldAlpha(i), FoldBeta(i));

        public static Vector2 FoldAlpha(Vector2 alpha) => new Vector2(-alpha.x, alpha.y);

        public static Vector2 FoldBeta(Vector2 beta, Vector2 alpha, float curvature, float torsion)
        {
            float cotBeta = beta.x / beta.y;
            float foldCotBeta = 2.0f * torsion / curvature / alpha.y - cotBeta;
            return CotToSinCos(foldCotBeta);
        }

        public static Vector3 GetFoldRuling(Vector3 ruling, FrenetFrame frame, float curvature, float torsion)
        {

            //Ruling
            Vector3 rulingCrossT = Vector3.Cross(ruling, frame.tangent);
            //Beta
            float sinB = rulingCrossT.magnitude;
            float cosB = Vector3.Dot(ruling, frame.tangent);
            Vector2 b = new Vector2(cosB, sinB);
            //Alpha
            rulingCrossT = rulingCrossT.normalized;
            float sinA = Vector3.Dot(rulingCrossT, frame.normal);
            float cosA = Vector3.Dot(rulingCrossT, -frame.binormal);
            Vector2 a = new Vector2(cosA, sinA);

            return Ruling(frame, FoldAlpha(a), FoldBeta(b, a, curvature, torsion));
        }

        public DividedCurve GetBihandleCurve()
        {
            return GetBihandleCurve(Lengths[0], out bool[] isModified);
        }

        public DividedCurve GetBihandleCurve(out bool[] isModified)
        {
            return GetBihandleCurve(Lengths[0], out isModified);
        }

        public DividedCurve GetBihandleCurve(float length)
        {
            return GetBihandleCurve(length, out bool[] isModified);
        }

        public DividedCurve GetBihandleCurve(float length, out bool[] isModified)
        {
            isModified = new bool[Curve.Positions.Count];
            if (Curve.Positions.Count == 0)
                return new DividedCurve();

            Vector3 start = Curve.Positions[0] + GetFoldRuling(0) * length;
            List<Vector3> positions = new List<Vector3>() { start };

            int n = Curve.Positions.Count;

            for (int i = 1; i < n; i++)
            {
                Vector3 fr = GetFoldRuling(i);
                var frame = Curve.Frames[i];
                Vector3 normal = Vector3.Cross(fr, frame.tangent).normalized;
                Plane pqStrip = new Plane(normal, Curve.Positions[i]);

                if (Curve.Torsions[i] * Alpha[i].y < 0)
                {
                    Vector3 dx = Curve.Positions[i] - Curve.Positions[i - 1];
                    Ray tangent = new Ray(positions[i - 1], dx);
                    if (pqStrip.Raycast(tangent, out float lng) && lng >= 0)
                    {
                        positions.Add(tangent.GetPoint(lng));
                        isModified[i] = false;
                    }
                    else
                    {
                        positions.Add(tangent.GetPoint(0f));
                        isModified[i] = true;
                    }
                }
                else
                {
                    for (int j = i - 1; 0 <= j; j--)
                    {
                        Vector3 dx = Curve.Positions[j + 1] - Curve.Positions[j];
                        Ray tangent = new Ray(positions[j], dx);
                        if (pqStrip.Raycast(tangent, out float lng) && lng >= 0)
                        {
                            Vector3 newPos = tangent.GetPoint(lng);
                            for (int k = j + 1; k < i; k++)
                            {
                                positions[k] = newPos;
                                isModified[k] = true;
                            }
                            positions.Add(newPos);
                            break;
                        }else if(j == 0)
                        {
                            Vector3 newPos = tangent.GetPoint(lng);
                            for (int k = 0; k < i; k++)
                            {
                                positions[k] = newPos;
                                isModified[k] = true;
                            }
                            positions.Add(newPos);
                        }
                    }
                }
            }

            return new DividedCurve(positions);
        }

        #endregion

        #region develop

        public static DevelopableSurface Develop(DevelopableSurface surf3d)
        {
            List<Vector2> A = surf3d.Alpha;
            if (A.Count == 0)
                return new DevelopableSurface();
            float side = Mathf.Sign(A[0].x);
            DividedCurve curve3d = surf3d.Curve;

            List<Vector2> devA = new List<Vector2>();
            List<float> kCosA = new List<float>();
            List<float> tau = new List<float>();
            int n = A.Count;
            for (int i = 0; i < n; i++)
            {
                devA.Add(new Vector2(side, 0f));
                kCosA.Add(side * curve3d.Curvatures[i] * A[i].x);
                tau.Add(0f);
            }
            DividedCurve curve2d = DividedCurve.Reconstruct(kCosA, tau, curve3d.ArcLengths);
            DevelopableSurface surf2d = new DevelopableSurface(curve2d, devA, surf3d.Beta);
            surf2d.Lengths = surf3d.Lengths;
            return surf2d;
        }

        public DividedCurve GetDevelopedCurve()
        {
            float side = Mathf.Sign(Alpha[0].x);
            List<float> kCosA = new List<float>();
            List<float> tau = new List<float>();
            for (int i = 0; i < Alpha.Count; i++)
            {
                kCosA.Add(side * Curve.Curvatures[i] * Alpha[i].x);
                tau.Add(0f);
            }
            return DividedCurve.Reconstruct(kCosA, tau, Curve.ArcLengths);
        }

        public static DevelopableSurface TestDevelop(DevelopableSurface surf3d)
        {
            List<Vector3> cross = new List<Vector3>() { Vector3.up * -Mathf.Sign(surf3d.Alpha[0].x) };
            List<Vector3> dXs = new List<Vector3>();
            for (int i = 0; i < surf3d.Alpha.Count - 1; i++)
            {
                Vector3 dx = surf3d.Curve.Positions[i + 1] - surf3d.Curve.Positions[i];
                cross.Add(Vector3.Cross(surf3d.Rulings[i], dx.normalized));
                dXs.Add(dx);
            }
            dXs.Add(dXs[^1]);

            List<Quaternion> rotations = new();
            for (int i = 1; i < cross.Count; i++)
            {
                rotations.Add(Quaternion.FromToRotation(cross[i], cross[i - 1]));
            }

            List<Vector3> positions = new() { Vector3.zero };
            List<Vector3> rulings = new();
            Quaternion sum = Quaternion.identity;
            for (int i = 0; i < rotations.Count; i++)
            {
                sum *= rotations[i];
                Vector3 nextPosition = positions[^1] + sum * dXs[i];
                positions.Add(nextPosition);
                Vector3 ruling = sum * surf3d.Rulings[i];
                rulings.Add(ruling);
            }
            rulings.Add(sum * surf3d.Rulings[^1]);
            DevelopableSurface surf2d = new DevelopableSurface(new DividedCurve(positions), rulings);
            surf2d.Lengths = surf3d.Lengths;
            return surf2d;
        }

        public void TestDevelopCurve()
        {
            List<Vector3> cross = new List<Vector3>();
            //List<Vector3> axis = new List<Vector3>();
            for(int i = 0; i < Alpha.Count - 1; i++)
            {
                Vector3 dx = Curve.Positions[i + 1] - Curve.Positions[i];
                cross.Add(Vector3.Cross(Rulings[i], dx.normalized));
            }
            string txt = "\n";
            for (int i = 1; i < cross.Count; i++)
            {
                Quaternion q = Quaternion.FromToRotation(cross[i], cross[i - 1]);
                q.ToAngleAxis(out float angle, out Vector3 axis);
                txt += i + "\t" + Rulings[i] + "\n\t" + axis + "\n";
            }
            Debug.Log(txt);
        }

        #endregion



        #region Inflection Point

        public List<int> GetInflectionPointsIndex()
        {
            List<int> index = new List<int>();
            float prevSign = Mathf.Sign(Alpha[0].x);
            for (int i = 1; i < Curve.Count; i++)
            {
                float currentSign = Mathf.Sign(Alpha[i].x);
                if (prevSign != currentSign)
                {
                    index.Add(i - 1);
                }
                prevSign = currentSign;
            }
            return index;
        }

        #endregion
    }
}