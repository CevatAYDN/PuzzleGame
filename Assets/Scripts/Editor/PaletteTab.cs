using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;

namespace PuzzleGame.Editor
{
    public class PaletteTab : IEditorTab
    {
        public string TabName => "Palette";
        private ForgeEditorWindow _window;

        private LevelData _selectedLevelForEdit;
        private Vector2 _paletteScroll;
        private float _batchStart = 1;
        private float _batchEnd = 10;
        private const int MaxPaletteColors = 16;

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Color Palette Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll);

            // ── Load LevelConfig ───────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Configuration", EditorStyles.miniBoldLabel);

                var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>(
                    $"{DataAssetCreator.DataPath}/LevelConfig.asset");

                if (levelConfig == null)
                {
                    EditorGUILayout.HelpBox("LevelConfig.asset not found. Create it from Data tab first.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField($"Current Palette: {levelConfig.palette?.Length ?? 0} colors");

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
                            _window.SetStatus($"Palette saved: {levelConfig.palette.Length} colors.", MessageType.Info);
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
                            _window.SetStatus("Palette reset to defaults.", MessageType.Info);
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
                    "Select a level to edit its properties (difficulty, Mold count, par moves, etc.)",
                    MessageType.None);

                _selectedLevelForEdit = (LevelData)EditorGUILayout.ObjectField(
                    "Select Level", _selectedLevelForEdit, typeof(LevelData), false);

                if (_selectedLevelForEdit != null)
                {
                    EditorGUILayout.Space(4);
                    DrawLevelEditor(_selectedLevelForEdit);
                }
            }

            EditorGUILayout.Space(8);

            // ── Batch Delete ─────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Batch Operations", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(
                    "Delete multiple levels at once. Use with caution!",
                    MessageType.Warning);

                EditorGUILayout.Space(4);
                EditorGUILayout.MinMaxSlider("Delete Range", ref _batchStart, ref _batchEnd, 1, 100);
                EditorGUILayout.LabelField($"Range: {(int)_batchStart} — {(int)_batchEnd}");

                EditorGUILayout.Space(6);
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button($"Delete Levels {(int)_batchStart}-{(int)_batchEnd}", GUILayout.Height(28)))
                {
                    int start = (int)_batchStart;
                    int end = (int)_batchEnd;

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
                        _window.SetStatus($"Deleted {deleted} levels.", MessageType.Info);
                        _window.RefreshLevelList();
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private void DrawLevelEditor(LevelData level)
        {
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Editing: Level {level.levelNumber:D2}", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                level.difficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", level.difficulty);
                level.MoldCount = EditorGUILayout.IntField("Mold Count", level.MoldCount);
                level.emptyMoldCount = EditorGUILayout.IntField("Empty Molds", level.emptyMoldCount);
                level.colorCount = EditorGUILayout.IntField("Color Count", level.colorCount);
                level.maxLayersPerMold = EditorGUILayout.IntField("Max Layers", level.maxLayersPerMold);
                level.randomSeed = EditorGUILayout.IntField("Random Seed", level.randomSeed);

                EditorGUILayout.Space(6);

                level.parMoves = EditorGUILayout.IntField("Par (3★)", level.parMoves);
                level.goodMoves = EditorGUILayout.IntField("Good (2★)", level.goodMoves);

                EditorGUILayout.Space(6);

                level.autoGenerate = EditorGUILayout.ToggleLeft("Auto-Generate", level.autoGenerate);

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("══════════ Features ═══════════", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    level.enableMultiLayerCast = EditorGUILayout.ToggleLeft(
                        "Multi-Layer Cast (birlikte dökme)", level.enableMultiLayerCast);

                    if (level.enableMultiLayerCast)
                    {
                        EditorGUI.indentLevel++;
                        if (level.multiLayerCastConfig == null)
                            level.multiLayerCastConfig = new MultiLayerCastData();

                        level.multiLayerCastConfig.CastAllMatching = EditorGUILayout.Toggle(
                            "Tüm eşleşen katmanları dök", level.multiLayerCastConfig.CastAllMatching);
                        level.multiLayerCastConfig.CastConsecutiveOnly = EditorGUILayout.Toggle(
                            "Sadece ardışık eşleşmeleri", level.multiLayerCastConfig.CastConsecutiveOnly);
                        level.multiLayerCastConfig.minConsecutiveForCast = EditorGUILayout.IntSlider(
                            "Min. ardışık katman", level.multiLayerCastConfig.minConsecutiveForCast, 2, 4);
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.Space(4);

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
                                rule.colorA = (OreColor)EditorGUILayout.EnumPopup(rule.colorA, GUILayout.Width(80));
                                EditorGUILayout.LabelField("+", GUILayout.Width(20));
                                EditorGUILayout.LabelField("Renk B", GUILayout.Width(50));
                                rule.colorB = (OreColor)EditorGUILayout.EnumPopup(rule.colorB, GUILayout.Width(80));
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
                                    rule.resultColor = (OreColor)EditorGUILayout.EnumPopup(rule.resultColor, GUILayout.Width(80));
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

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Changes", GUILayout.Height(26)))
                {
                    EditorUtility.SetDirty(level);
                    AssetDatabase.SaveAssets();
                    _window.SetStatus($"Level {level.levelNumber:D2} saved.", MessageType.Info);
                    _window.RefreshLevelList();
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
                            _window.SetStatus("Level deleted.", MessageType.Info);
                            _window.RefreshLevelList();
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }
}
