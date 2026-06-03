using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Models.FeatureSystem;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Services;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// PuzzleGame için tüm editor araçları — tek pencerede 4 sekme:
    ///   - Data: ScriptableObject asset yönetimi
    ///   - Levels: Batch level creation ve yönetimi
    ///   - Scene: Sahne oluşturma kontrolü
    ///   - Validate: Proje sağlık kontrolleri
    /// </summary>
    public class PuzzleGameEditorWindow : EditorWindow
    {
        private enum Tab { Data, Levels, Scene, Validate, Palette }
        private Tab _activeTab = Tab.Data;

        // ── Data tab ────────────────────────────────────────────────────────
        private bool _overrideExisting = false;
        private Dictionary<string, bool> _dataPresence = new Dictionary<string, bool>();
        private Vector2 _dataScroll;

        // ── Levels tab ──────────────────────────────────────────────────────
        private float _levelStart = 1;
        private float _levelEnd = 10;
        private int _levelSeedBase = 1337;
        private Difficulty _levelDifficulty = Difficulty.Easy;
        private int _levelBottleCount = 5;
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

        // ── Scene tab ───────────────────────────────────────────────────────
        private SceneBuilder.BuildOptions _buildOpts = SceneBuilder.All;
        private SceneBuilder.BottleLayout _bottleLayout = SceneBuilder.BottleLayout.Grid;
        private SceneBuilder.ShaderVariant _shaderVariant = SceneBuilder.ShaderVariant.Premium;
        private int _bottleCount = 2;
        private bool _firstEmpty = true;
        private Vector2 _sceneScroll;
        private LevelData _stageLevelAsset;

        // ── Validate tab ────────────────────────────────────────────────────
        private List<ValidationResult> _validationResults = new List<ValidationResult>();
        private Vector2 _validateScroll;

        // ── Palette tab ────────────────────────────────────────────────────
        private LevelData _selectedLevelForEdit;
        private Vector2 _paletteScroll;
        private Color[] _editingPalette;
        private const int MaxPaletteColors = 16;

        // ── Status bar ──────────────────────────────────────────────────────
        private string _statusMessage = "Ready.";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/PuzzleGame/Open Editor")]
        public static void Open()
        {
            var window = GetWindow<PuzzleGameEditorWindow>("PuzzleGame Editor");
            window.minSize = new Vector2(420, 360);
            window.RefreshDataPresence();
        }

        private void OnEnable()
        {
            RefreshDataPresence();
        }

        private void OnGUI()
        {
            DrawTabs();
            EditorGUILayout.Space(6);
            switch (_activeTab)
            {
                case Tab.Data:     DrawDataTab();     break;
                case Tab.Levels:   DrawLevelsTab();   break;
                case Tab.Scene:    DrawSceneTab();    break;
                case Tab.Validate: DrawValidateTab(); break;
                case Tab.Palette:  DrawPaletteTab();  break;
            }
            DrawStatusBar();
        }

        // ── Tab bar ─────────────────────────────────────────────────────────

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_activeTab == Tab.Data, "Data", EditorStyles.toolbarButton)) _activeTab = Tab.Data;
            if (GUILayout.Toggle(_activeTab == Tab.Levels, "Levels", EditorStyles.toolbarButton)) _activeTab = Tab.Levels;
            if (GUILayout.Toggle(_activeTab == Tab.Scene, "Scene", EditorStyles.toolbarButton)) _activeTab = Tab.Scene;
            if (GUILayout.Toggle(_activeTab == Tab.Validate, "Validate", EditorStyles.toolbarButton)) _activeTab = Tab.Validate;
            if (GUILayout.Toggle(_activeTab == Tab.Palette, "Palette", EditorStyles.toolbarButton)) _activeTab = Tab.Palette;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                EditorApplication.delayCall += () =>
                {
                    RefreshDataPresence();
                    SetStatus("Refreshed.", MessageType.Info);
                };
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── DATA TAB ────────────────────────────────────────────────────────

        private void DrawDataTab()
        {
            EditorGUILayout.LabelField("ScriptableObject Asset Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Asset'ler " + DataAssetCreator.DataPath + " altında oluşturulur.\n" +
                "Aktif değerlerin üzerine yazılsın mı?",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            _overrideExisting = EditorGUILayout.ToggleLeft("Reset existing assets on update", _overrideExisting);
            if (EditorGUI.EndChangeCheck()) Repaint();

            _dataScroll = EditorGUILayout.BeginScrollView(_dataScroll);
            EditorGUILayout.LabelField("Asset Durumu", EditorStyles.miniBoldLabel);
            foreach (var kvp in _dataPresence.OrderBy(x => x.Key))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var icon = kvp.Value ? "✓" : "✗";
                    var color = kvp.Value ? Color.green : Color.gray;
                    GUILayout.Label(icon, GUILayout.Width(20));
                    GUI.contentColor = color;
                    GUILayout.Label(kvp.Key + ".asset", GUILayout.MinWidth(160));
                    GUI.contentColor = Color.white;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    {
                        string key = kvp.Key;
                        EditorApplication.delayCall += () => PingAsset(key);
                    }
                    if (GUILayout.Button("Reset", GUILayout.Width(50)))
                    {
                        string key = kvp.Key;
                        EditorApplication.delayCall += () => ResetSingle(key);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create / Update All", GUILayout.Height(28)))
                    EditorApplication.delayCall += CreateAllData;
                if (GUILayout.Button("Select Folder", GUILayout.Height(28), GUILayout.MaxWidth(140)))
                    EditorApplication.delayCall += SelectDataFolder;
            }

            EditorGUILayout.Space(12);
            DrawPlayerSaveSection();
        }

        // ── PLAYER SAVE SECTION ───────────────────────────────────────────────

        private void DrawPlayerSaveSection()
        {
            EditorGUILayout.LabelField("Player Save Data", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var exists = Application.Services.GameSaveManager.HasSaveData;
            var size = Application.Services.GameSaveManager.FileSizeBytes;
            var integ = exists ? Application.Services.GameSaveManager.VerifyIntegrity() : false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("File", Application.Services.GameSaveManager.SaveFilePath);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Exists:  ", GUILayout.Width(60));
                    GUI.contentColor = exists ? Color.green : Color.gray;
                    GUILayout.Label(exists ? "✓" : "✗", GUILayout.Width(20));
                    GUI.contentColor = Color.white;
                    GUILayout.FlexibleSpace();

                    if (exists)
                    {
                        GUILayout.Label("Integrity:", GUILayout.Width(65));
                        GUI.contentColor = integ ? Color.green : Color.red;
                        GUILayout.Label(integ ? "OK" : "TAMPERED", GUILayout.Width(80));
                        GUI.contentColor = Color.white;
                        GUILayout.FlexibleSpace();

                        GUILayout.Label((size / 1024.0).ToString("F1") + " KB", GUILayout.Width(60));
                    }
                }

                if (exists && integ)
                {
                    var data = Application.Services.GameSaveManager.PeekVerified();
                    EditorGUILayout.LabelField("Last Played Level", data.lastPlayedLevel.ToString());
                    EditorGUILayout.LabelField("Completed Levels",
                        data.levels.FindAll(l => l.isCompleted).Count + "/" + data.levels.Count);
                }

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reveal in Explorer", GUILayout.Height(22)))
                        EditorApplication.delayCall += RevealSaveFile;
                    if (GUILayout.Button("Verify Integrity", GUILayout.Height(22)))
                        EditorApplication.delayCall += VerifySaveFile;
                    GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
                    if (GUILayout.Button("Delete Save", GUILayout.Height(22)))
                        EditorApplication.delayCall += DeleteSaveFile;
                    GUI.backgroundColor = Color.white;
                }
            }
        }

        private void RevealSaveFile()
        {
            if (!Application.Services.GameSaveManager.HasSaveData)
            {
                SetStatus("No save file to reveal.", MessageType.Warning);
                return;
            }
            EditorUtility.RevealInFinder(Application.Services.GameSaveManager.SaveFilePath);
            SetStatus("Save file revealed.", MessageType.Info);
        }

        private void VerifySaveFile()
        {
            if (!Application.Services.GameSaveManager.HasSaveData)
            {
                SetStatus("No save file to verify.", MessageType.Warning);
                return;
            }

            bool ok = Application.Services.GameSaveManager.VerifyIntegrity();
            SetStatus(ok
                ? "Save integrity: OK (HMAC-SHA256 matches)."
                : "Save integrity: FAILED — file tampered or corrupted!",
                ok ? MessageType.Info : MessageType.Error);
        }

        private void DeleteSaveFile()
        {
            if (!Application.Services.GameSaveManager.HasSaveData)
            {
                SetStatus("No save data to delete.", MessageType.Info);
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Delete all save data?",
                "Bu işlem geri alınamaz. Oyuncunun tüm kayıtlı level ilerlemesi silinecek.",
                "Delete", "Cancel"))
            {
                return;
            }

            Application.Services.GameSaveManager.DeleteAll();
            SetStatus("Save data deleted.", MessageType.Info);
        }

        private void PingAsset(string fileName)
        {
            var path = $"{DataAssetCreator.DataPath}/{fileName}.asset";
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
            else
            {
                SetStatus($"{fileName}.asset not found.", MessageType.Warning);
            }
        }

        private void ResetSingle(string fileName)
        {
            if (!EditorUtility.DisplayDialog(
                "Reset asset",
                $"{fileName}.asset will be overwritten with default values. Continue?",
                "Reset", "Cancel"))
            {
                return;
            }
            var results = DataAssetCreator.CreateAllDefaults(_ => true);
            SetStatus($"Reset {fileName}.asset to defaults.", MessageType.Info);
            RefreshDataPresence();
        }

        private void CreateAllData()
        {
            int toCreate = 0, toOverwrite = 0;
            foreach (var kvp in _dataPresence)
            {
                if (kvp.Value) toOverwrite++;
                else toCreate++;
            }

            string summary = $"{toCreate} new, {toOverwrite} to overwrite.";
            if (_overrideExisting && toOverwrite > 0)
            {
                if (!EditorUtility.DisplayDialog(
                    "Confirm overwrite",
                    "Existing assets will be overwritten with default values.\n\n" + summary + "\n\nProceed?",
                    "Create / Update", "Cancel"))
                {
                    return;
                }
            }

            try
            {
                EditorUtility.DisplayProgressBar("PuzzleGame Data", "Creating assets...", 0.5f);
                var results = DataAssetCreator.CreateAllDefaults(name =>
                    _overrideExisting || !_dataPresence.TryGetValue(name, out var exists) || !exists);

                int created = results.Count(r => r.created);
                int updated = results.Count(r => r.overwritten);
                int skipped = results.Count(r => !r.created && !r.overwritten);
                SetStatus($"Created: {created}, Updated: {updated}, Skipped: {skipped}", MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                RefreshDataPresence();
            }
        }

        private void SelectDataFolder()
        {
            if (!System.IO.Directory.Exists(DataAssetCreator.DataPath))
            {
                System.IO.Directory.CreateDirectory(DataAssetCreator.DataPath);
                AssetDatabase.Refresh();
            }
            var obj = AssetDatabase.LoadAssetAtPath(DataAssetCreator.DataPath, typeof(UnityEngine.Object));
            if (obj != null) EditorGUIUtility.PingObject(obj);
        }

        private void RefreshDataPresence()
        {
            _dataPresence = DataAssetCreator.CheckAllExist();
            RefreshLevelList();
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
                _levelBottleCount = EditorGUILayout.IntField("Bottle Count", _levelBottleCount);
                _levelColorCount = EditorGUILayout.IntField("Color Count", _levelColorCount);
                _levelEmptyCount = EditorGUILayout.IntField("Empty Bottles", _levelEmptyCount);
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
                level.bottleCount = _levelBottleCount;
                level.colorCount = _levelColorCount;
                level.emptyBottleCount = _levelEmptyCount;
                level.maxLayersPerBottle = _levelMaxLayers;
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

        // ── SCENE TAB ───────────────────────────────────────────────────────

        private void DrawSceneTab()
        {
            EditorGUILayout.LabelField("Scene Builder", EditorStyles.boldLabel);
            _sceneScroll = EditorGUILayout.BeginScrollView(_sceneScroll);

            // ── Environment section ─────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Environment", EditorStyles.miniBoldLabel);
                _buildOpts.newScene = EditorGUILayout.ToggleLeft("Replace current scene with new one", _buildOpts.newScene);
                _buildOpts.lighting = EditorGUILayout.ToggleLeft("Lighting (Directional + Fill + Rim)", _buildOpts.lighting);
                _buildOpts.ground = EditorGUILayout.ToggleLeft("Ground + Back wall + Dust", _buildOpts.ground);
                _buildOpts.camera = EditorGUILayout.ToggleLeft("Main Camera", _buildOpts.camera);
                _buildOpts.postProcessing = EditorGUILayout.ToggleLeft("Post-processing (Bloom + Vignette)", _buildOpts.postProcessing);
                _buildOpts.cauldron = EditorGUILayout.ToggleLeft("Cauldron + Fire particles", _buildOpts.cauldron);
                _buildOpts.gameManager = EditorGUILayout.ToggleLeft("GameManager", _buildOpts.gameManager);
            }

            // ── Quick Add Bottle ────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Quick Add Bottles", EditorStyles.miniBoldLabel);
                GUILayout.Label($"Current bottle count: {SceneBuilder.CountBottles()}", EditorStyles.miniLabel);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Number of bottles", EditorStyles.miniLabel);
                _bottleCount = EditorGUILayout.IntSlider(_bottleCount, 1, 20);

                EditorGUILayout.LabelField("Layout", EditorStyles.miniLabel);
                _bottleLayout = (SceneBuilder.BottleLayout)EditorGUILayout.EnumPopup(_bottleLayout);

                EditorGUILayout.LabelField("Shader", EditorStyles.miniLabel);
                _shaderVariant = (SceneBuilder.ShaderVariant)EditorGUILayout.EnumPopup(_shaderVariant);

                _firstEmpty = EditorGUILayout.ToggleLeft("First bottle empty", _firstEmpty);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.backgroundColor = new Color(0.15f, 0.55f, 0.90f);
                    if (GUILayout.Button("Add 1 Filled", GUILayout.Height(26)))
                        EditorApplication.delayCall += () => AddBottles(1, false);
                    if (GUILayout.Button("Add 1 Empty", GUILayout.Height(26)))
                        EditorApplication.delayCall += () => AddBottles(1, true);
                    GUI.backgroundColor = Color.white;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button($"Add {_bottleCount} Bottles", GUILayout.Height(24)))
                        EditorApplication.delayCall += () => AddBottles(_bottleCount, _firstEmpty);
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("Remove All", GUILayout.Height(24), GUILayout.MinWidth(100)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (EditorUtility.DisplayDialog("Remove all bottles?",
                                "This will delete all BottleController objects from the scene. Undo supported.", "Yes", "Cancel"))
                                SceneBuilder.RemoveBottles();
                        };
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            // ── Full scene preset ───────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Full Scene Presets", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("All (20 bottles + env)", GUILayout.Height(24)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            _buildOpts = SceneBuilder.All;
                            BuildScene();
                        };
                    }
                    if (GUILayout.Button("Minimal (bottles + camera)", GUILayout.Height(24)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            _buildOpts = SceneBuilder.Minimal;
                            BuildScene();
                        };
                    }
                    if (GUILayout.Button("Env Only", GUILayout.Height(24)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            _buildOpts = SceneBuilder.All;
                            _buildOpts.bottles = false;
                            BuildScene();
                        };
                    }
                }
            }

            // ── Setup Current Scene (DI) ─────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Setup Current Scene (DI)", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(
                    "Mevcut sahneye GameManager + GameInstaller (VContainer DI) ekler.\n" +
                    "Sahne sekmesinde Play dediğinizde VContainer hatası alıyorsanız bu butonu kullanın.",
                    MessageType.Info);

                GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
                if (GUILayout.Button("Setup Current Scene (GameManager + DI)", GUILayout.Height(28)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        SceneBuilder.SetupCurrentScene();
                        SetStatus("Current scene set up with GameManager + DI.", MessageType.Info);
                    };
                }
                GUI.backgroundColor = Color.white;
            }

            // ── Level Staging & Serialization ───────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Staging & Serialization", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(2);

                _stageLevelAsset = (LevelData)EditorGUILayout.ObjectField("Target Level", _stageLevelAsset, typeof(LevelData), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledGroupScope(_stageLevelAsset == null))
                    {
                        if (GUILayout.Button("Load Level into Scene", GUILayout.Height(26)))
                        {
                            var lvl = _stageLevelAsset;
                            EditorApplication.delayCall += () => LoadLevelIntoScene(lvl);
                        }

                        GUI.backgroundColor = new Color(0.15f, 0.65f, 0.25f);
                        if (GUILayout.Button("Export Scene to Level", GUILayout.Height(26)))
                        {
                            var lvl = _stageLevelAsset;
                            EditorApplication.delayCall += () => ExportSceneToLevel(lvl);
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void AddBottles(int count, bool firstEmpty)
        {
            try
            {
                EditorUtility.DisplayProgressBar("PuzzleGame", $"Creating {count} bottles...", 0f);
                Vector3 center = new Vector3(0f, 0f, 0f);
                var positions = SceneBuilder.ComputePositions(_bottleLayout, count, center);
                for (int i = 0; i < count; i++)
                {
                    Color[] colors;
                    if (firstEmpty && i == 0)
                        colors = System.Array.Empty<Color>();
                    else
                        colors = new[] { SceneBuilder.DefaultPalette[i % SceneBuilder.DefaultPalette.Length] };

                    SceneBuilder.CreateBottle(SceneBuilder.BottleConfig.WithColors(
                        positions[i], colors, _shaderVariant, "Bottle"));
                }
                SetStatus($"Added {count} bottles ({(firstEmpty ? "1 empty, " : "")}{count - (firstEmpty ? 1 : 0)} filled).", MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void BuildScene()
        {
            if (_buildOpts.newScene)
            {
                if (!EditorUtility.DisplayDialog("Replace current scene?",
                    "The current scene will be lost. Save first?", "Continue", "Cancel"))
                    return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("PuzzleGame Scene", "Building...", 0.3f);
                SceneBuilder.Build(_buildOpts);
                SetStatus("Scene built. Ctrl+Z to undo.", MessageType.Info);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        // ── VALIDATE TAB ────────────────────────────────────────────────────

        private struct ValidationResult
        {
            public string label;
            public string detail;
            public bool ok;
        }

        private void DrawValidateTab()
        {
            EditorGUILayout.LabelField("Project Validation", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Shader varlığı, eksik referans, palette kontrolü.",
                MessageType.None);

            if (GUILayout.Button("Run Validation", GUILayout.Height(26)))
                EditorApplication.delayCall += RunValidation;

            EditorGUILayout.Space(6);
            _validateScroll = EditorGUILayout.BeginScrollView(_validateScroll);

            foreach (var result in _validationResults)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(result.ok ? "✓" : "✗", GUILayout.Width(20));
                    EditorGUILayout.LabelField(result.label, GUILayout.MinWidth(160));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(result.detail, EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunValidation()
        {
            _validationResults.Clear();

            // Data presence
            var presence = DataAssetCreator.CheckAllExist();
            int missing = presence.Count(kvp => !kvp.Value);
            _validationResults.Add(new ValidationResult
            {
                label = "Data Assets",
                detail = missing == 0 ? "All present" : $"{missing} missing",
                ok = missing == 0
            });

            // Premium shaders
            var glassShader = AssetDatabase.FindAssets("t:Shader PremiumBottleGlass");
            var liquidShader = AssetDatabase.FindAssets("t:Shader PremiumLayeredLiquid");
            _validationResults.Add(new ValidationResult
            {
                label = "PremiumBottleGlass",
                detail = glassShader.Length > 0 ? "Found" : "Missing (fallback used)",
                ok = glassShader.Length > 0
            });
            _validationResults.Add(new ValidationResult
            {
                label = "PremiumLayeredLiquid",
                detail = liquidShader.Length > 0 ? "Found" : "Missing (fallback used)",
                ok = liquidShader.Length > 0
            });

            // Standard shaders
            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            _validationResults.Add(new ValidationResult
            {
                label = "URP/Lit shader",
                detail = litShader != null ? "OK" : "MISSING",
                ok = litShader != null
            });
            _validationResults.Add(new ValidationResult
            {
                label = "URP/Unlit shader",
                detail = unlitShader != null ? "OK" : "MISSING",
                ok = unlitShader != null
            });

            // LevelConfig palette
            var levelCfg = AssetDatabase.LoadAssetAtPath<Configuration.LevelConfig>(
                $"{DataAssetCreator.DataPath}/LevelConfig.asset");
            if (levelCfg != null)
            {
                bool paletteValid = levelCfg.palette != null && levelCfg.palette.Length >= 2;
                _validationResults.Add(new ValidationResult
                {
                    label = "LevelConfig palette",
                    detail = paletteValid ? $"{levelCfg.palette.Length} colors" : "Empty or too few",
                    ok = paletteValid
                });
            }
            else
            {
                _validationResults.Add(new ValidationResult
                {
                    label = "LevelConfig palette",
                    detail = "LevelConfig.asset missing — palette not validated",
                    ok = false
                });
            }

            // GameManager / BottleController in scene
            var gms = FindObjectsByType<GameManager>(FindObjectsInactive.Include);
            var bottles = FindObjectsByType<BottleController>(FindObjectsInactive.Include);
            _validationResults.Add(new ValidationResult
            {
                label = "Scene: GameManager",
                detail = gms.Length == 0 ? "Missing" : $"{gms.Length} found",
                ok = gms.Length > 0
            });
            _validationResults.Add(new ValidationResult
            {
                label = "Scene: BottleController",
                detail = bottles.Length == 0 ? "Missing" : $"{bottles.Length} found",
                ok = bottles.Length > 0
            });

            int failures = _validationResults.Count(r => !r.ok);
            SetStatus(failures == 0
                ? $"All {(_validationResults.Count)} checks passed."
                : $"{failures} issue(s) found.",
                failures == 0 ? MessageType.Info : MessageType.Warning);
        }

        // ── Solver & Scene Helpers ──────────────────────────────────────────

        private static List<List<LiquidLayer>> GetLevelAssignments(LevelData level, Configuration.LevelConfig levelConfig)
        {
            if (level == null) return new List<List<LiquidLayer>>();
            if (level.autoGenerate)
            {
                var generator = new DifficultyBasedLevelGenerator();
                Color[] palette = levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilder.DefaultPalette;
                var domainPalette = new DomainColor[palette.Length];
                for (int i = 0; i < palette.Length; i++)
                    domainPalette[i] = ColorAdapter.FromUnity(palette[i]);
                
                return generator.Generate(
                    level.bottleCount,
                    level.maxLayersPerBottle,
                    level.emptyBottleCount,
                    domainPalette,
                    level.difficulty,
                    level.randomSeed);
            }
            else
            {
                var assignments = new List<List<LiquidLayer>>();
                if (level.bottles != null)
                {
                    foreach (var bottle in level.bottles)
                    {
                        var layers = new List<LiquidLayer>();
                        if (!bottle.isEmpty && bottle.layers != null)
                        {
                            foreach (var layer in bottle.layers)
                            {
                                layers.Add(new LiquidLayer(ColorAdapter.FromUnity(layer.color), layer.amount));
                            }
                        }
                        assignments.Add(layers);
                    }
                }
                return assignments;
            }
        }

        private static DomainColor[] ConvertPalette(Color[] colors)
        {
            var result = new DomainColor[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                result[i] = ColorAdapter.FromUnity(colors[i]);
            return result;
        }

        private void SolveSingleLevel(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            if (levelData == null) return;

            var levelConfig = AssetDatabase.LoadAssetAtPath<Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = GetLevelAssignments(levelData, levelConfig);
            int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerBottle : 4;

            var result = LiquidSortSolver.Solve(assignments, maxLayers);
            lvl.hasSolved = true;
            lvl.isSolvable = result.IsSolvable;
            lvl.optimalMoves = result.IsSolvable ? result.SolutionPath.Count : 0;
            _existingLevels[index] = lvl;

            if (result.IsSolvable)
            {
                SetStatus($"Level {lvl.number:D2}: Solvable in {result.SolutionPath.Count} moves.", MessageType.Info);
            }
            else
            {
                SetStatus($"Level {lvl.number:D2}: UNSOLVABLE!", MessageType.Error);
            }
        }

        private void CopyLevel(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var sourceLevel = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            if (sourceLevel == null) return;

            // Find next available level number
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
                SetStatus("No available slot for copy (101-200). Delete some levels first.", MessageType.Warning);
                return;
            }

            string newPath = $"{LevelDataBatchCreator.LevelPath}/Level_{copyNumber:D2}.asset";
            
            // Create copy
            var newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.levelNumber = copyNumber;
            newLevel.difficulty = sourceLevel.difficulty;
            newLevel.bottleCount = sourceLevel.bottleCount;
            newLevel.emptyBottleCount = sourceLevel.emptyBottleCount;
            newLevel.colorCount = sourceLevel.colorCount;
            newLevel.maxLayersPerBottle = sourceLevel.maxLayersPerBottle;
            newLevel.randomSeed = copyNumber * 1337;
            newLevel.autoGenerate = sourceLevel.autoGenerate;
            
            // Copy pre-built bottles if not auto-generating
            if (!sourceLevel.autoGenerate && sourceLevel.bottles != null)
            {
                newLevel.bottles = new List<LevelBottleData>();
                foreach (var bottle in sourceLevel.bottles)
                {
                    var copy = new LevelBottleData
                    {
                        isEmpty = bottle.isEmpty,
                        layers = new List<LevelLayerData>()
                    };
                    foreach (var layer in bottle.layers)
                    {
                        copy.layers.Add(new LevelLayerData
                        {
                            color = layer.color,
                            amount = layer.amount
                        });
                    }
                    newLevel.bottles.Add(copy);
                }
            }
            
            // Set par values (slightly higher than source as it's a new level)
            newLevel.parMoves = sourceLevel.parMoves + 2;
            newLevel.goodMoves = sourceLevel.goodMoves + 3;
            
            // Copy preview image reference
            newLevel.previewImage = sourceLevel.previewImage;

            AssetDatabase.CreateAsset(newLevel, newPath);
            AssetDatabase.SaveAssets();
            
            SetStatus($"Level {lvl.number:D2} copied to Level {copyNumber:D2}.", MessageType.Info);
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

            SetStatus($"Level {lvl.number:D2} par moves optimized to {levelData.parMoves} (good moves: {levelData.goodMoves}).", MessageType.Info);
        }

        private void SolveAndVerifyAll()
        {
            var levelConfig = AssetDatabase.LoadAssetAtPath<Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int total = _existingLevels.Count;
            int unsolvableCount = 0;
            int solvableCount = 0;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    EditorUtility.DisplayProgressBar("Solving Levels", $"Solving Level {lvl.number:D2}...", (float)i / total);

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData == null) continue;

                    var assignments = GetLevelAssignments(levelData, levelConfig);
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerBottle : 4;

                    var result = LiquidSortSolver.Solve(assignments, maxLayers);
                    
                    lvl.hasSolved = true;
                    lvl.isSolvable = result.IsSolvable;
                    lvl.optimalMoves = result.IsSolvable ? result.SolutionPath.Count : 0;
                    _existingLevels[i] = lvl;

                    if (result.IsSolvable) solvableCount++;
                    else unsolvableCount++;
                }

                SetStatus($"Verification completed. Solvable: {solvableCount}, Unsolvable: {unsolvableCount}", 
                    unsolvableCount == 0 ? MessageType.Info : MessageType.Warning);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void AutoReseedUnsolvableLevels()
        {
            var levelConfig = AssetDatabase.LoadAssetAtPath<Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int reseededCount = 0;

            try
            {
                for (int i = 0; i < _existingLevels.Count; i++)
                {
                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData == null) continue;

                    // Solve first to check
                    var assignments = GetLevelAssignments(levelData, levelConfig);
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerBottle : 4;
                    var result = LiquidSortSolver.Solve(assignments, maxLayers);

                    if (result.IsSolvable) continue;

                    // Unsolvable! Let's find a working seed
                    EditorUtility.DisplayProgressBar("Reseeding", $"Finding seed for Level {lvl.number:D2}...", (float)i / _existingLevels.Count);

                    int seed = levelData.randomSeed;
                    bool found = false;
                    for (int attempt = 1; attempt <= 100; attempt++)
                    {
                        seed += 1337; // Change seed
                        // Generate with new seed
                        var tempAssignments = new DifficultyBasedLevelGenerator().Generate(
                            levelData.bottleCount,
                            levelData.maxLayersPerBottle,
                            levelData.emptyBottleCount,
                            ConvertPalette(levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilder.DefaultPalette),
                            levelData.difficulty,
                            seed);

                        var tempResult = LiquidSortSolver.Solve(tempAssignments, maxLayers);
                        if (tempResult.IsSolvable)
                        {
                            // Save seed
                            levelData.randomSeed = seed;
                            
                            // Auto-optimize par
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
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SetStatus($"Reseed complete. Successfully reseeded & solved {reseededCount} levels.", MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void AutoOptimizeAllPars()
        {
            var levelConfig = AssetDatabase.LoadAssetAtPath<Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int optimizedCount = 0;

            try
            {
                for (int i = 0; i < _existingLevels.Count; i++)
                {
                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData == null) continue;

                    var assignments = GetLevelAssignments(levelData, levelConfig);
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerBottle : 4;
                    var result = LiquidSortSolver.Solve(assignments, maxLayers);

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
                }

                AssetDatabase.SaveAssets();
                SetStatus($"Optimized par/good moves for {optimizedCount} solvable levels.", MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
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
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            LoadLevelIntoScene(levelData);
            EditorApplication.isPlaying = true;
        }

        private void LoadLevelIntoScene(LevelData level)
        {
            if (level == null)
            {
                SetStatus("No level selected to load.", MessageType.Warning);
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Load Level {level.levelNumber}");

            // 1. Clear existing bottles
            SceneBuilder.RemoveBottles();

            // 2. Load level Config
            var levelConfig = AssetDatabase.LoadAssetAtPath<Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = GetLevelAssignments(level, levelConfig);

            // 3. Compute positions and instantiate bottles
            int count = assignments.Count;
            var layout = SceneBuilder.BottleLayout.Grid;
            Vector3 center = Vector3.zero;
            var positions = SceneBuilder.ComputePositions(layout, count, center);

            for (int i = 0; i < count; i++)
            {
                var layers = assignments[i];
                var colors = new Color[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                    colors[j] = ColorAdapter.ToUnity(layers[j].Color);

                // Build with layers
                var bottleCfg = SceneBuilder.BottleConfig.WithLayers(
                    positions[i],
                    layers,
                    SceneBuilder.ShaderVariant.Premium,
                    $"Bottle_{i:D2}");
                
                SceneBuilder.CreateBottle(bottleCfg);
            }

            Undo.CollapseUndoOperations(undoGroup);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            SetStatus($"Loaded Level {level.levelNumber} into active scene ({count} bottles).", MessageType.Info);
        }

        private void ExportSceneToLevel(LevelData level)
        {
            if (level == null)
            {
                SetStatus("No level selected to export to.", MessageType.Warning);
                return;
            }

            var bottles = FindObjectsByType<BottleController>(FindObjectsInactive.Include);
            if (bottles.Length == 0)
            {
                SetStatus("No bottles found in the scene to export.", MessageType.Warning);
                return;
            }

            var sortedBottles = bottles
                .OrderByDescending(b => b.transform.position.z)
                .ThenBy(b => b.transform.position.x)
                .ToArray();

            level.autoGenerate = false;
            level.bottleCount = sortedBottles.Length;
            level.bottles.Clear();

            int emptyCount = 0;

            foreach (var bottle in sortedBottles)
            {
                var bottleData = new LevelBottleData();
                bottleData.isEmpty = bottle.IsEmpty;
                if (bottleData.isEmpty)
                {
                    emptyCount++;
                }

                bottleData.layers = new List<LevelLayerData>();
                if (!bottle.IsEmpty && bottle.State != null && bottle.State.Layers != null)
                {
                    foreach (var layer in bottle.State.Layers)
                    {
                        var layerData = new LevelLayerData();
                        layerData.color = ColorAdapter.ToUnity(layer.Color);
                        layerData.amount = layer.Amount;
                        bottleData.layers.Add(layerData);
                    }
                }
                level.bottles.Add(bottleData);
            }

            level.emptyBottleCount = emptyCount;

            var levelConfig = AssetDatabase.LoadAssetAtPath<Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = GetLevelAssignments(level, levelConfig);
            int maxLayers = level.bottles.Count > 0 && level.bottles[0].layers != null && level.bottles[0].layers.Count > 0 
                ? level.bottles.Select(b => b.layers?.Count ?? 0).Max() 
                : 4;
            if (maxLayers < 4) maxLayers = 4;

            var result = LiquidSortSolver.Solve(assignments, maxLayers);
            if (result.IsSolvable)
            {
                level.parMoves = result.SolutionPath.Count;
                level.goodMoves = Mathf.RoundToInt(result.SolutionPath.Count * 1.4f);
                if (level.goodMoves < level.parMoves + 2) level.goodMoves = level.parMoves + 2;
                SetStatus($"Exported scene to Level {level.levelNumber} successfully. Solvable in {result.SolutionPath.Count} moves (Par auto-assigned).", MessageType.Info);
            }
            else
            {
                level.parMoves = 10;
                level.goodMoves = 15;
                SetStatus($"Exported scene to Level {level.levelNumber} successfully, but layout is UNSOLVABLE! Reset par to defaults.", MessageType.Warning);
            }

            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshLevelList();
        }

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
                
                var levelConfig = AssetDatabase.LoadAssetAtPath<Configuration.LevelConfig>(
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

        // ── Status bar ──────────────────────────────────────────────────────

        private void DrawStatusBar()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }
    }
}
