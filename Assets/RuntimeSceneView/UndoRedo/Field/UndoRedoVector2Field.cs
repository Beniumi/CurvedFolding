using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeSceneView.UndoRedoField
{
    public class UndoRedoVector2Field : Vector2Field, IUndoRedoField<Vector2>
    {
        public PointerEventManipulator[] Pointers { get; } = new PointerEventManipulator[2];

        public Vector2 PreEditValue { get; set; }

        public bool UndoRedoing { get; set; }

        public Action AddUndoHistory { get; set; }

        public UndoRedoVector2Field()
        {
            string[] fieldName = new string[2] { "unity-x-input", "unity-y-input" };
            Label[] labels = new Label[fieldName.Length];
            for (int i = 0; i < fieldName.Length; i++)
            {
                labels[i] = this.Q<FloatField>(fieldName[i]).labelElement;
            }
            this.SetupCompositeField(labels);
        }
    }
}
