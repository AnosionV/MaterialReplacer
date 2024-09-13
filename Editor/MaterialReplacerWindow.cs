using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MaterialReplacerWindow : EditorWindow
{
    /// <summary>
    /// �����Ώۂ̃}�e���A�����X�g
    /// </summary>
    [SerializeField]
    private List<Material> targetMaterials = new();

    /// <summary>
    /// �u��������̃}�e���A��
    /// </summary>
    private Material replaceMaterial;

    private SerializedObject serializedObject;
    private SerializedProperty targetMaterialsProperty;

    /// <summary>
    /// �������ʂ��i�[���鎫��
    /// </summary>
    private Dictionary<GameObject, List<GameObject>> searchResults = new();

    /// <summary>
    /// ���[�g�I�u�W�F�N�g�̃g�O�����
    /// </summary>
    private Dictionary<GameObject, bool> toggles = new();

    /// <summary>
    /// �e�I�u�W�F�N�g���Ƃ̃g�O�����
    /// </summary>
    private Dictionary<GameObject, bool> objectToggles = new();

    private GUIStyle boxStyle;
    private GUIStyle contentStyle;
    private Vector2 scrollPosition;

    [MenuItem("Window/Material Replacer")]
    public static void ShowWindow()
    {
        GetWindow<MaterialReplacerWindow>("Material Replacer");
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
        targetMaterialsProperty = serializedObject.FindProperty("targetMaterials");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Search and Replace Materials", EditorStyles.boldLabel);

        serializedObject.Update();

        // �����Ώۂ̃}�e���A�����X�g���ύX���ꂽ�ꍇ�Ɍ������Ď��s����
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(targetMaterialsProperty, new GUIContent("Target Materials"), true);

        // �}�e���A�����X�g���ύX���ꂽ���`�F�b�N
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();

            // �}�e���A�����X�g����̏ꍇ�͌������ʂ��N���A����
            if (targetMaterials.Count == 0 || !targetMaterials.Any(material => material != null))
            {
                searchResults.Clear();
            }
            else
            {
                SearchObjectsWithMaterials();
            }
        }

        // �u��������}�e���A����I������t�B�[���h
        replaceMaterial = (Material)EditorGUILayout.ObjectField("Replace Material", replaceMaterial, typeof(Material), false);

        if (searchResults.Count > 0)
        {
            InitializeStyles();
            DisplaySearchResults();

            // �u���{�^���̕\���ƗL�����ݒ�
            GUI.enabled = replaceMaterial != null;
            if (GUILayout.Button("Replace"))
            {
                ReplaceMaterials();
            }
            GUI.enabled = true;
        }
    }

    /// <summary>
    /// �w�肳�ꂽ�}�e���A�������Q�[���I�u�W�F�N�g����������
    /// </summary>
    private void SearchObjectsWithMaterials()
    {
        if (targetMaterials.Count == 0) return;

        // �q�G�����L�[���̂��ׂẴQ�[���I�u�W�F�N�g���������A�w�肳�ꂽ�}�e���A���������̂𒊏o����
        searchResults = GameObject.FindObjectsOfType<GameObject>(true)
            .Where(gameObject => gameObject.hideFlags == HideFlags.None)
            .Where(gameObject => gameObject.GetComponent<Renderer>() != null)
            .Where(gameObject => gameObject.GetComponent<Renderer>().sharedMaterials.Intersect(targetMaterials).Any())
            .GroupBy(gameObject => gameObject.transform.root.gameObject)
            .ToDictionary(group => group.Key, group => group.ToList());

        // �e�I�u�W�F�N�g�̃g�O����Ԃ�������
        toggles = searchResults.Keys.ToDictionary(key => key, key => true);
        objectToggles = searchResults.Values.SelectMany(list => list).ToDictionary(obj => obj, obj => true);
    }

    /// <summary>
    /// GUI�X�^�C��������������
    /// </summary>
    private void InitializeStyles()
    {
        // �^�C�g���̃{�b�N�X�X�^�C��
        boxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            margin = new RectOffset(10, 10, 5, 5),
            padding = new RectOffset(10, 10, 10, 10)
        };

        // �R���e���c�̃X�^�C��
        contentStyle = new GUIStyle(EditorStyles.label)
        {
            margin = new RectOffset(20, 10, 5, 5),
            padding = new RectOffset(10, 10, 5, 5)
        };
    }

    /// <summary>
    /// �������ʂ�\������
    /// </summary>
    private void DisplaySearchResults()
    {
        // �X�N���[���r���[�̊J�n
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var (root, resultMeshes) in searchResults)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(boxStyle);

            // ���[�g�I�u�W�F�N�g�̃g�O���ƕ\��
            EditorGUILayout.BeginHorizontal();
            toggles[root] = EditorGUILayout.Toggle(toggles[root], GUILayout.Width(15));
            CreateReadonlyObjectField("", root);
            EditorGUILayout.EndHorizontal();

            if (toggles[root])
            {
                EditorGUILayout.Space();

                // �R���e���c�̐������C�A�E�g
                EditorGUILayout.BeginVertical(contentStyle);

                foreach (var mesh in resultMeshes)
                {
                    EditorGUILayout.BeginHorizontal();
                    objectToggles[mesh] = EditorGUILayout.Toggle(objectToggles[mesh], GUILayout.Width(15));
                    CreateReadonlyObjectField("", mesh);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        // �X�N���[���r���[�̏I��
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// �I�����ꂽ�I�u�W�F�N�g�̃}�e���A����u��������
    /// </summary>
    private void ReplaceMaterials()
    {
        // Undo�O���[�v�̊J�n
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Materials");

        // �������ʂ̊e�I�u�W�F�N�g�̃}�e���A����u��������
        var replaceTargetRenderer = searchResults
            .Where(kv => toggles[kv.Key]) // �g�O���� true �̃I�u�W�F�N�g�݂̂ɍi��
            .SelectMany(kv => kv.Value)
            .Where(obj => objectToggles[obj]) // �X�̃I�u�W�F�N�g�̃`�F�b�N��Ԃ��m�F
            .Select(obj => obj.GetComponent<Renderer>())
            .Where(renderer => renderer != null);

        foreach (var renderer in replaceTargetRenderer)
        {
            // Renderer�̏�Ԃ�Undo�̑ΏۂƂ��ċL�^
            Undo.RecordObject(renderer, "Replace Material");

            // �}�e���A����u��������
            renderer.sharedMaterials = renderer.sharedMaterials
                .Select(material => targetMaterials.Contains(material) ? replaceMaterial : material)
                .ToArray();
        }

        // Undo�O���[�v����̑���Ƃ��Ă܂Ƃ߂�
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        // �������Ď��s���čŐV�̏�Ԃ𔽉f
        SearchObjectsWithMaterials();
    }

    /// <summary>
    /// �ǂݎ���p�̃I�u�W�F�N�g�t�B�[���h���쐬����
    /// </summary>
    /// <param name="label">�I�u�W�F�N�g�t�B�[���h�̃��x��</param>
    /// <param name="obj">�\������I�u�W�F�N�g</param>
    private void CreateReadonlyObjectField(string label, Object obj)
    {
        GUI.enabled = false;
        EditorGUILayout.ObjectField(label, obj, typeof(GameObject), false);
        GUI.enabled = true;
    }
}