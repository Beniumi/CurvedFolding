using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace RuntimeSceneView
{

    public class BeforeRaycastEventTrigger : MonoBehaviour, IBeforeRaycastHandler
    {
        protected static readonly float ReferenceDistanceZ = 30.0f;
        protected static readonly float ReferenceOrthographicSize = 15.0f;

        protected UnityEvent<Camera> onBeforeRaycast = new UnityEvent<Camera>();
        protected float raycastOrder = 10.0f;

        public void AddListener(UnityAction<Camera> action)
        {
            onBeforeRaycast.AddListener(action);
        }

        public void SetEnabled(bool value)
        {
            gameObject.SetActive(value);
        }

        public virtual void OnBeforeRaycast(PointerEventData eventData)
        {
            Camera camera = eventData.enterEventCamera;
            transform.position = BeforeRaycastPosition(camera);
            transform.rotation = BeforeRaycastRotation(camera);
            transform.localScale = BeforeRaycastScale(camera);
            onBeforeRaycast.Invoke(camera);
        }

        protected virtual Vector3 BeforeRaycastPosition(Camera camera)
        {
            //TODO
            Vector3 anchorPoint = transform.parent != null ? transform.parent.position : camera.transform.position;

            return ConstantSizePosition(anchorPoint, raycastOrder, camera);
        }

        protected static Vector3 ConstantSizePosition(Vector3 worldPosition, float order, Camera camera)
        {
            float z = order;
            if (camera.orthographic)
            {
                float factor = camera.orthographicSize / ReferenceOrthographicSize;
                z *= factor;
            }
            Vector3 screenPos = camera.WorldToScreenPoint(worldPosition);
            return camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        }

        protected virtual Quaternion BeforeRaycastRotation(Camera camera)
        {
            return transform.rotation;
        }

        protected virtual Vector3 BeforeRaycastScale(Camera camera)
        {
            //TODO
            Vector3 scale = Vector3.one;
            if (transform.parent != null)
            {
                float parentScale = transform.parent.lossyScale.x;
                scale = parentScale == 0 ? Vector3.zero : Vector3.one * (1.0f / parentScale);
            }

            return ConstantSizeScale(scale, raycastOrder, camera);
        }

        protected static Vector3 ConstantSizeScale(Vector3 scale, float order, Camera camera)
        {
            float factor;
            if (camera.orthographic)
            {
                factor = camera.orthographicSize / ReferenceOrthographicSize;
            }
            else
            {
                factor = order / ReferenceDistanceZ;
            }
            return scale * factor;
        }

        private void OnEnable()
        {
            if (!BeforeRaycastEventTriggerRaycaster.Objects.Contains(this))
            {
                BeforeRaycastEventTriggerRaycaster.Objects.Add(this);
            }
        }

        private void OnDisable()
        {
            if (BeforeRaycastEventTriggerRaycaster.Objects.Contains(this))
            {
                BeforeRaycastEventTriggerRaycaster.Objects.Remove(this);
            }
        }
    }

}