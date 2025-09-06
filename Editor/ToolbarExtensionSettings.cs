using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YujiAp.UnityToolbarExtension.Editor
{
    public class ToolbarExtensionSettings
    {
        private const string PrefsKeyPrefix = "ToolbarExtension_";
        private const string ElementCountKey = PrefsKeyPrefix + "ElementCount";
        
        private List<ToolbarElementSetting> elementSettings = new List<ToolbarElementSetting>();
        private static ToolbarExtensionSettings _instance;

        public static ToolbarExtensionSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ToolbarExtensionSettings();
                    _instance.LoadSettings();
                }
                return _instance;
            }
        }

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
            SaveSettings();
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

        public void UpdateElementSettings(List<Type> newAvailableTypes)
        {
            // 新しい要素を追加
            foreach (var type in newAvailableTypes)
            {
                if (elementSettings.All(s => s.TypeName != type.FullName))
                {
                    var nextOrder = elementSettings.Count > 0 ? elementSettings.Max(s => s.Order) + 1 : 0;
                    var setting = new ToolbarElementSetting(type.FullName, GetDisplayName(type), true, nextOrder);
                    elementSettings.Add(setting);
                }
            }

            // 存在しない要素を削除
            var availableTypeNames = newAvailableTypes.Select(t => t.FullName).ToHashSet();
            elementSettings.RemoveAll(s => !availableTypeNames.Contains(s.TypeName));
            
            SaveSettings();
        }

        public void ReorderElements(ToolbarElementLayoutType layoutType, List<ToolbarElementSetting> reorderedSettings)
        {
            // ReorderableListで並び替えられた順序で Order を更新
            for (int i = 0; i < reorderedSettings.Count; i++)
            {
                var setting = reorderedSettings[i];
                var originalSetting = elementSettings.FirstOrDefault(s => s.TypeName == setting.TypeName);
                if (originalSetting != null)
                {
                    originalSetting.SetOrder(i);
                }
            }
            SaveSettings();
        }

        private static List<Type> availableTypes;
        
        public void SetAvailableTypes(List<Type> types)
        {
            availableTypes = types;
        }

        private static ToolbarElementLayoutType GetLayoutType(Type type)
        {
            if (Activator.CreateInstance(type) is IToolbarElementRegister register)
            {
                return register.LayoutType;
            }
            return ToolbarElementLayoutType.LeftSideLeftAlign;
        }

        public List<ToolbarElementSetting> GetSettingsForLayoutType(ToolbarElementLayoutType layoutType)
        {
            return elementSettings
                .Where(s =>
                {
                    var type = availableTypes?.FirstOrDefault(t => t.FullName == s.TypeName);
                    return type != null && GetLayoutType(type) == layoutType;
                })
                .OrderBy(s => s.Order)
                .ToList();
        }

        private void LoadSettings()
        {
            var count = EditorPrefs.GetInt(ElementCountKey, 0);
            elementSettings.Clear();
            
            for (int i = 0; i < count; i++)
            {
                var typeNameKey = PrefsKeyPrefix + "TypeName_" + i;
                var displayNameKey = PrefsKeyPrefix + "DisplayName_" + i;
                var isEnabledKey = PrefsKeyPrefix + "IsEnabled_" + i;
                var orderKey = PrefsKeyPrefix + "Order_" + i;
                
                if (EditorPrefs.HasKey(typeNameKey))
                {
                    var typeName = EditorPrefs.GetString(typeNameKey);
                    var displayName = EditorPrefs.GetString(displayNameKey);
                    var isEnabled = EditorPrefs.GetBool(isEnabledKey, true);
                    var order = EditorPrefs.GetInt(orderKey, 0);
                    
                    elementSettings.Add(new ToolbarElementSetting(typeName, displayName, isEnabled, order));
                }
            }
        }

        private void SaveSettings()
        {
            EditorPrefs.SetInt(ElementCountKey, elementSettings.Count);
            
            for (int i = 0; i < elementSettings.Count; i++)
            {
                var setting = elementSettings[i];
                var typeNameKey = PrefsKeyPrefix + "TypeName_" + i;
                var displayNameKey = PrefsKeyPrefix + "DisplayName_" + i;
                var isEnabledKey = PrefsKeyPrefix + "IsEnabled_" + i;
                var orderKey = PrefsKeyPrefix + "Order_" + i;
                
                EditorPrefs.SetString(typeNameKey, setting.TypeName);
                EditorPrefs.SetString(displayNameKey, setting.DisplayName);
                EditorPrefs.SetBool(isEnabledKey, setting.IsEnabled);
                EditorPrefs.SetInt(orderKey, setting.Order);
            }
        }
    }

    [Serializable]
    public class ToolbarElementSetting
    {
        [SerializeField] private string _typeName;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isEnabled = true;
        [SerializeField] private int _order;
        
        public string TypeName => _typeName;
        public string DisplayName => _displayName;
        public bool IsEnabled => _isEnabled;
        public int Order => _order;

        public ToolbarElementSetting(string typeName, string displayName, bool isEnabled, int order = 0)
        {
            _typeName = typeName;
            _displayName = displayName;
            _isEnabled = isEnabled;
            _order = order;
        }
        
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }
        
        public void SetOrder(int order)
        {
            _order = order;
        }
    }
}