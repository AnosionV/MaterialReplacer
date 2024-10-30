using UnityEditor;
using UnityEngine;

namespace Anosion.MaterialReplacer
{
    public class MaterialReplacerWindow : EditorWindow
    {
        private IMaterialReplacementView[] views;
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Avatar Replacement" };

        [MenuItem("Window/Material Replacer")]
        public static void ShowWindow()
        {
            GetWindow<MaterialReplacerWindow>("Material Replacer");
        }

        private void OnEnable()
        {
            views = new IMaterialReplacementView[] { new AvatarReplacementView() };
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
            views[selectedTab].OnGUI();
        }
    }
}
