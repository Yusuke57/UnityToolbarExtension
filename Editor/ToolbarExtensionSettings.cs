using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YujiAp.UnityToolbarExtension.Editor
{
    [Serializable]
    public class ToolbarElementSettingsData
    {
        [SerializeField] private List<ToolbarElementSetting> _elementSettings = new();
        
        public List<ToolbarElementSetting> ElementSettings => _elementSettings;
    }
    
    public class ToolbarExtensionSettings
    {
        private const string PrefsKey = "ToolbarExtensionSettings";
        
        private readonly List<ToolbarElementSetting> _elementSettings = new();
        private static ToolbarExtensionSettings _instance;
        private bool _isDirty;

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

        public void SetElementEnabled(Type elementType, bool enabled)
        {
            var setting = _elementSettings.FirstOrDefault(s => s.TypeName == elementType.FullName);
            if (setting == null)
            {
                setting = new ToolbarElementSetting(elementType.FullName, GetDisplayName(elementType), enabled);
                _elementSettings.Add(setting);
            }
            else
            {
                setting.SetEnabled(enabled);
            }
            _isDirty = true;
        }

        private static string GetDisplayName(Type type)
        {
            return RemoveToolbarExtensionPrefix(type.Name);
        }
        
        private static string RemoveToolbarExtensionPrefix(string name)
        {
            const string prefix = "ToolbarExtension";
            return name.StartsWith(prefix) ? name[prefix.Length..] : name;
        }

        public void UpdateElementSettings(List<Type> newAvailableTypes)
        {
            var hasChanges = false;
            
            // 新しい要素を追加
            foreach (var type in newAvailableTypes)
            {
                if (_elementSettings.All(s => s.TypeName != type.FullName))
                {
                    var nextOrder = _elementSettings.Count > 0 ? _elementSettings.Max(s => s.Order) + 1 : 0;
                    var setting = new ToolbarElementSetting(type.FullName, GetDisplayName(type), true, nextOrder);
                    _elementSettings.Add(setting);
                    hasChanges = true;
                }
            }

            // 存在しない要素を削除
            var availableTypeNames = newAvailableTypes.Select(t => t.FullName).ToHashSet();
            var removedCount = _elementSettings.RemoveAll(s => !availableTypeNames.Contains(s.TypeName));
            if (removedCount > 0) hasChanges = true;
            
            if (hasChanges)
            {
                _isDirty = true;
            }
        }

        public void ReorderElements(List<ToolbarElementSetting> reorderedSettings)
        {
            // ReorderableListで並び替えられた順序で Order を更新
            for (var i = 0; i < reorderedSettings.Count; i++)
            {
                var setting = reorderedSettings[i];
                var originalSetting = _elementSettings.FirstOrDefault(s => s.TypeName == setting.TypeName);
                originalSetting?.SetOrder(i);
            }
            _isDirty = true;
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
            return _elementSettings
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
            _elementSettings.Clear();
            
            if (EditorPrefs.HasKey(PrefsKey))
            {
                var json = EditorPrefs.GetString(PrefsKey);
                try
                {
                    var data = JsonUtility.FromJson<ToolbarElementSettingsData>(json);
                    _elementSettings.AddRange(data.ElementSettings);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load ToolbarExtension settings from JSON: {e.Message}");
                }
            }
        }

        public void SaveSettingsIfDirty()
        {
            if (!_isDirty)
            {
                return;
            }
            
            var data = new ToolbarElementSettingsData();
            data.ElementSettings.AddRange(_elementSettings);
            
            try
            {
                var json = JsonUtility.ToJson(data, true);
                EditorPrefs.SetString(PrefsKey, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save ToolbarExtension settings to JSON: {e.Message}");
            }
            
            _isDirty = false;
        }
    }

    [Serializable]
    public class ToolbarElementSetting
    {
        [SerializeField] private string _typeName;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isEnabled;
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