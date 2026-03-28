using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor.Register
{
    public class ToolbarExtensionPrefabHistoryButton : IToolbarElement
    {
        private static readonly HistoryHandler _historyHandler = new("Prefab");

        private const string ClearHistoryText = "Clear History";

        public ToolbarElementLayoutType DefaultLayoutType => ToolbarElementLayoutType.RightSideRightAlign;

        public VisualElement CreateElement()
        {
            PrefabStage.prefabStageOpened -= AddHistory;
            PrefabStage.prefabStageOpened += AddHistory;

            var dropdown = new EditorToolbarDropdown();
            dropdown.name = "PrefabHistoryButton";
            dropdown.tooltip = "Open prefab from history";
            dropdown.icon = (Texture2D) EditorGUIUtility.IconContent("d_Prefab Icon").image;
            dropdown.clicked += OpenPrefabHistoryMenu;

            return dropdown;

            void AddHistory(PrefabStage prefabStage)
            {
                _historyHandler.AddHistory(prefabStage.assetPath);
            }
        }

        private static void OpenPrefabHistoryMenu()
        {
            var menu = new GenericMenu();
            var prefabHistory = _historyHandler.History;
            foreach (var prefabPath in prefabHistory)
            {
                var prefabAssetName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                menu.AddItem(new GUIContent(prefabAssetName), false, () => PrefabStageUtility.OpenPrefab(prefabPath));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent(ClearHistoryText), false, () => _historyHandler.ClearHistory());

            menu.ShowAsContext();
        }
    }
}