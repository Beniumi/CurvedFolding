using RuntimeSceneView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace CurvedFoldingSystem.Components
{
    public class SingleCurvedFold : MonoBehaviour
    {
        /*
        [field: SerializeField]
        public int BaseRulingIndex { get; set; } = 0;
        [field: SerializeField]
        public bool AutoLengthSet { get; set; } = true;
        */

        public enum Mode
        {
            RawRuling,
            AllowGap,
            Untwist,
            Optimize,
            Press
        }

        [System.Serializable]
        private class Input
        {
            public BasicCurve crease;
            public BasicCurve handle;
        }

        [System.Serializable]
        private class Output
        {
            public BasicCurve crease;
            public BasicCurve handle;
            public BasicCurve subhandle;
        }

        [SerializeField]
        Input input;
        [SerializeField]
        Output output;

        [field: SerializeField]
        public Mode FoldMethod { get; set; } = Mode.AllowGap;
        [field: SerializeField, Range(0.01f, 100f)]
        public float BaseRulingLength { get; set; } = 10f;

        public bool[] isModified;

        private void OnValidate()
        {
            ConstructFold();
        }

        public void ConstructFold()
        {
            isModified = new bool[input.crease.WorldDivided.Positions.Count];
            switch (FoldMethod)
            {
                case Mode.RawRuling:
                    RawRulingFold(input.crease.WorldDivided, input.handle.WorldDivided);
                    break;
                case Mode.AllowGap:
                    AllowGapFold(input.crease.WorldDivided, input.handle.WorldDivided);
                    break;
                case Mode.Untwist:
                    UntwistFold();
                    break;
                case Mode.Optimize:
                    OptimizeFold();
                    break;
                case Mode.Press:
                    PressFold();
                    break;
            }
        }

        private void RawRulingFold(DividedCurve crease, DividedCurve handle)
        {
            DevelopableSurface dev = new DevelopableSurface(crease, handle);
            output.crease.WorldDivided = new DividedCurve(crease);
            output.handle.WorldDivided = new DividedCurve(handle);

            List<Vector3> positions = new List<Vector3>();
            for(int i = 0; i < crease.Positions.Count; i++)
            {
                Vector3 position = crease.Positions[i] + BaseRulingLength * dev.GetFoldRuling(i);
                positions.Add(position);
            }
            output.subhandle.WorldDivided = new DividedCurve(positions);
        }

        private void AllowGapFold(DividedCurve crease, DividedCurve handle)
        {
            DevelopableSurface dev = new DevelopableSurface(crease, handle);
            output.crease.WorldDivided = new DividedCurve(crease);
            output.handle.WorldDivided = new DividedCurve(handle);
            output.subhandle.WorldDivided = dev.GetBihandleCurve(BaseRulingLength, out isModified);
        }

        private void PressFold()
        {
            DividedCurve initialCrease = input.crease.WorldDivided;
            DividedCurve initialHandle = input.handle.WorldDivided;
            Matrix4x4 handleMat = input.handle.transform.localToWorldMatrix * input.crease.transform.worldToLocalMatrix;
            DevelopableSurface initialSurf = new DevelopableSurface(initialCrease, initialHandle);
            float[] pressures = new float[initialCrease.Count];
            //List<List<int>> curveIndexList = UntwistMethod.DivideCurveIndexAtInflectionPoint(initialSurf);

            DividedCurve crease = null, handle = null, subhandle = null;
            int count = 0;
            for (; count < 100; count++)
            {
                crease = UntwistMethod.PressCurve(initialSurf, pressures);
                handle = handleMat * crease;
                DevelopableSurface surf = new DevelopableSurface(crease, handle);
                subhandle = surf.GetBihandleCurve(BaseRulingLength, out isModified);
                DevelopableSurface subsurf = new DevelopableSurface(crease, subhandle);

                bool existMod = false;
                for (int i = 0; i < isModified.Length; i++)
                {
                    if (isModified[i])
                    {
                        float suiTau = UntwistMethod.SuitableTorsion(surf, subsurf, i);
                        float p = 1.0f - suiTau / initialCrease.Torsions[i];
                        if (p < pressures[i])
                            continue;

                        List<int> index = UntwistMethod.GetNonInflectionPointRange(initialSurf, i);
                        foreach(int j in index)
                            pressures[j] = p;

                        existMod = true;
                        break;
                    }
                }
                if (!existMod)
                    break;
            }
            output.crease.WorldDivided = crease;
            output.handle.WorldDivided = handle;
            output.subhandle.WorldDivided = subhandle;
            Debug.Log("Count\t" + count);
            string text = "";
            for(int i = 0; i < crease.Count; i++)
            {
                text += pressures[i] + "\n";
            }
            Debug.Log(text);
        }

        private void UntwistFold()
        {
            Matrix4x4 creaseToHandle = input.handle.transform.localToWorldMatrix * input.crease.transform.worldToLocalMatrix;
            UntwistMethod untwist = new UntwistMethod(input.crease.WorldDivided, creaseToHandle, BaseRulingLength);
            //UntwistMethod untwist = new UntwistMethod(input.crease.WorldDivided, input.handle.WorldDivided, BaseRulingLength);
            output.crease.WorldDivided = untwist.crease;
            output.handle.WorldDivided = untwist.handle;
            output.subhandle.WorldDivided= untwist.subhandle;
        }

        private void OptimizeFold()
        {
            AllowGapFold(input.crease.WorldDivided, input.handle.WorldDivided);
            string text = "error\n";
            for(int count = 0; count < 100; count++)
            {
                DividedCurve crease = output.crease.WorldDivided;
                DividedCurve handle = output.handle.WorldDivided;
                DividedCurve subhandle = output.subhandle.WorldDivided;
                DevelopableSurface devH = new DevelopableSurface(crease, handle);
                DevelopableSurface devSH = new DevelopableSurface(crease, subhandle);
                List<float> tau = new List<float>();
                List<float> kappa = new List<float>();
                float error = 0;
                for (int i = 0; i < crease.Count; i++)
                {
                    float cotH = devH.Beta[i].x / devH.Beta[i].y;
                    float cotSH = devSH.Beta[i].x / devSH.Beta[i].y;
                    float tauSH = (cotH + cotSH) / 2.0f * crease.Curvatures[i] * devSH.Alpha[i].y;
                    float kappaSH = crease.Curvatures[i];
                    tau.Add((crease.Torsions[i] + tauSH) / 2.0f);
                    kappa.Add(kappaSH);
                    error += Mathf.Abs(crease.Torsions[i] - tauSH);
                }
                DividedCurve newCrease = DividedCurve.Reconstruct(kappa, tau, crease.ArcLengths, crease.Frames[0], crease.Positions[0]);
                DividedCurve newHandle = input.handle.transform.localToWorldMatrix * output.crease.Divided;
                AllowGapFold(newCrease, newHandle);
                text += error + "\n";
            }
            Debug.Log(text);

        }
    }
}
