using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeSceneView.UndoRedoField
{
    public static class UndoRedoFieldExtensions
    {
        public static void SetupField<T>(this IUndoRedoField<T> field)
        {
            BaseField<T> baseField = field as BaseField<T>;
            field.Pointers[0] = new PointerEventManipulator();
            field.Pointers[0].downAction = (ev) =>
            {
                field.PreEditValue = baseField.value;
            };
            field.Pointers[0].upAction = (ev) =>
            {
                if (EqualityComparer<T>.Default.Equals(field.PreEditValue, baseField.value))
                    return;

                field.AddUndoHistory();
            };
            baseField.labelElement.AddManipulator(field.Pointers[0]);

            baseField.RegisterValueChangedCallback((ev) =>
            {
                if (field.UndoRedoing)
                {
                    field.UndoRedoing = false;
                    return;
                }
                if (field.Pointers[0].IsDragging)
                    return;
                field.PreEditValue = ev.previousValue;
                field.AddUndoHistory();
            });
        }

        public static void SetupCompositeField<T>(this IUndoRedoField<T> field, Label[] labels)
        {
            BaseField<T> baseField = field as BaseField<T>;
            for (int i = 0; i < labels.Length; i++)
            {
                field.Pointers[i] = new PointerEventManipulator();
                field.Pointers[i].downAction = (ev) =>
                {
                    field.PreEditValue = baseField.value;
                };
                field.Pointers[i].upAction = (ev) =>
                {
                    if (EqualityComparer<T>.Default.Equals(field.PreEditValue, baseField.value))
                        return;

                    field.AddUndoHistory();
                };
                labels[i].AddManipulator(field.Pointers[i]);
            }
            baseField.RegisterValueChangedCallback((ev) =>
            {
                if (field.UndoRedoing)
                {
                    field.UndoRedoing = false;
                    return;
                }
                foreach (PointerEventManipulator pointer in field.Pointers)
                {
                    if (pointer.IsDragging)
                        return;
                }
                field.PreEditValue = ev.previousValue;
                field.AddUndoHistory();
            });
        }

        public static void SetupSliderField<T>(this IUndoRedoField<T> field) where T : IComparable<T>
        {
            BaseSlider<T> slider = field as BaseSlider<T>;
            slider.showInputField = true;

            field.Pointers[0] = new PointerEventManipulator();
            field.Pointers[0].downAction = (ev) =>
            {
                field.PreEditValue = slider.value;
            };
            field.Pointers[0].upAction = (ev) =>
            {
                if (EqualityComparer<T>.Default.Equals(field.PreEditValue, slider.value))
                    return;

                field.AddUndoHistory();
            };
            slider.labelElement.AddManipulator(field.Pointers[0]);

            VisualElement drag = slider.Q<VisualElement>("unity-drag-container");
            bool isSliderDragging = false;
            drag.RegisterCallback((PointerDownEvent ev) =>
            {
                drag.CapturePointer(ev.pointerId);
                isSliderDragging = true;
                field.PreEditValue = slider.value;
            }, TrickleDown.TrickleDown);

            drag.RegisterCallback((PointerUpEvent ev) =>
            {
                drag.ReleasePointer(ev.pointerId);
                isSliderDragging = false;
                if (EqualityComparer<T>.Default.Equals(field.PreEditValue, slider.value))
                    return;

                field.AddUndoHistory();
            });



            slider.RegisterValueChangedCallback((ev) =>
            {
                if (field.UndoRedoing)
                {
                    field.UndoRedoing = false;
                    return;
                }
                if (field.Pointers[0].IsDragging || isSliderDragging)
                    return;
                field.PreEditValue = ev.previousValue;
                field.AddUndoHistory();
            });
        }

        public static void SetOnUndoRedoInvokeOnValueChanged<T>(this IUndoRedoField<T> field)
        {
            BaseField<T> baseField = field as BaseField<T>;
            Action<T> onUndoRedo = (value) =>
            {
                field.UndoRedoing = true;
                using ChangeEvent<T> changeEvent = ChangeEvent<T>.GetPooled(baseField.value, value);
                changeEvent.target = baseField;
                baseField.SendEvent(changeEvent);
            };
            field.AddUndoHistory = () =>
            {
                UndoRedoSystem.Current.AddUndoHistory(onUndoRedo, field.PreEditValue, baseField.value);
            };
        }

    }
}

