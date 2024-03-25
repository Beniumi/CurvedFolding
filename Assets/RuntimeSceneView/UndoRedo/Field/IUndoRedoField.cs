using System;
using System.Collections;
using System.Collections.Generic;

namespace RuntimeSceneView.UndoRedoField
{
    public interface IUndoRedoField<TFieldValue>
    {
        public PointerEventManipulator[] Pointers { get; }

        public TFieldValue PreEditValue { get; set; }

        public bool UndoRedoing { get; set; }

        public Action AddUndoHistory { get; set; }

    }
}
