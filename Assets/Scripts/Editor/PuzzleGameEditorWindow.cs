using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// PuzzleGame için tüm editor araçları — tek pencerede 3 sekme:
    ///   - Data: ScriptableObject asset yönetimi
    ///   - Scene: Sahne oluşturma kontrolü
    ///   - Validate: Proje sağlık kontrolleri
    /// </summary>
    public class PuzzleGameEditorWindow : EditorWindow
    {
        private enum Tab { Data, Scene, Validate }
        private Tab _activeTab = Tab.Data;

        // ── Data tab ────────────────────────────────────────────────────────
        private bool _overrideExisting = false;
        private Dictionary<string, bool> _dataPresence = new Dictionary<string, bool>();
        private Vector2 _dataScroll;

        // ── Scene tab ───────────────────────────────────────────────────────
        private SceneBuilder.BuildOptions _buildOpts = SceneBuilder.All;
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
            if (GUILayout.Toggle(_activeTab == Tab.Scene, "Scene", EditorStyles.toolbarButton)) _activeTab = Tab.Scene;
            if (GUILayout.Toggle(_activeTab == Tab.Validate, "Validate", EditorStyles.toolbarButton)) _activeTab = Tab.Validate;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshDataPresence();
                SetStatus("Refreshed.", MessageType.Info);
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
                        PingAsset(kvp.Key);
                    if (GUILayout.Button("Reset", GUILayout.Width(50)))
                        ResetSingle(kvp.Key);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create / Update All", GUILayout.Height(28)))
                    CreateAllData();
                if (GUILayout.Button("Select Folder", GUILayout.Height(28), GUILayout.MaxWidth(140)))
                    SelectDataFolder();
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
                        RevealSaveFile();
                    if (GUILayout.Button("Verify Integrity", GUILayout.Height(22)))
                        VerifySaveFile();
                    GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
                    if (GUILayout.Button("Delete Save", GUILayout.Height(22)))
                        DeleteSaveFile();
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
        }

        // ── SCENE TAB ───────────────────────────────────────────────────────

        private void DrawSceneTab()
        {
            EditorGUILayout.LabelField("Scene Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Sahneye eklenecek öğeleri seçin. New Scene = mevcut sahneyi sil ve temiz başla.",
                MessageType.None);

            _sceneScroll = EditorGUILayout.BeginScrollView(_sceneScroll);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Scene", EditorStyles.miniBoldLabel);
                _buildOpts.newScene = EditorGUILayout.ToggleLeft("Replace current scene with new one", _buildOpts.newScene);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Environment", EditorStyles.miniBoldLabel);
                _buildOpts.lighting = EditorGUILayout.ToggleLeft("Lighting (Main + Fill + Rim)", _buildOpts.lighting);
                _buildOpts.ground = EditorGUILayout.ToggleLeft("Ground + Back wall + Dust", _buildOpts.ground);
                _buildOpts.postProcessing = EditorGUILayout.ToggleLeft("Post-processing (Bloom + Vignette)", _buildOpts.postProcessing);
                _buildOpts.cauldron = EditorGUILayout.ToggleLeft("Cauldron + Fire particles", _buildOpts.cauldron);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Gameplay", EditorStyles.miniBoldLabel);
                _buildOpts.camera = EditorGUILayout.ToggleLeft("Main Camera", _buildOpts.camera);
                _buildOpts.bottles = EditorGUILayout.ToggleLeft("Bottles (20 in grid layout)", _buildOpts.bottles);
                _buildOpts.gameManager = EditorGUILayout.ToggleLeft("GameManager", _buildOpts.gameManager);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("All", GUILayout.Width(60))) _buildOpts = SceneBuilder.All;
                if (GUILayout.Button("Minimal", GUILayout.Width(60))) _buildOpts = SceneBuilder.Minimal;
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(0.7f, 0.9f, 0.7f);
                if (GUILayout.Button("Build Scene", GUILayout.Height(28), GUILayout.MaxWidth(180)))
                    BuildScene();
                GUI.backgroundColor = Color.white;
            }
        }

        private void BuildScene()
        {
            if (_buildOpts.newScene)
            {
                if (!EditorUtility.DisplayDialog(
                    "Replace current scene?",
                    "Mevcut sahne kaybolacak. Önce kaydetmek ister misin?",
                    "Devam", "İptal"))
                {
                    return;
                }
            }

            try
            {
                EditorUtility.DisplayProgressBar("PuzzleGame Scene", "Building...", 0.3f);
                SceneBuilder.Build(_buildOpts);
                SetStatus("Scene built. Undo supported.", MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
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
                RunValidation();

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
