using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FieldListView<TField, TFieldValue> : ListView, INotifyValueChanged<List<TFieldValue>> where TField : BaseField<TFieldValue>, new()
{
    public List<TFieldValue> value
    {
        get { return itemsSource as List<TFieldValue>; }
        set
        {
            List<TFieldValue> previousValue = itemsSource as List<TFieldValue>;
            SetValueWithoutNotify(value);
            using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(previousValue, itemsSource as List<TFieldValue>);
            changeEvent.target = this;
            SendEvent(changeEvent);
        }
    }

    public void SetValueWithoutNotify(List<TFieldValue> newValue)
    {
        itemsSource = newValue;
    }

    public FieldListView()
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
            EventCallback<ChangeEvent<TFieldValue>> onChanged = (evt) =>
            {
                List<TFieldValue> previousValue = new List<TFieldValue>(value);
                value[i] = evt.newValue;
                using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(previousValue, value);
                changeEvent.target = this;
                SendEvent(changeEvent);
            };
            bindingCallbacks.Add(i, onChanged);
            field.RegisterValueChangedCallback(onChanged);
        };

        unbindItem = (e, i) =>
        {
            TField field = e as TField;
            field.UnregisterValueChangedCallback(bindingCallbacks[i]);
            bindingCallbacks.Remove(i);
        };

        itemsAdded += (enumerator) =>
        {
            List<TFieldValue> previousValue = new List<TFieldValue>(value);
            foreach (var i in enumerator)
            {
                previousValue.Remove(value[i]);
            }
            using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(previousValue, value);
            changeEvent.target = this;
            SendEvent(changeEvent);
        };

        itemsRemoved += (enumerator) =>
        {
            List<TFieldValue> newValue = new List<TFieldValue>(value);
            foreach (var i in enumerator)
            {
                newValue.Remove(value[i]);
            }
            using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(value, newValue);
            changeEvent.target = this;
            SendEvent(changeEvent);
        };
        itemIndexChanged += (a, b) =>
        {
            List<TFieldValue> previousValue = new List<TFieldValue>(value);
            previousValue[b] = value[a];
            previousValue[a] = value[b];
            using ChangeEvent<List<TFieldValue>> changeEvent = ChangeEvent<List<TFieldValue>>.GetPooled(previousValue, value);
            changeEvent.target = this;
            SendEvent(changeEvent);
        };
    }
}
