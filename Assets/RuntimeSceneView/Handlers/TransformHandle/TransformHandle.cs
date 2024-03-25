using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView
{
    public class TransformHandle : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        public enum HandleType
        {
            Default,
            HandleXZ,
        }

        [field: SerializeField]
        public HandleType Handle { get; set; } = HandleType.Default;

        private TransformHandleTrigger handle;

        void Start()
        {
            switch (Handle)
            {
                case HandleType.HandleXZ:
                    handle = TransformHandleTrigger.InstantiateXZ(transform);
                    break;
                default:
                    handle = TransformHandleTrigger.Instantiate(transform);
                    break;
            }
        }

        void OnDestroy()
        {
            if (handle != null)
                Destroy(handle.gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            handle.SetEnabled(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            handle.SetEnabled(false);
        }
    }

}