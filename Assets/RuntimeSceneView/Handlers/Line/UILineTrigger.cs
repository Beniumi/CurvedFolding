using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView.Objects
{
    [RequireComponent(typeof(MeshCollider))]
    public class UILineTrigger : BeforeRaycastEventTrigger
    {
        static class Loader
        {
            public static UILineTrigger prefab;

            static Loader()
            {
                prefab = Resources.Load<UILineTrigger>("Bold Line");
            }
        }

        public static UILineTrigger Instantiate(UILine drawer)
        {
            UILineTrigger trigger = Instantiate(Loader.prefab, drawer.transform);
            trigger.drawer = drawer;
            trigger.gameObject.layer = drawer.gameObject.layer;
            return trigger;
        }

        private UILine drawer;
        private MeshCollider meshCollider;

        private void Awake()
        {
            meshCollider = gameObject.GetComponent<MeshCollider>();
        }

        public override void OnBeforeRaycast(PointerEventData eventData)
        {
            Camera camera = eventData.enterEventCamera;

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            float parentScale = drawer.transform.lossyScale.x;
            transform.localScale = parentScale == 0 ? Vector3.zero : Vector3.one * (1.0f / parentScale);
            drawer.Render(camera);
            if(drawer.Mesh.vertexCount > 0)
                meshCollider.sharedMesh = drawer.Mesh;
            else 
                meshCollider.sharedMesh = null;
        }
    }

}