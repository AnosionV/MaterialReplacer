using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;

namespace Anosion.MaterialReplacer
{
    public class SceneWideMaterialReplacementView : BaseMaterialReplacementView
    {
        private ReorderableList replaceableMaterials;
        private Material targetMaterial;
        private List<MaterialReplacementSettings> materialReplacementSettingsList = new List<MaterialReplacementSettings>();
        private bool enableSwitch = true;

        // �u���\�ȃ}�e���A���̃��X�g�����������܂��B
        private void InitializeReplaceableMaterialsList()
        {
            replaceableMaterials = new ReorderableList(new List<Material>(), typeof(Material), true, true, true, true);

            // �w�b�_�[�̕`��ݒ�
            replaceableMaterials.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "�u�����}�e���A��");
            };

            // �e�v�f�̕`��ݒ�
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

            // �v�f���ǉ����ꂽ�ۂ̏���
            replaceableMaterials.onAddCallback = (ReorderableList list) =>
            {
                list.list.Add(null);
                UpdateMaterialReplacementSettings();
            };

            // �v�f���폜���ꂽ�ۂ̏���
            replaceableMaterials.onRemoveCallback = (ReorderableList list) =>
            {
                list.list.RemoveAt(list.index);
                UpdateMaterialReplacementSettings();
            };
        }

        // �r���[���L���ɂȂ����Ƃ��ɌĂяo����܂��i�G�f�B�^�[�E�B���h�E���J���A�܂��̓^�u��؂�ւ���ꍇ�Ȃǁj�B
        public override void OnEnable()
        {
            base.OnEnable();
            InitializeReplaceableMaterialsList();
        }

        // �r���[�������ɂȂ����Ƃ��ɌĂяo����܂��i�G�f�B�^�[�E�B���h�E�����A�܂��̓^�u��؂�ւ���ꍇ�Ȃǁj�B
        public override void OnDisable()
        {
            base.OnDisable();
        }

        // �}�e���A���u���r���[��GUI��`�悵�܂��i�}�e���A���I������уI�u�W�F�N�g�������X�g���܂ށj�B
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

            targetMaterial = (Material)EditorGUILayout.ObjectField("�u����}�e���A��", targetMaterial, typeof(Material), false);

            GUILayout.Space(15);

            enableSwitch = EditorGUILayout.Toggle("�u����Ƀ}�e���A�������ւ�", enableSwitch, GUILayout.Width(15));
            if (GUILayout.Button("�u�����s", GUILayout.Height(30)))
            {
                ExecuteReplacement();
            }
            EditorGUILayout.EndVertical();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(EditorGUIUtility.currentViewWidth - 100));
            GUILayout.Label("�u���ݒ�", EditorStyles.boldLabel);
            if (GUILayout.Button("�}�e���A�����z�̍X�V", GUILayout.Height(20)))
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

        // Undo/Redo�C�x���g���������܂��B
        protected override void OnUndoRedoPerformed()
        {
            // Undo/Redo���삪�s��ꂽ�Ƃ��Ƀ}�e���A���u���ݒ���č\�z���܂��B
            UpdateMaterialReplacementSettings();
        }

        // �u���\�ȃ}�e���A���ƒu����̃}�e���A���Ɋ�Â��āA�V�[���I�u�W�F�N�g�̃}�e���A���u���ݒ���X�V���܂��B
        private void UpdateMaterialReplacementSettings()
        {
            materialReplacementSettingsList = Object.FindObjectsOfType<VRCAvatarDescriptor>()
                .Select(avatarDescriptor => avatarDescriptor.gameObject)
                .Select(avatar => new AvatarMaterialConfiguration(avatar, AvatarMaterialConfiguration.ExtractMaterialData(avatar)))
                .Where(config => replaceableMaterials.list.OfType<Material>().Any(material => config.Materials.ContainsKey(material)))
                .Select(config => new MaterialReplacementSettings(config))
                .ToList();
        }

        // �A�o�^�[�̃}�e���A���ݒ��`�悵�܂��B
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
