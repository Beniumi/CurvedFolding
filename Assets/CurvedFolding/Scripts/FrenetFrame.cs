using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
namespace CurvedFoldingSystem
{
    public struct FrenetFrame
    {

        public Vector3 tangent;
        public Vector3 normal;
        public Vector3 binormal;

        public FrenetFrame(Vector3 tangent, Vector3 normal, Vector3 binormal)
        {
            this.tangent = tangent;
            this.normal = normal;
            this.binormal = binormal;
        }

        public static FrenetFrame Identity => new FrenetFrame(Vector3.forward, Vector3.right, Vector3.up);

        public static FrenetFrame OrthoFrame(Vector3 tangent, Vector3 binormal)
        {
            Vector3.OrthoNormalize(ref tangent, ref binormal);
            Vector3 normal = Vector3.Cross(binormal, tangent);
            return new FrenetFrame(tangent, normal, binormal);
        }

        public static Quaternion TowardRotation(FrenetFrame from, FrenetFrame to)
        {
            Quaternion tangentRot = Quaternion.FromToRotation(from.tangent, to.tangent);
            FrenetFrame tmp = tangentRot * from;
            Quaternion binormalRot = Quaternion.FromToRotation(tmp.binormal, to.binormal);
            return binormalRot * tangentRot;
        }

        public static FrenetFrame operator -(FrenetFrame frame)
        {
            return new FrenetFrame(-frame.tangent, -frame.normal, -frame.binormal);
        }

        public static FrenetFrame operator *(Quaternion rot, FrenetFrame frame)
        {
            return new FrenetFrame(rot * frame.tangent, rot * frame.normal, rot * frame.binormal);
        }

        #region curvature and torsion

        public static FrenetFrame Rotate(FrenetFrame frame, float curvature, float torsion, float arcLength)
        {
            Quaternion qt = Quaternion.AngleAxis(curvature * Mathf.Rad2Deg * arcLength, frame.binormal);
            Quaternion qb = Quaternion.AngleAxis(torsion * Mathf.Rad2Deg * arcLength, frame.tangent);
            Vector3 t = qt * frame.tangent;
            Vector3 b = qb * frame.binormal;
            Vector3 n = Vector3.Cross(b, t);
            return new FrenetFrame(t, n, b);
        }

        public static void RotationBetween(FrenetFrame prevFrame, FrenetFrame nextFrame, float arcLength, out float curvature, out float torsion)
        {
            curvature = Vector3.SignedAngle(prevFrame.tangent, nextFrame.tangent, prevFrame.binormal) * Mathf.Deg2Rad / arcLength;
            torsion = Vector3.SignedAngle(prevFrame.binormal, nextFrame.binormal, prevFrame.tangent) * Mathf.Deg2Rad / arcLength;
        }

        #endregion

        #region rulings

        public Vector3 GetRuling(float alpha, float beta)
        {
            float sinA = Mathf.Sin(alpha);
            float cosA = Mathf.Cos(alpha);
            float sinB = Mathf.Sin(beta);
            float cosB = Mathf.Cos(beta);
            return GetRulingSinCos(new Vector2(cosA, sinA), new Vector2(cosB, sinB));
        }

        public float GetBeta(Vector3 ruling)
        {
            Vector2 beta = GetBetaSinCos(ruling);
            return Mathf.Atan2(beta.y, beta.x);
        }

        public float GetAlpha(Vector3 ruling)
        {
            Vector2 alpha = GetAlphaSinCos(ruling);
            return Mathf.Atan2(alpha.y, alpha.x);
        }

        public Vector3 GetFoldRuling(Vector3 ruling, float curvature, float torsion)
        {
            Vector2 alpha = GetAlphaSinCos(ruling);
            Vector2 foldAlpha = new Vector2(-alpha.x, alpha.y);

            Vector2 beta = GetBetaSinCos(ruling);
            float denominator = curvature * alpha.y * beta.y;
            Vector2 foldBeta;
            if (Mathf.Approximately(denominator, 0))
            {
                foldBeta = new Vector2(1.0f, 0) * Mathf.Sign(2.0f * torsion * beta.y - curvature * alpha.y * beta.x);
            }
            else
            {
                float foldCotBeta = (2.0f * torsion * beta.y - curvature * alpha.y * beta.x) / denominator;
                foldBeta = new Vector2(foldCotBeta, 1.0f) / Mathf.Sqrt(1.0f + foldCotBeta * foldCotBeta);
            }

            return GetRulingSinCos(foldAlpha, foldBeta);
        }

        public Vector3 GetRulingSinCos(Vector2 alpha, Vector2 beta)
        {
            return beta.x * tangent + beta.y * (alpha.x * normal + alpha.y * binormal);
        }

        public Vector2 GetAlphaSinCos(Vector3 ruling)
        {
            Vector3 rCrossTn = Vector3.Cross(ruling, tangent).normalized;
            float sinA = Vector3.Dot(rCrossTn, normal);
            float cosA = Vector3.Dot(rCrossTn, -binormal);
            return new Vector2(cosA, sinA);
        }

        public Vector2 GetBetaSinCos(Vector3 ruling)
        {
            Vector3 rn = ruling.normalized;
            Vector3 rCrossT = Vector3.Cross(rn, tangent);
            float sinB = rCrossT.magnitude;
            float cosB = Vector3.Dot(rn, tangent);
            return new Vector2(cosB, sinB);
        }

        #endregion
    }
}
