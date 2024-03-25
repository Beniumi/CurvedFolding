using CurvedFoldingSystem.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem.Components.FileManager
{
    public static class Export
    {
        public class SVGDrawer
        {
            private string body = "";
            private List<Vector3> points = new List<Vector3>();

            public float StrokeWidth { get; set; } = 0.028f;

            public Color Stroke { get; set; } = Color.black;

            public string Code
            {
                get
                {
                    Bounds bounds = GetBounds();
                    string code = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>\r\n<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n";
                    code += "<svg version=\"1.1\" width=\"" + bounds.size.x + "cm\" height=\"" + bounds.size.z + "cm\" viewBox=\"" + bounds.min.x + " " + bounds.min.z + " " + bounds.size.x + " " + bounds.size.z + "\" overflow=\"visible\" xmlns=\"http://www.w3.org/2000/svg\">\n";
                    code += body + "</svg>";
                    return code;
                }
            }

            private Bounds GetBounds()
            {
                Bounds bounds;
                if (points.Count > 0)
                {
                    bounds = new Bounds(points[0], Vector3.zero);
                        for (int i = 1; i < points.Count; i++)
                    {
                        bounds.Encapsulate(points[i]);
                    }
                }
                else
                {
                    bounds = new Bounds();
                }
                return bounds;
            }

            private static string PointToCode(Vector3 p) => p.x + "," + p.z + " ";

            private string StrokeWidthCode => "stroke-width=\"" + 0.028 + "\" ";

            private string StyleCode => "style=\"stroke:#" + ColorUtility.ToHtmlStringRGB(Stroke) + "\" ";
            private string FillCode => "fill=\"none\" ";

            public void AddPolyline(List<Vector3> pts)
            {
                body += "<polyline points=\"";
                foreach (Vector3 p in pts)
                {
                    body += PointToCode(p);
                    points.Add(p);
                }
                body += "\" ";
                body += FillCode + StrokeWidthCode + StyleCode + "/>\n";
            }

            public void Addline(Vector3 p1, Vector3 p2)
            {
                points.Add(p1);
                points.Add(p2);
                body += "<line x1=\"" + p1.x + "\" y1=\"" + p1.z + "\" x2=\"" + p2.x + "\" y2=\"" + p2.z + "\" ";
                body += FillCode + StrokeWidthCode + StyleCode + "/>\n";
            }
        }

        private static string CurveToSVG(DividedCurve curve)
        {
            string svg = "<polyline points=\"";
            foreach (Vector3 pts in curve.Positions)
            {
                svg += pts.x + "," + pts.z + " ";
            }
            svg += "\" stroke=\"red\" fill=\"none\" stroke-width=\"0.028\" />\n";
            return svg;
        }

        private static string PaperToSVG(Paper paper)
        {
            string svg = "<polyline points=\"";
            foreach (Vector3 pts in paper.Vertices)
            {
                svg += pts.x + "," + pts.z + " ";
            }
            svg += "\" stroke=\"black\" fill=\"none\" stroke-width=\"0.028\" />\n";
            return svg;
        }

        public static string ToSVG(List<DividedCurve> curves, Paper paper)
        {
            string code = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>\r\n<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n";
            
            Bounds bounds = new Bounds();
            foreach (Vector3 pts in paper.Vertices)
                bounds.Encapsulate(pts);
            code += "<svg version=\"1.1\" width=\"" + bounds.size.x + "cm\" height=\"" + bounds.size.z + "cm\" viewBox=\"" + bounds.min.x + " " + bounds.min.z + " " + bounds.size.x + " " + bounds.size.z + "\" overflow=\"visible\" xmlns=\"http://www.w3.org/2000/svg\">\n";

            code += PaperToSVG(paper);
            foreach(DividedCurve curve in curves)
                code += CurveToSVG(curve);

            return code + "</svg>";
        }

        public static string ToOBJ(List<Mesh> meshes)
        {
            string vertices = "";
            string normals = "";
            string triangles = "";
            int vCount = 0;
            foreach(Mesh mesh in meshes)
            {
                foreach (Vector3 vm in mesh.vertices)
                {
                    float tocm = 0.01f;
                    Vector3 v = vm * tocm;
                    vertices += "v " + v.x.ToString("F8") + " " + v.y.ToString("F8") + " " + v.z.ToString("F8") + "\n";
                }
                foreach (Vector3 n in mesh.normals)
                    normals += "vn " + n.x.ToString("F8") + " " + n.y.ToString("F8") + " " + n.z.ToString("F8") + "\n";
                int tCount = 0;
                foreach (int f in mesh.triangles)
                {
                    if (tCount == 0)
                        triangles += "f";
                    triangles += " " + (vCount + f + 1) + "//" + (vCount + f + 1);
                    tCount++;
                    if (tCount == 3)
                    {
                        triangles += "\n";
                        tCount = 0;
                    }
                }
                vCount += mesh.vertices.Length;
            }
            string code = vertices + normals + triangles;
            return code;
        }
    }
}

