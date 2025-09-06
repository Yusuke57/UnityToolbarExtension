using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YujiAp.UnityToolbarExtension.Editor
{
    [Serializable]
    public class ToolbarExtensionSettings : ScriptableObject
    {
        public const string SettingsPath = "Assets/Editor/ToolbarExtensionSettings.asset";
        
        [SerializeField] private List<ToolbarElementSetting> elementSettings = new();

        public IReadOnlyList<ToolbarElementSetting> ElementSettings => elementSettings;

        public bool IsElementEnabled(Type elementType)
        {
            var setting = elementSettings.FirstOrDefault(s => s.TypeName == elementType.FullName);
            return setting?.IsEnabled ?? true; // デフォルトは有効
        }

        public void SetElementEnabled(Type elementType, bool enabled)
        {
            var setting = elementSettings.FirstOrDefault(s => s.TypeName == elementType.FullName);
            if (setting == null)
            {
                setting = new ToolbarElementSetting(elementType.FullName, GetDisplayName(elementType), enabled);
                elementSettings.Add(setting);
            }
            else
            {
                setting.SetEnabled(enabled);
            }
        }

        private string GetDisplayName(Type type)
        {
            // クラス名からToolbarExtensionプレフィックスを除去
            const string prefix = "ToolbarExtension";
            var displayName = type.Name;
            if (displayName.StartsWith(prefix))
            {
                displayName = displayName[prefix.Length..];
            }

            return displayName;
        }

        public void UpdateElementSettings(List<Type> availableTypes)
        {
            // 新しい要素を追加
            foreach (var type in availableTypes)
            {
                if (elementSettings.All(s => s.TypeName != type.FullName))
                {
                    var setting = new ToolbarElementSetting(type.FullName, GetDisplayName(type), true);
                    elementSettings.Add(setting);
                }
            }

            // 存在しない要素を削除
            var availableTypeNames = availableTypes.Select(t => t.FullName).ToHashSet();
            elementSettings.RemoveAll(s => !availableTypeNames.Contains(s.TypeName));
        }
    }

    [Serializable]
    public class ToolbarElementSetting
    {
        [SerializeField] private string _typeName;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isEnabled = true;
        
        public string TypeName => _typeName;
        public string DisplayName => _displayName;
        public bool IsEnabled => _isEnabled;

        public ToolbarElementSetting(string typeName, string displayName, bool isEnabled)
        {
            _typeName = typeName;
            _displayName = displayName;
            _isEnabled = isEnabled;
        }
        
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }
    }
}