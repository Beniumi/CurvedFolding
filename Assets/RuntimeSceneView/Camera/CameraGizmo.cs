using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace RuntimeSceneView.Cameras
{
    [RequireComponent(typeof(Camera))]
    public class CameraGizmo : MonoBehaviour
    {
        static class Loader
        {
            public static CameraGizmo prefab;

            static Loader()
            {
                prefab = Resources.Load<CameraGizmo>("Camera Gizmo");
            }
        }

        static int gizmoCount = 0;

        public static CameraGizmo Instantiate(FreeCamera target)
        {
            CameraGizmo gizmo = Instantiate(Loader.prefab);
            gizmo.gameObject.name = target.gameObject.name + " Gizmo";
            gizmo.target = target;
            //gizmo.gameObject.hideFlags = HideFlags.HideInHierarchy;
            gizmo.transform.position = new Vector3(gizmoCount * 10f, 0, 0);
            gizmoCount++;
            return gizmo;
        }

        static readonly float defaultFieldOfView = 53.1301f;

        [SerializeField]
        FreeCamera target;
        [SerializeField]
        Transform controllersParent;
        [SerializeField]
        TextMeshPro textMesh;

        new Camera camera;

        [HideInInspector]
        public bool isLocked = false;

        private void Awake()
        {
            camera = GetComponent<Camera>();
        }

        private void Start()
        {
            camera.orthographic = target.Camera.orthographic;
            SetGizmoFieldOfView();
            SetGizmoPosition();
            SetGizmoRotation();
        }

        public void SetGizmoFieldOfView()
        {
            float distance = controllersParent.localPosition.z;
            float fieldOfView = target.Camera.fieldOfView;
            camera.orthographic = target.Camera.orthographic;
            camera.orthographicSize = new DollyZoom(defaultFieldOfView).DollyZoomOrthographicSize(distance);
            camera.ResetWorldToCameraMatrix();

            if (camera.orthographic)
            {
                textMesh.text = "= Iso";
                float height = camera.orthographicSize;
                float width = height * camera.aspect;
                camera.projectionMatrix = Matrix4x4.Ortho(-width, width, -height, height, camera.nearClipPlane, camera.farClipPlane);
            }
            else
            {
                textMesh.text = "< Persp";
                camera.worldToCameraMatrix = DollyZoomWorldToCameraMatrix(camera.worldToCameraMatrix, distance, defaultFieldOfView, fieldOfView);
                camera.projectionMatrix = DollyZoomProjectionMatrix(camera, fieldOfView);
            }

            camera.fieldOfView = fieldOfView;
        }

        public void SetGizmoPosition()
        {
            Vector2 min = target.Camera.rect.max - camera.rect.size;
            Rect gizmoRect = new Rect(min.x, min.y, camera.rect.width, camera.rect.height);
            camera.rect = gizmoRect;
            /*
            Vector2 pixelSize = Vector2.one * 200;
            Vector2 pixelMin = target.Camera.pixelRect.max - pixelSize;
            camera.pixelRect = new Rect(pixelMin, pixelSize);
            */
        }

        public void SetGizmoRotation()
        {
            controllersParent.rotation = Quaternion.Inverse(target.transform.rotation);
        }

        public bool HaveSameOrientation(Transform cone)
        {
            float dot = Vector3.Dot(target.transform.forward, cone.localRotation * Vector3.forward);
            return dot > 0.9f;
        }

        public IEnumerator RotateCamera(Quaternion to)
        {
            if (isLocked)
                yield break;
            isLocked = true;

            Vector3 center = target.FocusPoint;
            Vector3 dir = Vector3.forward * target.Distance;
            Quaternion from = target.transform.rotation;

            float changeTime = 0.3f;
            float dt = 1.0f / changeTime * Time.deltaTime;
            float t = dt;
            while (t < 1.0f)
            {
                Quaternion q = Quaternion.Slerp(from, to, t);
                target.SetRotation(q);
                target.SetPosition(center - q * dir);

                t += dt;
                yield return null;
                dt = 1.0f / changeTime * Time.deltaTime;
            }
            target.SetRotation(to);
            target.SetPosition(center - to * dir);

            isLocked = false;
        }

        public IEnumerator SmoothProjectionTransition()
        {
            if (isLocked)
                yield break;
            isLocked = true;

            bool toOrthographic = !camera.orthographic;
            float fromFOV, toFOV;
            if (toOrthographic)
            {
                fromFOV = target.Camera.fieldOfView;
                toFOV = 4f;
            }
            else
            {
                fromFOV = 4f;
                toFOV = target.Camera.fieldOfView;
            }


            camera.ResetWorldToCameraMatrix();
            Matrix4x4 gizmoOriginalWorldToCameraMatrix = camera.worldToCameraMatrix;
            Matrix4x4 targetOriginalWorldToCameraMatrix = target.Camera.worldToCameraMatrix;

            float gizmoDistance = controllersParent.localPosition.z;
            float targetDistance = target.Distance;
            List<Camera> targetCameras = new List<Camera>(target.DependentCameras);
            targetCameras.Insert(0, target.Camera);
            camera.fieldOfView = defaultFieldOfView;

            float changeTime = 0.3f;
            float dt = 1.0f / changeTime * Time.deltaTime;
            float t = dt;
            while (t < 1.0f)
            {
                float currentFOV = Mathf.SmoothStep(fromFOV, toFOV, t);

                Matrix4x4 gizmoWorldToCameraMatrix = DollyZoomWorldToCameraMatrix(gizmoOriginalWorldToCameraMatrix, gizmoDistance, camera.fieldOfView, currentFOV);
                Matrix4x4 targetWorldToCameraMatrix = DollyZoomWorldToCameraMatrix(targetOriginalWorldToCameraMatrix, targetDistance, target.Camera.fieldOfView, currentFOV);
                camera.worldToCameraMatrix = gizmoWorldToCameraMatrix;
                target.Camera.worldToCameraMatrix = targetWorldToCameraMatrix;
                foreach (Camera cam in targetCameras)
                    cam.worldToCameraMatrix = targetWorldToCameraMatrix;



                Matrix4x4 gizmoProjectionMatrix = DollyZoomProjectionMatrix(camera, currentFOV);
                Matrix4x4 targetProjectionMatrix = DollyZoomProjectionMatrix(target.Camera, currentFOV);
                camera.projectionMatrix = gizmoProjectionMatrix;
                target.Camera.projectionMatrix = targetProjectionMatrix;
                foreach (Camera cam in targetCameras)
                    cam.projectionMatrix = targetProjectionMatrix;

                t += dt;
                yield return null;
                dt = 1.0f / changeTime * Time.deltaTime;
            }

            foreach (Camera cam in targetCameras)
            {
                cam.orthographic = toOrthographic;
                cam.ResetWorldToCameraMatrix();
                cam.ResetProjectionMatrix();
            }
            SetGizmoFieldOfView();

            isLocked = false;
        }

        static Matrix4x4 DollyZoomWorldToCameraMatrix(Matrix4x4 original, float distance, float fromFoV, float toFoV)
        {
            float z = distance - new DollyZoom(fromFoV).DollyZoomPerspectiveDistance(distance, toFoV);
            return Matrix4x4.Translate(new Vector3(0, 0, z)) * original;
        }

        static Matrix4x4 DollyZoomProjectionMatrix(Camera camera, float fieldOfView)
        {
            return Matrix4x4.Perspective(fieldOfView, camera.aspect, camera.nearClipPlane, 5000000.0f);
        }
    }

}
