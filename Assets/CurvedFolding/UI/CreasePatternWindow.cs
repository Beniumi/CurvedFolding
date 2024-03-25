using RuntimeSceneView;
using RuntimeSceneView.UndoRedoField;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CurvedFoldingSystem.Components.UIElements
{
    public class CreasePatternWindow : WindowUI
    {
        [SerializeField]
        CreasePatternEdge edge;
        [SerializeField]
        BasicCurve developed;

        private UndoRedoVector2Field position;
        private UndoRedoFloatField scale;
        private UndoRedoListView<UndoRedoVector2Field, Vector2> controlPoints;
        private UndoRedoVector2Field developedPosition;
        private UndoRedoFloatField developedRotation;
        private bool endAwake;

        protected override void Awake()
        {
            base.Awake();
            window.style.width = Length.Percent(40);
            window.style.height = Length.Percent(80);


            Label edgeLabel = new Label();
            edgeLabel.text = "Paper";
            content.Add(edgeLabel);

            position = new UndoRedoVector2Field();
            position.label = "Position";
            position.SetOnUndoRedoInvokeOnValueChanged();
            position.RegisterValueChangedCallback((ev) =>
            {
                edge.transform.localPosition = Vector2XYtoVector3XZ(ev.newValue);
            });
            content.Add(position);

            scale = new UndoRedoFloatField();
            scale.label = "Scale";
            scale.SetOnUndoRedoInvokeOnValueChanged();
            scale.RegisterValueChangedCallback((ev) =>
            {
                edge.transform.localScale = Vector3.one * ev.newValue;
            });
            content.Add(scale);

            controlPoints = new();
            controlPoints.headerTitle = "Vertices";
            controlPoints.RegisterValueChangedCallback((pts) =>
            {
                List<Vector3> vertices = new List<Vector3>();
                foreach(Vector2 v2 in pts.newValue)
                {
                    Vector3 v3 = Vector2XYtoVector3XZ(v2);
                    vertices.Add(v3);
                }
                edge.ControlPoints.Positions = vertices;
            });
            content.Add(controlPoints);

            Button SetA4 = new Button();
            SetA4.text = "Set A4 Paper";
            SetA4.clicked += () =>
            {
                List<Vector3> previousValue = edge.ControlPoints.Positions;
                List<Vector3> newValue = CreasePatternEdge.A4Vertices;
                edge.ControlPoints.Positions = newValue;
                Action<List<Vector3>> undoRedo = (v) =>
                {
                    edge.ControlPoints.Positions = v;
                };
                UndoRedoSystem.Current.AddUndoHistory(undoRedo, previousValue, newValue);
            };
            content.Add(SetA4);

            Label developedLabel = new Label();
            developedLabel.text = "Crease";
            content.Add(developedLabel);

            developedPosition = new UndoRedoVector2Field();
            developedPosition.label = "Position";
            developedPosition.SetOnUndoRedoInvokeOnValueChanged();
            developedPosition.RegisterValueChangedCallback((ev) =>
            {
                developed.transform.localPosition = Vector2XYtoVector3XZ(ev.newValue);
            });
            content.Add(developedPosition);

            developedRotation = new UndoRedoFloatField();
            developedRotation.label = "Rotation";
            developedRotation.SetOnUndoRedoInvokeOnValueChanged();
            developedRotation.RegisterValueChangedCallback((ev) =>
            {
                developed.transform.localRotation = Quaternion.AngleAxis(ev.newValue, Vector3.up);
            });
            content.Add(developedRotation);

            endAwake = true;
        }

        private Vector3 Vector2XYtoVector3XZ(Vector2 v2)
        {
            return new Vector3(v2.x, 0, v2.y);
        }

        private Vector2 Vector3XZtoVector2XY(Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }

        public void UpdateCreasePatternEdge()
        {
            if (!endAwake)
                return;

            position.SetValueWithoutNotify(Vector3XZtoVector2XY(edge.transform.localPosition));
            scale.SetValueWithoutNotify(edge.transform.localScale.x);
            List<Vector2> vertices = new List<Vector2>();
            foreach (Vector3 v3 in edge.ControlPoints.Positions)
            {
                Vector2 v2 = Vector3XZtoVector2XY(v3);
                vertices.Add(v2);
            }
            controlPoints.SetValueWithoutNotify(vertices);
        }

        public void UpdateDevelopedCrease()
        {
            if (!endAwake)
                return;

            developedPosition.SetValueWithoutNotify(Vector3XZtoVector2XY(developed.transform.localPosition));
            developedRotation.SetValueWithoutNotify(developed.transform.localRotation.eulerAngles.y);
        }
    }
}