using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RuntimeSceneView.UndoRedoField;

namespace CurvedFoldingSystem.Components.UIElements
{
    public class CreaseCurveWindow : WindowUI
    {
        [SerializeField]
        CreaseCurve crease;
        [SerializeField]
        ScalableCurve handle;

        private UndoRedoVector3Field creasePosition;
        private UndoRedoVector3Field creaseRotation;
        private UndoRedoIntegerField divisionCount;
        private UndoRedoListView<UndoRedoVector3Field, Vector3> controlPoints;

        private UndoRedoVector3Field handlePosition;
        private UndoRedoFloatSliderField handleScale;

        private bool endAwake;


        protected override void Awake()
        {
            base.Awake();
            window.style.width = Length.Percent(40);
            window.style.height = Length.Percent(80);

            /*
            Label creaseLabel = new Label();
            creaseLabel.text = "Crease Curve";
            content.Add(creaseLabel);
            */

            creasePosition = new UndoRedoVector3Field();
            creasePosition.label = "Position";
            creasePosition.SetOnUndoRedoInvokeOnValueChanged();
            creasePosition.RegisterValueChangedCallback((ev) =>
            {
                crease.transform.localPosition = ev.newValue;
            });
            content.Add(creasePosition);

            creaseRotation = new UndoRedoVector3Field();
            creaseRotation.label = "Rotation";
            creaseRotation.SetOnUndoRedoInvokeOnValueChanged();
            creaseRotation.RegisterValueChangedCallback((ev) =>
            {
                crease.transform.localRotation = Quaternion.Euler(ev.newValue);
            });
            content.Add(creaseRotation);

            divisionCount = new UndoRedoIntegerField();
            divisionCount.label = "Division Count";
            divisionCount.SetOnUndoRedoInvokeOnValueChanged();
            divisionCount.RegisterValueChangedCallback((ev) =>
            {
                crease.DivisionCount = ev.newValue;
                crease.UpdateCurve();
            });
            content.Add(divisionCount);

            controlPoints = new();
            controlPoints.headerTitle = "Control Points";
            controlPoints.RegisterValueChangedCallback((pts) =>
            {
                crease.ControlPoints.Positions = pts.newValue;
            });
            content.Add(controlPoints);


            Label handleLabel = new Label();
            handleLabel.text = "Handle Curve";
            content.Add(handleLabel);

            handlePosition = new UndoRedoVector3Field();
            handlePosition.label = "Local Position";
            handlePosition.SetOnUndoRedoInvokeOnValueChanged();
            handlePosition.RegisterValueChangedCallback((ev) =>
            {
                handle.transform.localPosition = ev.newValue;
            });
            content.Add(handlePosition);

            handleScale = new UndoRedoFloatSliderField();
            handleScale.lowValue = 0.01f;
            handleScale.highValue = 1.99f;
            handleScale.label = "Local Scale";
            handleScale.SetOnUndoRedoInvokeOnValueChanged();
            handleScale.RegisterValueChangedCallback((ev) =>
            {
                handle.transform.localScale = Vector3.one * ev.newValue;
            });
            content.Add(handleScale);

            endAwake = true;
        }

        public void UpdateCreaseCurve()
        {
            if (!endAwake)
                return;
            creasePosition.SetValueWithoutNotify(crease.transform.localPosition);
            creaseRotation.SetValueWithoutNotify(crease.transform.localRotation.eulerAngles);
            divisionCount.SetValueWithoutNotify(crease.DivisionCount);
            controlPoints.SetValueWithoutNotify(crease.ControlPoints.Positions);
        }

        public void UpdateHandleCurve()
        {
            if (!endAwake)
                return;
            handlePosition.SetValueWithoutNotify(handle.transform.localPosition);
            handleScale.SetValueWithoutNotify(handle.transform.localScale.x);
        }
    }
}
