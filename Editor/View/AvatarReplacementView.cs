using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

namespace Anosion.MaterialReplacer.View
{
    public class AvatarReplacementView : MaterialReplacementView
    {
        private List<VRCAvatarDescriptor> avatars = new List<VRCAvatarDescriptor>();
        private Dictionary<VRCAvatarDescriptor, MaterialReplacementSettings> materialReplacementSettings = new Dictionary<VRCAvatarDescriptor, MaterialReplacementSettings>();
        private Dictionary<VRCAvatarDescriptor, Dictionary<Material, bool>> foldoutStates = new Dictionary<VRCAvatarDescriptor, Dictionary<Material, bool>>();
        private ReorderableList avatarList;

        public override void OnEnable()
        {
            base.OnEnable();
            SetupAvatarList();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnUndoRedoPerformed()
        {
            if (avatars.Any(avatar => avatar != null && materialReplacementSettings.ContainsKey(avatar) && materialReplacementSettings[avatar].AvatarMaterialConfig.HasDifferences(AvatarMaterialConfiguration.ExtractMaterialData(avatar.gameObject))))
            {
                EditorApplication.delayCall += UpdateMaterialReplacementSettings;
            }
        }

        private void SetupAvatarList()
        {
            avatarList = new ReorderableList(avatars, typeof(VRCAvatarDescriptor), true, true, true, true);
            avatarList.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, "対象アバター");
            };

            avatarList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                avatars[index] = (VRCAvatarDescriptor)EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    avatars[index], typeof(VRCAvatarDescriptor), true);
            };

            avatarList.onAddCallback = (list) =>
            {
                avatars.Add(null);
                UpdateMaterialReplacementSettings();
            };

            avatarList.onRemoveCallback = (list) =>
            {
                VRCAvatarDescriptor removedAvatar = avatars[list.index];
                avatars.RemoveAt(list.index);
                materialReplacementSettings.Remove(removedAvatar);
                foldoutStates.Remove(removedAvatar);
            };
        }

        private void UpdateMaterialReplacementSettings()
        {
            foreach (var avatar in avatars)
            {
                if (avatar == null) continue;

                Dictionary<GameObject, List<Material>> materialData = AvatarMaterialConfiguration.ExtractMaterialData(avatar.gameObject);
                AvatarMaterialConfiguration avatarMaterialConfig = new AvatarMaterialConfiguration(avatar.gameObject, materialData);
                materialReplacementSettings[avatar] = new MaterialReplacementSettings(avatarMaterialConfig);

                if (!foldoutStates.ContainsKey(avatar))
                {
                    foldoutStates[avatar] = new Dictionary<Material, bool>();
                }

                var existingFoldouts = foldoutStates[avatar];
                var newFoldouts = new Dictionary<Material, bool>();
                foreach (var material in avatarMaterialConfig.Materials.Keys)
                {
                    newFoldouts[material] = existingFoldouts.TryGetValue(material, out var state) ? state : false;
                }

                foldoutStates[avatar] = newFoldouts;
            }
        }

        public override void OnGUI()
        {
            DrawInputSection();
            EnsureViewState();
            DrawActionSection();
            DrawResultSection();
        }

        private void EnsureViewState()
        {
            foreach (var avatar in avatars)
            {
                if (avatar != null && (!materialReplacementSettings.ContainsKey(avatar) || !foldoutStates.ContainsKey(avatar)))
                {
                    UpdateMaterialReplacementSettings();
                    break;
                }
            }
        }

        private void DrawInputSection()
        {
            EditorGUILayout.BeginVertical();

            Rect listRect = GUILayoutUtility.GetRect(0, avatarList.GetHeight(), GUILayout.ExpandWidth(true));
            avatarList.DoList(listRect);

            List<Object> droppedObjects = HandleDragAndDrop(listRect);
            foreach (Object obj in droppedObjects)
            {
                if (obj is GameObject gameObject)
                {
                    VRCAvatarDescriptor avatar = gameObject.GetComponent<VRCAvatarDescriptor>();
                    if (avatar != null && !avatars.Contains(avatar))
                    {
                        avatars.Add(avatar);
                        UpdateMaterialReplacementSettings();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionSection()
        {
            if (GUILayout.Button("置換実行", Layout.ActionButtonHeight))
            {
                ExecuteReplacement();
            }

            GUILayout.Label("置換設定", EditorStyles.boldLabel);
            if (GUILayout.Button("マテリアル分布の更新", Layout.RefreshButtonHeight))
            {
                UpdateMaterialReplacementSettings();
            }
        }

        private void DrawResultSection()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            if (avatars.Count > 0)
            {
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
            if (!materialReplacementSettings.TryGetValue(avatar, out var settings))
            {
                return;
            }

            EnsureFoldoutStates(avatar, settings.AvatarMaterialConfig);
            var avatarFoldoutStates = foldoutStates[avatar];

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(avatar.gameObject.name);
                    DrawDisabledObjectField(avatar, typeof(VRCAvatarDescriptor), false);
                }
                AvatarMaterialConfiguration avatarMaterialConfig = settings.AvatarMaterialConfig;

                foreach (var materialGroup in avatarMaterialConfig.MaterialGroups)
                {
                    GUILayout.Space(10);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        Indent(1);
                        EditorGUILayout.LabelField(materialGroup.Key);
                    }

                    foreach (var material in materialGroup.Value.Keys)
                    {
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                Indent(2);

                                bool isFolded = avatarFoldoutStates[material];
                                Rect foldoutRect = GUILayoutUtility.GetRect(Layout.FoldoutWidth, EditorGUIUtility.singleLineHeight, GUILayout.Width(Layout.FoldoutWidth));
                                bool newFolded = EditorGUI.Foldout(foldoutRect, isFolded, GUIContent.none, true);
                                avatarFoldoutStates[material] = newFolded;

                                DrawDisabledObjectField(material, typeof(Material), false);

                                EditorGUILayout.LabelField("→", Styles.CenteredLabel, Layout.ArrowLabelWidth);

                                Material previousTargetMaterial = settings.ReplacementMap[material];
                                Material newTargetMaterial = (Material)EditorGUILayout.ObjectField(previousTargetMaterial, typeof(Material), false);
                                if (previousTargetMaterial == null && newTargetMaterial != null)
                                {
                                    avatarFoldoutStates[material] = true;
                                }

                                settings.ReplacementMap[material] = newTargetMaterial;

                                if (GUILayout.Button("×", Layout.ClearButtonWidth))
                                {
                                    settings.ReplacementMap[material] = null;
                                }
                            }

                            if (avatarFoldoutStates[material])
                            {
                                bool hasReplacement = settings.ReplacementMap[material] != null;
                                foreach (var location in avatarMaterialConfig.Materials[material])
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        Indent(3);

                                        EditorGUI.BeginDisabledGroup(!hasReplacement);
                                        bool isSelected = EditorGUILayout.Toggle(settings.SelectedMeshLocations[location], Layout.ToggleWidth);
                                        EditorGUI.EndDisabledGroup();

                                        settings.SelectedMeshLocations[location] = isSelected;

                                        DrawDisabledObjectField(location.Mesh, typeof(GameObject), true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            GUILayout.Space(10);
        }

        private void EnsureFoldoutStates(VRCAvatarDescriptor avatar, AvatarMaterialConfiguration avatarMaterialConfig)
        {
            if (!foldoutStates.ContainsKey(avatar))
            {
                foldoutStates[avatar] = new Dictionary<Material, bool>();
            }

            var existingFoldouts = foldoutStates[avatar];
            var newFoldouts = new Dictionary<Material, bool>();
            foreach (var material in avatarMaterialConfig.Materials.Keys)
            {
                newFoldouts[material] = existingFoldouts.TryGetValue(material, out var state) ? state : false;
            }

            foldoutStates[avatar] = newFoldouts;
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

                    var renderers = avatar.gameObject.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers)
                    {
                        Undo.RegisterCompleteObjectUndo(renderer, "Material Replacement");
                    }

                    AvatarMaterialConfiguration transformedConfig = settings.AvatarMaterialConfig.TransformMaterials(filteredReplacementMap, settings.SelectedMeshLocations);
                    AvatarMaterialConfiguration.ApplyMaterials(transformedConfig);
                }
            }
            UpdateMaterialReplacementSettings();
        }
    }
}
