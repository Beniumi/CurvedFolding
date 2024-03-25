using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeSceneView.UndoRedoField
{
    public class UndoRedoFloatField : FloatField, IUndoRedoField<float>
    {
        public PointerEventManipulator[] Pointers { get; } = new PointerEventManipulator[1];

        public float PreEditValue { get; set; }

        public bool UndoRedoing { get; set; }

        public Action AddUndoHistory { get; set; }

        public UndoRedoFloatField()
        {
            this.SetupField();
        }
    }
}
