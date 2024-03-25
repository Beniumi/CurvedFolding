using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView.Objects
{
    [RequireComponent(typeof(Collider))]
    public class ControlPointTrigger : BeforeRaycastEventTrigger
    {
        [SerializeField]
        Mesh mesh;
        [SerializeField]
        Material select;
        [SerializeField]
        Material unselect;

        Material drawMaterial;

        private void Awake()
        {
            raycastOrder = 30.0f;
        }

        private void Start()
        {
            drawMaterial = unselect;
            AddListener(DrawMesh);
        }

        public void DrawMesh(Camera camera)
        {
            Graphics.DrawMesh(mesh, transform.localToWorldMatrix, drawMaterial, gameObject.layer, camera);
        }

        public void OnSelect()
        {
            drawMaterial = select;
        }

        public void OnDeselect()
        {
            drawMaterial = unselect;
        }
    }

}