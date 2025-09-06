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

        private ToolbarElementSettingsData _settingsData = new();
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
            var setting = _settingsData.ElementSettings.FirstOrDefault(s => s.TypeName == elementType.FullName);
            if (setting == null)
            {
                var originalLayoutType = GetLayoutType(elementType);
                setting = new ToolbarElementSetting(elementType.FullName, GetDisplayName(elementType), enabled, 0, originalLayoutType);
                _settingsData.ElementSettings.Add(setting);
            }
            else
            {
                setting.SetEnabled(enabled);
            }

            _isDirty = true;
        }

        public void SetElementLayoutType(Type elementType, ToolbarElementLayoutType layoutType)
        {
            var setting = _settingsData.ElementSettings.FirstOrDefault(s => s.TypeName == elementType.FullName);
            if (setting != null)
            {
                    setting.SetLayoutType(layoutType);
                _isDirty = true;
            }
            else
            {
                Debug.LogError($"SetElementLayoutType: Setting not found for {elementType.FullName}");
            }
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

            // 新しい要素を追加（保存済み設定がない場合のみDefaultLayoutTypeを使用）
            foreach (var type in newAvailableTypes)
            {
                var existingSetting = _settingsData.ElementSettings.FirstOrDefault(s => s.TypeName == type.FullName);
                if (existingSetting == null)
                {
                    var nextOrder = _settingsData.ElementSettings.Count > 0 ? _settingsData.ElementSettings.Max(s => s.Order) + 1 : 0;
                    var defaultLayoutType = GetLayoutType(type);
                    var setting = new ToolbarElementSetting(type.FullName, GetDisplayName(type), true, nextOrder, defaultLayoutType);
                    _settingsData.ElementSettings.Add(setting);
                    hasChanges = true;
                }
                // 既存の設定は一切変更しない（EditorPrefsに保存済みの設定を保持）
            }

            // 存在しない要素を削除
            var availableTypeNames = newAvailableTypes.Select(t => t.FullName).ToHashSet();
            var removedCount = _settingsData.ElementSettings.RemoveAll(s => !availableTypeNames.Contains(s.TypeName));
            if (removedCount > 0) hasChanges = true;

            if (hasChanges)
            {
                _isDirty = true;
            }
        }
        
        /// <summary>
        /// 既存の設定を全てDefaultLayoutTypeにリセット（開発・テスト用）
        /// </summary>
        public void ResetAllLayoutTypesToDefault(List<Type> availableTypes)
        {
            var hasChanges = false;
            
            foreach (var setting in _settingsData.ElementSettings)
            {
                var type = availableTypes.FirstOrDefault(t => t.FullName == setting.TypeName);
                if (type != null)
                {
                    var defaultLayoutType = GetLayoutType(type);
                    if (setting.LayoutType != defaultLayoutType)
                    {
                        setting.SetLayoutType(defaultLayoutType);
                        hasChanges = true;
                    }
                }
            }
            
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
                var originalSetting = _settingsData.ElementSettings.FirstOrDefault(s => s.TypeName == setting.TypeName);
                originalSetting?.SetOrder(i);
            }

            _isDirty = true;
        }

        private static ToolbarElementLayoutType GetLayoutType(Type type)
        {
            if (Activator.CreateInstance(type) is IToolbarElementRegister register)
            {
                return register.DefaultLayoutType;
            }

            return ToolbarElementLayoutType.LeftSideLeftAlign;
        }

        public List<ToolbarElementSetting> GetSettingsForLayoutType(ToolbarElementLayoutType layoutType)
        {
            // 元のオブジェクトへの参照を保持するため、新しいListは作らずフィルタリングのみ
            var filteredSettings = new List<ToolbarElementSetting>();
            foreach (var setting in _settingsData.ElementSettings.Where(s => s.LayoutType == layoutType).OrderBy(s => s.Order))
            {
                filteredSettings.Add(setting); // 同じオブジェクト参照を追加
            }
            return filteredSettings;
        }

        private void LoadSettings()
        {
            _settingsData.ElementSettings.Clear();

            if (EditorPrefs.HasKey(PrefsKey))
            {
                var json = EditorPrefs.GetString(PrefsKey);
                try
                {
                    _settingsData = JsonUtility.FromJson<ToolbarElementSettingsData>(json);
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

            try
            {
                var json = JsonUtility.ToJson(_settingsData, true);
                EditorPrefs.SetString(PrefsKey, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save ToolbarExtension settings to JSON: {e.Message}");
            }

            _isDirty = false;
        }
        
        public void SetSettingsDirty()
        {
            _isDirty = true;
        }
    }

    [Serializable]
    public class ToolbarElementSetting
    {
        [SerializeField] private string _typeName;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isEnabled;
        [SerializeField] private int _order;
        [SerializeField] private ToolbarElementLayoutType _layoutType;

        public string TypeName => _typeName;
        public string DisplayName => _displayName;
        public bool IsEnabled => _isEnabled;
        public int Order => _order;
        public ToolbarElementLayoutType LayoutType => _layoutType;

        public ToolbarElementSetting(string typeName, string displayName, bool isEnabled, int order = 0, ToolbarElementLayoutType layoutType = ToolbarElementLayoutType.LeftSideLeftAlign)
        {
            _typeName = typeName;
            _displayName = displayName;
            _isEnabled = isEnabled;
            _order = order;
            _layoutType = layoutType;
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        public void SetOrder(int order)
        {
            _order = order;
        }

        public void SetLayoutType(ToolbarElementLayoutType layoutType)
        {
            _layoutType = layoutType;
        }
    }
}