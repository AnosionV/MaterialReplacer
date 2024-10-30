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

            // �h���b�O���܂��̓h���b�v�C�x���g�ł���A�h���b�v�G���A���ɃJ�[�\��������ꍇ
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                // �h���b�O���̃t�B�[�h�o�b�N��ݒ�
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                // �h���b�v�����s���ꂽ�ꍇ
                if (evt.type == EventType.DragPerform)
                {
                    // �h���b�v���ꂽ�I�u�W�F�N�g���󂯓����
                    DragAndDrop.AcceptDrag();
                    droppedObjects.AddRange(DragAndDrop.objectReferences);
                }

                // �C�x���g���g�p�ς݂ɐݒ�
                Event.current.Use();
            }

            return droppedObjects;
        }

        protected abstract void OnUndoRedoPerformed();
        public abstract void OnGUI();
    }
}
