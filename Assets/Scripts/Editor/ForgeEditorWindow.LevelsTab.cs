using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Services;

namespace PuzzleGame.Editor
{
    public partial class ForgeEditorWindow
    {
        // ── Levels tab ──────────────────────────────────────────────────────
        private float _levelStart = 1;
        private float _levelEnd = 10;
        private int _levelSeedBase = 1337;
        private Difficulty _levelDifficulty = Difficulty.Easy;
        private int _levelMoldCount = 5;
        private int _levelColorCount = 3;
        private int _levelEmptyCount = 2;
        private int _levelMaxLayers = 4;
        private int _levelPar = 10;
        private int _levelGood = 15;
        private Vector2 _levelsScroll;
        private List<LevelInfo> _existingLevels = new List<LevelInfo>();

        private struct LevelInfo
        {
            public int number;
            public Difficulty difficulty;
            public string path;
            public bool exists;
            public bool hasSolved;
            public bool isSolvable;
            public int optimalMoves;
        }

        private void RefreshLevelList()
        {
            _existingLevels.Clear();
            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelDataBatchCreator.LevelPath });
            for (int i = 1; i <= 100; i++)
            {
                string path = $"{LevelDataBatchCreator.LevelPath}/Level_{i:D2}.asset";
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                _existingLevels.Add(new LevelInfo
                {
                    number = i,
                    exists = level != null,
                    difficulty = level != null ? level.difficulty : Difficulty.Trivial,
                    path = path,
                    hasSolved = false,
                    isSolvable = false,
                    optimalMoves = 0
                });
            }
        }

        // ── LEVELS TAB ──────────────────────────────────────────────────────

        private void DrawLevelsTab()
        {
            EditorGUILayout.LabelField("Level Asset Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _levelsScroll = EditorGUILayout.BeginScrollView(_levelsScroll);

            // ── Batch Create ─────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Batch Create Levels", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                EditorGUILayout.MinMaxSlider("Level Range", ref _levelStart, ref _levelEnd, 1, 999);
                EditorGUILayout.LabelField($"Range: {(int)_levelStart} — {(int)_levelEnd}");

                _levelSeedBase = EditorGUILayout.IntField("Seed Base", _levelSeedBase);
                _levelDifficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", _levelDifficulty);
                _levelMoldCount = EditorGUILayout.IntField("Mold Count", _levelMoldCount);
                _levelColorCount = EditorGUILayout.IntField("Color Count", _levelColorCount);
                _levelEmptyCount = EditorGUILayout.IntField("Empty Molds", _levelEmptyCount);
                _levelMaxLayers = EditorGUILayout.IntField("Max Layers", _levelMaxLayers);
                _levelPar = EditorGUILayout.IntField("Par (3★)", _levelPar);
                _levelGood = EditorGUILayout.IntField("Good (2★)", _levelGood);

                EditorGUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Custom Range", GUILayout.Height(28)))
                        EditorApplication.delayCall += CreateCustomLevels;
                    if (GUILayout.Button("Create 100 Levels (Progressive)", GUILayout.Height(28)))
                        EditorApplication.delayCall += LevelDataBatchCreator.Create100Levels;
                }
            }

            EditorGUILayout.Space(8);

            // ── Existing Levels ──────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Existing Levels ({_existingLevels.Count(l => l.exists)}/100)", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Verify All 100", EditorStyles.miniButton, GUILayout.Width(100)))
                        EditorApplication.delayCall += SolveAndVerifyAll;
                    if (GUILayout.Button("Auto-Reseed", EditorStyles.miniButton, GUILayout.Width(90)))
                        EditorApplication.delayCall += AutoReseedUnsolvableLevels;
                    if (GUILayout.Button("Optimize Pars", EditorStyles.miniButton, GUILayout.Width(95)))
                        EditorApplication.delayCall += AutoOptimizeAllPars;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Refresh List", EditorStyles.miniButton, GUILayout.Width(90)))
                        EditorApplication.delayCall += RefreshLevelList;
                }

                EditorGUILayout.Space(4);

                _levelsScroll = EditorGUILayout.BeginScrollView(_levelsScroll, GUILayout.Height(350));
                for (int i = 0; i < _existingLevels.Count; i++)
                {
                    var lvl = _existingLevels[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.contentColor = lvl.exists ? Color.green : Color.gray;
                        GUILayout.Label(lvl.exists ? "✓" : "✗", GUILayout.Width(15));
                        GUI.contentColor = Color.white;

                        GUILayout.Label($"Level {lvl.number:D2}", GUILayout.Width(65));

                        if (lvl.exists)
                        {
                            GUILayout.Label(lvl.difficulty.ToString(), GUILayout.Width(60));

                            // Solver status
                            if (lvl.hasSolved)
                            {
                                if (lvl.isSolvable)
                                {
                                    GUI.contentColor = Color.green;
                                    GUILayout.Label($"Solvable ({lvl.optimalMoves} moves)", GUILayout.Width(120));
                                }
                                else
                                {
                                    GUI.contentColor = Color.red;
                                    GUILayout.Label("UNSOLVABLE", GUILayout.Width(120));
                                }
                                GUI.contentColor = Color.white;
                            }
                            else
                            {
                                GUI.contentColor = Color.gray;
                                GUILayout.Label("Not Verified", GUILayout.Width(120));
                                GUI.contentColor = Color.white;
                            }
                        }
                        else
                        {
                            GUILayout.Label("—", GUILayout.Width(60));
                            GUILayout.Label("Missing Asset", GUILayout.Width(120));
                        }

                        GUILayout.FlexibleSpace();

                        if (lvl.exists)
                        {
                            if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40)))
                            {
                                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lvl.path);
                                if (obj != null) EditorGUIUtility.PingObject(obj);
                            }

                            if (GUILayout.Button("Solve", EditorStyles.miniButton, GUILayout.Width(45)))
                            {
                                int idx = i;
                                EditorApplication.delayCall += () => SolveSingleLevel(idx);
                            }

                            using (new EditorGUI.DisabledGroupScope(!lvl.hasSolved || !lvl.isSolvable))
                            {
                                if (GUILayout.Button("Opt", EditorStyles.miniButton, GUILayout.Width(35)))
                                {
                                    int idx = i;
                                    EditorApplication.delayCall += () => OptimizeParSingleLevel(idx);
                                }
                            }

                            if (GUILayout.Button("Load", EditorStyles.miniButton, GUILayout.Width(40)))
                            {
                                int idx = i;
                                EditorApplication.delayCall += () => LoadLevelIntoActiveScene(idx);
                            }

                            if (GUILayout.Button("Play", EditorStyles.miniButton, GUILayout.Width(40)))
                            {
                                int idx = i;
                                EditorApplication.delayCall += () => PlayLevelInActiveScene(idx);
                            }

                            if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(40)))
                            {
                                int idx = i;
                                EditorApplication.delayCall += () => CopyLevel(idx);
                            }

                            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
                            {
                                string p = lvl.path;
                                int n = lvl.number;
                                EditorApplication.delayCall += () =>
                                {
                                    if (AssetDatabase.DeleteAsset(p))
                                    {
                                        AssetDatabase.Refresh();
                                        SetStatus($"Level {n:D2} deleted.", MessageType.Info);
                                        RefreshLevelList();
                                    }
                                };
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndScrollView();
        }

        private void CreateCustomLevels()
        {
            if (!System.IO.Directory.Exists(LevelDataBatchCreator.LevelPath))
                System.IO.Directory.CreateDirectory(LevelDataBatchCreator.LevelPath);

            int count = 0;
            int skipped = 0;
            for (int i = (int)_levelStart; i <= (int)_levelEnd; i++)
            {
                string fileName = $"Level_{i:D2}";
                string fullPath = $"{LevelDataBatchCreator.LevelPath}/{fileName}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<LevelData>(fullPath);
                if (existing != null)
                {
                    skipped++;
                    continue;
                }

                var level = ScriptableObject.CreateInstance<LevelData>();
                level.levelNumber = i;
                level.randomSeed = i * _levelSeedBase;
                level.difficulty = _levelDifficulty;
                level.MoldCount = _levelMoldCount;
                level.colorCount = _levelColorCount;
                level.emptyMoldCount = _levelEmptyCount;
                level.maxLayersPerMold = _levelMaxLayers;
                level.parMoves = _levelPar;
                level.goodMoves = _levelGood;

                AssetDatabase.CreateAsset(level, fullPath);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus($"Created {count} levels, skipped {skipped} (already exist).", MessageType.Info);
            RefreshLevelList();
        }

        private void PingAllLevels()
        {
            int pinged = 0;
            foreach (var lvl in _existingLevels.Where(l => l.exists).Take(10))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lvl.path);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    pinged++;
                }
            }
            SetStatus($"Pinged {pinged} level assets.", MessageType.Info);
        }

        private void DeleteMissingLevels()
        {
            int start = (int)_levelStart;
            int end = (int)_levelEnd;
            SetStatus($"Checked range {start}-{end}. Use Delete button per-level.", MessageType.Info);
        }
    }
}
