using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Services;

namespace PuzzleGame.Editor
{
    public partial class PuzzleGameEditorWindow
    {
        // ── Palette tab ────────────────────────────────────────────────────
        private LevelData _selectedLevelForEdit;
        private Vector2 _paletteScroll;
        private Color[] _editingPalette;
        private const int MaxPaletteColors = 16;

        // ── PALETTE TAB ───────────────────────────────────────────────────

        private void DrawPaletteTab()
        {
            EditorGUILayout.LabelField("Color Palette Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll);

            // ── Load LevelConfig ───────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Configuration", EditorStyles.miniBoldLabel);

                var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>(
                    $"{DataAssetCreator.DataPath}/LevelConfig.asset");

                if (levelConfig == null)
                {
                    EditorGUILayout.HelpBox("LevelConfig.asset not found. Create it from Data tab first.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField($"Current Palette: {levelConfig.palette?.Length ?? 0} colors");

                    // Display and edit palette
                    if (levelConfig.palette == null || levelConfig.palette.Length == 0)
                    {
                        EditorGUILayout.HelpBox("Palette is empty. Add colors below.", MessageType.Info);
                        Undo.RecordObject(levelConfig, "Initialize Palette");
                        levelConfig.palette = new Color[4]; // Default 4 colors
                        EditorUtility.SetDirty(levelConfig);
                    }

                    EditorGUILayout.Space(4);
                    EditorGUI.BeginChangeCheck();
                    int colorCount = EditorGUILayout.IntSlider("Color Count", levelConfig.palette.Length, 2, MaxPaletteColors);

                    Color[] tempPalette = (Color[])levelConfig.palette.Clone();
                    if (colorCount != tempPalette.Length)
                    {
                        Array.Resize(ref tempPalette, colorCount);
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Edit Colors:", EditorStyles.miniBoldLabel);

                    for (int i = 0; i < tempPalette.Length; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField($"Color {i + 1}", GUILayout.Width(60));
                            tempPalette[i] = EditorGUILayout.ColorField(tempPalette[i], GUILayout.Width(200));
                            GUILayout.FlexibleSpace();
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(levelConfig, "Modify Palette");
                        levelConfig.palette = tempPalette;
                        EditorUtility.SetDirty(levelConfig);
                    }

                    EditorGUILayout.Space(8);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Save Palette", GUILayout.Height(28)))
                        {
                            EditorUtility.SetDirty(levelConfig);
                            AssetDatabase.SaveAssets();
                            SetStatus($"Palette saved: {levelConfig.palette.Length} colors.", MessageType.Info);
                        }

                        GUI.backgroundColor = new Color(0.2f, 0.5f, 0.9f);
                        if (GUILayout.Button("Reset to Default", GUILayout.Height(28)))
                        {
                            Undo.RecordObject(levelConfig, "Reset Palette to Default");
                            levelConfig.palette = new Color[]
                            {
                                new Color(0.9f, 0.2f, 0.2f),  // Red
                                new Color(0.2f, 0.6f, 0.9f),  // Blue
                                new Color(0.2f, 0.8f, 0.2f),  // Green
                                new Color(0.95f, 0.9f, 0.2f), // Yellow
                                new Color(0.9f, 0.5f, 0.2f),  // Orange
                                new Color(0.7f, 0.2f, 0.9f),  // Purple
                            };
                            EditorUtility.SetDirty(levelConfig);
                            AssetDatabase.SaveAssets();
                            SetStatus("Palette reset to defaults.", MessageType.Info);
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            EditorGUILayout.Space(8);

            // ── Single Level Edit ───────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Property Editor", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(
                    "Select a level to edit its properties (difficulty, bottle count, par moves, etc.)",
                    MessageType.None);

                _selectedLevelForEdit = (LevelData)EditorGUILayout.ObjectField(
                    "Select Level", _selectedLevelForEdit, typeof(LevelData), false);

                if (_selectedLevelForEdit != null)
                {
                    EditorGUILayout.Space(4);
                    DrawLevelEditor(_selectedLevelForEdit);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLevelEditor(LevelData level)
        {
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Editing: Level {level.levelNumber:D2}", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                // Basic properties
                level.difficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", level.difficulty);
                level.bottleCount = EditorGUILayout.IntField("Bottle Count", level.bottleCount);
                level.emptyBottleCount = EditorGUILayout.IntField("Empty Bottles", level.emptyBottleCount);
                level.colorCount = EditorGUILayout.IntField("Color Count", level.colorCount);
                level.maxLayersPerBottle = EditorGUILayout.IntField("Max Layers", level.maxLayersPerBottle);
                level.randomSeed = EditorGUILayout.IntField("Random Seed", level.randomSeed);

                EditorGUILayout.Space(6);

                // Star thresholds
                level.parMoves = EditorGUILayout.IntField("Par (3★)", level.parMoves);
                level.goodMoves = EditorGUILayout.IntField("Good (2★)", level.goodMoves);

                EditorGUILayout.Space(6);

                // Auto-generate toggle
                level.autoGenerate = EditorGUILayout.ToggleLeft("Auto-Generate", level.autoGenerate);

                EditorGUILayout.Space(10);

                // ═══════════════════════════════════════════════════════════
                // MODULAR FEATURES SETTINGS
                // ═══════════════════════════════════════════════════════════
                EditorGUILayout.LabelField("══════════ Features ═══════════", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                // Multi-Layer Pour
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    level.enableMultiLayerPour = EditorGUILayout.ToggleLeft(
                        "Multi-Layer Pour (birlikte dökme)", level.enableMultiLayerPour);

                    if (level.enableMultiLayerPour)
                    {
                        EditorGUI.indentLevel++;
                        if (level.multiLayerPourConfig == null)
                            level.multiLayerPourConfig = new MultiLayerPourData();

                        level.multiLayerPourConfig.pourAllMatching = EditorGUILayout.Toggle(
                            "Tüm eşleşen katmanları dök", level.multiLayerPourConfig.pourAllMatching);
                        level.multiLayerPourConfig.pourConsecutiveOnly = EditorGUILayout.Toggle(
                            "Sadece ardışık eşleşmeleri", level.multiLayerPourConfig.pourConsecutiveOnly);
                        level.multiLayerPourConfig.minConsecutiveForPour = EditorGUILayout.IntSlider(
                            "Min. ardışık katman", level.multiLayerPourConfig.minConsecutiveForPour, 2, 4);
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.Space(4);

                // Reaction System
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    level.enableReactionSystem = EditorGUILayout.ToggleLeft(
                        "Reaction System (kimyasal reaksiyon)", level.enableReactionSystem);

                    if (level.enableReactionSystem)
                    {
                        EditorGUI.indentLevel++;
                        if (level.reactionConfig == null)
                            level.reactionConfig = new ReactionSystemData();

                        level.reactionConfig.enableReactions = EditorGUILayout.Toggle(
                            "Reaksiyonları aktif et", level.reactionConfig.enableReactions);

                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Reaction Kuralları:", EditorStyles.miniBoldLabel);

                        // Display and edit reaction rules
                        if (level.reactionConfig.reactionRules == null)
                            level.reactionConfig.reactionRules = new System.Collections.Generic.List<ReactionRule>();

                        int ruleCount = EditorGUILayout.IntSlider("Kural sayısı",
                            level.reactionConfig.reactionRules.Count, 0, 10);

                        while (level.reactionConfig.reactionRules.Count < ruleCount)
                            level.reactionConfig.reactionRules.Add(new ReactionRule());

                        while (level.reactionConfig.reactionRules.Count > ruleCount)
                            level.reactionConfig.reactionRules.RemoveAt(level.reactionConfig.reactionRules.Count - 1);

                        for (int i = 0; i < level.reactionConfig.reactionRules.Count; i++)
                        {
                            var rule = level.reactionConfig.reactionRules[i];
                            EditorGUILayout.Space(2);
                            EditorGUILayout.LabelField($"Kural {i + 1}:", EditorStyles.miniLabel);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Renk A", GUILayout.Width(50));
                                rule.colorA = (LiquidColor)EditorGUILayout.EnumPopup(rule.colorA, GUILayout.Width(80));
                                EditorGUILayout.LabelField("+", GUILayout.Width(20));
                                EditorGUILayout.LabelField("Renk B", GUILayout.Width(50));
                                rule.colorB = (LiquidColor)EditorGUILayout.EnumPopup(rule.colorB, GUILayout.Width(80));
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Tür", GUILayout.Width(50));
                                rule.reactionType = (ReactionRule.ReactionType)EditorGUILayout.EnumPopup(
                                    rule.reactionType, GUILayout.Width(120));

                                if (rule.reactionType == ReactionRule.ReactionType.Transform)
                                {
                                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                                    EditorGUILayout.LabelField("Sonuç", GUILayout.Width(40));
                                    rule.resultColor = (LiquidColor)EditorGUILayout.EnumPopup(rule.resultColor, GUILayout.Width(80));
                                }
                            }

                            EditorGUILayout.Space(2);
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(level, "Modify Level Properties");
                EditorUtility.SetDirty(level);
            }

            EditorGUILayout.Space(8);

            // Action buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Changes", GUILayout.Height(26)))
                {
                    EditorUtility.SetDirty(level);
                    AssetDatabase.SaveAssets();
                    SetStatus($"Level {level.levelNumber:D2} saved.", MessageType.Info);
                    RefreshLevelList();
                }

                GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
                if (GUILayout.Button("Delete Level", GUILayout.Height(26)))
                {
                    string path = AssetDatabase.GetAssetPath(level);
                    if (EditorUtility.DisplayDialog("Delete Level?",
                        $"This will permanently delete {System.IO.Path.GetFileName(path)}",
                        "Delete", "Cancel"))
                    {
                        if (AssetDatabase.DeleteAsset(path))
                        {
                            AssetDatabase.Refresh();
                            _selectedLevelForEdit = null;
                            SetStatus("Level deleted.", MessageType.Info);
                            RefreshLevelList();
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            // ── Batch Delete ─────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Batch Operations", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(
                    "Delete multiple levels at once. Use with caution!",
                    MessageType.Warning);

                EditorGUILayout.Space(4);
                float batchStart = 1, batchEnd = 10;
                EditorGUILayout.MinMaxSlider("Delete Range", ref batchStart, ref batchEnd, 1, 100);
                EditorGUILayout.LabelField($"Range: {(int)batchStart} — {(int)batchEnd}");

                EditorGUILayout.Space(6);
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button($"Delete Levels {(int)batchStart}-{(int)batchEnd}", GUILayout.Height(28)))
                {
                    int start = (int)batchStart;
                    int end = (int)batchEnd;

                    if (EditorUtility.DisplayDialog("Batch Delete?",
                        $"This will delete levels {start} through {end}. This cannot be undone!",
                        "Delete All", "Cancel"))
                    {
                        int deleted = 0;
                        for (int i = start; i <= end; i++)
                        {
                            string path = $"{LevelDataBatchCreator.LevelPath}/Level_{i:D2}.asset";
                            if (AssetDatabase.LoadAssetAtPath<LevelData>(path) != null)
                            {
                                if (AssetDatabase.DeleteAsset(path)) deleted++;
                            }
                        }
                        AssetDatabase.Refresh();
                        SetStatus($"Deleted {deleted} levels.", MessageType.Info);
                        RefreshLevelList();
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }
}
