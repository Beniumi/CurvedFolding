using CurvedFoldingSystem;
using CurvedFoldingSystem.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UntwistTest : MonoBehaviour
{
    public enum UntwistMode 
    { 
        Untwist,
        Press,
    }

    [SerializeField, Range(0f, 1f)]
    float torsionRatio = 1.0f;
    [SerializeField]
    UntwistMode mode = UntwistMode.Untwist;
    [SerializeField]
    BasicCurve input;
    [SerializeField]
    BasicCurve handle;
    [SerializeField]
    BasicCurve output;

    private void Start()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (input == null || output == null) return;

        switch (mode)
        {
            case UntwistMode.Untwist:
                output.WorldDivided = UntwistMethod.UntwistCurve(input.WorldDivided, torsionRatio, out Quaternion sumRotation);
                break;
            case UntwistMode.Press:
                float[] ratios = new float[input.WorldDivided.Count];
                for(int i = 0; i < input.WorldDivided.Count; i++)
                {
                    ratios[i] = torsionRatio;
                }
                /*
                output.WorldDivided = UntwistMethod.PressCurve(input.WorldDivided, ratios, Vector3.up);
                */
                DevelopableSurface surf = new DevelopableSurface(input.WorldDivided, handle.WorldDivided);
                output.WorldDivided = UntwistMethod.PressCurve(surf, ratios);
                break;
        }
    }
}
