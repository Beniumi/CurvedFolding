using CurvedFoldingSystem;
using CurvedFoldingSystem.Components;
using CurvedFoldingSystem.Components.FileManager;
using MeshDataStructures.Parameterization;
using SFB;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using static SingleCreasePattern;

public class FoldSetting : MonoBehaviour
{
    public enum PaintMode
    {
        Off,
        Normal,
        Intersection,
        Flatness,
        Developability,
    }

    [SerializeField]
    SingleCurvedFold curvedFold;

    [SerializeField]
    SingleCreasePattern creasePattern;

    [SerializeField]
    MeshOptimizer optimizer;

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
    public class Results
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
        [field: SerializeField]
        public Mesh FoldMesh { get; set; }
    }

    [SerializeField]
    public Results outputs;

    private RulingVertexManager manager;

    private void Start()
    {
        outputs.FoldMesh = new Mesh();
    }

    private void OnValidate()
    {
        if (outputs.FoldMesh == null) outputs.FoldMesh = new Mesh();
        else outputs.FoldMesh.Clear(false);
        Verification();
    }

    public void Verification()
    {
        creasePattern.surface2D.HandleSurface.SurfMesh.CreateMesh();
        creasePattern.surface2D.SubhandleSurface.SurfMesh.CreateMesh();
        creasePattern.surface3D.HandleSurface.SurfMesh.CreateMesh();
        creasePattern.surface3D.SubhandleSurface.SurfMesh.CreateMesh();
        BakeMesh();

        outputs.isRulingsModified = curvedFold.isModified;
        outputs.H_FlatnessError = FloatListInfo.AbsInfo(creasePattern.surface3D.HandleSurface.SurfMesh.GetFlatness());
        outputs.B_FlatnessError = FloatListInfo.AbsInfo(creasePattern.surface3D.SubhandleSurface.SurfMesh.GetFlatness());
        outputs.DevelopabilityError = FloatListInfo.AbsInfo(creasePattern.surface3D.HandleSurface.SurfMesh.GetDevelopabilities(creasePattern.surface3D.SubhandleSurface.SurfMesh.rulingDirections));
        outputs.isH_RulingsIntersect = creasePattern.surface2D.HandleSurface.SurfMesh.GetCrossed().ToArray();
        outputs.isB_RulingsIntersect = creasePattern.surface2D.SubhandleSurface.SurfMesh.GetCrossed().ToArray();


        creasePattern.surface2D.GapSurface.SurfMesh.CreateMesh();
        outputs.DistanceError = new FloatListInfo(creasePattern.surface2D.GapSurface.SurfMesh.GetLengths());
        List<float> gapEvaluations = outputs.DistanceError.Evaluate(ToleranceDistance);
        creasePattern.surface2D.GapSurface.SurfMesh.SetStripColors(FloatListInfo.EvaluationsToColors(gapEvaluations));
        creasePattern.surface2D.GapSurface.SurfMesh.SetRulingColor(Color.gray);

        switch (Paint)
        {
            case PaintMode.Normal:
                {
                    List<Color> B_colors = boolsToColors(outputs.isRulingsModified, Modified_RulingColor, B_RulingColor);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetRulingColor(H_RulingColor);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetRulingColors(B_colors);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetRulingColor(H_RulingColor);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetRulingColors(B_colors);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetStripColor(Color.white);
                }
                break;
            case PaintMode.Intersection:
                {
                    List<Color> H_colors = boolsToColors(outputs.isH_RulingsIntersect, ErrorColor, Color.grey);
                    List<Color> B_colors = boolsToColors(outputs.isB_RulingsIntersect, ErrorColor, Color.grey);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetRulingColors(H_colors);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetRulingColors(B_colors);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetRulingColors(H_colors);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetRulingColors(B_colors);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetStripColor(Color.white);
                }
                break;
            case PaintMode.Flatness:
                {
                    List<float> H_evaluations = outputs.H_FlatnessError.Evaluate(ToleranceFlatness);
                    List<float> B_evaluations = outputs.B_FlatnessError.Evaluate(ToleranceFlatness);
                    List<Color> H_colors = FloatListInfo.EvaluationsToColors(H_evaluations);
                    List<Color> B_colors = FloatListInfo.EvaluationsToColors(B_evaluations);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetStripColors(H_colors);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetStripColors(B_colors);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetStripColors(H_colors);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetStripColors(B_colors);
                }
                break;
            case PaintMode.Developability:
                {
                    List<float> evaluations = outputs.DevelopabilityError.Evaluate(ToleranceDevelopability);
                    List<Color> colors = FloatListInfo.EvaluationsToColors(evaluations);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetStripColors(colors);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetStripColors(colors);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetStripColors(colors);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetStripColors(colors);
                }
                break;
            default:
                {
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetRulingColor(Color.gray);
                    creasePattern.surface2D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface2D.SubhandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface3D.HandleSurface.SurfMesh.SetStripColor(Color.white);
                    creasePattern.surface3D.SubhandleSurface.SurfMesh.SetStripColor(Color.white);
                }
                break;
        }
    }

    private static List<Color> boolsToColors(bool[] bools, Color tColor, Color fColor)
    {
        List<Color> colors = new List<Color>();
        foreach (bool b in bools)
        {
            Color c = b ? tColor : fColor;
            colors.Add(c);
        }
        return colors;
    }

    public void BakeMesh()
    {
        manager = new RulingVertexManager();
        var surf0 = creasePattern.surface3D.HandleSurface.SurfMesh;
        var surf1 = creasePattern.surface3D.SubhandleSurface.SurfMesh;
        int n = surf0.curvePositions.Count;

        manager.AddNewSurface();
        for (int i = 0; i < n; i++)
        {
            manager.AddRuling(surf0.curvePositions[i], surf0.rulingDirections[i], surf0.rulingLengths[i]);
        }
        manager.AddNewSurface();
        for (int i = 0; i < n; i++)
        {
            manager.AddRuling(surf1.curvePositions[i], surf1.rulingDirections[i], surf1.rulingLengths[i]);
        }
        List<int> triangles = manager.GetTriangles(new List<bool>() { false, true });

        outputs.FoldMesh = new Mesh();
        outputs.FoldMesh.SetVertices(manager.vertices);
        outputs.FoldMesh.SetTriangles(triangles, 0);
        outputs.FoldMesh.RecalculateNormals();

        if(optimizer != null)
            optimizer.InputMesh = outputs.FoldMesh;
    }

    [ContextMenu("SaveOptimizedMesh")]
    public void SaveOptimizedMesh()
    {
        Export.SVGDrawer drawer = new Export.SVGDrawer();
        Vector3[] vertices = optimizer.Result.vertices;

        List<Vector3> crease = new List<Vector3>();
        for (int i = 0; i < manager.RulingCount(0); i++)
        {
            if (manager.TryGetStartVertex(0, i, out int ci))
                crease.Add(vertices[ci]);
        }
        drawer.AddPolyline(crease);

        List<Vector3> creasePatternEdgeH = new List<Vector3>() { crease[0] };
        drawer.Stroke = H_RulingColor;
        for (int i = 0; i < manager.RulingCount(0); i++)
        {
            if(manager.TryGetStartVertex(0, i, out int ci) && manager.TryGetEndVertex(0, i, out int ri))
            {
                drawer.Addline(vertices[ci], vertices[ri]);
                creasePatternEdgeH.Add(vertices[ri]);
            }
        }
        creasePatternEdgeH.Add(crease[^1]);

        List<Vector3> creasePatternEdgeSH = new List<Vector3>() { crease[0] };
        drawer.Stroke = B_RulingColor;
        for (int i = 0; i < manager.RulingCount(1); i++)
        {
            if (manager.TryGetStartVertex(1, i, out int ci) && manager.TryGetEndVertex(1, i, out int ri))
            {
                drawer.Addline(vertices[ci], vertices[ri]);
                creasePatternEdgeSH.Add(vertices[ri]);
            }
        }
        creasePatternEdgeSH.Add(crease[^1]);

        drawer.Stroke = Color.gray;
        drawer.AddPolyline(creasePatternEdgeH);
        drawer.AddPolyline(creasePatternEdgeSH);

        var path = StandaloneFileBrowser.SaveFilePanel("Save SVG", "", "OptimizedMesh", "svg");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, drawer.Code);
        }
    }

    [ContextMenu("SaveGapCreasePattern")]
    public void SaveGapMesh()
    {
        Export.SVGDrawer drawer = new Export.SVGDrawer();

        drawer.Stroke = H_RulingColor;
        Vector3[] hRulings = creasePattern.surface2D.HandleSurface.SurfMesh.Ruling.vertices;
        for (int i = 1; i < hRulings.Length; i+=2)
        {
            drawer.Addline(hRulings[i - 1], hRulings[i]);
        }

        Vector3[] shRulings = creasePattern.surface2D.SubhandleSurface.SurfMesh.Ruling.vertices;
        for (int i = 1; i < shRulings.Length; i += 2)
        {
            if (outputs.isRulingsModified[i / 2])
            {
                drawer.Stroke = Modified_RulingColor;
            }
            else
            {
                drawer.Stroke = B_RulingColor;
            }
            drawer.Addline(shRulings[i - 1], shRulings[i]);
        }

        drawer.Stroke = Color.black;
        List<Vector3> hCrease = creasePattern.surface2D.HandleSurface.Curve.WorldDivided.Positions;
        drawer.AddPolyline(hCrease);
        List<Vector3> shCrease = creasePattern.surface2D.SubhandleSurface.Curve.WorldDivided.Positions;
        drawer.AddPolyline(shCrease);

        drawer.Stroke = Color.gray;
        if (creasePattern.UseEdge)
        {
            List<Vector3> creasePatternEdge = creasePattern.surface2D.edge.Paper.Vertices;
            drawer.AddPolyline(creasePatternEdge);
        }
        else
        {
            Vector3[] hOutline = creasePattern.surface2D.HandleSurface.SurfMesh.Outline.vertices;
            drawer.AddPolyline(new List<Vector3>(hOutline));
            Vector3[] shOutline = creasePattern.surface2D.SubhandleSurface.SurfMesh.Outline.vertices;
            drawer.AddPolyline(new List<Vector3>(shOutline));
        }

        var path = StandaloneFileBrowser.SaveFilePanel("Save SVG", "", "CreasePattern", "svg");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, drawer.Code);
        }
    }
}
