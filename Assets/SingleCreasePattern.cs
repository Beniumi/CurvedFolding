using CurvedFoldingSystem.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleCreasePattern : MonoBehaviour
{
    [System.Serializable]
    public class Surface3D
    {
        public RuledSurface HandleSurface;
        public RuledSurface SubhandleSurface;
    }

    [System.Serializable]
    public class Surface2D
    {
        public CreasePatternEdge edge;
        public RuledSurface HandleSurface;
        public RuledSurface SubhandleSurface;
        public RuledSurface GapSurface;
    }

    [SerializeField]
    public Surface3D surface3D;
    [SerializeField]
    public Surface2D surface2D;
    [SerializeField]
    FoldSetting foldSetting;

    public void Develop()
    {
        surface2D.SubhandleSurface.DevelopSurface(surface3D.SubhandleSurface);
        surface2D.HandleSurface.DevelopSurface(surface3D.HandleSurface);
        //UpdateCreasePattern(); //If you don't call on unity event.
    }

    public bool UseEdge => surface2D.edge != null && surface2D.edge.gameObject.activeSelf;

    public void UpdateCreasePattern()
    {
        surface2D.SubhandleSurface.Curve.OnTransformChanged();
        surface2D.HandleSurface.OnTransformChanged();
        surface2D.SubhandleSurface.OnTransformChanged();
        surface2D.GapSurface.SetRulings(surface2D.SubhandleSurface.Curve);
        if (UseEdge)
        {
            surface2D.HandleSurface.Trimming(surface2D.edge);
            surface2D.SubhandleSurface.Trimming(surface2D.edge);
            surface3D.HandleSurface.SetTrimSize(surface2D.HandleSurface);
            surface3D.SubhandleSurface.SetTrimSize(surface2D.SubhandleSurface);
        }
        else
        {
            surface2D.HandleSurface.ResetTrimming();
            surface2D.SubhandleSurface.ResetTrimming();
            surface3D.HandleSurface.ResetTrimming();
            surface3D.SubhandleSurface.ResetTrimming();
        }
        foldSetting.Verification();
    }
}
