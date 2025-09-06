using System;
using System.Collections.Generic;
using System.Linq;
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
        private SerializedObject _serializedSettings;
        private readonly Dictionary<ToolbarElementLayoutType, ReorderableList> _reorderableLists = new Dictionary<ToolbarElementLayoutType, ReorderableList>();
        private bool _needsRefresh;

        private ToolbarExtensionSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope)
        {
            keywords = new[] { "toolbar", "extension" };
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings = GetOrCreateSettings();
            _serializedSettings = new SerializedObject(_settings);
        }

        public override void OnGUI(string searchContext)
        {
            if (_settings == null || _serializedSettings == null)
            {
                EditorGUILayout.HelpBox("Settings could not be loaded.", MessageType.Error);
                return;
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Enable/disable toolbar elements and drag to reorder them within the same layout type.",
                MessageType.Info);
            
            EditorGUILayout.Space();

            // 利用可能な要素タイプを取得して設定を更新
            var availableTypes = GetAvailableToolbarElementTypes();
            _settings.SetAvailableTypes(availableTypes);
            _settings.UpdateElementSettings(availableTypes);

            _serializedSettings.Update();

            EditorGUI.BeginChangeCheck();

            // LayoutType別にグループ化して表示
            var layoutTypes = System.Enum.GetValues(typeof(ToolbarElementLayoutType)).Cast<ToolbarElementLayoutType>();
            
            foreach (var layoutType in layoutTypes)
            {
                var settingsForLayout = _settings.GetSettingsForLayoutType(layoutType);
                if (settingsForLayout.Count == 0) continue;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(GetLayoutTypeName(layoutType), EditorStyles.boldLabel);
                
                DrawReorderableElementList(layoutType, settingsForLayout, availableTypes);
            }

            if (EditorGUI.EndChangeCheck() || _needsRefresh)
            {
                EditorUtility.SetDirty(_settings);
                _serializedSettings.ApplyModifiedProperties();
                // 設定変更時にツールバーを即座に更新
                ToolbarExtension.ForceRefresh();
                _needsRefresh = false;
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
                if (index >= settings.Count) return;
                
                var setting = settings[index];
                var elementType = availableTypes.FirstOrDefault(t => t.FullName == setting.TypeName);
                
                // 有効/無効トグル
                var toggleRect = new Rect(rect.x, rect.y + 1, 20, EditorGUIUtility.singleLineHeight);
                var newEnabled = EditorGUI.Toggle(toggleRect, setting.IsEnabled);
                if (newEnabled != setting.IsEnabled && elementType != null)
                {
                    _settings.SetElementEnabled(elementType, newEnabled);
                    _needsRefresh = true;
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
                _settings.ReorderElements(layoutType, settings);
                _needsRefresh = true;
            };
            
            reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 2;
            
            return reorderableList;
        }

        private static ToolbarExtensionSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ToolbarExtensionSettings>(ToolbarExtensionSettings.SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ToolbarExtensionSettings>();
                
                // Editorフォルダを作成（存在しない場合）
                var editorFolderPath = "Assets/Editor";
                if (!AssetDatabase.IsValidFolder(editorFolderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Editor");
                }
                
                AssetDatabase.CreateAsset(settings, ToolbarExtensionSettings.SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        private static List<Type> GetAvailableToolbarElementTypes()
        {
            var interfaceType = typeof(IToolbarElementRegister);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (System.Reflection.ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(t => t != null);
                    }
                })
                .Where(t => t != null && interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new ToolbarExtensionSettingsProvider(SettingsPath);
        }
    }
}