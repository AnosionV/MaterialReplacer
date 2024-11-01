using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

namespace Anosion.MaterialReplacer
{
    public class SceneWideMaterialReplacementView : MaterialReplacementView
    {
        private ReorderableList replaceableMaterials;
        private Material targetMaterial;
        private List<MaterialReplacementSettings> materialReplacementSettingsList = new List<MaterialReplacementSettings>();
        private bool includeInactive = true;
        private bool enableSwitch = true;

        // 置換可能なマテリアルのリストを初期化します。
        private void InitializeReplaceableMaterialsList()
        {
            replaceableMaterials = new ReorderableList(new List<Material>(), typeof(Material), true, true, true, true);

            // ヘッダーの描画設定
            replaceableMaterials.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "置換元マテリアル");
            };

            // 各要素の描画設定
            replaceableMaterials.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;
                Material newMaterial = (Material)EditorGUI.ObjectField(rect, (Material)replaceableMaterials.list[index], typeof(Material), false);
                if ((Material)replaceableMaterials.list[index] != newMaterial)
                {
                    replaceableMaterials.list[index] = newMaterial;
                    UpdateMaterialReplacementSettings();
                }
            };

            // 要素が追加された際の処理
            replaceableMaterials.onAddCallback = (ReorderableList list) =>
            {
                list.list.Add(null);
                UpdateMaterialReplacementSettings();
            };

            // 要素が削除された際の処理
            replaceableMaterials.onRemoveCallback = (ReorderableList list) =>
            {
                list.list.RemoveAt(list.index);
                UpdateMaterialReplacementSettings();
            };
        }

        // ビューが有効になったときに呼び出されます（エディターウィンドウを開く、またはタブを切り替える場合など）。
        public override void OnEnable()
        {
            base.OnEnable();
            InitializeReplaceableMaterialsList();
        }

        // ビューが無効になったときに呼び出されます（エディターウィンドウを閉じる、またはタブを切り替える場合など）。
        public override void OnDisable()
        {
            base.OnDisable();
        }

        // マテリアル置換ビューのGUIを描画します（マテリアル選択およびオブジェクト処理リストを含む）。
        public override void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            Rect listRect = GUILayoutUtility.GetRect(0, replaceableMaterials.GetHeight(), GUILayout.ExpandWidth(true));
            replaceableMaterials.DoList(listRect);

            List<Object> droppedObjects = HandleDragAndDrop(listRect);
            foreach (Object obj in droppedObjects)
            {
                if (obj is Material material && !replaceableMaterials.list.Contains(material))
                {
                    replaceableMaterials.list.Add(material);
                    UpdateMaterialReplacementSettings();
                }
            }

            targetMaterial = (Material)EditorGUILayout.ObjectField("置換先マテリアル", targetMaterial, typeof(Material), false);

            GUILayout.Space(15);

            var includeInactive = EditorGUILayout.Toggle("無効化アバターも含める", this.includeInactive, GUILayout.Width(15));
            if(includeInactive != this.includeInactive)
            {
                this.includeInactive = includeInactive;
                UpdateMaterialReplacementSettings();
            }
            enableSwitch = EditorGUILayout.Toggle("置換後にマテリアルを入れ替え", enableSwitch, GUILayout.Width(15));
            if (GUILayout.Button("置換実行", GUILayout.Height(30)))
            {
                ExecuteReplacement();
            }
            EditorGUILayout.EndVertical();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            GUILayout.Label("置換設定", EditorStyles.boldLabel);
            if (GUILayout.Button("マテリアル分布の更新", GUILayout.Height(20)))
            {
                UpdateMaterialReplacementSettings();
            }
            if (materialReplacementSettingsList.Count > 0)
            {
                foreach (var settings in materialReplacementSettingsList)
                {
                    DrawAvatarMaterialConfiguration(settings);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // Undo/Redoイベントを処理します。
        protected override void OnUndoRedoPerformed()
        {
            // Undo/Redo操作が行われたときにマテリアル置換設定を再構築します。
            UpdateMaterialReplacementSettings();
        }

        // 置換可能なマテリアルと置換後のマテリアルに基づいて、シーンオブジェクトのマテリアル置換設定を更新します。
        private void UpdateMaterialReplacementSettings()
        {
            materialReplacementSettingsList = Object.FindObjectsOfType<VRCAvatarDescriptor>(includeInactive)
                .Select(avatarDescriptor => avatarDescriptor.gameObject)
                .Select(avatar => new AvatarMaterialConfiguration(avatar, AvatarMaterialConfiguration.ExtractMaterialData(avatar)))
                .Where(config => replaceableMaterials.list.OfType<Material>().Any(material => config.Materials.ContainsKey(material)))
                .Select(config => new MaterialReplacementSettings(config))
                .ToList();
        }

        // アバターのマテリアル設定を描画します。
        private void DrawAvatarMaterialConfiguration(MaterialReplacementSettings settings)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    settings.Enable = EditorGUILayout.Toggle(settings.Enable, GUILayout.Width(15));
                    DrawDisabledObjectField(settings.AvatarMaterialConfig.Avatar, typeof(VRCAvatarDescriptor), true);
                }

                if (settings.Enable)
                {
                    foreach (var material in settings.AvatarMaterialConfig.Materials.Keys.Where(material => replaceableMaterials.list.Contains(material)))
                    {
                        GUILayout.Space(10);
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Space(15);

                                DrawDisabledObjectField(material, typeof(Material), false);
                            }

                            foreach (var location in settings.AvatarMaterialConfig.Materials[material])
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    GUILayout.Space(30);
                                    bool isSelectedLocation = EditorGUILayout.Toggle(settings.SelectedMeshLocations[location], GUILayout.Width(15));
                                    settings.SelectedMeshLocations[location] = isSelectedLocation;

                                    DrawDisabledObjectField(location.Mesh, typeof(GameObject), true);
                                }
                            }
                        }
                    }
                }
            }
            GUILayout.Space(10);
        }

        private void ExecuteReplacement()
        {
            foreach (var settings in materialReplacementSettingsList.Where(settings => settings.Enable))
            {
                var renderers = settings.AvatarMaterialConfig.Avatar.gameObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    Undo.RegisterCompleteObjectUndo(renderer, "Material Replacement");
                }

                AvatarMaterialConfiguration transformedConfig = settings.AvatarMaterialConfig.Map(replaceableMaterials.list.OfType<Material>().ToDictionary(material => material, _ => targetMaterial), settings.SelectedMeshLocations);
                AvatarMaterialConfiguration.Applymaterials(transformedConfig);
            }

            if (enableSwitch)
            {
                replaceableMaterials.list.Clear();
                replaceableMaterials.list.Add(targetMaterial);
                targetMaterial = null;
            }

            UpdateMaterialReplacementSettings();
        }
    }
}
