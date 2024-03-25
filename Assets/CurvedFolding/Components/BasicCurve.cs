using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RuntimeSceneView;

namespace CurvedFoldingSystem.Components
{
    public class BasicCurve : MonoBehaviour, ITransformChangedReceiver
    {
        [SerializeField]
        LineDrawer line;
        [SerializeField]
        UnityEvent<BasicCurve> onChanged;

        private DividedCurve divided = new DividedCurve();

        public DividedCurve Divided
        {
            get { return divided; }
            set
            {
                divided = value;
                worldDivided = transform.localToWorldMatrix * divided;
                if (line != null)
                {
                    line.Positions = divided.Positions;
                }
                onChanged?.Invoke(this);
            }
        }

        private DividedCurve worldDivided = new DividedCurve();

        public DividedCurve WorldDivided
        {
            get { return worldDivided; }
            set
            {
                worldDivided = value;
                divided = transform.worldToLocalMatrix * worldDivided;
                if (line != null)
                {
                    line.Positions = divided.Positions;
                }
                onChanged?.Invoke(this);
            }
        }

        public void OnTransformChanged()
        {
            if (Divided != null)
                worldDivided = transform.localToWorldMatrix * Divided;
            onChanged?.Invoke(this);
        }

        public void SetCurve(BasicCurve curve)
        {
            Divided = curve.Divided;
        }

    }
}
