using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Anosion.MaterialReplacer
{

    public class MaterialReplacerWindow : EditorWindow
    {
        /// <summary>
        /// 検索対象のマテリアルリスト
        /// </summary>
        [SerializeField]
        private List<Material> targetMaterials = new();

        /// <summary>
        /// 置き換え先のマテリアル
        /// </summary>
        private Material replaceMaterial;

        /// <summary>
        /// 置換後に設定を更新するかどうかを決めるチェックボックス
        /// </summary>
        private bool switchMaterialsAfterReplace = false;

        private SerializedObject serializedObject;
        private SerializedProperty targetMaterialsProperty;

        /// <summary>
        /// 検索結果を格納する辞書
        /// </summary>
        private Dictionary<GameObject, List<GameObject>> searchResults = new();

        /// <summary>
        /// ルートオブジェクトのトグル状態
        /// </summary>
        private Dictionary<GameObject, bool> toggles = new();

        /// <summary>
        /// 各オブジェクトごとのトグル状態
        /// </summary>
        private Dictionary<GameObject, bool> objectToggles = new();

        private Vector2 scrollPosition;

        public GUIStyle BoxStyle { get; set; }

        public GUIStyle ContentStyle { get; set; }

        [MenuItem("Window/Material Replacer")]
        public static void ShowWindow()
        {
            GetWindow<MaterialReplacerWindow>("Material Replacer");
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            targetMaterialsProperty = serializedObject.FindProperty("targetMaterials");

            BoxStyle = new GUIStyle();
            ContentStyle = new GUIStyle()
            {
                margin = new RectOffset(20, 0, 0, 0)
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Search and Replace Materials", EditorStyles.boldLabel);

            serializedObject.Update();

            // 検索対象のマテリアルリストが変更された場合に検索を再実行する
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(targetMaterialsProperty, new GUIContent("Target Materials"), true);

            // マテリアルリストが変更されたかチェック
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                // マテリアルリストが空の場合は検索結果をクリアする
                if (targetMaterials.Count == 0 || !targetMaterials.Any(material => material != null))
                {
                    searchResults.Clear();
                }
                else
                {
                    SearchObjectsWithMaterials();
                }
            }

            // 置き換え先マテリアルを選択するフィールド
            replaceMaterial = (Material)EditorGUILayout.ObjectField("Replace Material", replaceMaterial, typeof(Material), false);

            // 再検索ボタン
            if (GUILayout.Button("Reload Search Results"))
            {
                SearchObjectsWithMaterials();
            }

            // スクロールビューの開始
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (searchResults.Count > 0)
            {
                DisplaySearchResults();
            }

            // スクロールビューの終了
            EditorGUILayout.EndScrollView();

            // 追加したチェックボックスを表示
            switchMaterialsAfterReplace = EditorGUILayout.Toggle("Switch After Replace", switchMaterialsAfterReplace);

            // 置換ボタンの表示と有効化設定
            GUI.enabled = replaceMaterial != null;
            if (GUILayout.Button("Replace"))
            {
                ReplaceMaterials();
            }
            GUI.enabled = true;
        }

        /// <summary>
        /// 指定されたマテリアルを持つゲームオブジェクトを検索する
        /// </summary>
        private void SearchObjectsWithMaterials()
        {
            if (targetMaterials.Count == 0) return;

            // ヒエラルキー内のすべてのゲームオブジェクトを検索し、指定されたマテリアルを持つものを抽出する
            searchResults = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(gameObject => gameObject.hideFlags == HideFlags.None)
                .Where(gameObject => gameObject.GetComponent<Renderer>() != null)
                .Where(gameObject => gameObject.GetComponent<Renderer>().sharedMaterials.Intersect(targetMaterials).Any())
                .GroupBy(gameObject => gameObject.transform.root.gameObject)
                .ToDictionary(group => group.Key, group => group.ToList());

            // 各オブジェクトのトグル状態を初期化
            toggles = searchResults.Keys.ToDictionary(key => key, key => true);
            objectToggles = searchResults.Values.SelectMany(list => list).ToDictionary(obj => obj, obj => true);
        }

        /// <summary>
        /// 検索結果を表示する
        /// </summary>
        private void DisplaySearchResults()
        {
            foreach (var (root, resultMeshes) in searchResults)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(BoxStyle);

                // ルートオブジェクトのトグルと表示
                EditorGUILayout.BeginHorizontal();
                toggles[root] = EditorGUILayout.Toggle(toggles[root], GUILayout.Width(15));
                CreateReadonlyObjectField("", root);
                EditorGUILayout.EndHorizontal();

                if (toggles[root])
                {
                    // コンテンツの垂直レイアウト
                    EditorGUILayout.BeginVertical(ContentStyle);

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
        }

        /// <summary>
        /// 選択されたオブジェクトのマテリアルを置き換える
        /// </summary>
        private void ReplaceMaterials()
        {
            // Undoグループの開始
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Replace Materials");

            // 置換前のマテリアルを保存
            Material previousReplaceMaterial = replaceMaterial;

            // 検索結果の各オブジェクトのマテリアルを置き換える
            var replaceTargetRenderer = searchResults
                .Where(kv => toggles[kv.Key]) // トグルが true のオブジェクトのみに絞る
                .SelectMany(kv => kv.Value)
                .Where(obj => objectToggles[obj]) // 個々のオブジェクトのチェック状態も確認
                .Select(obj => obj.GetComponent<Renderer>())
                .Where(renderer => renderer != null);

            foreach (var renderer in replaceTargetRenderer)
            {
                // Rendererの状態をUndoの対象として記録
                Undo.RecordObject(renderer, "Replace Material");

                // マテリアルを置き換える
                renderer.sharedMaterials = renderer.sharedMaterials
                    .Select(material => targetMaterials.Contains(material) ? replaceMaterial : material)
                    .ToArray();
            }

            // Undoグループを一つの操作としてまとめる
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            // 置換後の設定を更新する機能が有効な場合
            if (switchMaterialsAfterReplace)
            {
                // 置き換えマテリアルをNoneに設定
                replaceMaterial = null;

                // 対象マテリアルを先ほどの置換マテリアル1つに設定
                targetMaterials.Clear();
                targetMaterials.Add(previousReplaceMaterial);

                // シリアライズされたオブジェクトに変更を適用
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();
            }

            // 検索を再実行して最新の状態を反映
            SearchObjectsWithMaterials();
        }

        /// <summary>
        /// 読み取り専用のオブジェクトフィールドを作成する
        /// </summary>
        /// <param name="label">オブジェクトフィールドのラベル</param>
        /// <param name="obj">表示するオブジェクト</param>
        private void CreateReadonlyObjectField(string label, Object obj)
        {
            GUI.enabled = false;
            EditorGUILayout.ObjectField(label, obj, typeof(GameObject), false);
            GUI.enabled = true;
        }
    }
}
