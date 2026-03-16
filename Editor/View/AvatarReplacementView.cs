using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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

                    DrawMaterialGroupHeader(avatar, materialGroup);

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

        private void DrawMaterialGroupHeader(
            VRCAvatarDescriptor avatar,
            KeyValuePair<string, SortedDictionary<Material, List<AvatarMaterialConfiguration.MaterialLocation>>> materialGroup)
        {
            Rect dropAreaRect;
            var sourceFolderAsset = GetFolderAssetForGroup(materialGroup.Key);
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Indent(1);
                    EditorGUILayout.LabelField(GetDisplayGroupPath(materialGroup.Key), Styles.WrappedPathLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    Indent(1);
                    if (sourceFolderAsset != null)
                    {
                        DrawDisabledObjectField(sourceFolderAsset, typeof(DefaultAsset), false, GUILayout.Width(Layout.GroupFolderFieldWidth));
                    }
                    else
                    {
                        GUILayout.Space(Layout.GroupFolderFieldWidth);
                    }

                    EditorGUILayout.LabelField("→", Styles.CenteredLabel, Layout.ArrowLabelWidth);

                    dropAreaRect = GUILayoutUtility.GetRect(
                        Layout.GroupFolderDropAreaWidth,
                        Layout.GroupFolderDropAreaWidth,
                        Layout.GroupFolderDropAreaHeight,
                        Layout.GroupFolderDropAreaHeight);

                    bool isDragOver = IsFolderDragOver(dropAreaRect);
                    DrawFolderDropArea(dropAreaRect, isDragOver);
                }
            }

            if (TryHandleFolderDrop(dropAreaRect, out var droppedFolder))
            {
                ApplyFolderMappingToGroup(avatar, materialGroup, droppedFolder);
                return;
            }

            if (TryHandleFolderSelectionClick(dropAreaRect, materialGroup.Key, out var selectedFolder))
            {
                ApplyFolderMappingToGroup(avatar, materialGroup, selectedFolder);
            }
        }

        private void DrawFolderDropArea(Rect dropAreaRect, bool isDragOver)
        {
            var backgroundColor = isDragOver
                ? new Color(0.24f, 0.42f, 0.72f, 0.28f)
                : new Color(0f, 0f, 0f, 0.12f);

            EditorGUI.DrawRect(dropAreaRect, backgroundColor);
            GUI.Box(dropAreaRect, GUIContent.none, EditorStyles.helpBox);
            GUI.Label(
                dropAreaRect,
                "一括置換フォルダをドロップ / クリック",
                isDragOver ? Styles.DropAreaLabelActive : Styles.DropAreaLabel);
            EditorGUIUtility.AddCursorRect(dropAreaRect, MouseCursor.Link);
        }

        private bool IsFolderDragOver(Rect dropAreaRect)
        {
            Event currentEvent = Event.current;
            if ((currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform) ||
                !dropAreaRect.Contains(currentEvent.mousePosition))
            {
                return false;
            }

            return DragAndDrop.objectReferences
                .OfType<DefaultAsset>()
                .Any(folderAsset => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folderAsset)));
        }

        private bool TryHandleFolderDrop(Rect dropAreaRect, out DefaultAsset folderAsset)
        {
            folderAsset = null;
            if (!IsFolderDragOver(dropAreaRect))
            {
                return false;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (Event.current.type != EventType.DragPerform)
            {
                Event.current.Use();
                return false;
            }

            DragAndDrop.AcceptDrag();
            folderAsset = DragAndDrop.objectReferences
                .OfType<DefaultAsset>()
                .FirstOrDefault(asset => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(asset)));
            Event.current.Use();
            return folderAsset != null;
        }

        private bool TryHandleFolderSelectionClick(string groupPath, out DefaultAsset folderAsset)
        {
            var initialFolderPath = GetInitialFolderPathForSelection(groupPath);
            string selectedFolderPath = EditorUtility.OpenFolderPanel("置換先フォルダを選択", initialFolderPath, string.Empty);
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                folderAsset = null;
                return false;
            }

            if (!TryConvertToAssetFolderPath(selectedFolderPath, out var assetFolderPath))
            {
                EditorUtility.DisplayDialog("Material Replacer", "Assets フォルダ内のフォルダを選択してください。", "OK");
                folderAsset = null;
                return false;
            }

            folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetFolderPath);
            if (folderAsset == null || !AssetDatabase.IsValidFolder(assetFolderPath))
            {
                EditorUtility.DisplayDialog("Material Replacer", "有効なプロジェクト内フォルダを選択してください。", "OK");
                folderAsset = null;
                return false;
            }

            return true;
        }

        private DefaultAsset GetFolderAssetForGroup(string groupPath)
        {
            string assetFolderPath = string.IsNullOrEmpty(groupPath)
                ? "Assets"
                : $"Assets/{groupPath.Replace('\\', '/')}";
            return AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetFolderPath);
        }

        private string GetDisplayGroupPath(string groupPath)
        {
            return string.IsNullOrEmpty(groupPath) ? "Assets" : groupPath.Replace('\\', '/');
        }

        private string GetInitialFolderPathForSelection(string groupPath)
        {
            string currentFolderPath = string.IsNullOrEmpty(groupPath)
                ? Application.dataPath
                : Path.Combine(Application.dataPath, groupPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            return Path.GetDirectoryName(currentFolderPath) ?? Application.dataPath;
        }

        private bool TryHandleFolderSelectionClick(Rect dropAreaRect, string groupPath, out DefaultAsset folderAsset)
        {
            folderAsset = null;
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0 || !dropAreaRect.Contains(currentEvent.mousePosition))
            {
                return false;
            }

            currentEvent.Use();
            return TryHandleFolderSelectionClick(groupPath, out folderAsset);
        }

        private bool TryConvertToAssetFolderPath(string absoluteFolderPath, out string assetFolderPath)
        {
            string normalizedAssetsPath = Application.dataPath.Replace('\\', '/');
            string normalizedFolderPath = absoluteFolderPath.Replace('\\', '/');
            if (string.Equals(normalizedFolderPath, normalizedAssetsPath, System.StringComparison.OrdinalIgnoreCase))
            {
                assetFolderPath = "Assets";
                return true;
            }

            string assetsPrefix = normalizedAssetsPath + "/";
            if (!normalizedFolderPath.StartsWith(assetsPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                assetFolderPath = null;
                return false;
            }

            assetFolderPath = "Assets/" + normalizedFolderPath.Substring(assetsPrefix.Length);
            return true;
        }

        private void ApplyFolderMappingToGroup(
            VRCAvatarDescriptor avatar,
            KeyValuePair<string, SortedDictionary<Material, List<AvatarMaterialConfiguration.MaterialLocation>>> materialGroup,
            DefaultAsset folderAsset)
        {
            string folderPath = AssetDatabase.GetAssetPath(folderAsset);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var normalizedFolderPath = folderPath.Replace('\\', '/');
            var materialsInFolder = AssetDatabase.FindAssets("t:Material", new[] { folderPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(
                    Path.GetDirectoryName(path)?.Replace('\\', '/'),
                    normalizedFolderPath,
                    System.StringComparison.OrdinalIgnoreCase))
                .Select(path => AssetDatabase.LoadAssetAtPath<Material>(path))
                .Where(material => material != null)
                .GroupBy(material => material.name)
                .ToDictionary(group => group.Key, group => group.First());

            if (materialsInFolder.Count == 0)
            {
                return;
            }

            if (!materialReplacementSettings.TryGetValue(avatar, out var settings))
            {
                return;
            }

            EnsureFoldoutStates(avatar, settings.AvatarMaterialConfig);
            var avatarFoldoutStates = foldoutStates[avatar];

            foreach (var material in materialGroup.Value.Keys)
            {
                if (materialsInFolder.TryGetValue(material.name, out var replacement))
                {
                    settings.ReplacementMap[material] = replacement;
                    avatarFoldoutStates[material] = true;
                }
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
