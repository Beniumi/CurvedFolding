using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeSceneView.UndoRedoField
{
    public class UndoRedoListView<TField, TFieldValue> : ListView, INotifyValueChanged<List<TFieldValue>> where TField : BaseField<TFieldValue>, IUndoRedoField<TFieldValue>, new()
    {
        public List<TFieldValue> value
        {
            get { return new List<TFieldValue>(itemsSource as List<TFieldValue>); }
            set
            {
                List<TFieldValue> previousValue = new List<TFieldValue>(itemsSource as List<TFieldValue>);
                SetValueWithoutNotify(value);
                using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(previousValue, this.value);
                changeEvent.target = this;
                SendEvent(changeEvent);
            }
        }

        public void SetValueWithoutNotify(List<TFieldValue> newValue)
        {
            itemsSource = newValue;
            //Rebuild();
        }

        private void SetValues(List<TFieldValue> newValue)
        {
            value = newValue;
        }

        public UndoRedoListView()
        {
            //Uxml Attributes
            showBorder = true;
            reorderable = true;
            horizontalScrollingEnabled = true;
            showFoldoutHeader = true;
            showAddRemoveFooter = true;
            //reorderMode = ListViewReorderMode.Animated; //Unity has bug.
            fixedItemHeight = 100f;
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            //remember binding callback references
            Dictionary<int, EventCallback<ChangeEvent<TFieldValue>>> bindingCallbacks = new();

            //Setup ListView
            itemsSource = new List<TFieldValue>();
            makeItem = () => new TField();

            bindItem = (e, i) =>
            {
                TField field = e as TField;
                field.label = "Element " + i;
                field.SetValueWithoutNotify((TFieldValue)itemsSource[i]);

                field.AddUndoHistory = () =>
                {
                    var newValue = value;
                    var previousValue = value;
                    previousValue[i] = field.PreEditValue;
                    Action<List<TFieldValue>> undoRedoValues = (v) =>
                    {
                        field.UndoRedoing = true;
                        value = v;
                    };
                    UndoRedoSystem.Current.AddUndoHistory(undoRedoValues, previousValue, newValue);
                };

                EventCallback<ChangeEvent<TFieldValue>> changed = (evt) =>
                {
                    var previousValue = value;
                    (itemsSource as List<TFieldValue>)[i] = evt.newValue;
                    using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(previousValue, value);
                    changeEvent.target = this;
                    SendEvent(changeEvent);
                };
                bindingCallbacks.Add(i, changed);
                field.RegisterValueChangedCallback(changed);
            };

            unbindItem = (e, i) =>
            {
                TField field = e as TField;
                field.UnregisterValueChangedCallback(bindingCallbacks[i]);
                bindingCallbacks.Remove(i);
            };

            itemsAdded += (enumerator) =>
            {
                enumerator.OrderByDescending(e => e);
                List<TFieldValue> previousValue = value;
                foreach (var i in enumerator)
                {
                    previousValue.RemoveAt(i);
                }
                UndoRedoSystem.Current.AddUndoHistory(SetValues, previousValue, value);
                using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(previousValue, value);
                changeEvent.target = this;
                SendEvent(changeEvent);
            };

            itemsRemoved += (enumerator) =>
            {
                enumerator.OrderByDescending(e => e);
                List<TFieldValue> newValue = value;
                foreach (var i in enumerator)
                {
                    newValue.RemoveAt(i);
                }
                UndoRedoSystem.Current.AddUndoHistory(SetValues, value, newValue);
                using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(value, newValue);
                changeEvent.target = this;
                SendEvent(changeEvent);
            };
            itemIndexChanged += (a, b) =>
            {
                List<TFieldValue> previousValue = value;
                previousValue[b] = value[a];
                previousValue[a] = value[b];
                UndoRedoSystem.Current.AddUndoHistory(SetValues, previousValue, value);
                using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(previousValue, value);
                changeEvent.target = this;
                SendEvent(changeEvent);
            };
        }
    }
}
