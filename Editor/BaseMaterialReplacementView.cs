using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Anosion.MaterialReplacer
{
    public abstract class BaseMaterialReplacementView : IMaterialReplacementView
    {
        protected Vector2 scrollPosition = Vector2.zero;

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
