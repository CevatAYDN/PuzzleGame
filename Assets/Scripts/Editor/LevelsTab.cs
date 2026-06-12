using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Presentation;

namespace PuzzleGame.Editor
{
    public class LevelsTab : IEditorTab
    {
        public string TabName => "Levels";
        private ForgeEditorWindow _window;

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
        private Vector2 _listScroll;
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

        private bool _isLongRunning = false;
        private bool _cancelLongRunning = false;

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
            RefreshLevelList();
            EditorApplication.update += UpdatePlaybackLoop;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void OnDisable()
        {
            EditorApplication.update -= UpdatePlaybackLoop;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        /// <inheritdoc />
        public void Refresh() => RefreshLevelList();

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                if (_isPlayingPlayback)
                {
                    StopPlayback("Playback stopped due to Play Mode transition.", MessageType.Info);
                }
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Level Asset Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _levelsScroll = EditorGUILayout.BeginScrollView(_levelsScroll);

            // ── Solution Playback Section ─────────────────────────────────
            if (_playbackStates != null && _playbackStates.Count > 0 && _playbackLevelData != null)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"🎬 Solution Playback - Level {_playbackLevelData.levelNumber}", EditorStyles.miniBoldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("◀◀ Reset", GUILayout.Width(70)))
                        {
                            _isPlayingPlayback = false;
                            _playbackStep = 0;
                            ApplyPlaybackState(0);
                        }
                        if (GUILayout.Button("◀ Prev", GUILayout.Width(60)))
                        {
                            _isPlayingPlayback = false;
                            if (_playbackStep > 0)
                            {
                                _playbackStep--;
                                ApplyPlaybackState(_playbackStep);
                            }
                        }
                        
                        string playBtnText = _isPlayingPlayback ? "⏸ Pause" : "▶ Play";
                        if (GUILayout.Button(playBtnText, GUILayout.Width(70)))
                        {
                            _isPlayingPlayback = !_isPlayingPlayback;
                            _lastPlaybackUpdateTime = EditorApplication.timeSinceStartup;
                        }
                        
                        if (GUILayout.Button("Next ▶", GUILayout.Width(60)))
                        {
                            _isPlayingPlayback = false;
                            if (_playbackStep < _playbackStates.Count - 1)
                            {
                                _playbackStep++;
                                ApplyPlaybackState(_playbackStep);
                            }
                        }
                    }
                    
                    EditorGUILayout.Space(2);
                    EditorGUI.BeginChangeCheck();
                    int newStep = EditorGUILayout.IntSlider("Step", _playbackStep, 0, _playbackStates.Count - 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _isPlayingPlayback = false;
                        _playbackStep = newStep;
                        ApplyPlaybackState(_playbackStep);
                    }
                    
                    _playbackSpeed = EditorGUILayout.Slider("Step Delay (sec)", _playbackSpeed, 0.1f, 3f);
                }
                EditorGUILayout.Space(6);
            }

            // ── Long Running Operation Control ─────────────────────────
            if (_isLongRunning)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("⏳ Background Operation", EditorStyles.miniBoldLabel);
                    EditorGUILayout.HelpBox("İş devam ediyor. İptal etmek için aşağıdaki butona bas.", MessageType.Info);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        var cancelText = _cancelLongRunning ? "Cancelling..." : "✖ Cancel";
                        if (GUILayout.Button(cancelText, GUILayout.Width(110)))
                        {
                            _cancelLongRunning = true;
                            _window.SetStatus("Cancellation requested. Waiting for current step to finish...", MessageType.Warning);
                        }
                    }
                }

                EditorGUILayout.Space(6);
            }

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
                    if (GUILayout.Button("Create 50 Levels (GDD-aligned)", GUILayout.Height(28)))
                        EditorApplication.delayCall += LevelDataBatchCreator.CreateAllLevels;
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
                    using (new EditorGUI.DisabledGroupScope(_isLongRunning))
                    {
                        if (GUILayout.Button("Verify All 100", EditorStyles.miniButton, GUILayout.Width(100)))
                            EditorApplication.delayCall += () => SolveAndVerifyAll();
                        if (GUILayout.Button("Auto-Reseed", EditorStyles.miniButton, GUILayout.Width(90)))
                            EditorApplication.delayCall += () => AutoReseedUnsolvableLevels();
                        if (GUILayout.Button("Optimize Pars", EditorStyles.miniButton, GUILayout.Width(95)))
                            EditorApplication.delayCall += () => AutoOptimizeAllPars();
                        if (GUILayout.Button("Refresh List", EditorStyles.miniButton, GUILayout.Width(90)))
                            EditorApplication.delayCall += RefreshLevelList;
                    }
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(4);

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(350));
                for (int i = 0; i < _existingLevels.Count; i++)
                {
                    var lvl = _existingLevels[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var statusIcon = lvl.exists ? (lvl.hasSolved && !lvl.isSolvable ? "❌" : "✅") : "⚙";
                        var statusColor = lvl.exists ? (lvl.hasSolved && !lvl.isSolvable ? Color.red : Color.green) : Color.gray;

                        GUI.contentColor = statusColor;
                        GUILayout.Label(statusIcon, GUILayout.Width(15));
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
                                GUUniformLabel("Not Verified", 120);
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
                                        _window.SetStatus($"Level {n:D2} deleted.", MessageType.Info);
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

        private void GUUniformLabel(string text, float width)
        {
            GUILayout.Label(text, GUILayout.Width(width));
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        public void RefreshLevelList()
        {
            _existingLevels.Clear();
            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelDataBatchCreator.LevelPath });
            
            int maxFoundLevel = 0;
            var foundPaths = new HashSet<string>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                {
                    foundPaths.Add(path);
                    if (level.levelNumber > maxFoundLevel) maxFoundLevel = level.levelNumber;
                }
            }

            int targetMax = Mathf.Max(100, Mathf.Max((int)_levelEnd, maxFoundLevel));

            for (int i = 1; i <= targetMax; i++)
            {
                string path = $"{LevelDataBatchCreator.LevelPath}/Level_{i:D2}.asset";
                bool exists = foundPaths.Contains(path) || System.IO.File.Exists(path);
                var level = exists ? AssetDatabase.LoadAssetAtPath<LevelData>(path) : null;
                
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
            _window.SetStatus($"Created {count} levels, skipped {skipped} (already exist).", MessageType.Info);
            RefreshLevelList();
        }

        private bool TryBeginLongRunning(string reasonStatus)
        {
            if (_isLongRunning) return false;
            _isLongRunning = true;
            _cancelLongRunning = false;
            _window.SetStatus(reasonStatus, MessageType.Info);
            return true;
        }

        private void EndLongRunning(string finalStatus = null, MessageType finalType = MessageType.Info)
        {
            _isLongRunning = false;
            _cancelLongRunning = false;
            if (!string.IsNullOrEmpty(finalStatus))
                _window.SetStatus(finalStatus, finalType);
        }

        private void SolveSingleLevel(int index)
        {
            if (_isPlayingPlayback)
                StopPlayback("Playback stopped (starting Solve).", MessageType.Info);

            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            if (levelData == null) return;

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var result = LevelSolverUtility.SolveLevel(levelData, levelConfig);

            lvl.hasSolved = true;
            lvl.isSolvable = result.IsSolvable;
            lvl.optimalMoves = result.IsSolvable ? result.SolutionPath.Count : 0;
            _existingLevels[index] = lvl;

            if (result.IsSolvable)
            {
                _window.SetStatus($"Level {lvl.number:D2}: Solvable in {result.SolutionPath.Count} moves.", MessageType.Info);
                InitPlayback(levelData);
            }
            else
            {
                _window.SetStatus($"Level {lvl.number:D2}: UNSOLVABLE!", MessageType.Error);
            }
        }

        private void CopyLevel(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var sourceLevel = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            if (sourceLevel == null) return;

            int copyNumber = 101;
            for (int i = 101; i <= 200; i++)
            {
                string path = $"{LevelDataBatchCreator.LevelPath}/Level_{i:D2}.asset";
                if (AssetDatabase.LoadAssetAtPath<LevelData>(path) == null)
                {
                    copyNumber = i;
                    break;
                }
            }

            if (copyNumber > 200)
            {
                _window.SetStatus("No available slot for copy (101-200). Delete some levels first.", MessageType.Warning);
                return;
            }

            string newPath = $"{LevelDataBatchCreator.LevelPath}/Level_{copyNumber:D2}.asset";

            var newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.levelNumber = copyNumber;
            newLevel.difficulty = sourceLevel.difficulty;
            newLevel.MoldCount = sourceLevel.MoldCount;
            newLevel.emptyMoldCount = sourceLevel.emptyMoldCount;
            newLevel.colorCount = sourceLevel.colorCount;
            newLevel.maxLayersPerMold = sourceLevel.maxLayersPerMold;
            newLevel.randomSeed = copyNumber * 1337;
            newLevel.autoGenerate = sourceLevel.autoGenerate;

            if (!sourceLevel.autoGenerate && sourceLevel.Molds != null)
            {
                newLevel.Molds = new List<LevelMoldData>();
                foreach (var Mold in sourceLevel.Molds)
                {
                    var copy = new LevelMoldData
                    {
                        isEmpty = Mold.isEmpty,
                        layers = new List<LevelLayerData>()
                    };
                    foreach (var layer in Mold.layers)
                    {
                        copy.layers.Add(new LevelLayerData
                        {
                            color = layer.color,
                            amount = layer.amount
                        });
                    }
                    newLevel.Molds.Add(copy);
                }
            }

            newLevel.parMoves = sourceLevel.parMoves + 2;
            newLevel.goodMoves = sourceLevel.goodMoves + 3;
            newLevel.previewImage = sourceLevel.previewImage;

            AssetDatabase.CreateAsset(newLevel, newPath);
            AssetDatabase.SaveAssets();

            _window.SetStatus($"Level {lvl.number:D2} copied to Level {copyNumber:D2}.", MessageType.Info);
            RefreshLevelList();
        }

        private void OptimizeParSingleLevel(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists || !lvl.hasSolved || !lvl.isSolvable) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            if (levelData == null) return;

            levelData.parMoves = lvl.optimalMoves;
            levelData.goodMoves = Mathf.RoundToInt(lvl.optimalMoves * 1.4f);
            if (levelData.goodMoves < levelData.parMoves + 2) levelData.goodMoves = levelData.parMoves + 2;

            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();

            _window.SetStatus($"Level {lvl.number:D2} par moves optimized to {levelData.parMoves} (good moves: {levelData.goodMoves}).", MessageType.Info);
        }

        private async void SolveAndVerifyAll()
        {
            if (_isPlayingPlayback)
                StopPlayback("Playback stopped (starting Verify All).", MessageType.Info);

            if (!TryBeginLongRunning("Verifying all levels...")) return;

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int total = _existingLevels.Count;
            int unsolvableCount = 0;
            int solvableCount = 0;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    if (_cancelLongRunning)
                    {
                        _window.SetStatus("Verification cancelled.", MessageType.Warning);
                        break;
                    }

                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    EditorUtility.DisplayProgressBar("Solving Levels", $"Solving Level {lvl.number:D2}...", (float)i / total);

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData != null)
                    {
                        var result = LevelSolverUtility.SolveLevel(levelData, levelConfig);

                        lvl.hasSolved = true;
                        lvl.isSolvable = result.IsSolvable;
                        lvl.optimalMoves = result.IsSolvable ? result.SolutionPath.Count : 0;
                        _existingLevels[i] = lvl;

                        if (result.IsSolvable) solvableCount++;
                        else unsolvableCount++;
                    }

                    await System.Threading.Tasks.Task.Yield();
                }

                if (!_cancelLongRunning)
                {
                    _window.SetStatus($"Verification completed. Solvable: {solvableCount}, Unsolvable: {unsolvableCount}",
                        unsolvableCount == 0 ? MessageType.Info : MessageType.Warning);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelsTab] SolveAndVerifyAll failed: {ex.Message}");
                _window.SetStatus($"Error: {ex.Message}", MessageType.Error);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EndLongRunning(_cancelLongRunning ? "Verification cancelled." : null, _cancelLongRunning ? MessageType.Warning : MessageType.Info);
            }
        }

        private async void AutoReseedUnsolvableLevels()
        {
            if (_isPlayingPlayback)
                StopPlayback("Playback stopped (starting Reseed).", MessageType.Info);

            if (!TryBeginLongRunning("Reseeding unsolvable levels...")) return;

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int reseededCount = 0;

            try
            {
                for (int i = 0; i < _existingLevels.Count; i++)
                {
                    if (_cancelLongRunning)
                    {
                        _window.SetStatus("Reseed cancelled.", MessageType.Warning);
                        break;
                    }

                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData == null) continue;

                    var result = LevelSolverUtility.SolveLevel(levelData, levelConfig);
                    if (result.IsSolvable) continue;

                    EditorUtility.DisplayProgressBar("Reseeding", $"Finding seed for Level {lvl.number:D2}...", (float)i / _existingLevels.Count);

                    int seed = levelData.randomSeed;
                    bool found = false;
                    for (int attempt = 1; attempt <= 100; attempt++)
                    {
                        seed += 1337;
                        var tempAssignments = new ProceduralLevelGenerator().Generate(
                            levelData.MoldCount,
                            levelData.maxLayersPerMold,
                            levelData.emptyMoldCount,
                            LevelSolverUtility.ConvertPalette(levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilderModel.DefaultPalette),
                            levelData.difficulty,
                            seed);

                        int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerMold : 4;
                        if (maxLayers < 4) maxLayers = 4;
                        var tempResult = OreSortSolver.Solve(tempAssignments, maxLayers);
                        if (tempResult.IsSolvable)
                        {
                            levelData.randomSeed = seed;
                            levelData.parMoves = tempResult.SolutionPath.Count;
                            levelData.goodMoves = Mathf.RoundToInt(tempResult.SolutionPath.Count * 1.4f);
                            if (levelData.goodMoves < levelData.parMoves + 2) levelData.goodMoves = levelData.parMoves + 2;

                            EditorUtility.SetDirty(levelData);

                            lvl.hasSolved = true;
                            lvl.isSolvable = true;
                            lvl.optimalMoves = tempResult.SolutionPath.Count;
                            _existingLevels[i] = lvl;
                            reseededCount++;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Debug.LogWarning($"[Auto-Reseed] Could not find solvable seed for Level {lvl.number:D2} in 100 attempts.");
                    }

                    await System.Threading.Tasks.Task.Yield();
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                _window.SetStatus($"Reseed complete. Successfully reseeded & solved {reseededCount} levels.", MessageType.Info);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelsTab] AutoReseedUnsolvableLevels failed: {ex.Message}");
                _window.SetStatus($"Error: {ex.Message}", MessageType.Error);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EndLongRunning(_cancelLongRunning ? "Reseed cancelled." : null, _cancelLongRunning ? MessageType.Warning : MessageType.Info);
            }
        }

        private async void AutoOptimizeAllPars()
        {
            if (_isPlayingPlayback)
                StopPlayback("Playback stopped (starting Optimize Pars).", MessageType.Info);

            if (!TryBeginLongRunning("Optimizing pars...")) return;

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int optimizedCount = 0;

            try
            {
                for (int i = 0; i < _existingLevels.Count; i++)
                {
                    if (_cancelLongRunning)
                    {
                        _window.SetStatus("Optimize cancelled.", MessageType.Warning);
                        break;
                    }

                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData == null) continue;

                    EditorUtility.DisplayProgressBar("Optimizing Pars", $"Optimizing Level {lvl.number:D2}...", (float)i / _existingLevels.Count);

                    var result = LevelSolverUtility.SolveLevel(levelData, levelConfig);
                    if (result.IsSolvable)
                    {
                        levelData.parMoves = result.SolutionPath.Count;
                        levelData.goodMoves = Mathf.RoundToInt(result.SolutionPath.Count * 1.4f);
                        if (levelData.goodMoves < levelData.parMoves + 2) levelData.goodMoves = levelData.parMoves + 2;

                        EditorUtility.SetDirty(levelData);
                        optimizedCount++;

                        lvl.hasSolved = true;
                        lvl.isSolvable = true;
                        lvl.optimalMoves = result.SolutionPath.Count;
                        _existingLevels[i] = lvl;
                    }

                    await System.Threading.Tasks.Task.Yield();
                }

                AssetDatabase.SaveAssets();
                _window.SetStatus($"Optimized par/good moves for {optimizedCount} solvable levels.", MessageType.Info);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelsTab] AutoOptimizeAllPars failed: {ex.Message}");
                _window.SetStatus($"Error: {ex.Message}", MessageType.Error);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EndLongRunning(_cancelLongRunning ? "Optimize cancelled." : null, _cancelLongRunning ? MessageType.Warning : MessageType.Info);
            }
        }

        private void LoadLevelIntoActiveScene(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            LoadLevelIntoScene(levelData);
        }

        private void PlayLevelInActiveScene(int index)
        {
            // Playback UI/engine state çakışmasını engelle.
            if (_isPlayingPlayback)
                StopPlayback("Playback stopped (starting Play Mode).", MessageType.Info);

            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            LoadLevelIntoScene(levelData);

            // LoadLevelIntoScene zaten scene state set eder; bundan sonra Play Mode başlasın.
            EditorApplication.isPlaying = true;
        }

        public void LoadLevelIntoScene(LevelData level)
        {
            if (level == null)
            {
                _window.SetStatus("No level selected to load.", MessageType.Warning);
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Load Level {level.levelNumber}");

            SceneBuilder.RemoveMolds();

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = LevelSolverUtility.GetLevelAssignments(level, levelConfig);

            int count = assignments.Count;
            var layout = SceneBuilderModel.MoldLayout.Grid;
            Vector3 center = Vector3.zero;
            var positions = SceneBuilder.ComputePositions(layout, count, center);

            for (int i = 0; i < count; i++)
            {
                var layers = assignments[i];
                var colors = new Color[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                    colors[j] = ColorAdapter.ToUnityStatic(layers[j].Color);

                var MoldCfg = SceneBuilderModel.MoldConfig.WithLayers(
                    positions[i],
                    layers,
                    SceneBuilderModel.ShaderVariant.Premium,
                    $"Mold_{i:D2}");

                var go = SceneBuilder.CreateMold(MoldCfg);
                var ctrl = go != null ? go.GetComponent<MoldController>() : null;
                if (ctrl != null) ctrl.MoldIndex = i;
            }

            Undo.CollapseUndoOperations(undoGroup);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            _window.SetStatus($"Loaded Level {level.levelNumber} into active scene ({count} Molds).", MessageType.Info);
        }

        // ── Solution Playback Engine ─────────────────────────────────
        private List<OreSortSolver.Move> _playbackMoves = new List<OreSortSolver.Move>();
        private List<List<List<OreLayer>>> _playbackStates = new List<List<List<OreLayer>>>();
        private int _playbackStep = 0;
        private bool _isPlayingPlayback = false;
        private double _lastPlaybackUpdateTime = 0;
        private float _playbackSpeed = 1.0f;
        private LevelData _playbackLevelData;

        private void StopPlayback(string reason, MessageType type)
        {
            _isPlayingPlayback = false;
            _playbackStep = 0;
            _lastPlaybackUpdateTime = 0;

            if (!string.IsNullOrEmpty(reason))
                _window.SetStatus(reason, type);

            _window.Repaint();
        }

        private void UpdatePlaybackLoop()
        {
            // Play mode devreye girdiyse playback state güncellemeleri çakışmasın.
            // (EditorApplication.isPlaying true iken kesinlikle hiçbir scene/serialize state değiştirmiyoruz.)
            if (EditorApplication.isPlaying) return;

            if (!_isPlayingPlayback) return;

            double time = EditorApplication.timeSinceStartup;
            if (time - _lastPlaybackUpdateTime >= _playbackSpeed)
            {
                _lastPlaybackUpdateTime = time;
                if (_playbackStates != null && _playbackStates.Count > 0)
                {
                    if (_playbackStep < _playbackStates.Count - 1)
                    {
                        _playbackStep++;
                        ApplyPlaybackState(_playbackStep);
                        _window.Repaint();
                    }
                    else
                    {
                        _isPlayingPlayback = false;
                        _window.SetStatus("Playback completed.", MessageType.Info);
                        _window.Repaint();
                    }
                }
            }
        }

        private void InitPlayback(LevelData level)
        {
            if (level == null) return;

            // Play mode açıkken playback başlatma (state/serialize çakışması).
            if (EditorApplication.isPlaying)
            {
                _window.SetStatus("Close Play Mode to start playback.", MessageType.Warning);
                _isPlayingPlayback = false;
                return;
            }

            // Önce mevcut playback'i kes.
            _isPlayingPlayback = false;
            _playbackStep = 0;
            _lastPlaybackUpdateTime = 0;

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var initial = LevelSolverUtility.GetLevelAssignments(level, levelConfig);

            var result = LevelSolverUtility.SolveLevel(level, levelConfig);
            if (!result.IsSolvable)
            {
                _window.SetStatus($"Cannot start playback: Level {level.levelNumber} is unsolvable.", MessageType.Warning);
                return;
            }

            _playbackLevelData = level;
            _playbackMoves = result.SolutionPath;

            int maxLayers = level.autoGenerate ? level.maxLayersPerMold : 4;
            if (maxLayers < 4) maxLayers = 4;

            _playbackStates = GeneratePlaybackStates(initial, _playbackMoves, maxLayers, level.enableMultiLayerCast);
            _playbackStep = 0;
            _isPlayingPlayback = false;

            LoadLevelIntoScene(level);
            ApplyPlaybackState(0);
        }

        private void ApplyPlaybackState(int step)
        {
            if (_playbackStates == null || step < 0 || step >= _playbackStates.Count) return;
            
            var state = _playbackStates[step];
            var controllers = UnityEngine.Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            
            foreach (var ctrl in controllers)
            {
                if (ctrl == null) continue;
                int idx = ctrl.MoldIndex;
                if (idx >= 0 && idx < state.Count)
                {
                    var layers = state[idx];
                    
                    Undo.RecordObject(ctrl, "Playback Step Change");
                    SerializedObject so = new SerializedObject(ctrl);
                    SerializedProperty layersProp = so.FindProperty("_serializedLayers");
                    if (layersProp != null)
                    {
                        layersProp.ClearArray();
                        for (int i = 0; i < layers.Count; i++)
                        {
                            layersProp.InsertArrayElementAtIndex(i);
                            SerializedProperty element = layersProp.GetArrayElementAtIndex(i);
                            element.FindPropertyRelative("color").colorValue = ColorAdapter.ToUnityStatic(layers[i].Color);
                            element.FindPropertyRelative("amount").floatValue = layers[i].Amount;
                        }
                        so.ApplyModifiedProperties();
                    }
                    
                    EditorUtility.SetDirty(ctrl);
                    
                    ctrl.RestoreStateFromSerialized(false);
                }
            }
            
            string moveMsg = "";
            if (step > 0 && step - 1 < _playbackMoves.Count)
            {
                var move = _playbackMoves[step - 1];
                moveMsg = $" (Move: Mold {move.FromIndex} ➔ Mold {move.ToIndex})";
            }
            _window.SetStatus($"Playback Step {step}/{_playbackStates.Count - 1}{moveMsg}", MessageType.Info);
        }

        private List<List<OreLayer>> CloneState(List<List<OreLayer>> state)
        {
            return state.Select(m => m.Select(l => new OreLayer(l.Color, l.Amount)).ToList()).ToList();
        }

        private List<List<List<OreLayer>>> GeneratePlaybackStates(List<List<OreLayer>> initial, List<OreSortSolver.Move> moves, int maxLayers, bool enableMultiLayerCast)
        {
            var states = new List<List<List<OreLayer>>>();
            var current = CloneState(initial);
            states.Add(CloneState(current));

            foreach (var move in moves)
            {
                int from = move.FromIndex;
                int to = move.ToIndex;
                
                var src = current[from];
                var tgt = current[to];
                
                if (src.Count > 0)
                {
                    var top = src[src.Count - 1];
                    int countToCast = 0;
                    int idx = src.Count - 1;
                    if (!enableMultiLayerCast)
                    {
                        if (tgt.Count < maxLayers)
                            countToCast = 1;
                    }
                    else
                    {
                        while (idx >= 0 && src[idx].Color.Equals(top.Color) && (tgt.Count + countToCast) < maxLayers)
                        {
                            countToCast++;
                            idx--;
                        }
                    }
                    
                    if (countToCast > 0)
                    {
                        var toAdd = new List<OreLayer>();
                        for (int c = 0; c < countToCast; c++)
                        {
                            toAdd.Add(src[src.Count - 1]);
                            src.RemoveAt(src.Count - 1);
                        }
                        toAdd.Reverse();
                        tgt.AddRange(toAdd);
                    }
                }
                states.Add(CloneState(current));
            }
            return states;
        }
    }
}
