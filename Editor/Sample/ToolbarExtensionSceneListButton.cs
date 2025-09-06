using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor.Sample
{
    public class ToolbarExtensionSceneListButton : IToolbarElementRegister
    {
        public ToolbarElementLayoutType LayoutType => ToolbarElementLayoutType.RightSideRightAlign;

        public VisualElement CreateElement()
        {
            var button = new EditorToolbarButton(OpenSceneListMenu);
            button.name = "SceneListButton";
            button.style.width = 40;

            var image = new Image();
            image.image = EditorGUIUtility.IconContent("d_SceneAsset Icon").image;
            button.Add(image);

            var arrow = new VisualElement();
            arrow.AddToClassList("unity-icon-arrow");
            button.Add(arrow);

            return button;
        }

        private static void OpenSceneListMenu()
        {
            var menu = new GenericMenu();
            
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            var scenePaths = sceneGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(Path.GetFileNameWithoutExtension)
                .ToArray();

            if (scenePaths.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes found"));
            }
            else
            {
                var displayNames = GenerateUniqueDisplayNames(scenePaths);
                
                for (var i = 0; i < scenePaths.Length; i++)
                {
                    var scenePath = scenePaths[i];
                    var displayName = displayNames[i];
                    
                    menu.AddItem(new GUIContent(displayName), false, () => 
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            EditorSceneManager.OpenScene(scenePath);
                        }
                    });
                }
            }

            menu.ShowAsContext();
        }

        private static string[] GenerateUniqueDisplayNames(string[] scenePaths)
        {
            var displayNames = new string[scenePaths.Length];
            
            // まずはシンプルなファイル名で初期化
            for (var i = 0; i < scenePaths.Length; i++)
            {
                displayNames[i] = Path.GetFileNameWithoutExtension(scenePaths[i]);
            }
            
            // 重複するシーン名を見つけて、区別できるまでパスを遡る
            var groups = scenePaths
                .Select((path, index) => new { Path = path, Index = index, FileName = Path.GetFileNameWithoutExtension(path) })
                .GroupBy(x => x.FileName)
                .Where(g => g.Count() > 1)
                .ToArray();

            foreach (var group in groups)
            {
                var pathsInGroup = group.Select(x => x.Path).ToArray();
                var indicesInGroup = group.Select(x => x.Index).ToArray();
                
                // 区別できる最小のパス深度を見つける
                var uniqueParts = FindUniquePathParts(pathsInGroup);
                for (var i = 0; i < indicesInGroup.Length; i++)
                {
                    displayNames[indicesInGroup[i]] = uniqueParts[i];
                }
            }
            
            return displayNames;
        }

        private static string[] FindUniquePathParts(string[] paths)
        {
            var result = new string[paths.Length];
            
            for (var i = 0; i < paths.Length; i++)
            {
                var currentPath = paths[i].Replace("Assets/", "");
                var pathParts = currentPath.Split('/');
                var fileName = Path.GetFileNameWithoutExtension(pathParts[^1]);
                
                // 最低でもファイル名は含める
                var displayParts = new[] { fileName };
                
                // 他のパスと区別できるまで親ディレクトリを追加
                for (var depth = 1; depth < pathParts.Length - 1; depth++)
                {
                    var parentDir = pathParts[pathParts.Length - 1 - depth];
                    displayParts = new[] { parentDir }.Concat(displayParts).ToArray();
                    var currentDisplay = string.Join("/", displayParts);
                    
                    // 他のパスと区別できるかチェック
                    var isUnique = true;
                    for (var j = 0; j < paths.Length; j++)
                    {
                        if (i == j) continue;
                        
                        var otherPath = paths[j].Replace("Assets/", "");
                        var otherParts = otherPath.Split('/');
                        var otherFileName = Path.GetFileNameWithoutExtension(otherParts[^1]);
                        
                        // 同じ深度での比較
                        var otherDisplayParts = new[] { otherFileName };
                        for (var d = 1; d <= depth && d < otherParts.Length - 1; d++)
                        {
                            var otherParentDir = otherParts[otherParts.Length - 1 - d];
                            otherDisplayParts = new[] { otherParentDir }.Concat(otherDisplayParts).ToArray();
                        }
                        var otherDisplay = string.Join("/", otherDisplayParts);
                        
                        if (currentDisplay == otherDisplay)
                        {
                            isUnique = false;
                            break;
                        }
                    }
                    
                    if (isUnique)
                    {
                        break;
                    }
                }
                
                result[i] = string.Join("\u29F8", displayParts);
            }
            
            return result;
        }
    }
}