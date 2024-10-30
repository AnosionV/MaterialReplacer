using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Anosion.MaterialReplacer
{
    public class MaterialReplacerWindow : EditorWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private AvatarReplacementView avatarReplacementView;

        [MenuItem("Window/Material Replacer")]
        public static void ShowWindow()
        {
            GetWindow<MaterialReplacerWindow>("Material Replacer");
        }

        private void OnEnable()
        {
            avatarReplacementView = new AvatarReplacementView();
            avatarReplacementView.OnEnable();
        }

        private void OnDisable()
        {
            avatarReplacementView.OnDisable();
        }

        private void OnGUI()
        {
            avatarReplacementView.OnGUI();
        }
    }
}
