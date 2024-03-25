using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeSceneView.Objects
{
    public interface IControllPointHandler
    {
        public void OnControlPointChanged(List<Vector3> positions);
    }

    [RequireComponent(typeof(IControllPointHandler))]
    public class ControlPointManager : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField]
        bool useWorldSpace = false;
        [SerializeField]
        List<Vector3> positions = new List<Vector3>();
        [field: SerializeField]
        public TransformHandle.HandleType Handle;
        [SerializeField]
        LineDrawer line;

        public List<Transform> ControlPoints { get; } = new List<Transform>();

        public IControllPointHandler Handler { get; set; }

        public List<Vector3> Positions
        {
            get
            {
                return positions;
            }
            set
            {
                positions = value;
                if(Handler == null) Handler = GetComponent<IControllPointHandler>();
                Handler.OnControlPointChanged(value);
                if (Application.isPlaying)
                    UpdateMangerToControlPointPositions();
                if (line != null)
                    line.Positions = value;
            }
        }

        [HideInInspector]
        public bool currentSelected = false;
        private bool hasChanged = false;

        private void Awake()
        {
            SetEnabled(false);
            Handler = GetComponent<IControllPointHandler>();
            UpdateMangerToControlPointPositions();
        }

        private void OnValidate()
        {
            if(Handler != null)
                Handler.OnControlPointChanged(positions);
        }

        private void Update()
        {
            hasChanged = false;
        }

        public void UpdateControlPointToManagerPositions()
        {
            if (hasChanged)
                return;

            hasChanged = true;
            positions = new List<Vector3>();
            if (useWorldSpace)
            {
                foreach (Transform cp in ControlPoints)
                    positions.Add(cp.position);
            }
            else
            {
                foreach (Transform cp in ControlPoints)
                    positions.Add(cp.localPosition);
            }
            Handler.OnControlPointChanged(positions);
            if (line != null)
                line.Positions = positions;
        }

        private void UpdateMangerToControlPointPositions()
        {
            if (ControlPoints.Count != positions.Count)
                MatchQuantity(positions.Count);

            if (useWorldSpace)
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    Transform t = ControlPoints[i];
                    //if (t.hasChanged) continue;
                    t.position = positions[i];
                    t.hasChanged = false;
                }
            }
            else
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    Transform t = ControlPoints[i];
                    //if (t.hasChanged) continue;
                    t.localPosition = positions[i];
                    t.hasChanged = false;
                }
            }
        }

        private void MatchQuantity(int count)
        {
            if (ControlPoints.Count < count)
            {
                for (int i = ControlPoints.Count; i < count; i++)
                {
                    Transform cp = ControlPoint.Instantiate(this).transform;
                    ControlPoints.Add(cp);
                }
                SetEnabled(currentSelected);
            }
            else
            {
                for (int i = ControlPoints.Count - 1; count <= i; i--)
                {
                    Transform cp = ControlPoints[i];
                    ControlPoints.RemoveAt(i);
                    Destroy(cp.gameObject);
                }
            }
        }

        private void SetEnabled(bool value)
        {
            foreach (Transform cp in ControlPoints)
            {
                ControlPoint controlPoint = cp.GetComponent<ControlPoint>();
                controlPoint.SetTriggerEnabled(value);
            }
            if (line != null)
            {
                line.enabled = value;
                line.Positions = positions;
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetEnabled(true);
            currentSelected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            currentSelected = false;
            StartCoroutine(DelayDeselect());
        }

        private IEnumerator DelayDeselect()
        {
            do
            {
                yield return null;
            } while (currentSelected);
            SetEnabled(false);
        }
    }

}