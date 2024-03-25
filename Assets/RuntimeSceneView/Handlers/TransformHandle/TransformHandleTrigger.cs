using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeSceneView
{
    public class TransformHandleTrigger : BeforeRaycastEventTrigger
    {
        static class Loader
        {
            public static TransformHandleTrigger prefab;
            public static TransformHandleTrigger prefabXZ;

            static Loader()
            {
                prefab = Resources.Load<TransformHandleTrigger>("Transform Handle");
                prefabXZ = Resources.Load<TransformHandleTrigger>("Transform Handle XZ");
            }
        }

        public static TransformHandleTrigger Instantiate(Transform owner)
        {
            TransformHandleTrigger handle = Instantiate(Loader.prefab, owner);
            handle.gameObject.name = owner.gameObject.name + " Handle";
            handle.gameObject.SetActive(false);
            return handle;
        }

        public static TransformHandleTrigger InstantiateXZ(Transform owner)
        {
            TransformHandleTrigger handle = Instantiate(Loader.prefabXZ, owner);
            handle.gameObject.name = owner.gameObject.name + " Handle";
            handle.gameObject.SetActive(false);
            return handle;
        }

        [SerializeField]
        Material selected;
        [SerializeField]
        Material unselected;

        public Material Selected => selected;

        public Material Unselected => unselected;

        public Vector3 OwnerPosition => transform.parent.position;

        private void Awake()
        {
            raycastOrder = 20.0f;
        }

        protected override Quaternion BeforeRaycastRotation(Camera camera)
        {
            return Quaternion.identity;
        }

        public void Move(Vector3 pos)
        {
            transform.parent.position = pos;
        }

        public void ExitMove(Vector3 startPosition, Vector3 endPosition)
        {
            if (UndoRedoSystem.Current == null)
                return;

            UndoRedoCommandBody<Vector3> undo = new UndoRedoCommandBody<Vector3>(Move, startPosition);
            UndoRedoCommandBody<Vector3> redo = new UndoRedoCommandBody<Vector3>(Move, endPosition);
            UndoRedoSystem.Current.AddUndoHistory(new UndoRedoState(undo, redo));
        }
    }

}