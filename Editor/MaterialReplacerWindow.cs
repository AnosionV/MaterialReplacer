using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anosion.MaterialReplacer
{
    public class MaterialReplacerWindow : EditorWindow
    {
        private (string Name, MaterialReplacementView View)[] tabs;
        private MaterialReplacementView[] views;
        private string[] tabNames;
        private int selectedTab = 0;

        [MenuItem("Window/Material Replacer")]
        public static void ShowWindow()
        {
            GetWindow<MaterialReplacerWindow>("Material Replacer");
        }

        private void OnEnable()
        {
            if (tabs == null)
            {
                tabs = new (string Name, MaterialReplacementView View)[]
                {
                    ("アバター単位", new AvatarReplacementView()),
                    ("マテリアル単位", new SceneWideMaterialReplacementView())
                };
                views = tabs.Select(tab => tab.View).ToArray();
                tabNames = tabs.Select(tab => tab.Name).ToArray();
            }

            foreach (var view in views)
            {
                view.OnEnable();
            }
        }

        private void OnDisable()
        {
            foreach (var view in views)
            {
                view.OnDisable();
            }
        }

        private void OnGUI()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            tabs[selectedTab].View.OnGUI();
        }
    }
}
