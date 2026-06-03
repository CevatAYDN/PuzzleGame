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
        // ── Features tab ───────────────────────────────────────────────────
        private LevelData _selectedLevelForFeatures;
        private Vector2 _featuresScroll;
        private int _selectedFeatureTab = 0;
        private string[] _featureTabNames = new string[] { "Multi-Layer Pour", "Reaction System" };

        // ── FEATURES TAB ───────────────────────────────────────────────────

        private void DrawFeaturesTab()
        {
            EditorGUILayout.LabelField("Level Features Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Her seviye için özellikleri etkinleştirin veya devre dışı bırakın.\n" +
                "Multi-Layer Pour: Ardışık aynı renk katmanlarını tek seferde döker.\n" +
                "Reaction System: Renkler arası kimyasal reaksiyonları yönetir.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // ── Level Selection ─────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Select Level", EditorStyles.miniBoldLabel);

                var levels = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Resources/Levels" });
                var levelOptions = new List<string> { "-- Select Level --" };
                var levelPaths = new List<string> { null };

                foreach (var guid in levels)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                    if (level != null)
                    {
                        levelOptions.Add($"Level {level.levelNumber} - {level.difficulty}");
                        levelPaths.Add(path);
                    }
                }

                int currentIndex = _selectedLevelForFeatures != null ? 
                    levelPaths.FindIndex(p => p != null && AssetDatabase.GetAssetPath(_selectedLevelForFeatures).Contains(p)) : 0;
                
                EditorGUILayout.Space(4);
                int selected = EditorGUILayout.Popup("Level", currentIndex, levelOptions.ToArray());
                
                if (selected >= 0 && selected < levelPaths.Count)
                {
                    if (selected != currentIndex)
                    {
                        _selectedLevelForFeatures = selected > 0 ? 
                            AssetDatabase.LoadAssetAtPath<LevelData>(levelPaths[selected]) : null;
                    }
                }
            }

            if (_selectedLevelForFeatures == null)
            {
                EditorGUILayout.HelpBox("Lütfen düzenlemek için bir seviye seçin.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4);

            // Feature tabs
            _selectedFeatureTab = GUILayout.Toolbar(_selectedFeatureTab, _featureTabNames);
            EditorGUILayout.Space(4);

            switch (_selectedFeatureTab)
            {
                case 0:
                    DrawMultiLayerPourSettings();
                    break;
                case 1:
                    DrawReactionSystemSettings();
                    break;
            }

            // ── Save Button ─────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Changes", GUILayout.Height(28)))
                {
                    Undo.RecordObject(_selectedLevelForFeatures, "Save Features");
                    EditorUtility.SetDirty(_selectedLevelForFeatures);
                    AssetDatabase.SaveAssets();
                    SetStatus("Features saved!", MessageType.Info);
                }

                if (GUILayout.Button("Reset to Default", GUILayout.Height(28)))
                {
                    ResetFeaturesToDefault();
                }
            }
        }

        private void DrawMultiLayerPourSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Multi-Layer Pour Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                Undo.RecordObject(_selectedLevelForFeatures, "MultiLayer Pour");

                // Enable toggle
                _selectedLevelForFeatures.enableMultiLayerPour = EditorGUILayout.ToggleLeft(
                    "Enable Multi-Layer Pour", 
                    _selectedLevelForFeatures.enableMultiLayerPour);

                EditorGUILayout.Space(4);

                if (_selectedLevelForFeatures.enableMultiLayerPour)
                {
                    // Initialize config if null
                    if (_selectedLevelForFeatures.multiLayerPourConfig == null)
                    {
                        _selectedLevelForFeatures.multiLayerPourConfig = new MultiLayerPourData
                        {
                            isEnabled = true,
                            featureType = LevelFeatureType.MultiLayerPour,
                            pourAllMatching = true,
                            pourConsecutiveOnly = true,
                            minConsecutiveForPour = 2
                        };
                    }

                    var config = _selectedLevelForFeatures.multiLayerPourConfig;

                    config.pourAllMatching = EditorGUILayout.ToggleLeft(
                        "Pour All Matching Layers", 
                        config.pourAllMatching);

                    config.pourConsecutiveOnly = EditorGUILayout.ToggleLeft(
                        "Pour Consecutive Only", 
                        config.pourConsecutiveOnly);

                    EditorGUILayout.Space(4);
                    config.minConsecutiveForPour = EditorGUILayout.IntSlider(
                        "Min Consecutive for Pour", 
                        config.minConsecutiveForPour, 
                        2, 
                        _selectedLevelForFeatures.maxLayersPerBottle);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        $"Bu seviye için: En az {config.minConsecutiveForPour} ardışık aynı renk katmanı varsa döküm yapılır.",
                        MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("Multi-layer pour devre dışı. Oyuncular her seferinde tek katman dökecek.", MessageType.Info);
                }
            }
        }

        private void DrawReactionSystemSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Reaction System Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                Undo.RecordObject(_selectedLevelForFeatures, "Reaction System");

                // Enable toggle
                _selectedLevelForFeatures.enableReactionSystem = EditorGUILayout.ToggleLeft(
                    "Enable Reaction System", 
                    _selectedLevelForFeatures.enableReactionSystem);

                EditorGUILayout.Space(4);

                if (_selectedLevelForFeatures.enableReactionSystem)
                {
                    // Initialize config if null
                    if (_selectedLevelForFeatures.reactionConfig == null)
                    {
                        _selectedLevelForFeatures.reactionConfig = new ReactionSystemData
                        {
                            isEnabled = true,
                            featureType = LevelFeatureType.ReactionSystem,
                            enableReactions = true,
                            reactionRules = new List<ReactionRule>()
                        };
                    }

                    var config = _selectedLevelForFeatures.reactionConfig;

                    config.enableReactions = EditorGUILayout.ToggleLeft(
                        "Enable Reactions", 
                        config.enableReactions);

                    EditorGUILayout.Space(4);

                    // Reaction rules list
                    EditorGUILayout.LabelField("Reaction Rules", EditorStyles.miniBoldLabel);
                    
                    if (config.reactionRules == null)
                        config.reactionRules = new List<ReactionRule>();

                    // Add new rule button
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+ Add Rule", GUILayout.Width(100)))
                        {
                            config.reactionRules.Add(new ReactionRule
                            {
                                colorA = LiquidColor.Red,
                                colorB = LiquidColor.Blue,
                                resultColor = LiquidColor.Green,
                                reactionType = ReactionRule.ReactionType.Transform
                            });
                        }
                    }

                    EditorGUILayout.Space(4);

                    // List existing rules
                    for (int i = 0; i < config.reactionRules.Count; i++)
                    {
                        var rule = config.reactionRules[i];
                        
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            EditorGUILayout.LabelField($"Rule {i + 1}", EditorStyles.miniBoldLabel);
                            
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                rule.colorA = (LiquidColor)EditorGUILayout.EnumPopup("Color A", rule.colorA, GUILayout.Width(150));
                                rule.colorB = (LiquidColor)EditorGUILayout.EnumPopup("+", rule.colorB, GUILayout.Width(80));
                                rule.reactionType = (ReactionRule.ReactionType)EditorGUILayout.EnumPopup("Type", rule.reactionType, GUILayout.Width(120));
                            }

                            if (rule.reactionType == ReactionRule.ReactionType.Transform)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField("→ Result:", GUILayout.Width(60));
                                    rule.resultColor = (LiquidColor)EditorGUILayout.EnumPopup(rule.resultColor, GUILayout.Width(150));
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                                {
                                    config.reactionRules.RemoveAt(i);
                                    i--;
                                }
                            }
                        }
                    }

                    if (config.reactionRules.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Henüz kural eklenmemiş. '+ Add Rule' ile reaksiyon ekleyin.", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Reaction system devre dışı. Seviye normal sıvı sıralama olarak oynanacak.", MessageType.Info);
                }
            }
        }

        private void ResetFeaturesToDefault()
        {
            if (_selectedLevelForFeatures == null) return;

            Undo.RecordObject(_selectedLevelForFeatures, "Reset Features");

            _selectedLevelForFeatures.enableMultiLayerPour = true;
            _selectedLevelForFeatures.enableReactionSystem = false;
            _selectedLevelForFeatures.multiLayerPourConfig = null;
            _selectedLevelForFeatures.reactionConfig = null;

            EditorUtility.SetDirty(_selectedLevelForFeatures);
            SetStatus("Features reset to default.", MessageType.Info);
        }
    }
}
