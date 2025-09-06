using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor
{
    public class ToolbarExtensionSettingsProvider : SettingsProvider
    {
        private const string SettingsPath = "Project/Unity Toolbar Extension";
        private ToolbarExtensionSettings _settings;
        private SerializedObject _serializedSettings;

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
                "Enable or disable individual toolbar elements.",
                MessageType.Info);
            
            EditorGUILayout.Space();

            // 利用可能な要素タイプを取得して設定を更新
            var availableTypes = GetAvailableToolbarElementTypes();
            _settings.UpdateElementSettings(availableTypes);

            _serializedSettings.Update();

            EditorGUI.BeginChangeCheck();

            // 各要素の有効/無効設定を表示
            foreach (var elementSetting in _settings.ElementSettings)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    var newEnabled = EditorGUILayout.Toggle(elementSetting.IsEnabled, GUILayout.Width(20));
                    EditorGUILayout.LabelField(elementSetting.DisplayName);
                    
                    if (newEnabled != elementSetting.IsEnabled)
                    {
                        var elementType = availableTypes.FirstOrDefault(t => t.FullName == elementSetting.TypeName);
                        if (elementType != null)
                        {
                            _settings.SetElementEnabled(elementType, newEnabled);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settings);
                _serializedSettings.ApplyModifiedProperties();
                // 設定変更時にツールバーを即座に更新
                ToolbarExtension.ForceRefresh();
            }
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