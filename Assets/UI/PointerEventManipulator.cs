using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PointerEventManipulator : IManipulator
{
    private VisualElement _targetElement;
    public bool IsDragging {  get; private set; }

    public VisualElement target
    {
        get => _targetElement;
        set
        {
            if (value == null) throw new ArgumentNullException();
            else if (_targetElement == value) return;

            if (_targetElement != null) RemoveEvents(_targetElement);
            _targetElement = value;
            AddEvents(_targetElement);
        }
    }

    public EventCallback<PointerDownEvent> downAction = null;
    public EventCallback<PointerMoveEvent> dragAction = null;
    public EventCallback<PointerUpEvent> upAction = null;

    private void AddEvents(VisualElement targetElement)
    {
        targetElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        targetElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
        targetElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void RemoveEvents(VisualElement targetElement)
    {
        targetElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        targetElement.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        targetElement.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent ev)
    {
        _targetElement.CapturePointer(ev.pointerId); // �|�C���^���L���v�`������UI�͈͊O�̃|�C���^�C�x���g���擾����悤�ɂ���
        IsDragging = true;
        if (downAction != null)
            downAction(ev);
    }

    private void OnPointerUp(PointerUpEvent ev)
    {
        _targetElement.ReleasePointer(ev.pointerId); // �|�C���^���J��
        IsDragging = false;
        if (upAction != null)
            upAction(ev);
    }

    private void OnPointerMove(PointerMoveEvent ev)
    {
        if (IsDragging && dragAction != null)
            dragAction(ev);
    }
}
