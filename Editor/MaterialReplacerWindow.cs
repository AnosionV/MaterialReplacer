using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;

namespace Anosion.MaterialReplacer
{
    public class MaterialReplacerWindow : EditorWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private List<VRCAvatarDescriptor> avatars = new List<VRCAvatarDescriptor>();
        private ReorderableList avatarList;

        [MenuItem("Window/Material Replacer")]
        public static void ShowWindow()
        {
            GetWindow<MaterialReplacerWindow>("Material Replacer");
        }

        private void OnEnable()
        {
            SetupAvatarList();
        }

        private void SetupAvatarList()
        {
            avatarList = new ReorderableList(avatars, typeof(VRCAvatarDescriptor), true, true, true, true);
            avatarList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Avatars");
            };

            avatarList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                avatars[index] = (VRCAvatarDescriptor)EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    avatars[index], typeof(VRCAvatarDescriptor), true);
            };

            avatarList.onAddCallback = (ReorderableList list) =>
            {
                avatars.Add(null);
            };

            avatarList.onRemoveCallback = (ReorderableList list) =>
            {
                avatars.RemoveAt(list.index);
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Material Replacer", EditorStyles.boldLabel);

            Rect listRect = GUILayoutUtility.GetRect(0, avatarList.GetHeight(), GUILayout.ExpandWidth(true));
            avatarList.DoList(listRect);

            HandleDragAndDrop(listRect);

            EditorGUILayout.EndVertical();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height - 100));
            if (avatars.Count > 0)
            {
                GUILayout.Label("Avatar Material Configuration", EditorStyles.boldLabel);
                foreach (var avatar in avatars)
                {
                    if (avatar != null)
                    {
                        DrawAvatarMaterialConfiguration(avatar);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawAvatarMaterialConfiguration(VRCAvatarDescriptor avatar)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(avatar.gameObject.name, avatar, typeof(VRCAvatarDescriptor), false);
                EditorGUI.EndDisabledGroup();

                Dictionary<GameObject, List<Material>> materialData = AvatarMaterialConfiguration.ExtractMaterialData(avatar.gameObject);
                AvatarMaterialConfiguration avatarMaterialConfig = new AvatarMaterialConfiguration(avatar.gameObject, materialData);

                foreach (var material in avatarMaterialConfig.Materials.Keys)
                {
                    using (new EditorGUI.IndentLevelScope())
                    using (new EditorGUILayout.VerticalScope("box"))
                    {

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(material, typeof(Material), false);
                            EditorGUI.EndDisabledGroup();

                            EditorGUILayout.LabelField("Å®", GUILayout.Width(40));

                            Material targetMaterial = (Material)EditorGUILayout.ObjectField(null, typeof(Material), false);
                        }
                        using (new EditorGUI.IndentLevelScope())
                        {
                            foreach (var location in avatarMaterialConfig.Materials[material])
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    bool isSelected = EditorGUILayout.Toggle(true, GUILayout.Width(20));
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.ObjectField(location.Mesh, typeof(GameObject), true);
                                    EditorGUI.EndDisabledGroup();
                                }
                            }
                        }

                    }
                }
            }
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            VRCAvatarDescriptor avatar = ((GameObject)draggedObject)?.GetComponent<VRCAvatarDescriptor>();
                            if (avatar != null && !avatars.Contains(avatar))
                            {
                                avatars.Add(avatar);
                            }
                        }
                    }
                    Event.current.Use();
                    break;
            }
        }
    }
}
