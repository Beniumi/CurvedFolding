using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem.Components
{
    public class CurvedFolding : MonoBehaviour
    {
        [SerializeField]
        RuledSurface testSurface;
        [SerializeField]
        RuledSurface testSurface2;
        [SerializeField]
        RuledSurface testSurface3;
        [field: SerializeField]
        public BasicCurve testCurve;

        public enum PaintMode
        {
            Off,
            Normal,
            Intersection,
            Flatness,
            Developability,
        }
        [field: SerializeField]
        public Color H_RulingColor { get; set; } = Color.red;
        [field: SerializeField]
        public Color B_RulingColor { get; set; } = Color.blue;
        [field: SerializeField]
        public Color Modified_RulingColor { get; set; } = Color.magenta;
        [field: SerializeField]
        public Color NormalColor { get; set; } = Color.green;
        [field: SerializeField]
        public Color ErrorColor { get; set; } = Color.red;
        [field: SerializeField]
        public float ToleranceDistance { get; set; } = 0.5f;
        [field: SerializeField]
        public float ToleranceFlatness { get; set; } = 1E-05f;
        [field: SerializeField]
        public float ToleranceDevelopability { get; set; } = 8.7f * 1E-04f;
        [field: SerializeField]
        public PaintMode Paint { get; set; } = PaintMode.Off;

        [Serializable]
        public class Crvf3D
        {
            [field: SerializeField]
            public BasicCurve HandleCurve { get; private set; }
            [field: SerializeField]
            public BasicCurve BihandleCurve { get; private set; }
            [field: SerializeField]
            public RuledSurface HandleSurface { get; private set; }
            [field: SerializeField]
            public RuledSurface BihandleSurface { get; private set; }
        }

        [Serializable]
        public class Crvf2D
        {
            [field: SerializeField]
            public RuledSurface HandleSurface { get; private set; }
            [field: SerializeField]
            public RuledSurface BihandleSurface { get; private set; }
            [field: SerializeField]
            public RuledSurface GapSurface { get; private set; }
            [field: SerializeField]
            public CreasePatternEdge PaperEdge { get; private set; }
        }

        [field: SerializeField]
        public Crvf3D _3D { get; private set; }
        [field: SerializeField]
        public Crvf2D _2D { get; private set; }


        [Serializable]
        private class CrvfOutputs
        {
            [SerializeField]
            public bool[] isRulingsModified;
            [field: SerializeField]
            public FloatListInfo DistanceError { get; set; }
            [field: SerializeField]
            public FloatListInfo H_FlatnessError { get; set; }
            [field: SerializeField]
            public FloatListInfo B_FlatnessError { get; set; }
            [field: SerializeField]
            public FloatListInfo DevelopabilityError { get; set; }
            [SerializeField]
            public bool[] isH_RulingsIntersect;
            [SerializeField]
            public bool[] isB_RulingsIntersect;
        }

        [SerializeField]
        private CrvfOutputs outputs;

        private static List<Color> boolsToColors(bool[] bools, Color tColor, Color fColor)
        {
            List<Color> colors = new List<Color>();
            foreach(bool b in bools)
            {
                Color c = b ? tColor : fColor;
                colors.Add(c);
            }
            return colors;
        }

        private void OnValidate()
        {
            FloatListInfo.normal = NormalColor;
            FloatListInfo.error = ErrorColor;
            if (_2D.BihandleSurface == null || _2D.HandleSurface == null)
                UpdateOutput3D();
            else
                UpdateOutput();
        }

        public void UpdateCurvedFolding()
        {
            UpdateCreaseCurve(_3D.HandleSurface.Curve);
        }

        public void UpdateCreaseCurve(BasicCurve creaseCurve)
        {
            _3D.HandleCurve.SetCurve(creaseCurve);
        }

        public void UpdateHandleCurve(BasicCurve handleCurve)
        {
            _3D.HandleSurface.SetRulings(handleCurve);
            _3D.HandleSurface.OutBihandleCurve(_3D.BihandleCurve, out outputs.isRulingsModified);
            _3D.BihandleSurface.SetRulings(_3D.BihandleCurve);

            if (_2D.BihandleSurface == null || _2D.HandleSurface == null)
            {
                _3D.HandleSurface.SurfMesh.CreateMesh();
                _3D.BihandleSurface.SurfMesh.CreateMesh();
                UpdateOutput3D();
                return;
            }

            _2D.BihandleSurface.DevelopSurface(_3D.BihandleSurface);
            _2D.HandleSurface.DevelopSurface(_3D.HandleSurface);


            if (testSurface != null)
            {
                testSurface.TestDevelopSurface(_3D.HandleSurface);
            }
            if (testSurface2 != null)
            {
                testSurface2.TestDevelopSurface(_3D.BihandleSurface);
            }
            if (testSurface3 != null)
            {
                testSurface3.SetFoldRulings(_3D.HandleSurface);
                foreach(List<float> lengths in testSurface3.SurfMesh.rulingLengths)
                {
                    lengths[^1] = 15f;
                }
                testSurface3.SurfMesh.CreateMesh();
                testSurface3.SurfMesh.SetRulingColor(Color.blue);
            }

        }

        private void UpdateOutput3D()
        {
            outputs.H_FlatnessError = new FloatListInfo(_3D.HandleSurface.SurfMesh.GetFlatness());
            outputs.B_FlatnessError = new FloatListInfo(_3D.BihandleSurface.SurfMesh.GetFlatness());
            outputs.DevelopabilityError = new FloatListInfo(_3D.HandleSurface.SurfMesh.GetDevelopabilities(_3D.BihandleSurface.SurfMesh.rulingDirections));

            switch (Paint)
            {
                case PaintMode.Normal:
                    {
                        List<Color> B_colors = boolsToColors(outputs.isRulingsModified, Modified_RulingColor, B_RulingColor);
                        _3D.HandleSurface.SurfMesh.SetRulingColor(H_RulingColor);
                        _3D.BihandleSurface.SurfMesh.SetRulingColors(B_colors);
                        _3D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                        _3D.BihandleSurface.SurfMesh.SetStripColor(Color.white);
                    }
                    break;
                case PaintMode.Flatness:
                    {
                        List<float> H_evaluations = outputs.H_FlatnessError.Evaluate(ToleranceFlatness);
                        List<float> B_evaluations = outputs.B_FlatnessError.Evaluate(ToleranceFlatness);
                        List<Color> H_colors = FloatListInfo.EvaluationsToColors(H_evaluations);
                        List<Color> B_colors = FloatListInfo.EvaluationsToColors(B_evaluations);
                        _3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.HandleSurface.SurfMesh.SetStripColors(H_colors);
                        _3D.BihandleSurface.SurfMesh.SetStripColors(B_colors);
                    }
                    break;
                case PaintMode.Developability:
                    {
                        List<float> evaluations = outputs.DevelopabilityError.Evaluate(ToleranceDevelopability);
                        List<Color> colors = FloatListInfo.EvaluationsToColors(evaluations);
                        _3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.HandleSurface.SurfMesh.SetStripColors(colors);
                        _3D.BihandleSurface.SurfMesh.SetStripColors(colors);
                    }
                    break;
                default:
                    {
                        _3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                        _3D.BihandleSurface.SurfMesh.SetStripColor(Color.white);
                    }
                    break;
            }
        }

        public void UpdateDevelopedCurve(BasicCurve developedCurve)
        {
            //_2D.BihandleSurface.Curve.BlendCurve(developedCurve);
            _2D.HandleSurface.OnTransformChanged();
            _2D.BihandleSurface.Curve.OnTransformChanged();
            _2D.BihandleSurface.OnTransformChanged();

            if (_2D.GapSurface != null)
            {
                _2D.GapSurface.SetRulings(developedCurve);
                _2D.GapSurface.SurfMesh.CreateMesh();
                outputs.DistanceError = new FloatListInfo(_2D.GapSurface.SurfMesh.GetLengths());
                List<float> evaluations = outputs.DistanceError.Evaluate(ToleranceDistance);
                _2D.GapSurface.SurfMesh.SetStripColors(FloatListInfo.EvaluationsToColors(evaluations));
                _2D.GapSurface.SurfMesh.SetRulingColor(Color.gray);
            }

            UpdateCreasePatternEdge(_2D.PaperEdge);



            if (testCurve != null)
            {
                List<float> lengths = _2D.GapSurface.SurfMesh.GetLengths();
                List<Vector3> positions = _2D.GapSurface.Curve.WorldDivided.Positions;
                List<Vector3> testPos = new List<Vector3>();
                for(int i = 0; i < positions.Count; i++)
                {
                    Vector3 pos = positions[i] + _2D.GapSurface.SurfMesh.rulingDirections[i] * lengths[i] / 2.0f;
                    testPos.Add(pos);
                }
                testCurve.WorldDivided = new DividedCurve(testPos);
            }
        }

        public void UpdateCreasePatternEdge(CreasePatternEdge edge)
        {
            if (edge != null && edge.gameObject.activeSelf)
            {
                _2D.HandleSurface.Trimming(edge);
                _2D.BihandleSurface.Trimming(edge);
                _3D.HandleSurface.SetTrimSize(_2D.HandleSurface);
                _3D.BihandleSurface.SetTrimSize(_2D.BihandleSurface);
            }
            else
            {
                _2D.HandleSurface.ResetTrimming();
                _2D.BihandleSurface.ResetTrimming();
                _3D.HandleSurface.ResetTrimming();
                _3D.BihandleSurface.ResetTrimming();
            }
            _2D.HandleSurface.SurfMesh.CreateMesh();
            _2D.BihandleSurface.SurfMesh.CreateMesh();
            _3D.HandleSurface.SurfMesh.CreateMesh();
            _3D.BihandleSurface.SurfMesh.CreateMesh();
            UpdateOutput();
        }

        private void UpdateOutput()
        {
            outputs.H_FlatnessError = FloatListInfo.AbsInfo(_3D.HandleSurface.SurfMesh.GetFlatness());
            outputs.B_FlatnessError = FloatListInfo.AbsInfo(_3D.BihandleSurface.SurfMesh.GetFlatness());
            outputs.DevelopabilityError = FloatListInfo.AbsInfo(_3D.HandleSurface.SurfMesh.GetDevelopabilities(_3D.BihandleSurface.SurfMesh.rulingDirections));
            outputs.isH_RulingsIntersect = _2D.HandleSurface.SurfMesh.GetCrossed().ToArray();
            outputs.isB_RulingsIntersect = _2D.BihandleSurface.SurfMesh.GetCrossed().ToArray();

            switch (Paint)
            {
                case PaintMode.Normal:
                    {
                        List<Color> B_colors = boolsToColors(outputs.isRulingsModified, Modified_RulingColor, B_RulingColor);
                        _2D.HandleSurface.SurfMesh.SetRulingColor(H_RulingColor);
                        _2D.BihandleSurface.SurfMesh.SetRulingColors(B_colors);
                        _3D.HandleSurface.SurfMesh.SetRulingColor(H_RulingColor);
                        _3D.BihandleSurface.SurfMesh.SetRulingColors(B_colors);
                        _2D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                        _2D.BihandleSurface.SurfMesh.SetStripColor(Color.white);
                        _3D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                        _3D.BihandleSurface.SurfMesh.SetStripColor(Color.white);
                    }
                    break;
                case PaintMode.Intersection:
                    {
                        List<Color> H_colors = boolsToColors(outputs.isH_RulingsIntersect, ErrorColor, Color.grey);
                        List<Color> B_colors = boolsToColors(outputs.isB_RulingsIntersect, ErrorColor, Color.grey);
                        _2D.HandleSurface.SurfMesh.SetRulingColors(H_colors);
                        _2D.BihandleSurface.SurfMesh.SetRulingColors(B_colors);
                        _3D.HandleSurface.SurfMesh.SetRulingColors(H_colors);
                        _3D.BihandleSurface.SurfMesh.SetRulingColors(B_colors);
                        _2D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                        _2D.BihandleSurface.SurfMesh.SetStripColor(Color.white);
                        _3D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                        _3D.BihandleSurface.SurfMesh.SetStripColor(Color.white);
                    }
                    break;
                case PaintMode.Flatness:
                    {
                        List<float> H_evaluations = outputs.H_FlatnessError.Evaluate(ToleranceFlatness);
                        List<float> B_evaluations = outputs.B_FlatnessError.Evaluate(ToleranceFlatness);
                        List<Color> H_colors = FloatListInfo.EvaluationsToColors(H_evaluations);
                        List<Color> B_colors = FloatListInfo.EvaluationsToColors(B_evaluations);
                        _2D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _2D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _2D.HandleSurface.SurfMesh.SetStripColors(H_colors);
                        _2D.BihandleSurface.SurfMesh.SetStripColors(B_colors);
                        _3D.HandleSurface.SurfMesh.SetStripColors(H_colors);
                        _3D.BihandleSurface.SurfMesh.SetStripColors(B_colors);
                    }
                    break;
                case PaintMode.Developability:
                    {
                        List<float> evaluations = outputs.DevelopabilityError.Evaluate(ToleranceDevelopability);
                        List<Color> colors = FloatListInfo.EvaluationsToColors(evaluations);
                        _2D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _2D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _2D.HandleSurface.SurfMesh.SetStripColors(colors);
                        _2D.BihandleSurface.SurfMesh.SetStripColors(colors);
                        _3D.HandleSurface.SurfMesh.SetStripColors(colors);
                        _3D.BihandleSurface.SurfMesh.SetStripColors(colors);
                    }
                    break;
                default:
                    {
                        _2D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _2D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _3D.BihandleSurface.SurfMesh.SetRulingColor(Color.gray);
                        _2D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                        _2D.BihandleSurface.SurfMesh.SetStripColor(Color.white);
                        _3D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                        _3D.BihandleSurface.SurfMesh.SetStripColor(Color.white);
                    }
                    break;
            }
        }
    }
}
