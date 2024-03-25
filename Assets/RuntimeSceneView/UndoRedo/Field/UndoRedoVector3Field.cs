using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeSceneView.UndoRedoField
{
    public class UndoRedoVector3Field : Vector3Field, IUndoRedoField<Vector3>
    {
        public PointerEventManipulator[] Pointers { get; } = new PointerEventManipulator[3];

        public Vector3 PreEditValue { get; set; }

        public bool UndoRedoing { get; set; }

        public Action AddUndoHistory { get; set; }

        public UndoRedoVector3Field()
        {
            string[] fieldName = new string[3] { "unity-x-input", "unity-y-input", "unity-z-input" };
            Label[] labels = new Label[fieldName.Length];
            for(int i = 0; i < fieldName.Length; i++)
            {
                labels[i] = this.Q<FloatField>(fieldName[i]).labelElement;
            }
            this.SetupCompositeField(labels);
        }
    }
}
