using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeSceneView.UndoRedoField
{
    public class UndoRedoIntegerField :IntegerField, IUndoRedoField<int>
    {
        public PointerEventManipulator[] Pointers { get; } = new PointerEventManipulator[1];

        public int PreEditValue { get; set; }

        public bool UndoRedoing { get; set; }

        public Action AddUndoHistory { get; set; }

        public UndoRedoIntegerField()
        {
            this.SetupField();
        }
    }
}
