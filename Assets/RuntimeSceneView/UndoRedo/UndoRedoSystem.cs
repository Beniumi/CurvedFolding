using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RuntimeSceneView
{
    public interface IUndoRedoCommand
    {
        public void OnUndoRedo();
    }

    public class UndoRedoCommandBody : IUndoRedoCommand
    {
        private readonly Action action;

        public UndoRedoCommandBody(Action action)
        {
            this.action = action;
        }

        public void OnUndoRedo()
        {
            action.Invoke();
        }
    }

    public class UndoRedoCommandBody<T> : IUndoRedoCommand
    {
        private readonly Action<T> action;
        private readonly T arg;

        public UndoRedoCommandBody(Action<T> action, T arg)
        {
            this.action = action;
            this.arg = arg;
        }

        public void OnUndoRedo()
        {
            action.Invoke(arg);
        }
    }

    public class UndoRedoState
    {
        private IUndoRedoCommand undo;
        private IUndoRedoCommand redo;

        public UndoRedoState(IUndoRedoCommand undo, IUndoRedoCommand redo)
        {
            this.undo = undo;
            this.redo = redo;
        }

        public void Undo()
        {
            undo.OnUndoRedo();
        }

        public void Redo()
        {
            redo.OnUndoRedo();
        }
    }

    public class UndoRedoSystem : MonoBehaviour
    {

        public static UndoRedoSystem Current { get; private set; }

        [SerializeField]
        private int stackLimit = 20;

        private LimitedStack<UndoRedoState> undoStack;
        private LimitedStack<UndoRedoState> redoStack;

        private void Awake()
        {
            Current = this;
            undoStack = new LimitedStack<UndoRedoState>(stackLimit);
            redoStack = new LimitedStack<UndoRedoState>(stackLimit);
        }

        public void AddUndoHistory<T>(Action<T> action, T previousValue, T newValue)
        {
            UndoRedoCommandBody<T> undo = new(action, previousValue);
            UndoRedoCommandBody<T> redo = new(action, newValue);
            AddUndoHistory(new UndoRedoState(undo, redo));
        }

        public void AddUndoHistory(UndoRedoState state)
        {
            undoStack.Push(state);
            redoStack.Clear();
        }

        public void ClearHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        public bool CanUndo => undoStack.Count > 0;

        [ContextMenu("Undo")]
        public void Undo()
        {
            UndoRedoState current = undoStack.Pop();
            current.Undo();
            redoStack.Push(current);
        }

        public bool CanRedo => redoStack.Count > 0;

        [ContextMenu("Redo")]
        public void Redo()
        {
            UndoRedoState current = redoStack.Pop();
            current.Redo();
            undoStack.Push(current);
        }

        public UndoRedoState GetCurrentState()
        {
            if(undoStack.SafePeek(out UndoRedoState state))
                return state;
            else
                return null;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if(keyboard == null)
                return;

            ButtonControl ctrl = keyboard.ctrlKey;
            KeyControl z = keyboard.zKey;
            KeyControl y = keyboard.yKey;

            if (ctrl.isPressed)
            {
                if (z.wasPressedThisFrame && CanUndo) Undo();
                if (y.wasPressedThisFrame && CanRedo) Redo();
            }
        }


        [ContextMenu("Debug")]
        public void DebugStack()
        {
            Debug.Log("undo stack count: " + undoStack.Count + "\nredo stack count: " + redoStack.Count);
        }
    }

}