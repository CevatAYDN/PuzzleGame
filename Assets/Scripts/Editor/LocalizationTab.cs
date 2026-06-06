using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Infrastructure.Models;
using PuzzleGame.Application.Services;

namespace PuzzleGame.Editor
{
    public class LocalizationTab : IEditorTab
    {
        public string TabName => "Localization";
        private ForgeEditorWindow _window;

        private Vector2 _localizationScroll;
        private SupportedLanguage _selectedLanguage = SupportedLanguage.Turkish;
        private string _localizationPath = "Assets/StreamingAssets/Localization/";
        private List<LocalizationEntry> _localizationEntries = new List<LocalizationEntry>();
        private string _newKeyName = "";
        private string _newTranslationTR = "";
        private string _newTranslationEN = "";
        private string _searchFilter = "";

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
        }

        public void OnDisable()
        {
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Localization (i18n)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Çeviri anahtarlarını yönetin.\n" +
                "Her dil için metinleri ekleyin ve düzenleyin.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            _localizationScroll = EditorGUILayout.BeginScrollView(_localizationScroll);

            // ── Language Selection ───────────────────────────────────────────
            DrawLanguageSelection();

            EditorGUILayout.Space(8);

            // ── Key Management ───────────────────────────────────────────────
            DrawKeyManagement();

            EditorGUILayout.Space(8);

            // ── Translation Editor ───────────────────────────────────────────
            DrawTranslationEditor();

            EditorGUILayout.EndScrollView();

            // ── Save/Export ───────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save All", GUILayout.Height(28)))
                {
                    SaveLocalization();
                }

                if (GUILayout.Button("Export to JSON", GUILayout.Height(28)))
                {
                    ExportToJSON();
                }

                if (GUILayout.Button("Import from JSON", GUILayout.Height(28)))
                {
                    ImportFromJSON();
                }
            }
        }

        private void DrawLanguageSelection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Language Selection", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                var languages = Enum.GetValues(typeof(SupportedLanguage));
                var languageNames = new string[languages.Length];
                for (int i = 0; i < languages.Length; i++)
                {
                    languageNames[i] = languages.GetValue(i).ToString();
                }

                int langIdx = Mathf.Clamp((int)_selectedLanguage, 0, languages.Length - 1);
                _selectedLanguage = (SupportedLanguage)EditorGUILayout.Popup(
                    "Current Language", 
                    langIdx, 
                    languageNames);

                EditorGUILayout.Space(4);

                // Show language status
                EditorGUILayout.LabelField("Translation Status:", EditorStyles.miniBoldLabel);
                
                int totalKeys = _localizationEntries.Count;
                int translatedForLang = _localizationEntries.Count(e => 
                    e.Translations != null && e.Translations.ContainsKey(_selectedLanguage) && 
                    !string.IsNullOrEmpty(e.Translations[_selectedLanguage]));

                float percentage = totalKeys > 0 ? (float)translatedForLang / totalKeys * 100 : 0;
                
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), percentage / 100f, 
                    $"{translatedForLang}/{totalKeys} ({percentage:F0}%)");

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Load Translations", GUILayout.Width(140)))
                    {
                        LoadLocalization();
                    }

                    if (GUILayout.Button("Add New Key", GUILayout.Width(120)))
                    {
                        AddNewKey();
                    }
                }
            }
        }

        private void DrawKeyManagement()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Key Management", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                // Search filter
                _searchFilter = EditorGUILayout.TextField("Search:", _searchFilter, GUILayout.Width(250));

                EditorGUILayout.Space(4);

                // Key list
                var filteredEntries = string.IsNullOrEmpty(_searchFilter) 
                    ? _localizationEntries 
                    : _localizationEntries.Where(e => e.Key.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();

                EditorGUILayout.LabelField($"Keys: {filteredEntries.Count}", EditorStyles.miniBoldLabel);

                foreach (var entry in filteredEntries)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(entry.Key, GUILayout.Width(180));

                        // Show translation for selected language
                        string translation = "";
                        if (entry.Translations != null && entry.Translations.ContainsKey(_selectedLanguage))
                        {
                            translation = entry.Translations[_selectedLanguage];
                        }

                        EditorGUILayout.LabelField(
                            string.IsNullOrEmpty(translation) ? "[Missing]" : translation.Length > 30 ? translation.Substring(0, 30) + "..." : translation,
                            EditorStyles.miniLabel);

                        GUILayout.FlexibleSpace();

                        // Delete key
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            _localizationEntries.Remove(entry);
                            break;
                        }
                    }
                }

                if (filteredEntries.Count == 0)
                {
                    EditorGUILayout.HelpBox("No keys found. Add a new key or adjust the search filter.", MessageType.Info);
                }
            }
        }

        private void DrawTranslationEditor()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Translation Editor", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                EditorGUILayout.HelpBox(
                    "Yeni anahtar eklemek için aşağıdaki alanları doldurun.",
                    MessageType.None);

                EditorGUILayout.Space(4);

                // New key input
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Add New Translation Key", EditorStyles.miniBoldLabel);
                    EditorGUILayout.Space(4);

                    _newKeyName = EditorGUILayout.TextField("Key Name:", _newKeyName, GUILayout.Width(300));

                    EditorGUILayout.Space(4);

                    // Translations for each language
                    var allLanguages = Enum.GetValues(typeof(SupportedLanguage));

                    foreach (SupportedLanguage lang in allLanguages)
                    {
                        string currentValue = GetNewTranslationForLanguage(lang);
                        string label = lang.ToString() + ":";

                        if (lang == SupportedLanguage.Turkish)
                            _newTranslationTR = EditorGUILayout.TextField(label, currentValue, GUILayout.Width(300));
                        else if (lang == SupportedLanguage.English)
                            _newTranslationEN = EditorGUILayout.TextField(label, currentValue, GUILayout.Width(300));
                        else
                            EditorGUILayout.TextField(label, currentValue, GUILayout.Width(300));
                    }

                    EditorGUILayout.Space(4);

                    if (GUILayout.Button("Add Key", GUILayout.Height(28)))
                    {
                        AddTranslationKey();
                    }
                }

                EditorGUILayout.Space(8);

                // Common keys quick editor
                EditorGUILayout.LabelField("Common UI Strings", EditorStyles.miniBoldLabel);
                DrawCommonStringsEditor();
            }
        }

        private string GetNewTranslationForLanguage(SupportedLanguage lang)
        {
            switch (lang)
            {
                case SupportedLanguage.Turkish: return _newTranslationTR;
                case SupportedLanguage.English: return _newTranslationEN;
                default: return "";
            }
        }

        private void DrawCommonStringsEditor()
        {
            // Common game strings that are frequently used
            string[] commonKeys = new string[]
            {
                "menu_play",
                "menu_levels", 
                "menu_settings",
                "menu_quit",
                "game_paused",
                "game_complete",
                "game_failed",
                "ui_restart",
                "ui_undo",
                "ui_next_level",
                "ui_prev_level",
                "level_select",
                "moves_count",
                "time_count",
                "stars_earned",
                "tutorial_drag",
                "tutorial_Cast"
            };

            var missingKeys = commonKeys.Where(k => !_localizationEntries.Any(e => e.Key == k)).ToArray();

            if (missingKeys.Length > 0)
            {
                EditorGUILayout.LabelField($"Missing common keys: {missingKeys.Length}", EditorStyles.miniBoldLabel);

                if (GUILayout.Button("Generate Missing Keys", GUILayout.Width(180)))
                {
                    GenerateMissingKeys(missingKeys);
                }
            }
            else
            {
                EditorGUILayout.LabelField("✓ All common keys are defined", EditorStyles.miniBoldLabel);
            }
        }

        private void AddNewKey()
        {
            if (string.IsNullOrEmpty(_newKeyName))
            {
                _window.SetStatus("Please enter a key name", MessageType.Warning);
                return;
            }

            if (_localizationEntries.Any(e => e.Key == _newKeyName))
            {
                _window.SetStatus("Key already exists", MessageType.Warning);
                return;
            }

            var entry = new LocalizationEntry
            {
                Key = _newKeyName,
                Translations = new Dictionary<SupportedLanguage, string>()
            };

            _localizationEntries.Add(entry);
            _newKeyName = "";
            _window.SetStatus($"Added key: {_newKeyName}", MessageType.Info);
        }

        private void AddTranslationKey()
        {
            if (string.IsNullOrEmpty(_newKeyName))
            {
                _window.SetStatus("Lütfen bir anahtar adı girin", MessageType.Warning);
                return;
            }

            if (_localizationEntries.Any(e => e.Key == _newKeyName))
            {
                _window.SetStatus("Bu anahtar zaten mevcut", MessageType.Warning);
                return;
            }

            var entry = new LocalizationEntry
            {
                Key = _newKeyName,
                Translations = new Dictionary<SupportedLanguage, string>()
            };

            // Add translations
            if (!string.IsNullOrEmpty(_newTranslationTR))
                entry.Translations[SupportedLanguage.Turkish] = _newTranslationTR;
            if (!string.IsNullOrEmpty(_newTranslationEN))
                entry.Translations[SupportedLanguage.English] = _newTranslationEN;

            _localizationEntries.Add(entry);

            // Clear inputs
            _newKeyName = "";
            _newTranslationTR = "";
            _newTranslationEN = "";

            _window.SetStatus($"Added: {_newKeyName}", MessageType.Info);
        }

        private void GenerateMissingKeys(string[] keys)
        {
            foreach (var key in keys)
            {
                if (!_localizationEntries.Any(e => e.Key == key))
                {
                    _localizationEntries.Add(new LocalizationEntry
                    {
                        Key = key,
                        Translations = new Dictionary<SupportedLanguage, string>
                        {
                            { SupportedLanguage.Turkish, key.Replace("_", " ").ToUpper() },
                            { SupportedLanguage.English, key.Replace("_", " ").ToUpper() }
                        }
                    });
                }
            }
            _window.SetStatus($"Generated {keys.Length} missing keys", MessageType.Info);
        }

        private void LoadLocalization()
        {
            try
            {
                // Try file path
                string fullPath = Path.Combine(UnityEngine.Application.dataPath, _localizationPath.Replace("Assets/", ""));
                string filePath = Path.Combine(fullPath, "translations.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var wrapper = JsonUtility.FromJson<TranslationFile>(json);
                    _localizationEntries = FromTranslationFile(wrapper);
                    _window.SetStatus($"Loaded {_localizationEntries.Count} translation keys", MessageType.Info);
                    return;
                }

                // Try to load from Resources as fallback
                var textAsset = Resources.Load<TextAsset>("localization");
                if (textAsset != null)
                {
                    var wrapper = JsonUtility.FromJson<TranslationFile>(textAsset.text);
                    _localizationEntries = FromTranslationFile(wrapper);
                    _window.SetStatus($"Loaded {_localizationEntries.Count} translation keys", MessageType.Info);
                    return;
                }

                // Generate default keys
                _localizationEntries = GetDefaultLocalizationEntries();
                _window.SetStatus("Created default localization keys", MessageType.Info);
            }
            catch (Exception ex)
            {
                _window.SetStatus($"Error: {ex.Message}", MessageType.Error);
            }
        }

        private void SaveLocalization()
        {
            try
            {
                // Ensure directory exists
                string fullPath = Path.Combine(UnityEngine.Application.dataPath, _localizationPath.Replace("Assets/", ""));
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                var wrapper = ToTranslationFile(_localizationEntries);
                string json = JsonUtility.ToJson(wrapper, true);
                string filePath = Path.Combine(fullPath, "translations.json");
                
                File.WriteAllText(filePath, json);
                
                AssetDatabase.Refresh();
                _window.SetStatus($"Saved {_localizationEntries.Count} keys", MessageType.Info);
            }
            catch (Exception ex)
            {
                _window.SetStatus($"Save error: {ex.Message}", MessageType.Error);
            }
        }

        private void ExportToJSON()
        {
            string path = EditorUtility.SaveFilePanel("Export Localization", "Assets/", "localization.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                var wrapper = ToTranslationFile(_localizationEntries);
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(path, json);
                _window.SetStatus($"Exported to {path}", MessageType.Info);
            }
        }

        private void ImportFromJSON()
        {
            string path = EditorUtility.OpenFilePanel("Import Localization", "Assets/", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var wrapper = JsonUtility.FromJson<TranslationFile>(json);
                    if (wrapper?.entries != null)
                    {
                        _localizationEntries = FromTranslationFile(wrapper);
                        _window.SetStatus($"Imported {wrapper.entries.Count} keys", MessageType.Info);
                    }
                }
                catch (Exception ex)
                {
                    _window.SetStatus($"Import error: {ex.Message}", MessageType.Error);
                }
            }
        }

        private List<LocalizationEntry> GetDefaultLocalizationEntries()
        {
            return new List<LocalizationEntry>
            {
                new LocalizationEntry { Key = "menu_play", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Oyna" }, { SupportedLanguage.English, "Play" } } },
                new LocalizationEntry { Key = "menu_levels", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Seviyeler" }, { SupportedLanguage.English, "Levels" } } },
                new LocalizationEntry { Key = "menu_settings", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Ayarlar" }, { SupportedLanguage.English, "Settings" } } },
                new LocalizationEntry { Key = "menu_quit", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Çıkış" }, { SupportedLanguage.English, "Quit" } } },
                new LocalizationEntry { Key = "game_paused", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Duraklatıldı" }, { SupportedLanguage.English, "Paused" } } },
                new LocalizationEntry { Key = "game_complete", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Tebrikler!" }, { SupportedLanguage.English, "Congratulations!" } } },
                new LocalizationEntry { Key = "game_failed", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Başarısız" }, { SupportedLanguage.English, "Failed" } } },
                new LocalizationEntry { Key = "ui_restart", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Yeniden" }, { SupportedLanguage.English, "Restart" } } },
                new LocalizationEntry { Key = "ui_undo", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Geri Al" }, { SupportedLanguage.English, "Undo" } } },
                new LocalizationEntry { Key = "ui_next_level", Translations = new Dictionary<SupportedLanguage, string> { { SupportedLanguage.Turkish, "Sonraki Seviye" }, { SupportedLanguage.English, "Next Level" } } },
            };
        }

        private TranslationFile ToTranslationFile(List<LocalizationEntry> entries)
        {
            var file = new TranslationFile { entries = new List<TranslationEntry>() };
            foreach (var entry in entries)
            {
                var te = new TranslationEntry();
                te.key = entry.Key;
                if (entry.Translations != null)
                {
                    entry.Translations.TryGetValue(SupportedLanguage.Turkish, out te.tr);
                    entry.Translations.TryGetValue(SupportedLanguage.English, out te.en);
                    entry.Translations.TryGetValue(SupportedLanguage.German, out te.de);
                    entry.Translations.TryGetValue(SupportedLanguage.Spanish, out te.es);
                    entry.Translations.TryGetValue(SupportedLanguage.French, out te.fr);
                }
                file.entries.Add(te);
            }
            return file;
        }

        private List<LocalizationEntry> FromTranslationFile(TranslationFile file)
        {
            var list = new List<LocalizationEntry>();
            if (file?.entries != null)
            {
                foreach (var entry in file.entries)
                {
                    var le = new LocalizationEntry
                    {
                        Key = entry.key,
                        Translations = new Dictionary<SupportedLanguage, string>()
                    };
                    if (!string.IsNullOrEmpty(entry.tr)) le.Translations[SupportedLanguage.Turkish] = entry.tr;
                    if (!string.IsNullOrEmpty(entry.en)) le.Translations[SupportedLanguage.English] = entry.en;
                    if (!string.IsNullOrEmpty(entry.de)) le.Translations[SupportedLanguage.German] = entry.de;
                    if (!string.IsNullOrEmpty(entry.es)) le.Translations[SupportedLanguage.Spanish] = entry.es;
                    if (!string.IsNullOrEmpty(entry.fr)) le.Translations[SupportedLanguage.French] = entry.fr;
                    list.Add(le);
                }
            }
            return list;
        }
    }
}
