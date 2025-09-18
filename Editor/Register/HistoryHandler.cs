using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YujiAp.UnityToolbarExtension.Editor.Register
{
    public class HistoryHandler
    {
        private readonly List<string> _history;
        private readonly string _key;

        private const string HistorySeparator = ",";
        private const int MaxHistoryCount = 30;

        public IReadOnlyList<string> History => _history;
        private string SaveKey => $"{Application.dataPath}.{nameof(HistoryHandler)}.{_key}";

        public HistoryHandler(string key)
        {
            _key = key;

            var saveData = EditorPrefs.GetString(SaveKey, null);
            _history = string.IsNullOrEmpty(saveData)
                ? new List<string>()
                : saveData.Split(new[] { HistorySeparator }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private void Save()
        {
            var data = string.Join(HistorySeparator, _history);
            EditorPrefs.SetString(SaveKey, data);
        }

        public void AddHistory(string value)
        {
            if (_history.Contains(value))
            {
                _history.Remove(value);
            }

            if (_history.Count >= MaxHistoryCount)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            _history.Insert(0, value);
            Save();
        }

        public void RemoveHistories(List<string> values)
        {
            foreach (var value in values)
            {
                _history.Remove(value);
            }

            Save();
        }

        public void ClearHistory()
        {
            _history.Clear();
            Save();
        }
    }
}