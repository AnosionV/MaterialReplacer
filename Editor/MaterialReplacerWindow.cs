using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

namespace Anosion.MaterialReplacer
{
    public class MaterialReplacerWindow : EditorWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private List<VRCAvatarDescriptor> avatars = new List<VRCAvatarDescriptor>();
        private Dictionary<VRCAvatarDescriptor, MaterialReplacementSettings> materialReplacementSettings = new Dictionary<VRCAvatarDescriptor, MaterialReplacementSettings>();
        private ReorderableList avatarList;

        [MenuItem("Window/Material Replacer")]
        public static void ShowWindow()
        {
            GetWindow<MaterialReplacerWindow>("Material Replacer");
        }

        private void OnEnable()
        {
            SetupAvatarList();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            if (avatars.Any(avatar => avatar != null && materialReplacementSettings.ContainsKey(avatar) && materialReplacementSettings[avatar].AvatarMaterialConfig.HasDifferences(AvatarMaterialConfiguration.ExtractMaterialData(avatar.gameObject))))
            {
                EditorApplication.delayCall += UpdateMaterialReplacementSettings;
            }
        }


        private void UpdateMaterialReplacementSettings()
        {
            foreach (var avatar in avatars)
            {
                if (avatar != null)
                {
                    Dictionary<GameObject, List<Material>> materialData = AvatarMaterialConfiguration.ExtractMaterialData(avatar.gameObject);
                    AvatarMaterialConfiguration avatarMaterialConfig = new AvatarMaterialConfiguration(avatar.gameObject, materialData);
                    materialReplacementSettings[avatar] = new MaterialReplacementSettings(avatarMaterialConfig);
                }
            }
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
                UpdateMaterialReplacementSettings();
            };

            avatarList.onRemoveCallback = (ReorderableList list) =>
            {
                VRCAvatarDescriptor removedAvatar = avatars[list.index];
                avatars.RemoveAt(list.index);
                materialReplacementSettings.Remove(removedAvatar);
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Material Replacer", EditorStyles.boldLabel);

            Rect listRect = GUILayoutUtility.GetRect(0, avatarList.GetHeight(), GUILayout.ExpandWidth(true));
            avatarList.DoList(listRect);

            HandleDragAndDrop(listRect);

            if (GUILayout.Button("置換実行", GUILayout.Height(30)))
            {
                ExecuteReplacement();
            }
            EditorGUILayout.EndVertical();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height - 100));
            if (avatars.Count > 0)
            {
                GUILayout.Label("置換設定", EditorStyles.boldLabel);
                if (GUILayout.Button("マテリアル分布の更新", GUILayout.Height(20)))
                {
                    UpdateMaterialReplacementSettings();
                }
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
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(avatar.gameObject.name);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(avatar, typeof(VRCAvatarDescriptor), false);
                    EditorGUI.EndDisabledGroup();
                }

                if (!materialReplacementSettings.ContainsKey(avatar))
                {
                    UpdateMaterialReplacementSettings();
                }
                MaterialReplacementSettings settings = materialReplacementSettings[avatar];
                AvatarMaterialConfiguration avatarMaterialConfig = settings.AvatarMaterialConfig;

                foreach (var material in avatarMaterialConfig.Materials.Keys)
                {
                    GUILayout.Space(10);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(15); // EditorGUI.IndentLevelScope() を使うと Toggle のクリック判定がおかしくなるのでスペースでインデントする

                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(material, typeof(Material), false);
                            EditorGUI.EndDisabledGroup();

                            EditorGUILayout.LabelField("→", GUILayout.Width(40));

                            Material targetMaterial = (Material)EditorGUILayout.ObjectField(settings.ReplacementMap[material], typeof(Material), false);
                            settings.ReplacementMap[material] = targetMaterial;

                            if (GUILayout.Button("×", GUILayout.Width(20)))
                            {
                                settings.ReplacementMap[material] = null;
                            }
                        }

                        if (settings.ReplacementMap[material] != null)
                        {
                            foreach (var location in avatarMaterialConfig.Materials[material])
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    GUILayout.Space(30); // インデント
                                    bool isSelected = EditorGUILayout.Toggle(settings.SelectedMeshLocations[location], GUILayout.Width(15));
                                    settings.SelectedMeshLocations[location] = isSelected;

                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.ObjectField(location.Mesh, typeof(GameObject), true);
                                    EditorGUI.EndDisabledGroup();
                                }
                            }
                        }
                    }
                }
            }
            GUILayout.Space(10);
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

        private void ExecuteReplacement()
        {
            foreach (var avatar in avatars)
            {
                if (avatar != null && materialReplacementSettings.ContainsKey(avatar))
                {
                    MaterialReplacementSettings settings = materialReplacementSettings[avatar];
                    var filteredReplacementMap = settings.ReplacementMap
                                                          .Where(entry => entry.Value != null)
                                                          .ToDictionary(entry => entry.Key, entry => entry.Value);

                    // AvatarMaterialConfigurationの変更前にUndoを登録
                    var renderers = avatar.gameObject.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        Undo.RegisterCompleteObjectUndo(renderer, "Material Replacement");
                    }

                    AvatarMaterialConfiguration transformedConfig = settings.AvatarMaterialConfig.Map(filteredReplacementMap, settings.SelectedMeshLocations);
                    AvatarMaterialConfiguration.Applymaterials(transformedConfig);

                }
            }
            UpdateMaterialReplacementSettings();
        }
    }
}
