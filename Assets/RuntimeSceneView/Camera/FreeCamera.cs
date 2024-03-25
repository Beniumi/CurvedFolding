using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RuntimeSceneView.Cameras
{
    [RequireComponent(typeof(Camera))]
    public class FreeCamera : MonoBehaviour
    {
        private class InfiniteGrid
        {
            private class QuarterArc
            {
                private readonly float[] x;
                private readonly float[] y;

                public QuarterArc(int div_n)
                {
                    x = new float[div_n + 1];
                    y = new float[div_n + 1];
                    float dx = 1.0f / div_n;
                    for (int i = 0; i <= div_n; i++)
                    {
                        x[i] = dx * i;
                        y[i] = Mathf.Sqrt(1 - x[i] * x[i]);
                    }
                }

                public float GetY(float xPosition)
                {
                    int left = Mathf.FloorToInt(Mathf.Lerp(0, x.Length - 1, xPosition));
                    if (left == x.Length - 1) return x[left];
                    float t = Mathf.InverseLerp(x[left], x[left + 1], xPosition);
                    float yPosition = Mathf.Lerp(y[left], y[left + 1], t);
                    return yPosition;
                }
            }

            private static readonly int radiusFactor = 250;
            private static readonly int arcDivNum = 100;
            private static readonly QuarterArc arc;
            public static readonly Material material;

            static InfiniteGrid()
            {
                arc = new QuarterArc(arcDivNum);
                material = Resources.Load<Material>("GridMaterial");
            }

            private static int GetDigitCount(float f)
            {
                int intf = Mathf.FloorToInt(Mathf.Abs(f));
                int digitCount = (intf == 0) ? 1 : Mathf.FloorToInt(Mathf.Log10(intf)) + 1;
                return digitCount;
            }

            private static List<Vector3> GetParallelLines(Vector3 position, float squareSize, Vector3 horizontal, Vector3 vertical, out List<float> alpha)
            {
                float horizontalPosition = Vector3.Dot(horizontal, position);
                int toRound = Mathf.CeilToInt(horizontalPosition / squareSize);
                float offset = -horizontalPosition % squareSize;
                if (horizontalPosition > 0)
                    offset += squareSize;
                float rt = Mathf.InverseLerp(0, squareSize * 10, Mathf.Abs(position.y));
                float radius = Mathf.Lerp(1, squareSize * radiusFactor, rt);
                float ha = Mathf.Lerp(1.0f, 0, rt);

                List<Vector3> lines = new List<Vector3>();
                alpha = new List<float>();
                for (int i = -radiusFactor + 1; i < radiusFactor; i++)
                {
                    float x = offset + i * squareSize;
                    float xAbs = Mathf.Abs(x);
                    float y = arc.GetY(xAbs / radius) * radius;
                    Vector3 h = x * horizontal;
                    Vector3 v = y * vertical;
                    lines.Add(h + v);
                    lines.Add(h);
                    lines.Add(h - v);

                    float from = 1.0f;
                    if ((toRound + i) % 10 != 0)
                    {
                        from *= ha;
                    }
                    float at = Mathf.InverseLerp(0, radius, xAbs);
                    float a = Mathf.Lerp(from, 0f, at);

                    alpha.Add(0f);
                    alpha.Add(a);
                    alpha.Add(0f);
                }
                return lines;
            }

            private readonly Color color;
            public readonly Mesh mesh;
            public Vector3 Position { get; private set; }

            public InfiniteGrid(Color color)
            {
                this.color = color;
                mesh = new Mesh();
            }

            public void SetGridMesh(Vector3 cameraPosition)
            {
                float minSquareSize = Mathf.Pow(10, GetDigitCount(cameraPosition.y) - 1);

                List<Vector3> vertices = new List<Vector3>();
                List<Color> colors = new List<Color>();

                vertices.AddRange(GetParallelLines(cameraPosition, minSquareSize, Vector3.right, Vector3.forward, out List<float> xAlpha));
                foreach (float a in xAlpha)
                {
                    Color c = color;
                    c.a = a;
                    colors.Add(c);
                }

                vertices.AddRange(GetParallelLines(cameraPosition, minSquareSize, Vector3.forward, Vector3.right, out List<float> zAlpha));
                foreach (float a in zAlpha)
                {
                    Color c = color;
                    c.a = a;
                    colors.Add(c);
                }

                int lineCount = vertices.Count / 3;
                int[] indices = new int[lineCount * 4];
                for (int i = 0; i < lineCount; i++)
                {
                    int indexCount = 4 * i;
                    int vertexCount = 3 * i;
                    indices[indexCount] = vertexCount;
                    indices[indexCount + 1] = vertexCount + 1;
                    indices[indexCount + 2] = vertexCount + 1;
                    indices[indexCount + 3] = vertexCount + 2;
                }

                mesh.Clear(false);
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetIndices(indices, MeshTopology.Lines, 0);
                Position = new Vector3(cameraPosition.x, 0, cameraPosition.z);
            }
        }


        [SerializeField, Range(4f, 120f)]
        float fieldOfView = 60f;
        [field: SerializeField]
        public bool UseGizmo { get; set; } = false;
        [field: SerializeField]
        public bool UseGrid { get; set; } = false;
        [SerializeField]
        Color gridColor = new Color(0.37f, 0.37f, 0.37f);

        public Camera Camera { get; private set; }

        public Camera[] DependentCameras { get; private set; } = new Camera[0];

        public float FieldOfView
        {
            get { return fieldOfView; }
            set
            {
                fieldOfView = value;
                if (Camera == null)
                    return;


                Camera.fieldOfView = value;
                foreach (Camera cam in DependentCameras)
                    cam.fieldOfView = value;
                Gizmo.SetGizmoFieldOfView();
                SetOrthographicSize(Distance);
            }
        }

        private int zoomOutCount = 50;

        public float Distance
        {
            get
            {
                float distance = 0.1f;
                for (int i = 0; i < zoomOutCount; i++)
                    distance += 0.1f * distance;
                return distance;
            }
        }

        public Vector3 FocusPoint => transform.position + transform.forward * Distance;

        public CameraGizmo Gizmo { get; private set; }

        private InfiniteGrid grid;
        private bool pointerIsBusy;

        private void Awake()
        {
            Camera = GetComponent<Camera>();
            DependentCameras = GetComponentsInChildren<Camera>();
            foreach (Camera cam in DependentCameras)
                cam.rect = Camera.rect;
            SetOrthographicSize(Distance);

            Gizmo = CameraGizmo.Instantiate(this);
            Gizmo.gameObject.SetActive(UseGizmo);

            grid = new InfiniteGrid(gridColor);
            if (UseGrid)
                grid.SetGridMesh(transform.position);
        }

        private void OnValidate()
        {
            FieldOfView = fieldOfView;

            if (Gizmo != null)
                Gizmo.gameObject.SetActive(UseGizmo);

            if (Camera != null && UseGrid)
                grid.SetGridMesh(transform.position);
        }

        private void Update()
        {
            PointerThisFrame();
            if (UseGrid)
                Graphics.DrawMesh(grid.mesh, grid.Position, Quaternion.identity, InfiniteGrid.material, gameObject.layer);
        }

        private void SetOrthographicSize(float distance)
        {
            float size = new DollyZoom(fieldOfView).DollyZoomOrthographicSize(distance);
            Camera.orthographicSize = size;
            foreach (Camera cam in DependentCameras)
                cam.orthographicSize = size;
        }

        private void PointerThisFrame()
        {
            Mouse mouse = Mouse.current;
            bool wheels = mouse.scroll.ReadValue().y > 0;
            bool hasMouseInteraction = wheels | mouse.rightButton.wasPressedThisFrame | mouse.middleButton.wasPressedThisFrame;
            if (hasMouseInteraction)
                pointerIsBusy = EventSystem.current.IsPointerOverGameObject();
        }

        public void OnScrollWheel(InputValue value)
        {
            if (pointerIsBusy)
                return;

            float scroll = value.Get<Vector2>().y;
            if (scroll == 0)
                return;

            float oldDistance = Distance;
            zoomOutCount -= (int)Mathf.Clamp(scroll, -1.0f, 1.0f);
            if (zoomOutCount < 0)
                zoomOutCount = 0;
            if (zoomOutCount > 100)
                zoomOutCount = 100;
            float currentDistance = Distance;
            SetPosition(transform.position + transform.forward * (oldDistance - currentDistance));
            SetOrthographicSize(currentDistance);
        }

        public void OnRightDrag(InputValue value)
        {
            if (pointerIsBusy)
                return;

            Vector2 v = 0.2f * value.Get<Vector2>();
            Quaternion qx = Quaternion.AngleAxis(v.x, Vector3.up);
            Quaternion qy = Quaternion.AngleAxis(-v.y, Vector3.right);
            SetRotation(qx * transform.rotation * qy);
        }

        public void OnMiddleDrag(InputValue value)
        {
            if (pointerIsBusy)
                return;

            float distance = Distance;
            Vector3 delta = value.Get<Vector2>();
            Vector3 currentPoint = new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, distance);
            Vector3 worldPressPoint = Camera.ScreenToWorldPoint(currentPoint - delta);
            Vector3 worldCurrentPoint = Camera.ScreenToWorldPoint(currentPoint);
            Vector3 v = worldCurrentPoint - worldPressPoint;
            SetPosition(transform.position - v);
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            if (UseGrid)
                grid.SetGridMesh(position);
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
            if (UseGizmo)
                Gizmo.SetGizmoRotation();
        }
    }

}