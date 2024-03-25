using RuntimeSceneView.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem.Components.FileManager
{
    public interface IBehaviourData<T>
    {
        public void Load(T behaviour);
    }

    [Serializable]
    public class TransformData : IBehaviourData<Transform>
    {
        [SerializeField]
        Vector3 localPosition;
        [SerializeField]
        Quaternion localRotation;
        [SerializeField]
        Vector3 localScale;

        public TransformData(Transform transform)
        {
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
        }

        public void Load(Transform transform)
        {
            Transform[] childlen = transform.GetComponentsInChildren<Transform>();
            bool[] hasChanged = new bool[childlen.Length];
            for (int i = 0; i < childlen.Length; i++)
                hasChanged[i] = childlen[i].hasChanged;

            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;

            for (int i = 0; i < childlen.Length; i++)
                childlen[i].hasChanged = hasChanged[i];
        }
    }

    [Serializable]
    public class ControlPointManagerData : IBehaviourData<ControlPointManager>
    {
        [SerializeField]
        List<Vector3> positions;

        public ControlPointManagerData(ControlPointManager manager)
        {
            positions = manager.Positions;
        }

        public void Load(ControlPointManager manager)
        {
            manager.Positions = positions;
        }
    }

    [Serializable]
    public class CreaseCurveData : IBehaviourData<CreaseCurve>
    {
        [SerializeField]
        TransformData transform;
        [SerializeField]
        bool symmetrization;
        [SerializeField]
        int divisionCount;
        [SerializeField]
        ControlPointManagerData controlPoints;

        public CreaseCurveData(CreaseCurve curve)
        {
            transform = new TransformData(curve.transform);
            symmetrization = curve.Symmetrization;
            divisionCount = curve.DivisionCount;
            controlPoints = new ControlPointManagerData(curve.ControlPoints);
        }

        public void Load(CreaseCurve crease)
        {
            transform.Load(crease.transform);
            crease.Symmetrization = symmetrization;
            crease.DivisionCount = divisionCount;
            controlPoints.Load(crease.ControlPoints);
        }
    }

    [Serializable]
    public class CreasepatternEdgeData : IBehaviourData<CreasePatternEdge>
    {
        [SerializeField]
        TransformData transform;
        [SerializeField]
        ControlPointManagerData controlPoints;

        public CreasepatternEdgeData(CreasePatternEdge edge)
        {
            transform = new TransformData(edge.transform);
            controlPoints = new ControlPointManagerData(edge.ControlPoints);
        }

        public void Load(CreasePatternEdge edge)
        {
            transform.Load(edge.transform);
            controlPoints.Load(edge.ControlPoints);
        }
    }

    [Serializable]
    public class CurvedFoldingData : IBehaviourData<CurvedFolding>
    {
        [SerializeField]
        TransformData transform;
        [SerializeField]
        float toleranceDistance;
        [SerializeField]
        float toleranceFlatness;
        [SerializeField]
        float toleranceDevelopability;
        [SerializeField]
        CurvedFolding.PaintMode paint;

        public CurvedFoldingData(CurvedFolding crvf)
        {
            transform = new TransformData(crvf.transform);
            toleranceDistance = crvf.ToleranceDistance;
            toleranceFlatness = crvf.ToleranceFlatness;
            toleranceDevelopability = crvf.ToleranceDevelopability;
            paint = crvf.Paint;
        }

        public void Load(CurvedFolding crvf)
        {
            transform.Load(crvf.transform);
            crvf.ToleranceDistance = toleranceDistance;
            crvf.ToleranceFlatness = toleranceFlatness;
            crvf.ToleranceDevelopability = toleranceDevelopability;
            crvf.Paint = paint;
        }
    }

    [Serializable]
    public class SaveFileManagerData : IBehaviourData<SaveFileManager>
    {
        [SerializeField]
        CreaseCurveData crease;
        [SerializeField]
        TransformData handle;
        [SerializeField]
        TransformData developed;
        [SerializeField]
        CreasepatternEdgeData edge;
        [SerializeField]
        CurvedFoldingData crvf;

        public SaveFileManagerData(SaveFileManager manager)
        {
            crease = new CreaseCurveData(manager.Crease);
            handle = new TransformData(manager.Handle.transform);
            developed = new TransformData(manager.Developed.transform);
            edge = new CreasepatternEdgeData(manager.Edge);
            crvf = new CurvedFoldingData(manager.Folding);
        }

        public void Load(SaveFileManager manager)
        {
            crease.Load(manager.Crease);
            handle.Load(manager.Handle.transform);
            developed.Load(manager.Developed.transform);
            edge.Load(manager.Edge);
            crvf.Load(manager.Folding);
        }
    }
}
