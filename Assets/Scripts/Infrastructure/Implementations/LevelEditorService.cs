using System;
using System.Collections.Generic;
using System.IO;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public sealed class LevelEditorService : ILevelEditorService
    {
        private const string LogTag = "[LevelEditor]";
        private const string SaveDir = "CustomLevels";

        private readonly List<string> _savedNames = new List<string>();
        private EditorLevelData _currentEdit;
        private string _savePath;

        public IReadOnlyList<string> SavedLevelNames => _savedNames.AsReadOnly();
        public EditorLevelData CurrentEdit => _currentEdit;
        public bool HasActiveEdit => _currentEdit != null;

        public LevelEditorService()
        {
            _savePath = Path.Combine(UnityEngine.Application.persistentDataPath, SaveDir);
            try
            {
                if (!Directory.Exists(_savePath))
                    Directory.CreateDirectory(_savePath);
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"{LogTag} Failed to create save directory: {ex.Message}");
                _savePath = Path.Combine(UnityEngine.Application.temporaryCachePath, SaveDir);
                Directory.CreateDirectory(_savePath);
            }
            RefreshSavedLevels();
        }

        public void CreateNewLevel(string name, int moldCount, int colorCount, int emptyMolds)
        {
            _currentEdit = new EditorLevelData
            {
                levelName = SanitizeName(name),
                moldCount = Mathf.Clamp(moldCount, 3, 20),
                colorCount = Mathf.Clamp(colorCount, 2, 10),
                emptyMoldCount = Mathf.Clamp(emptyMolds, 1, 5),
                createdAt = DateTimeOffset.UtcNow.ToString("o")
            };

            // Create initial empty molds
            _currentEdit.molds.Clear();
            for (int i = 0; i < _currentEdit.moldCount; i++)
            {
                _currentEdit.molds.Add(new EditorMoldData { isEmpty = i < emptyMolds });
            }

            MoldLogger.LogInfo($"{LogTag} Created new level: {name} ({moldCount} molds, {colorCount} colors)");
        }

        public bool LoadLevel(string levelName)
        {
            var path = GetLevelPath(levelName);
            if (!File.Exists(path))
            {
                MoldLogger.LogWarning($"{LogTag} Level not found: {levelName}");
                return false;
            }

            try
            {
                var json = File.ReadAllText(path);
                _currentEdit = JsonUtility.FromJson<EditorLevelData>(json);
                MoldLogger.LogInfo($"{LogTag} Loaded level: {levelName}");
                return _currentEdit != null;
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"{LogTag} Failed to load level: {ex.Message}");
                return false;
            }
        }

        public void SaveCurrentLevel()
        {
            if (_currentEdit == null)
            {
                MoldLogger.LogWarning($"{LogTag} No active edit to save.");
                return;
            }

            try
            {
                if (!Directory.Exists(_savePath))
                    Directory.CreateDirectory(_savePath);

                _currentEdit.createdAt = DateTimeOffset.UtcNow.ToString("o");
                var json = JsonUtility.ToJson(_currentEdit, true);
                var path = GetLevelPath(_currentEdit.levelName);
                File.WriteAllText(path, json);
                MoldLogger.LogInfo($"{LogTag} Saved level: {_currentEdit.levelName}");

                if (!_savedNames.Contains(_currentEdit.levelName))
                    _savedNames.Add(_currentEdit.levelName);
                _savedNames.Sort();
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"{LogTag} Failed to save level: {ex.Message}");
            }
        }

        public bool DeleteLevel(string levelName)
        {
            var path = GetLevelPath(levelName);
            if (!File.Exists(path)) return false;

            try
            {
                File.Delete(path);
                _savedNames.Remove(levelName);
                if (_currentEdit != null && _currentEdit.levelName == levelName)
                    _currentEdit = null;
                MoldLogger.LogInfo($"{LogTag} Deleted level: {levelName}");
                return true;
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"{LogTag} Failed to delete level: {ex.Message}");
                return false;
            }
        }

        public void AddLayerToMold(int moldIndex, Color color)
        {
            if (_currentEdit == null) return;
            if (moldIndex < 0 || moldIndex >= _currentEdit.molds.Count) return;

            var mold = _currentEdit.molds[moldIndex];
            if (mold.isEmpty) return;
            if (mold.layerColors.Count >= _currentEdit.maxLayersPerMold) return;

            mold.layerColors.Add(color);
        }

        public void RemoveLayerFromMold(int moldIndex)
        {
            if (_currentEdit == null) return;
            if (moldIndex < 0 || moldIndex >= _currentEdit.molds.Count) return;

            var mold = _currentEdit.molds[moldIndex];
            if (mold.layerColors.Count > 0)
                mold.layerColors.RemoveAt(mold.layerColors.Count - 1);
        }

        public void ClearMold(int moldIndex)
        {
            if (_currentEdit == null) return;
            if (moldIndex < 0 || moldIndex >= _currentEdit.molds.Count) return;

            _currentEdit.molds[moldIndex].layerColors.Clear();
        }

        public void ApplyToLevelData(LevelData target)
        {
            _currentEdit?.ApplyTo(target);
        }

        public List<EditorLevelData> ListSavedLevels()
        {
            var levels = new List<EditorLevelData>();
            foreach (var name in _savedNames)
            {
                var data = LoadLevelData(name);
                if (data != null) levels.Add(data);
            }
            return levels;
        }

        public void RefreshSavedLevels()
        {
            _savedNames.Clear();
            try
            {
                if (!Directory.Exists(_savePath)) return;
                var files = Directory.GetFiles(_savePath, "*.json");
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    if (!string.IsNullOrEmpty(name))
                        _savedNames.Add(name);
                }
                _savedNames.Sort();
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"{LogTag} Failed to refresh level list: {ex.Message}");
            }
        }

        private EditorLevelData LoadLevelData(string levelName)
        {
            var path = GetLevelPath(levelName);
            if (!File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path);
                return JsonUtility.FromJson<EditorLevelData>(json);
            }
            catch
            {
                return null;
            }
        }

        private string GetLevelPath(string levelName)
        {
            return Path.Combine(_savePath, SanitizeName(levelName) + ".json");
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
                name = name.Replace(c, '_');
            return name.Trim();
        }
    }
}
