using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Services;

namespace PuzzleGame.Editor
{
    public partial class PuzzleGameEditorWindow
    {
        // ── Level UI tab ───────────────────────────────────────────────────
        private LevelData _selectedLevelForUI;
        private Vector2 _levelUIScroll;
        private int _selectedBottleIndex = -1;
        private string _newBottleColorHex = "#FF0000";
        private float _newBottleLayerAmount = 0.25f;

        // ── LEVEL UI TAB ───────────────────────────────────────────────────

        private void DrawLevelUITab()
        {
            EditorGUILayout.LabelField("Pre-built Level Designer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Manuel (auto-generate olmayan) seviyeler için şişe ve katman düzenleyici.\n" +
                "Seviyeyi seçin, şişe ekleyin ve katmanları yapılandırın.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // ── Level Selection ─────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Select Level to Edit", EditorStyles.miniBoldLabel);

                var levels = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Resources/Levels" });
                var levelOptions = new List<string> { "-- Select Level --" };
                var levelPaths = new List<string> { null };
                var levelList = new List<LevelData>();

                foreach (var guid in levels)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                    if (level != null)
                    {
                        string mode = level.autoGenerate ? "[Auto]" : "[Manual]";
                        levelOptions.Add($"Level {level.levelNumber} - {level.difficulty} {mode}");
                        levelPaths.Add(path);
                        levelList.Add(level);
                    }
                }

                int currentIndex = _selectedLevelForUI != null ? 
                    levelPaths.FindIndex(p => p != null && AssetDatabase.GetAssetPath(_selectedLevelForUI).Contains(p)) : 0;
                
                EditorGUILayout.Space(4);
                int selected = EditorGUILayout.Popup("Level", currentIndex, levelOptions.ToArray());
                
                if (selected >= 0 && selected < levelPaths.Count)
                {
                    if (selected != currentIndex)
                    {
                        _selectedLevelForUI = selected > 0 ? 
                            AssetDatabase.LoadAssetAtPath<LevelData>(levelPaths[selected]) : null;
                        _selectedBottleIndex = -1;
                    }
                }

                if (_selectedLevelForUI != null)
                {
                    EditorGUILayout.Space(4);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Mode:", GUILayout.Width(50));
                        EditorGUILayout.LabelField(
                            _selectedLevelForUI.autoGenerate ? "Auto-Generated" : "Manual (Pre-built)",
                            _selectedLevelForUI.autoGenerate ? EditorStyles.boldLabel : EditorStyles.miniBoldLabel);
                    }
                }
            }

            if (_selectedLevelForUI == null)
            {
                EditorGUILayout.HelpBox("Lütfen düzenlemek için bir seviye seçin.", MessageType.Warning);
                return;
            }

            // Auto-generate mode warning
            if (_selectedLevelForUI.autoGenerate)
            {
                EditorGUILayout.HelpBox(
                    "Bu seviye Auto-Generated modunda. Manuel düzenleme için önce 'Auto-Generate' kapatılmalı.\n" +
                    "Aşağıdaki butona tıklayarak Manual moda geçirin.",
                    MessageType.Warning);

                if (GUILayout.Button("Convert to Manual Mode"))
                {
                    Undo.RecordObject(_selectedLevelForUI, "Convert to Manual");
                    _selectedLevelForUI.autoGenerate = false;
                    _selectedLevelForUI.bottles = GenerateDefaultBottles(_selectedLevelForUI);
                    EditorUtility.SetDirty(_selectedLevelForUI);
                    SetStatus("Seviye Manual moda dönüştürüldü.", MessageType.Info);
                }
                return;
            }

            EditorGUILayout.Space(4);
            _levelUIScroll = EditorGUILayout.BeginScrollView(_levelUIScroll);

            // ── Bottle Management ───────────────────────────────────────────
            DrawBottleManagement();

            EditorGUILayout.EndScrollView();

            // ── Save Button ─────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Changes", GUILayout.Height(28)))
                {
                    Undo.RecordObject(_selectedLevelForUI, "Save Level UI");
                    EditorUtility.SetDirty(_selectedLevelForUI);
                    AssetDatabase.SaveAssets();
                    SetStatus("Level UI saved!", MessageType.Info);
                }
            }
        }

        private void DrawBottleManagement()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Bottle Management", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                if (_selectedLevelForUI.bottles == null)
                    _selectedLevelForUI.bottles = new List<LevelBottleData>();

                // Bottle list
                EditorGUILayout.LabelField($"Bottles: {_selectedLevelForUI.bottles.Count}", EditorStyles.miniBoldLabel);

                for (int i = 0; i < _selectedLevelForUI.bottles.Count; i++)
                {
                    var bottle = _selectedLevelForUI.bottles[i];
                    bool isSelected = _selectedBottleIndex == i;

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        string label = bottle.isEmpty ? $"Bottle {i + 1} [EMPTY]" : $"Bottle {i + 1} ({bottle.layers.Count} layers)";
                        if (GUILayout.Toggle(isSelected, label, EditorStyles.radioButton, GUILayout.Width(200)))
                        {
                            _selectedBottleIndex = i;
                        }

                        GUILayout.FlexibleSpace();

                        // Toggle empty
                        bool newEmpty = EditorGUILayout.Toggle("Empty", bottle.isEmpty, GUILayout.Width(80));
                        if (newEmpty != bottle.isEmpty)
                        {
                            Undo.RecordObject(_selectedLevelForUI, "Toggle Bottle Empty");
                            bottle.isEmpty = newEmpty;
                            if (newEmpty) bottle.layers.Clear();
                        }

                        // Delete bottle
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            Undo.RecordObject(_selectedLevelForUI, "Delete Bottle");
                            _selectedLevelForUI.bottles.RemoveAt(i);
                            if (_selectedBottleIndex >= _selectedLevelForUI.bottles.Count)
                                _selectedBottleIndex = _selectedLevelForUI.bottles.Count - 1;
                            i--;
                        }
                    }
                }

                // Add bottle buttons
                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Add Filled Bottle"))
                    {
                        Undo.RecordObject(_selectedLevelForUI, "Add Bottle");
                        var newBottle = new LevelBottleData
                        {
                            isEmpty = false,
                            layers = new List<LevelLayerData>
                            {
                                new LevelLayerData { color = Color.red, amount = 0.25f },
                                new LevelLayerData { color = Color.red, amount = 0.25f },
                                new LevelLayerData { color = Color.blue, amount = 0.25f },
                                new LevelLayerData { color = Color.blue, amount = 0.25f }
                            }
                        };
                        _selectedLevelForUI.bottles.Add(newBottle);
                        _selectedBottleIndex = _selectedLevelForUI.bottles.Count - 1;
                    }

                    if (GUILayout.Button("+ Add Empty Bottle"))
                    {
                        Undo.RecordObject(_selectedLevelForUI, "Add Empty Bottle");
                        var newBottle = new LevelBottleData
                        {
                            isEmpty = true,
                            layers = new List<LevelLayerData>()
                        };
                        _selectedLevelForUI.bottles.Add(newBottle);
                        _selectedBottleIndex = _selectedLevelForUI.bottles.Count - 1;
                    }
                }

                // Selected bottle layer editor
                if (_selectedBottleIndex >= 0 && _selectedBottleIndex < _selectedLevelForUI.bottles.Count)
                {
                    var selectedBottle = _selectedLevelForUI.bottles[_selectedBottleIndex];
                    
                    EditorGUILayout.Space(8);
                    DrawLayerEditor(selectedBottle);
                }
            }
        }

        private void DrawLayerEditor(LevelBottleData bottle)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Bottle {_selectedBottleIndex + 1} - Layer Editor", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                if (bottle.isEmpty)
                {
                    EditorGUILayout.HelpBox("Boş şişeye katman eklenemez.", MessageType.Info);
                    return;
                }

                if (bottle.layers == null)
                    bottle.layers = new List<LevelLayerData>();

                EditorGUILayout.LabelField($"Layers: {bottle.layers.Count}", EditorStyles.miniBoldLabel);

                // Display layers from top to bottom (visual order)
                for (int i = 0; i < bottle.layers.Count; i++)
                {
                    var layer = bottle.layers[i];
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Layer number (top = last index)
                        int displayNum = bottle.layers.Count - i;
                        EditorGUILayout.LabelField($"#{displayNum}", GUILayout.Width(30));

                        // Color picker
                        layer.color = EditorGUILayout.ColorField(layer.color, GUILayout.Width(80));

                        // Amount slider
                        EditorGUILayout.LabelField("Amount:", GUILayout.Width(60));
                        layer.amount = EditorGUILayout.Slider(layer.amount, 0.05f, 1f, GUILayout.Width(120));

                        // Show color preview
                        var rect = GUILayoutUtility.GetRect(20, 20);
                        EditorGUI.DrawRect(rect, layer.color);

                        GUILayout.FlexibleSpace();

                        // Remove layer
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            bottle.layers.RemoveAt(i);
                            i--;
                        }

                        // Move up/down
                        if (i > 0 && GUILayout.Button("▲", GUILayout.Width(25)))
                        {
                            var temp = bottle.layers[i];
                            bottle.layers[i] = bottle.layers[i - 1];
                            bottle.layers[i - 1] = temp;
                        }
                        if (i < bottle.layers.Count - 1 && GUILayout.Button("▼", GUILayout.Width(25)))
                        {
                            var temp = bottle.layers[i];
                            bottle.layers[i] = bottle.layers[i + 1];
                            bottle.layers[i + 1] = temp;
                        }
                    }
                }

                EditorGUILayout.Space(4);

                // Add new layer
                EditorGUILayout.LabelField("Add New Layer", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newBottleColorHex = EditorGUILayout.TextField("Color Hex", _newBottleColorHex, GUILayout.Width(150));
                    
                    Color parsedColor;
                    if (ColorUtility.TryParseHtmlString(_newBottleColorHex, out parsedColor))
                    {
                        var rect = GUILayoutUtility.GetRect(20, 20);
                        EditorGUI.DrawRect(rect, parsedColor);
                    }

                    _newBottleLayerAmount = EditorGUILayout.Slider("Amount", _newBottleLayerAmount, 0.05f, 1f, GUILayout.Width(150));

                    if (GUILayout.Button("Add Layer", GUILayout.Width(100)))
                    {
                        Color newColor;
                        if (ColorUtility.TryParseHtmlString(_newBottleColorHex, out newColor))
                        {
                            bottle.layers.Add(new LevelLayerData { color = newColor, amount = _newBottleLayerAmount });
                        }
                    }
                }

                // Quick add buttons
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Quick Add:", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Red")) AddQuickColor(bottle, Color.red);
                    if (GUILayout.Button("Blue")) AddQuickColor(bottle, Color.blue);
                    if (GUILayout.Button("Green")) AddQuickColor(bottle, Color.green);
                    if (GUILayout.Button("Yellow")) AddQuickColor(bottle, Color.yellow);
                }
            }
        }

        private void AddQuickColor(LevelBottleData bottle, Color color)
        {
            if (bottle.layers == null) bottle.layers = new List<LevelLayerData>();
            bottle.layers.Add(new LevelLayerData { color = color, amount = 0.25f });
        }

        private List<LevelBottleData> GenerateDefaultBottles(LevelData level)
        {
            var bottles = new List<LevelBottleData>();
            int colorsPerBottle = level.maxLayersPerBottle / 2;
            
            Color[] defaultColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

            for (int i = 0; i < level.colorCount; i++)
            {
                var bottle = new LevelBottleData
                {
                    isEmpty = false,
                    layers = new List<LevelLayerData>()
                };

                Color color = defaultColors[i % defaultColors.Length];
                for (int j = 0; j < colorsPerBottle; j++)
                {
                    bottle.layers.Add(new LevelLayerData { color = color, amount = 0.25f });
                }

                bottles.Add(bottle);
            }

            // Add empty bottles
            for (int i = 0; i < level.emptyBottleCount; i++)
            {
                bottles.Add(new LevelBottleData
                {
                    isEmpty = true,
                    layers = new List<LevelLayerData>()
                });
            }

            return bottles;
        }
    }
}
