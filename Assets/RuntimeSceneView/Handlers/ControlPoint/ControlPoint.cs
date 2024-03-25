using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView.Objects
{
    [RequireComponent(typeof(TransformChangedDetector), typeof(TransformHandle))]
    public class ControlPoint : MonoBehaviour, ITransformChangedReceiver, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        static class Loader
        {
            public static ControlPoint prefab;

            static Loader()
            {
                prefab = Resources.Load<ControlPoint>("Control Point");
            }
        }

        public static ControlPoint Instantiate(ControlPointManager manager, bool isActive = true)
        {
            ControlPoint cp = Instantiate(Loader.prefab, manager.transform);
            cp.manager = manager;
            cp.gameObject.SetActive(isActive);
            cp.GetComponent<TransformHandle>().Handle = manager.Handle;
            return cp;
        }

        [SerializeField]
        ControlPointManager manager;
        [SerializeField]
        ControlPointTrigger trigger;

        public void SetTriggerEnabled(bool value)
        {
            trigger.SetEnabled(value);
        }

        public void OnTransformChanged()
        {
            manager.UpdateControlPointToManagerPositions();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            manager.currentSelected = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            manager.currentSelected = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            manager.currentSelected = true;
            trigger.OnSelect();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            manager.currentSelected = false;
            trigger.OnDeselect();
        }
    }

}