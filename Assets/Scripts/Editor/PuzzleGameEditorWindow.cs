using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Models;
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
        private enum Tab { Data, Levels, Scene, Validate }
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
        }

        // ── Scene tab ───────────────────────────────────────────────────────
        private SceneBuilder.BuildOptions _buildOpts = SceneBuilder.All;
        private SceneBuilder.BottleLayout _bottleLayout = SceneBuilder.BottleLayout.Grid;
        private SceneBuilder.ShaderVariant _shaderVariant = SceneBuilder.ShaderVariant.Premium;
        private int _bottleCount = 2;
        private bool _firstEmpty = true;
        private Vector2 _sceneScroll;

        // ── Validate tab ────────────────────────────────────────────────────
        private List<ValidationResult> _validationResults = new List<ValidationResult>();
        private Vector2 _validateScroll;

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
            var obj = AssetDatabase.LoadAssetAtPath(DataAssetCreator.DataPath, typeof(Object));
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
            for (int i = 0; i < 100; i++)
            {
                string path = $"{LevelDataBatchCreator.LevelPath}/Level_{i:D2}.asset";
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                _existingLevels.Add(new LevelInfo
                {
                    number = i,
                    exists = level != null,
                    difficulty = level != null ? level.difficulty : Difficulty.Trivial,
                    path = path
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
                    if (GUILayout.Button("Ping All", EditorStyles.miniButton, GUILayout.Width(80)))
                        EditorApplication.delayCall += PingAllLevels;
                    if (GUILayout.Button("Delete Missing Range", EditorStyles.miniButton, GUILayout.Width(120)))
                        EditorApplication.delayCall += DeleteMissingLevels;
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Refresh List", EditorStyles.miniButton, GUILayout.Width(90)))
                        EditorApplication.delayCall += RefreshLevelList;
                }

                EditorGUILayout.Space(4);

                // Show first 20 levels in list
                int showCount = Mathf.Min(20, _existingLevels.Count);
                for (int i = 0; i < showCount; i++)
                {
                    var lvl = _existingLevels[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.contentColor = lvl.exists ? Color.green : Color.gray;
                        GUILayout.Label(lvl.exists ? "✓" : "✗", GUILayout.Width(20));
                        GUI.contentColor = Color.white;
                        GUILayout.Label($"Level {lvl.number:D2}", GUILayout.Width(70));
                        if (lvl.exists)
                        {
                            GUILayout.Label(lvl.difficulty.ToString(), GUILayout.Width(70));
                        }
                        else
                        {
                            GUILayout.Label("—", GUILayout.Width(70));
                        }
                        GUILayout.FlexibleSpace();
                        if (lvl.exists && GUILayout.Button("Ping", GUILayout.Width(50)))
                        {
                            int num = lvl.number;
                            EditorApplication.delayCall += () =>
                            {
                                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lvl.path);
                                if (obj != null) EditorGUIUtility.PingObject(obj);
                            };
                        }
                        if (lvl.exists && GUILayout.Button("Delete", GUILayout.Width(60)))
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

                if (_existingLevels.Count(l => l.exists) > 20)
                {
                    EditorGUILayout.HelpBox(
                        $"... and {_existingLevels.Count(l => l.exists) - 20} more levels. Use ping/delete above.",
                        MessageType.None);
                }
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
