using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView
{
    public interface IBeforeRaycastHandler
    {
        public void OnBeforeRaycast(PointerEventData eventData);
    }

    public class BeforeRaycastEventTriggerRaycaster : PhysicsRaycaster
    {
        static public List<BeforeRaycastEventTriggerRaycaster> Raycasters = new List<BeforeRaycastEventTriggerRaycaster>();
        static public List<IBeforeRaycastHandler> Objects = new List<IBeforeRaycastHandler>();

        static public List<Camera> AllRaycasterCameras
        {
            get
            {
                List<Camera> cameras = new List<Camera>();
                foreach (BeforeRaycastEventTriggerRaycaster raycaster in Raycasters)
                    cameras.Add(raycaster.eventCamera);
                return cameras;
            }
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            RaycastResult result = new RaycastResult();
            result.module = this;
            eventData.pointerCurrentRaycast = result;
            foreach (var obj in Objects)
                obj.OnBeforeRaycast(eventData);
            base.Raycast(eventData, resultAppendList);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Raycasters.Contains(this))
            {
                Raycasters.Add(this);
            }

        }

        protected override void OnDisable()
        {
            if (Raycasters.Contains(this))
            {
                Raycasters.Remove(this);
            }
            base.OnDisable();
        }
    }

}
