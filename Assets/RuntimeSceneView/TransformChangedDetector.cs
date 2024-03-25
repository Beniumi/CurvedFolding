using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeSceneView
{
    public interface ITransformChangedReceiver
    {
        void OnTransformChanged();
    }

    [ExecuteAlways]
    public class TransformChangedDetector : MonoBehaviour
    {
        List<ITransformChangedReceiver> receivers = new List<ITransformChangedReceiver>();


        void OnEnable()
        {
            receivers = new List<ITransformChangedReceiver>(GetComponents<ITransformChangedReceiver>());
            transform.hasChanged = false;
        }

        void Update()
        {
            if (transform.hasChanged)
            {
                foreach (ITransformChangedReceiver receiver in receivers)
                    receiver.OnTransformChanged();
                transform.hasChanged = false;
            }
        }
    }

}