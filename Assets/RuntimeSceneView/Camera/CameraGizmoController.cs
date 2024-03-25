using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView.Cameras
{
    [RequireComponent(typeof(MeshCollider))]
    public class CameraGizmoController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public enum Type
        {
            Rotate,
            Projection
        }

        [SerializeField]
        CameraGizmo gizmo;
        [SerializeField]
        Type type;
        [SerializeField]
        Material material;
        [SerializeField]
        Color color;

        MeshCollider meshCollider;
        Material drawMaterial;

        bool Visible => meshCollider.enabled;

        void Awake()
        {
            meshCollider = GetComponent<MeshCollider>();
            drawMaterial = new Material(material) { color = color };
        }

        void OnValidate()
        {
            if (drawMaterial != null)
                drawMaterial.color = color;
        }

        void Update()
        {
            if (type == Type.Rotate)
                SetVisible(!gizmo.HaveSameOrientation(transform));

            Graphics.DrawMesh(meshCollider.sharedMesh, transform.localToWorldMatrix, drawMaterial, gameObject.layer);
        }

        public void SetVisible(bool value)
        {
            if (Visible == value)
                return;

            meshCollider.enabled = value;
            if (value) StartCoroutine(ChangeGizmoAlpha(0f, 1f));
            else StartCoroutine(ChangeGizmoAlpha(1f, 0f));
        }

        IEnumerator ChangeGizmoAlpha(float from, float to)
        {
            Color color = this.color;
            color.a = from;

            float changeTime = 0.5f;
            float dt = 1.0f / changeTime * Time.deltaTime;
            float t = dt;
            while (t < 1.0f)
            {
                color.a = Mathf.SmoothStep(from, to, t);
                drawMaterial.color = color;

                t += dt;
                yield return null;
                dt = 1.0f / changeTime * Time.deltaTime;
            }

            color.a = to;
            drawMaterial.color = color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            drawMaterial.color = new Color(1.0f, 1.0f, 0.5f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            drawMaterial.color = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (type)
            {
                case Type.Rotate:
                    StartCoroutine(gizmo.RotateCamera(transform.localRotation));
                    break;
                case Type.Projection:
                    StartCoroutine(gizmo.SmoothProjectionTransition());
                    break;
            }
        }

    }

}