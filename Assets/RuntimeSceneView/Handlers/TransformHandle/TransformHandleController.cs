using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView
{
    [RequireComponent(typeof(MeshCollider))]
    public class TransformHandleController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler
    {
        public enum Type
        {
            Arrow,
            Plane
        }

        [SerializeField]
        Type type = Type.Arrow;
        [SerializeField]
        TransformHandleTrigger handle;
        [SerializeField]
        Material material;

        Mesh drawMesh;
        Material drawMaterial;

        Vector3 pressOffset = Vector3.zero;
        Vector3 positionOffset = Vector3.zero;

        Vector3 Dir => transform.rotation * Vector3.right;

        Vector3 PlaneNormal(Vector3 cameraFoward)
        {
            if (type == Type.Plane)
                return Dir;

            return Vector3.Normalize(Vector3.ProjectOnPlane(cameraFoward, Dir));
        }

        Vector3 PressPosition(PointerEventData eventData)
        {
            Camera cam = eventData.pressEventCamera;
            Ray ray = cam.ScreenPointToRay(eventData.position);
            Plane plane = new Plane(PlaneNormal(cam.transform.forward), handle.OwnerPosition);
            plane.Raycast(ray, out float distance);
            Vector3 point = ray.GetPoint(distance);
            return point;
        }

        void Start()
        {
            drawMesh = GetComponent<MeshCollider>().sharedMesh;
            drawMaterial = material;
            handle.AddListener(DrawMesh);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            drawMaterial = handle.Selected;
            pressOffset = PressPosition(eventData);
            positionOffset = handle.OwnerPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 v = PressPosition(eventData) - pressOffset;
            if (type == Type.Arrow)
                v = Vector3.Project(v, Dir);
            else
                v = Vector3.ProjectOnPlane(v, Dir);
            handle.Move(positionOffset + v);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            handle.ExitMove(positionOffset, handle.OwnerPosition);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            drawMaterial = material;
        }



        public void OnSelect()
        {
            drawMaterial = handle.Selected;
        }

        public void OnUnselect()
        {
            drawMaterial = handle.Unselected;
        }

        public void OnDeselect()
        {
            drawMaterial = material;
        }

        public void DrawMesh(Camera camera)
        {
            Graphics.DrawMesh(drawMesh, transform.localToWorldMatrix, drawMaterial, gameObject.layer, camera);
        }
    }

}