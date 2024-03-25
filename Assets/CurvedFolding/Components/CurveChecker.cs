using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeSceneView;

namespace CurvedFoldingSystem.Components
{
    public class CurveChecker : MonoBehaviour
    {
        public enum CheckMode
        {
            OFF,
            TANGENT,
            NORMAL,
            BINORMAL,
            CURVATURE,
            TORSION
        }

        [SerializeField]
        LineDrawer line;
        [SerializeField]
        CheckMode check = CheckMode.OFF;
        [SerializeField]
        float scale = 1.0f;
        [SerializeField]
        bool printLog = false;

        DividedCurve curve;

        public BasicCurve Curve
        {
            set
            {
                curve = value.Divided;
                CheckCurve(curve);
            }
        }

        public string Log { get; private set; }

        void OnValidate()
        {
            if (curve != null)
                CheckCurve(curve);
        }

        public void CheckCurve(DividedCurve curve)
        {
            if (curve == null)
                return;

            List<Vector3> lines = new List<Vector3>();
            Log = "Debug :" + check + "\n";

            switch (check)
            {
                case CheckMode.TANGENT:
                    Log += "X\tY\tZ\n";
                    for (int i = 0; i < curve.Positions.Count; i++)
                    {
                        Vector3 position = curve.Positions[i];
                        Vector3 direction = curve.Frames[i].tangent;
                        lines.Add(position);
                        lines.Add(position + direction * scale);
                        Log += direction.x + "\t" + direction.y + "\t" + direction.z + "\n";
                    }
                    break;
                case CheckMode.NORMAL:
                    Log += "X\tY\tZ\n";
                    for (int i = 0; i < curve.Positions.Count; i++)
                    {
                        Vector3 position = curve.Positions[i];
                        Vector3 direction = curve.Frames[i].normal;
                        lines.Add(position);
                        lines.Add(position + direction * scale);
                        Log += direction.x + "\t" + direction.y + "\t" + direction.z + "\n";
                    }
                    break;
                case CheckMode.BINORMAL:
                    Log += "X\tY\tZ\n";
                    for (int i = 0; i < curve.Positions.Count; i++)
                    {
                        Vector3 position = curve.Positions[i];
                        Vector3 direction = curve.Frames[i].binormal;
                        lines.Add(position);
                        lines.Add(position + direction * scale);
                        Log += direction.x + "\t" + direction.y + "\t" + direction.z + "\n";
                    }
                    break;
                case CheckMode.CURVATURE:
                    {
                        float sum = 1.0f;
                        foreach (float k in curve.Curvatures)
                        {
                            sum += k;
                        }
                        float factor = sum / (curve.Curvatures.Count + 1);
                        for (int i = 0; i < curve.Positions.Count; i++)
                        {
                            Vector3 position = curve.Positions[i];
                            Vector3 direction = curve.Frames[i].normal;
                            lines.Add(position);
                            lines.Add(position + direction * scale * curve.Curvatures[i] / factor);
                            Log += curve.Curvatures[i] + "\n";
                        }
                    }
                    break;
                case CheckMode.TORSION:
                    {
                        float sum = 1.0f;
                        foreach (float t in curve.Torsions)
                        {
                            sum += t;
                        }
                        float factor = sum / (curve.Torsions.Count + 1);
                        for (int i = 0; i < curve.Positions.Count; i++)
                        {
                            Vector3 position = curve.Positions[i];
                            Vector3 direction = curve.Frames[i].binormal;
                            lines.Add(position);
                            lines.Add(position + direction * scale * curve.Torsions[i] / factor);
                            Log += curve.Torsions[i] + "\n";
                        }
                    }
                    break;
            }
            line.Positions = lines;
            if (printLog)
                Debug.Log(Log);
        }
    }
}
