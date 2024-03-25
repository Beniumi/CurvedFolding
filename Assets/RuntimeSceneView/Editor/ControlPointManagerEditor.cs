using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace RuntimeSceneView.Objects.Editors
{
    [CustomEditor(typeof(ControlPointManager))]
    public class ControlPointManagerEditor : Editor
    {
        ControlPointManager manager;
        List<int> ids;
        int selected_id;

        void OnDisable()
        {
            Tools.hidden = false;
        }

        void OnEnable()
        {
            manager = target as ControlPointManager;
            manager.Handler = manager.GetComponent<IControllPointHandler>();
            ids = new List<int>();
            selected_id = -1;
        }

        int GetID(int n)
        {
            if (n >= ids.Count)
            {
                AddID(n);
            }
            return ids[n];
        }

        void AddID(int n)
        {
            for (int i = ids.Count; i <= n; i++)
                ids.Add(GUIUtility.GetControlID(FocusType.Passive));
        }

        void OnSceneGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                List<Vector3> worldPositions = WorldPositions;
                int n = worldPositions.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector3 screenPosition = Handles.matrix.MultiplyPoint(worldPositions[i]);
                    float radius = HandleUtility.GetHandleSize(screenPosition) * 0.1f;
                    int id = GetID(i);
                    switch (Event.current.GetTypeForControl(id))
                    {
                        case EventType.MouseDown:
                            if (HandleUtility.nearestControl == id && Event.current.button == 0)
                            {
                                selected_id = id;
                            }
                            break;
                        case EventType.MouseDrag:
                            if (GUIUtility.hotControl == id && Event.current.button == 0)
                            {
                                GUI.changed = true;
                                Event.current.Use();
                            }
                            break;
                    }

                    if (selected_id == id)
                    {
                        Handles.color = Color.yellow;
                        Handles.SphereHandleCap(id, screenPosition, Quaternion.identity, radius * 2.0f, Event.current.type);
                        Vector3 newPosition = Handles.PositionHandle(screenPosition, Quaternion.identity);
                        if (check.changed)
                        {
                            Undo.RecordObject(manager, "Move CP");
                            SerializedProperty positions = serializedObject.FindProperty("positions");
                            SerializedProperty element = positions.GetArrayElementAtIndex(i);
                            element.vector3Value = manager.transform.InverseTransformPoint(newPosition);
                            serializedObject.ApplyModifiedProperties();
                            manager.Handler.OnControlPointChanged(manager.Positions);
                        }
                        Tools.hidden = true;
                    }
                    else
                    {
                        Handles.color = Color.white;
                        Handles.SphereHandleCap(id, screenPosition, Quaternion.identity, radius * 2.0f, Event.current.type);
                    }
                }
                if (Event.current.type == EventType.Repaint)
                {
                    Handles.color = Color.black;
                    for (int i = 1; i < n; i++)
                    {
                        Handles.DrawDottedLine(worldPositions[i - 1], worldPositions[i], 5.0f);
                    }
                }
            }
        }

        private List<Vector3> WorldPositions
        {
            get
            {
                SerializedProperty useWorldSpace = serializedObject.FindProperty("useWorldSpace");
                List<Vector3> localPositions = manager.Positions;
                List<Vector3> worldPositions = new List<Vector3>();
                if (useWorldSpace.boolValue)
                {
                    foreach (Vector3 p in localPositions)
                        worldPositions.Add(p);
                }
                else
                {
                    foreach (Vector3 p in localPositions)
                        worldPositions.Add(manager.transform.TransformPoint(p));
                }
                return worldPositions;
            }
            set
            {
                SerializedProperty useWorldSpace = serializedObject.FindProperty("useWorldSpace");
                SerializedProperty positions = serializedObject.FindProperty("positions");
                int count = positions.arraySize;
                if (useWorldSpace.boolValue)
                {
                    for (int i = 0; i < count; i++)
                    {
                        SerializedProperty element = positions.GetArrayElementAtIndex(i);
                        element.vector3Value = value[i];
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        SerializedProperty element = positions.GetArrayElementAtIndex(i);
                        element.vector3Value = manager.transform.InverseTransformPoint(value[i]);
                    }
                }
                serializedObject.ApplyModifiedProperties();
            }
        }

    }
}
