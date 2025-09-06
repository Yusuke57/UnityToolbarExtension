using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor
{
    public class ToolbarExtensionSettingsProvider : SettingsProvider
    {
        private const string SettingsPath = "Project/Unity Toolbar Extension";
        private ToolbarExtensionSettings _settings;
        private readonly Dictionary<ToolbarElementLayoutType, ReorderableList> _reorderableLists = new();
        private List<Type> _cachedAvailableTypes;
        private bool _isInitialized;

        private ToolbarExtensionSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope)
        {
            keywords = new[] { "toolbar", "extension" };
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings = ToolbarExtensionSettings.Instance;
        }

        public override void OnGUI(string searchContext)
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("Settings could not be loaded.", MessageType.Error);
                return;
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Enable/disable toolbar elements and drag to reorder them within the same layout type.",
                MessageType.Info);
            
            EditorGUILayout.Space();

            // 初期化時のみ要素タイプを取得して設定を更新
            if (!_isInitialized)
            {
                _cachedAvailableTypes = GetAvailableToolbarElementTypes();
                _settings.SetAvailableTypes(_cachedAvailableTypes);
                _settings.UpdateElementSettings(_cachedAvailableTypes);
                _isInitialized = true;
            }

            EditorGUI.BeginChangeCheck();

            // LayoutType別にグループ化して表示
            var layoutTypes = (ToolbarElementLayoutType[])Enum.GetValues(typeof(ToolbarElementLayoutType));
            
            foreach (var layoutType in layoutTypes)
            {
                var settingsForLayout = _settings.GetSettingsForLayoutType(layoutType);
                if (settingsForLayout.Count == 0) continue;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(GetLayoutTypeName(layoutType), EditorStyles.boldLabel);
                
                DrawReorderableElementList(layoutType, settingsForLayout, _cachedAvailableTypes);
            }

            if (EditorGUI.EndChangeCheck())
            {
                // 設定を保存
                _settings.SaveSettingsIfDirty();
            }
        }

        private static string GetLayoutTypeName(ToolbarElementLayoutType layoutType)
        {
            return layoutType switch
            {
                ToolbarElementLayoutType.LeftSideLeftAlign => "Left Side - Left Align",
                ToolbarElementLayoutType.LeftSideRightAlign => "Left Side - Right Align", 
                ToolbarElementLayoutType.RightSideLeftAlign => "Right Side - Left Align",
                ToolbarElementLayoutType.RightSideRightAlign => "Right Side - Right Align",
                _ => layoutType.ToString()
            };
        }

        private void DrawReorderableElementList(ToolbarElementLayoutType layoutType, List<ToolbarElementSetting> settings, List<Type> availableTypes)
        {
            if (!_reorderableLists.TryGetValue(layoutType, out var reorderableList))
            {
                reorderableList = CreateReorderableList(layoutType, settings, availableTypes);
                _reorderableLists[layoutType] = reorderableList;
            }
            
            // リストの内容が変更されている場合は再作成
            if (reorderableList.count != settings.Count)
            {
                reorderableList = CreateReorderableList(layoutType, settings, availableTypes);
                _reorderableLists[layoutType] = reorderableList;
            }
            else
            {
                // 既存のリストを更新
                reorderableList.list = settings;
            }
            
            reorderableList.DoLayoutList();
        }

        private ReorderableList CreateReorderableList(ToolbarElementLayoutType layoutType, List<ToolbarElementSetting> settings, List<Type> availableTypes)
        {
            var reorderableList = new ReorderableList(settings, typeof(ToolbarElementSetting), true, false, false, false);
            
            reorderableList.drawElementCallback = (rect, index, _, _) =>
            {
                if (index >= settings.Count)
                {
                    return;
                }
                
                var setting = settings[index];
                var elementType = availableTypes.FirstOrDefault(t => t.FullName == setting.TypeName);
                
                // 有効/無効トグル
                var toggleRect = new Rect(rect.x, rect.y + 1, 20, EditorGUIUtility.singleLineHeight);
                var newEnabled = EditorGUI.Toggle(toggleRect, setting.IsEnabled);
                if (newEnabled != setting.IsEnabled && elementType != null)
                {
                    _settings.SetElementEnabled(elementType, newEnabled);
                    // 遅延でツールバー更新
                    EditorApplication.delayCall += ToolbarExtension.ForceRefresh;
                }
                
                // 要素名
                var labelRect = new Rect(rect.x + 25, rect.y + 1, rect.width - 25, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, setting.DisplayName);
            };
            
            reorderableList.onReorderCallbackWithDetails = (_, oldIndex, newIndex) =>
            {
                // 並び順を更新
                var setting = settings[oldIndex];
                settings.RemoveAt(oldIndex);
                settings.Insert(newIndex, setting);
                _settings.ReorderElements(settings);
                
                // 遅延でツールバー更新
                EditorApplication.delayCall += ToolbarExtension.ForceRefresh;
            };

            reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 2;
            
            return reorderableList;
        }


        private static List<Type> GetAvailableToolbarElementTypes()
        {
            return GetTypesImplementingInterface<IToolbarElementRegister>();
        }
        
        private static List<Type> GetTypesImplementingInterface<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(GetAssemblyTypes)
                .Where(t => t != null && interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();
        }
        
        private static IEnumerable<Type> GetAssemblyTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new ToolbarExtensionSettingsProvider(SettingsPath);
        }
    }
}