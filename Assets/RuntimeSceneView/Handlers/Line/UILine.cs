using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView.Objects
{

    [RequireComponent(typeof(TransformChangedDetector)), ExecuteAlways]
    public class UILine : LineDrawer, ITransformChangedReceiver, ISelectHandler, IDeselectHandler
    {
        public static class BoldLine
        {
            private class BaseBoldLine
            {
                private class IntersectionVector
                {
                    public readonly Vector3 v;
                    public readonly bool addedNormal;

                    public IntersectionVector(Vector3 v, bool addedNormal)
                    {
                        this.v = v;
                        this.addedNormal = addedNormal;
                    }
                }

                private Vector3[] positionsAndDepths;
                private Vector3[] normals;
                private IntersectionVector[] toIntersections;

                public float width = 5.0f;
                public float outlineWidth = 2.0f;

                public BaseBoldLine(List<Vector3> positionsAndDepths) : this(positionsAndDepths.ToArray()) { }

                public BaseBoldLine(Vector3[] positionsAndDepths)
                {
                    this.positionsAndDepths = positionsAndDepths;
                    int n = positionsAndDepths.Length;

                    Vector2[] positions = new Vector2[n];
                    for (int i = 0; i < n; i++)
                    {
                        positions[i] = positionsAndDepths[i];
                    }

                    Vector2[] tangents = new Vector2[n];
                    Vector2[] normals = new Vector2[n];
                    this.normals = new Vector3[n];
                    for (int i = 0; i < n - 1; i++)
                    {
                        tangents[i] = (positions[i + 1] - positions[i]).normalized;
                        normals[i] = Vector2.Perpendicular(tangents[i]);
                        this.normals[i] = normals[i];
                    }

                    toIntersections = new IntersectionVector[n];
                    for (int i = 1; i < n - 1; i++)
                    {
                        if (IsIntersect(positions[i - 1] + normals[i - 1], tangents[i - 1], positions[i] + normals[i], tangents[i], out Vector2 cross))
                        {
                            toIntersections[i] = new(cross - positions[i], true);
                        }
                        else if (IsIntersect(positions[i - 1] - normals[i - 1], tangents[i - 1], positions[i] - normals[i], tangents[i], out cross))
                        {
                            toIntersections[i] = new(cross - positions[i], false);
                        }
                    }
                }

                public static bool IsIntersect(Vector2 p0, Vector2 v0, Vector2 p1, Vector2 v1, out Vector2 cross)
                {
                    float d = v0.x * -v1.y + v1.x * v0.y;
                    Vector2 p = p1 - p0;
                    float t0 = (-v1.y * p.x + v1.x * p.y) / d;
                    float t1 = (-v0.y * p.x + v0.x * p.y) / d;
                    cross = p0 + v0 * t0;
                    if (Mathf.Abs(d) < 1E-05f || t0 < 0f || t1 < 0f)
                        return false;
                    return true;
                }

                public void GetMeshProperties(List<Vector3> vertices, out List<int> triangles, out List<int> outlineTriangles)
                {
                    triangles = new();
                    outlineTriangles = new();

                    int n = positionsAndDepths.Length;
                    if (n < 2)
                        return;

                    normals[^1] = normals[^2];
                    float combinedOutlineWidth = width + outlineWidth;
                    vertices.AddRange(new List<Vector3>()
        {
            positionsAndDepths[0] + normals[0] * combinedOutlineWidth,
            positionsAndDepths[0] + normals[0] * width,
            positionsAndDepths[0] - normals[0] * width,
            positionsAndDepths[0] - normals[0] * combinedOutlineWidth
        });
                    for (int i = 1; i < n; i++)
                    {
                        if (toIntersections[i] == null)
                        {
                            Vector3 p0 = positionsAndDepths[i] + normals[i] * combinedOutlineWidth;
                            Vector3 p1 = positionsAndDepths[i] + normals[i] * width;
                            Vector3 p2 = positionsAndDepths[i] - normals[i] * width;
                            Vector3 p3 = positionsAndDepths[i] - normals[i] * combinedOutlineWidth;
                            int count = vertices.Count;
                            vertices.Add(p0);
                            vertices.Add(p1);
                            vertices.Add(p2);
                            vertices.Add(p3);
                            outlineTriangles.AddRange(RectangleToTriangle(count - 3, count - 4, count + 0, count + 1));
                            triangles.AddRange(RectangleToTriangle(count - 2, count - 3, count + 1, count + 2));
                            outlineTriangles.AddRange(RectangleToTriangle(count - 1, count - 2, count + 2, count + 3));
                        }
                        else if (toIntersections[i].addedNormal)
                        {
                            Vector3 p0 = positionsAndDepths[i] - normals[i - 1] * width;
                            Vector3 p1 = positionsAndDepths[i] - normals[i - 1] * combinedOutlineWidth;
                            Vector3 p2 = positionsAndDepths[i] + toIntersections[i].v * combinedOutlineWidth;
                            Vector3 p3 = positionsAndDepths[i] + toIntersections[i].v * width;
                            Vector3 p4 = positionsAndDepths[i] - normals[i] * width;
                            Vector3 p5 = positionsAndDepths[i] - normals[i] * combinedOutlineWidth;
                            int count = vertices.Count;
                            vertices.Add(p0);
                            vertices.Add(p1);
                            vertices.Add(p2);
                            vertices.Add(p3);
                            vertices.Add(p4);
                            vertices.Add(p5);
                            outlineTriangles.AddRange(RectangleToTriangle(count - 3, count - 4, count + 2, count + 3));
                            triangles.AddRange(RectangleToTriangle(count - 2, count - 3, count + 3, count + 0));
                            triangles.AddRange(new List<int>() { count + 0, count + 3, count + 4 });
                            outlineTriangles.AddRange(RectangleToTriangle(count - 1, count - 2, count + 0, count + 1));
                            outlineTriangles.AddRange(RectangleToTriangle(count + 1, count + 0, count + 4, count + 5));
                        }
                        else
                        {
                            Vector3 p0 = positionsAndDepths[i] + normals[i - 1] * combinedOutlineWidth;
                            Vector3 p1 = positionsAndDepths[i] + normals[i - 1] * width;
                            Vector3 p2 = positionsAndDepths[i] + normals[i] * combinedOutlineWidth;
                            Vector3 p3 = positionsAndDepths[i] + normals[i] * width;
                            Vector3 p4 = positionsAndDepths[i] + toIntersections[i].v * width;
                            Vector3 p5 = positionsAndDepths[i] + toIntersections[i].v * combinedOutlineWidth;
                            int count = vertices.Count;
                            vertices.Add(p0);
                            vertices.Add(p1);
                            vertices.Add(p2);
                            vertices.Add(p3);
                            vertices.Add(p4);
                            vertices.Add(p5);
                            outlineTriangles.AddRange(RectangleToTriangle(count - 3, count - 4, count + 0, count + 1));
                            outlineTriangles.AddRange(RectangleToTriangle(count + 1, count + 0, count + 2, count + 3));
                            triangles.AddRange(RectangleToTriangle(count - 2, count - 3, count + 1, count + 4));
                            triangles.AddRange(new List<int>() { count + 1, count + 3, count + 4 });
                            outlineTriangles.AddRange(RectangleToTriangle(count - 1, count - 2, count + 4, count + 5));
                        }
                    }
                }

                private static List<int> RectangleToTriangle(int p0, int p1, int p2, int p3)
                {
                    return new List<int>() { p0, p1, p2, p2, p3, p0 };
                }
            }

            public static void BakeMesh(Mesh mesh, Camera camera, List<Vector3> positions, float width = 5.0f, float outlineWidth = 2.0f)
            {
                mesh.Clear(false);
                mesh.subMeshCount = 2;

                int n = positions.Count;
                if (n < 2)
                    return;


                Vector3 fwd = camera.transform.forward;
                Vector3 cameraPosition = camera.transform.position;
                List<List<Vector3>> eachLineScreenPositions = new() { new() };
                for (int i = 0; i < n; i++)
                {
                    Vector3 toPosition = positions[i] - cameraPosition;
                    Quaternion q = Quaternion.FromToRotation(fwd, toPosition);
                    q.ToAngleAxis(out float angle, out Vector3 axis);

                    if (angle < 80f)
                    {
                        Vector3 screenPosition = camera.WorldToScreenPoint(positions[i]);
                        eachLineScreenPositions[^1].Add(screenPosition);
                    }
                    else
                    {
                        if (eachLineScreenPositions[^1].Count != 0)
                            eachLineScreenPositions.Add(new());
                    }
                }

                List<Vector3> vertices2D = new();
                List<int> triangles = new();
                List<int> outlineTriangles = new();
                for (int i = 0; i < eachLineScreenPositions.Count; i++)
                {
                    BaseBoldLine line = new BaseBoldLine(eachLineScreenPositions[i]);
                    line.width = width;
                    line.outlineWidth = outlineWidth;
                    line.GetMeshProperties(vertices2D, out List<int> t, out List<int> ot);
                    triangles.AddRange(t);
                    outlineTriangles.AddRange(ot);
                }

                Vector3[] vertices = new Vector3[vertices2D.Count];
                Vector3[] verticesNormals = new Vector3[vertices2D.Count];
                Vector3 normal = -camera.transform.forward;
                for (int i = 0; i < vertices2D.Count; i++)
                {
                    vertices[i] = camera.ScreenToWorldPoint(vertices2D[i]);
                    verticesNormals[i] = normal;
                }
                mesh.vertices = vertices;
                mesh.normals = verticesNormals;
                mesh.SetTriangles(triangles, 0);
                mesh.SetTriangles(outlineTriangles, 1);
            }
        }

        [SerializeField, Range(0.1f, 2.0f)]
        float width = 1.0f;
        [SerializeField]
        Color color = new Color(0.1f, 0.1f, 0.1f);
        [SerializeField]
        Color outline = new Color(1.0f, 0.3f, 0f);

        List<Vector3> worldPositions = new List<Vector3>();

        private bool hasOutline = false;
        private Material lineMaterial;
        private Material outlineMaterial;

        private void Start()
        {
            if (Application.isPlaying)
            {
                UILineTrigger.Instantiate(this);
                lineMaterial = new Material(material) { color = color };
                outlineMaterial = new Material(material) { color = outline };
            }
        }

        private void OnValidate()
        {
            if (lineMaterial != null)
                lineMaterial = new Material(material) { color = color };
            if (outlineMaterial != null)
                outlineMaterial = new Material(material) { color = outline };
            PrepareLine();
        }

        private void OnDrawGizmos()
        {
            BoldLine.BakeMesh(Mesh, Camera.current, worldPositions, width);
            if (Mesh.vertexCount <= 3)
                return;
            Gizmos.color = color;
            Gizmos.DrawMesh(Mesh, 0);
        }

        public void OnTransformChanged()
        {
            PrepareLine();
        }

        public override void PrepareLine()
        {
            worldPositions = new List<Vector3>();
            if (useWorldSpace)
            {
                worldPositions.AddRange(Positions);
            }
            else
            {
                foreach (Vector3 p in Positions)
                    worldPositions.Add(transform.localToWorldMatrix.MultiplyPoint3x4(p));
            }
        }

        public void Render(Camera camera)
        {
            BoldLine.BakeMesh(Mesh, camera, worldPositions, width);
            Graphics.DrawMesh(Mesh, Matrix4x4.identity, lineMaterial, gameObject.layer, camera, 0);
            if (hasOutline) Graphics.DrawMesh(Mesh, Matrix4x4.identity, outlineMaterial, gameObject.layer, camera, 1);
        }

        public void OnSelect(BaseEventData eventData)
        {
            hasOutline = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hasOutline = false;
        }
    }
}