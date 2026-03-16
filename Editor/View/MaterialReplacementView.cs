using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Anosion.MaterialReplacer.View
{
    public abstract class MaterialReplacementView
    {
        protected Vector2 scrollPosition = Vector2.zero;

        protected static class Layout
        {
            public static readonly GUILayoutOption ToggleWidth = GUILayout.Width(15);
            public static readonly GUILayoutOption ArrowLabelWidth = GUILayout.Width(40);
            public static readonly GUILayoutOption ClearButtonWidth = GUILayout.Width(20);
            public static readonly GUILayoutOption ActionButtonHeight = GUILayout.Height(30);
            public static readonly GUILayoutOption RefreshButtonHeight = GUILayout.Height(20);
            public const float IndentWidth = 15f;
            public const float SectionSpacing = 15f;
        }

        protected static class Styles
        {
            private static GUIStyle centeredLabel;

            public static GUIStyle CenteredLabel => centeredLabel ??= new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        public virtual void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        protected void DrawDisabledObjectField(Object obj, System.Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(obj, objType, allowSceneObjects, options);
            EditorGUI.EndDisabledGroup();
        }

        protected void Indent(int level)
        {
            GUILayout.Space(Layout.IndentWidth * level);
        }

        protected List<Object> HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            List<Object> droppedObjects = new List<Object>();

            // ドラッグ中またはドロップイベントであり、ドロップエリア内にカーソルがある場合
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                // ドラッグ中のフィードバックを設定
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                // ドロップが実行された場合
                if (evt.type == EventType.DragPerform)
                {
                    // ドロップされたオブジェクトを受け入れる
                    DragAndDrop.AcceptDrag();
                    droppedObjects.AddRange(DragAndDrop.objectReferences);
                }

                // イベントを使用済みに設定
                Event.current.Use();
            }

            return droppedObjects;
        }

        protected abstract void OnUndoRedoPerformed();
        public abstract void OnGUI();
    }
}
