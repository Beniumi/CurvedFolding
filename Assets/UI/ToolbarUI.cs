using CurvedFoldingSystem.Components;
using CurvedFoldingSystem.Components.FileManager;
using RuntimeSceneView;
using RuntimeSceneView.Cameras;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CurvedFoldingSystem.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ToolbarUI : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset buttonElement;
        [SerializeField]
        VisualTreeAsset menuElement;
        [SerializeField]
        UndoRedoSystem undoRedoSystem;
        [SerializeField]
        SaveFileManager saveFileManager;
        [SerializeField]
        WindowUI window3D;
        [SerializeField]
        WindowUI window2D;
        [SerializeField]
        CreasePatternEdge edge;
        [SerializeField]
        WindowUI output;
        [SerializeField]
        FreeCamera freeCamera;

        private VisualElement toolbar;
        private UndoRedoState current;

        private bool FileIsModified => !(current == undoRedoSystem.GetCurrentState());

        private void SettingFileMenu()
        {
            VisualElement fileMenu = AddItemMenu("File", out Button toolbarButton);

            Button newFile = new Button();
            newFile.text = "New File";
            newFile.clicked += () =>
            {
                if (FileIsModified)
                    saveFileManager.SaveAs();

                SceneManager.LoadScene("MainSystem");
            };
            fileMenu.Add(newFile);

            Button open = new Button();
            open.text = "Open File";
            open.clicked += () =>
            {
                if (FileIsModified)
                    saveFileManager.SaveAs();

                bool result = saveFileManager.Load();
                if (result)
                {
                    undoRedoSystem.ClearHistory();
                    current = null;
                }
            };
            fileMenu.Add(open);

            Button save = new Button();
            save.text = "Save";
            save.clicked += () =>
            {
                bool result = saveFileManager.Save();
                if (result)
                {
                    current = undoRedoSystem.GetCurrentState();
                }
                else
                {
                    bool result2 = saveFileManager.SaveAs();
                    if (result2)
                    {
                        current = undoRedoSystem.GetCurrentState();
                    }
                }
            };
            fileMenu.Add(save);

            Button saveAs = new Button();
            saveAs.text = "Save As...";
            saveAs.clicked += () =>
            {
                bool result = saveFileManager.SaveAs();
                if (result)
                    current = undoRedoSystem.GetCurrentState();
            };
            fileMenu.Add(saveAs);

            Button exportCreasePattern = new Button();
            exportCreasePattern.text = "Export Crease Pattern";
            exportCreasePattern.clicked += () =>
            {
                saveFileManager.ExportSVG();
            };
            fileMenu.Add(exportCreasePattern);

            Button export3DShape = new Button();
            export3DShape.text = "Export 3D Shape";
            export3DShape.clicked += () =>
            {
                saveFileManager.ExportOBJ();
            };
            fileMenu.Add(export3DShape);

            Button exit = new Button();
            exit.text = "Exit";
            exit.clicked += () =>
            {
                if (FileIsModified)
                    saveFileManager.SaveAs();

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            };
            fileMenu.Add(exit);
        }

        private void SettingEditMenu()
        {
            VisualElement editMenu = AddItemMenu("Edit", out Button toolbarButton);

            Button undo = new Button();
            Button redo = new Button();
            undo.text = "Undo";
            redo.text = "Redo";
            undo.clicked += () =>
            {
                undoRedoSystem.Undo();
            };
            redo.clicked += () =>
            {
                undoRedoSystem.Redo();
            };
            toolbarButton.clicked += () =>
            {
                undo.SetEnabled(undoRedoSystem.CanUndo);
                redo.SetEnabled(undoRedoSystem.CanRedo);
            };
            editMenu.Add(undo);
            editMenu.Add(redo);

            Button open3D = new Button();
            open3D.text = "3D";
            open3D.clicked += () =>
            {
                window3D.Show();
            };
            editMenu.Add(open3D);

            Button open2D = new Button();
            open2D.text = "2D";
            open2D.clicked += () =>
            {
                window2D.Show();
            };
            editMenu.Add(open2D);
        }

        private void SettingViewMenu()
        {
            VisualElement viewMenu = AddItemMenu("View", out Button toolbarButton);

            Button switch3D = new Button();
            switch3D.text = "3D";
            switch3D.clicked += () =>
            {
                window3D.gameObject.SetActive(!window3D.gameObject.activeSelf);
            };
            viewMenu.Add(switch3D);

            Button switch2D = new Button();
            switch2D.text = "2D";
            switch2D.clicked += () =>
            {
                window2D.gameObject.SetActive(!window2D.gameObject.activeSelf);
            };
            viewMenu.Add(switch2D);

            Button switchPaper = new Button();
            switchPaper.text = "Paper";
            switchPaper.clicked += () =>
            {
                edge.gameObject.SetActive(!edge.gameObject.activeSelf);
            };
            viewMenu.Add(switchPaper);

            Button switchGrid = new Button();
            switchGrid.text = "Grid";
            switchGrid.clicked += () =>
            {
                freeCamera.UseGrid = !freeCamera.UseGrid;
            };
            viewMenu.Add(switchGrid);

            Button switchGizmo = new Button();
            switchGizmo.text = "Gizmo";
            switchGizmo.clicked += () =>
            {
                freeCamera.Gizmo.gameObject.SetActive(!freeCamera.Gizmo.gameObject.activeSelf);
            };
            viewMenu.Add(switchGizmo);
        }

        private void Start()
        {
            UIDocument toolbarDocument = GetComponent<UIDocument>();
            toolbar = toolbarDocument.rootVisualElement.Q<VisualElement>("Toolbar");
            SettingFileMenu();
            SettingEditMenu();
            SettingViewMenu();
        }

        private VisualElement AddItemMenu(string name, out Button toolbarButton)
        {
            toolbarButton = AddToolbarButton(name);
            VisualElement itemMenu = menuElement.Instantiate();
            itemMenu.style.display = DisplayStyle.None;
            toolbarButton.parent.Add(itemMenu);
            toolbarButton.clicked += () =>
            {
                itemMenu.BringToFront();
                itemMenu.style.display = DisplayStyle.Flex;
            };
            toolbarButton.RegisterCallback((BlurEvent ev) =>
            {
                if (!itemMenu.Contains(ev.relatedTarget as VisualElement))
                    itemMenu.style.display = DisplayStyle.None;
            });
            itemMenu.RegisterCallback((ClickEvent ev) =>
            {
                itemMenu.style.display = DisplayStyle.None;
            });
            itemMenu.RegisterCallback((FocusOutEvent ev) =>
            {
                itemMenu.style.display = DisplayStyle.None;
            });

            return itemMenu.Q<VisualElement>("Content");
        }

        private Button AddToolbarButton(string name)
        {
            VisualElement toolbarButton = buttonElement.Instantiate();
            toolbarButton.Q<Label>("Name").text = name;
            toolbar.Add(toolbarButton);
            return toolbarButton.Q<Button>("Button");
        }
    }
}
