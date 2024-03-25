using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WindowUI : MonoBehaviour
{
    [SerializeField]
    UIDocument rootDocument;
    [SerializeField]
    VisualTreeAsset uxml;

    protected VisualElement window;
    protected VisualElement content;

    protected virtual void Awake()
    {
        window = uxml.Instantiate().Q<VisualElement>("Window");
        rootDocument.rootVisualElement.Add(window);

        window.RegisterCallback<PointerDownEvent>(OnPointerDown);
        content = window.Q<ScrollView>("Content");
        Label name = window.Q<Label>("Name");
        name.text = this.name;
        Button close = window.Q<Button>("Close");
        close.clicked += () =>
        {
            window.style.display = DisplayStyle.None;
        };
        VisualElement titleBar = window.Q<VisualElement>("TitleBar");
        PointerEventManipulator manipulator = new PointerEventManipulator();
        manipulator.downAction = ToFront;
        manipulator.dragAction = DragWindow;
        titleBar.AddManipulator(manipulator);
        window.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        window.style.display = DisplayStyle.Flex;
        window.BringToFront();
        window.transform.position = rootDocument.transform.position;
    }

    private void OnPointerDown(PointerDownEvent ev)
    {
        window.BringToFront();
    }

    private void DragWindow(PointerMoveEvent ev)
    {
        window.transform.position += ev.deltaPosition;
    }

    private void ToFront(PointerDownEvent ev)
    {
        window.BringToFront();
    }



}
