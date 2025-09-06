using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor.Register
{
    public class ToolbarExtensionRestartSceneButton : IToolbarElement
    {
        public ToolbarElementLayoutType DefaultLayoutType => ToolbarElementLayoutType.LeftSideRightAlign;

        public VisualElement CreateElement()
        {
            var button = new EditorToolbarButton(ReloadScene);
            button.name = nameof(ReloadScene);
            button.icon = (Texture2D) EditorGUIUtility.IconContent("d_preAudioAutoPlayOff").image;
            button.SetEnabled(EditorApplication.isPlaying);

            EditorApplication.playModeStateChanged += state =>
            {
                switch (state)
                {
                    case PlayModeStateChange.EnteredPlayMode:
                        button.SetEnabled(true);
                        break;
                    case PlayModeStateChange.EnteredEditMode:
                        button.SetEnabled(false);
                        break;
                }
            };

            return button;
        }

        private static void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}