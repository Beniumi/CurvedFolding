using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using UnityEngine.UIElements;

namespace CurvedFoldingSystem
{
    public class DividedCurve
    {
        public List<Vector3> Positions { get; private set; } = new List<Vector3>();

        public List<FrenetFrame> Frames { get; private set; } = new List<FrenetFrame>();

        public List<float> ArcLengths { get; private set; } = new List<float>();

        public List<float> Curvatures { get; private set; } = new List<float>();

        public List<float> Torsions { get; private set; } = new List<float>();

        public int Count
        {
            get
            {
                return Positions.Count;
            }
            set
            {
                int removeNum = Count - value;
                for (int i = 0; i < removeNum; i++) { 
                    Positions.RemoveAt(Positions.Count - 1);
                    Frames.RemoveAt(Frames.Count - 1);
                    ArcLengths.RemoveAt(ArcLengths.Count - 1);
                    Curvatures.RemoveAt(Curvatures.Count - 1);
                    Torsions.RemoveAt(Torsions.Count - 1);
                }
            }
        }

        public DividedCurve() { }

        public DividedCurve(List<Vector3> positions)
        {
            List<Vector3> dX = Diff(positions);
            List<Vector3> ddX = Diff(dX);
            List<Vector3> dddX = Diff(ddX);
            for(int i = 0; i < positions.Count; i++)
            {
                Vector3 cross = Vector3.Cross(dX[i], ddX[i]);
                float dXNorm = dX[i].magnitude;
                float crossNorm = cross.magnitude;
                Vector3 tangent = dXNorm != 0 ? dX[i] / dXNorm : Vector3.zero;
                Vector3 binormal = crossNorm != 0 ? cross / crossNorm : Vector3.zero;
                FrenetFrame frame = FrenetFrame.OrthoFrame(tangent, binormal);
                float curvature = crossNorm / (dXNorm * dXNorm * dXNorm);
                float torsion = Vector3.Dot(cross, dddX[i]) / (crossNorm * crossNorm);
                Positions.Add(positions[i]);
                Frames.Add(frame);
                ArcLengths.Add(dXNorm);
                Curvatures.Add(curvature);
                Torsions.Add(torsion);
            }
        }

        List<Vector3> Diff(List<Vector3> x)
        {
            if (x.Count < 2)
                return new List<Vector3>();

            List<Vector3> dx = new List<Vector3>() { (x[1] - x[0]) };
            for (int i = 1; i < x.Count - 1; i++)
            {
                dx.Add((x[i + 1] - x[i - 1]) / 2);
            }
            dx.Add((x[x.Count - 1] - x[x.Count - 2]));
            return dx;
        }

        public DividedCurve(ParametricCurve curve, List<float> parameter, List<float> arcLengths)
        {
            ArcLengths.AddRange(arcLengths);
            foreach (float t in parameter)
            {
                Vector3 position = curve.Position(t);
                Vector3 dX = curve.Differential(t, 1);
                Vector3 ddX = curve.Differential(t, 2);
                Vector3 dddX = curve.Differential(t, 3);
                Vector3 cross = Vector3.Cross(dX, ddX);
                float dXNorm = dX.magnitude;
                float crossNorm = cross.magnitude;
                Vector3 tangent = dXNorm != 0 ? dX / dXNorm : Vector3.zero;
                Vector3 binormal = crossNorm != 0 ? cross / crossNorm : Vector3.zero;
                FrenetFrame frame = FrenetFrame.OrthoFrame(tangent, binormal);
                float curvature = crossNorm / (dXNorm * dXNorm * dXNorm);
                float torsion = Vector3.Dot(cross, dddX) / (crossNorm * crossNorm);
                Positions.Add(position);
                Frames.Add(frame);
                Curvatures.Add(curvature);
                Torsions.Add(torsion);
            }
        }

        public DividedCurve(List<float> curvatures, List<float> torsions, List<float> arcLengths, List<FrenetFrame> frames, List<Vector3> positions) 
        {
            Curvatures.AddRange(curvatures);
            Torsions.AddRange(torsions);
            ArcLengths.AddRange(arcLengths);
            Frames.AddRange(frames);
            Positions.AddRange(positions);
        }

        public DividedCurve(DividedCurve clone) : this(clone.Curvatures, clone.Torsions, clone.ArcLengths, clone.Frames, clone.Positions) { }

        #region reconstruct curve

        public static DividedCurve Reconstruct(List<float> curvatures)
        {
            List<float> torsions = new List<float>();
            for (int i = 0; i < curvatures.Count; i++)
            {
                torsions.Add(0f);
            }
            return Reconstruct(curvatures, torsions);
        }

        public static DividedCurve Reconstruct(List<float> curvatures, List<float> torsions)
        {
            List<float> arcLengths = new List<float>();
            for (int i = 0; i < curvatures.Count; i++)
            {
                arcLengths.Add(1.0f);
            }
            return Reconstruct(curvatures, torsions, arcLengths);
        }

        public static DividedCurve Reconstruct(List<float> curvatures, List<float> torsions, List<float> arcLengths)
        {
            return Reconstruct(curvatures, torsions, arcLengths, FrenetFrame.Identity);
        }

        public static DividedCurve Reconstruct(List<float> curvatures, List<float> torsions, List<float> arcLengths, FrenetFrame startFrame)
        {
            return Reconstruct(curvatures, torsions, arcLengths, startFrame, Vector3.zero);
        }

        public static DividedCurve Reconstruct(List<float> curvatures, List<float> torsions, List<float> arcLengths, FrenetFrame startFrame, Vector3 startPos)
        {
            List<FrenetFrame> frames = new List<FrenetFrame>() { startFrame };
            int n = curvatures.Count;
            if (0 < n)
            {
                float l = arcLengths[0];
                frames.Add(FrenetFrame.Rotate(frames[0], curvatures[0], torsions[0], l));
            }
            for (int i = 2; i < n; i++)
            {
                float l = arcLengths[i - 2] + arcLengths[i - 1];
                frames.Add(FrenetFrame.Rotate(frames[i - 2], curvatures[i - 1], torsions[i - 1], l));
            }
            return Reconstruct(curvatures, torsions, arcLengths, frames, startPos);
        }

        /*
        private static DividedCurve ReconstructDiff(List<float> curvatures, List<float> torsions, List<float> arcLengths)
        {
            List<FrenetFrame> frames = new List<FrenetFrame>() { FrenetFrame.Identity };
            for (int i = 1; i < curvatures.Count; i++)
            {
                Vector3 tangent, binormal;

                switch (i)
                {
                    case 1:
                        {
                            tangent = frames[i - 1].tangent + arcLengths[i - 1] * curvatures[i - 1] * frames[i - 1].normal;
                            binormal = frames[i - 1].binormal - arcLengths[i - 1] * torsions[i - 1] * frames[i - 1].normal;
                        }
                        break;
                    default:
                        {
                            tangent = frames[i - 2].tangent + (arcLengths[i - 2] + arcLengths[i - 1]) * curvatures[i - 1] * frames[i - 1].normal;
                            binormal = frames[i - 2].binormal - (arcLengths[i - 2] + arcLengths[i - 1]) * torsions[i - 1] * frames[i - 1].normal;
                        }
                        break;
                }
                frames.Add(FrenetFrame.OrthoFrame(tangent, binormal));
            }
            return Reconstruct(curvatures, torsions, arcLengths, frames);
        }
        */

        public static DividedCurve Reconstruct(List<float> curvatures, List<float> torsions, List<float> arcLengths, List<FrenetFrame> frames)
        {
            return Reconstruct(curvatures, torsions, arcLengths, frames, Vector3.zero);
        }

        public static DividedCurve Reconstruct(List<float> curvatures, List<float> torsions, List<float> arcLengths, List<FrenetFrame> frames, Vector3 startPos)
        {
            List<Vector3> positions = new List<Vector3>() { startPos };
            for (int i = 1; i < curvatures.Count; i++)
            {
                Vector3 x = positions[i - 1] + (arcLengths[i - 1] * frames[i - 1].tangent + arcLengths[i] * frames[i].tangent) / 2.0f;
                positions.Add(x);
            }
            return new DividedCurve(curvatures, torsions, arcLengths, frames, positions);
        }

        #endregion

        public static DividedCurve SubCurve(DividedCurve curve, int from, int to)
        {
            DividedCurve newCurve = new DividedCurve();
            for(int i = from; i <= to; i++)
            {
                newCurve.Positions.Add(curve.Positions[i]);
                newCurve.Frames.Add(curve.Frames[i]);
                newCurve.ArcLengths.Add(curve.ArcLengths[i]);
                newCurve.Curvatures.Add(curve.Curvatures[i]);
                newCurve.Torsions.Add(curve.Torsions[i]);
            }
            return newCurve;
        }

        public static DividedCurve MargeCurve(DividedCurve curve1, DividedCurve curve2)
        {
            DividedCurve newCurve = new DividedCurve();
            newCurve.Positions.AddRange(curve1.Positions);
            newCurve.Positions.AddRange(curve2.Positions);
            newCurve.Frames.AddRange(curve1.Frames);
            newCurve.Frames.AddRange(curve2.Frames);
            newCurve.ArcLengths.AddRange(curve1.ArcLengths);
            newCurve.ArcLengths.AddRange(curve2.ArcLengths);
            newCurve.Curvatures.AddRange(curve1.Curvatures);
            newCurve.Curvatures.AddRange(curve2.Curvatures);
            newCurve.Torsions.AddRange(curve1.Torsions);
            newCurve.Torsions.AddRange(curve2.Torsions);
            return newCurve;
        }

        public static DividedCurve AllignCurve(DividedCurve curve, Vector3 position0, FrenetFrame frame0)
        {
            Quaternion rot = FrenetFrame.TowardRotation(curve.Frames[0], frame0);
            DividedCurve rotCurve = rot * curve;
            Vector3 trans = position0 - rotCurve.Positions[0];
            for(int i = 0; i < curve.Positions.Count; i++)
            {
                rotCurve.Positions[i] += trans;
            }
            return rotCurve;
        }

        public static DividedCurve ScalingCurve(DividedCurve curve, float scale)
        {
            List<Vector3> positions = new List<Vector3>();
            List<float> arcLengths = new List<float>();
            List<float> curvatures = new List<float>();
            List<float> torsions = new List<float>();
            for (int i = 0; i < curve.Positions.Count; i++)
            {
                positions.Add(scale * curve.Positions[i]);
                arcLengths.Add(scale * curve.ArcLengths[i]);
                curvatures.Add(1.0f / scale * curve.Curvatures[i]);
                torsions.Add(1.0f / scale * curve.Torsions[i]);
            }
            return new DividedCurve(curvatures, torsions, arcLengths, curve.Frames, positions);
        }

        public static DividedCurve operator *(Quaternion rot, DividedCurve curve)
        {
            List<Vector3> positions = new List<Vector3>();
            List<FrenetFrame> frames = new List<FrenetFrame>();
            for (int i = 0; i < curve.Positions.Count; i++)
            {
                positions.Add(rot * curve.Positions[i]);
                frames.Add(rot * curve.Frames[i]);
            }
            return new DividedCurve(curve.Curvatures, curve.Torsions, curve.ArcLengths, frames, positions);
        }

        public static DividedCurve operator *(Matrix4x4 mat, DividedCurve curve)
        {
            Quaternion rot = mat.rotation;
            float scale = mat.lossyScale.x;
            List<Vector3> positions = new List<Vector3>();
            List<FrenetFrame> frames = new List<FrenetFrame>();
            List<float> arcLengths = new List<float>();
            List<float> curvatures = new List<float>();
            List<float> torsions = new List<float>();
            for (int i = 0; i < curve.Positions.Count; i++)
            {
                positions.Add(mat.MultiplyPoint3x4(curve.Positions[i]));
                frames.Add(rot * curve.Frames[i]);
                arcLengths.Add(scale * curve.ArcLengths[i]);
                curvatures.Add(1.0f / scale * curve.Curvatures[i]);
                torsions.Add(1.0f / scale * curve.Torsions[i]);
            }
            return new DividedCurve(curvatures, torsions, arcLengths, frames, positions);
        }

    }
}
