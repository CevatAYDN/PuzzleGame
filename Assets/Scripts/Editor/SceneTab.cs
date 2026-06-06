using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Infrastructure;

namespace PuzzleGame.Editor
{
    public class SceneTab : IEditorTab
    {
        public string TabName => "Scene";
        private ForgeEditorWindow _window;

        private SceneBuilderModel.BuildOptions _buildOpts = SceneBuilderModel.All;
        private SceneBuilderModel.MoldLayout _MoldLayout = SceneBuilderModel.MoldLayout.Grid;
        private SceneBuilderModel.ShaderVariant _shaderVariant = SceneBuilderModel.ShaderVariant.Premium;
        private int _MoldCount = 2;
        private bool _firstEmpty = true;
        private Vector2 _sceneScroll;
        private LevelData _stageLevelAsset;
        private bool _paintMode = false;
        private Color _paintColor = Color.red;
        private float _paintAmount = 0.25f;

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Scene Builder", EditorStyles.boldLabel);
            _sceneScroll = EditorGUILayout.BeginScrollView(_sceneScroll);

            // ── Paint Mode Section ──────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("🎨 Scene Paint Mode", EditorStyles.miniBoldLabel);
                EditorGUI.BeginChangeCheck();
                _paintMode = EditorGUILayout.ToggleLeft("Enable Visual Painter in Scene View", _paintMode);
                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }

                if (_paintMode)
                {
                    EditorGUILayout.HelpBox(
                        "Paint Mode is ACTIVE. Go to the Scene View to visually paint colors, push/pop layers, and clear molds.",
                        MessageType.Info);
                    _paintColor = EditorGUILayout.ColorField("Current Paint Color", _paintColor);
                    _paintAmount = EditorGUILayout.Slider("Layer Fill Amount", _paintAmount, 0.05f, 1f);
                }
            }

            EditorGUILayout.Space(4);

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

            // ── Quick Add Mold ────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Quick Add Molds", EditorStyles.miniBoldLabel);
                GUILayout.Label($"Current Mold count: {SceneBuilder.CountMolds()}", EditorStyles.miniLabel);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Number of Molds", EditorStyles.miniLabel);
                _MoldCount = EditorGUILayout.IntSlider(_MoldCount, 1, 20);

                EditorGUILayout.LabelField("Layout", EditorStyles.miniLabel);
                _MoldLayout = (SceneBuilderModel.MoldLayout)EditorGUILayout.EnumPopup(_MoldLayout);

                EditorGUILayout.LabelField("Shader", EditorStyles.miniLabel);
                _shaderVariant = (SceneBuilderModel.ShaderVariant)EditorGUILayout.EnumPopup(_shaderVariant);

                _firstEmpty = EditorGUILayout.ToggleLeft("First Mold empty", _firstEmpty);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.backgroundColor = new Color(0.15f, 0.55f, 0.90f);
                    if (GUILayout.Button("Add 1 Filled", GUILayout.Height(26)))
                        EditorApplication.delayCall += () => AddMolds(1, false);
                    if (GUILayout.Button("Add 1 Empty", GUILayout.Height(26)))
                        EditorApplication.delayCall += () => AddMolds(1, true);
                    GUI.backgroundColor = Color.white;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button($"Add {_MoldCount} Molds", GUILayout.Height(24)))
                        EditorApplication.delayCall += () => AddMolds(_MoldCount, _firstEmpty);
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("Remove All", GUILayout.Height(24), GUILayout.MinWidth(100)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (EditorUtility.DisplayDialog("Remove all Molds?",
                                "This will delete all MoldController objects from the scene. Undo supported.", "Yes", "Cancel"))
                                SceneBuilder.RemoveMolds();
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
                    if (GUILayout.Button("All (20 Molds + env)", GUILayout.Height(24)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            _buildOpts = SceneBuilderModel.All;
                            BuildScene();
                        };
                    }
                    if (GUILayout.Button("Minimal (Molds + camera)", GUILayout.Height(24)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            _buildOpts = SceneBuilderModel.Minimal;
                            BuildScene();
                        };
                    }
                    if (GUILayout.Button("Env Only", GUILayout.Height(24)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            _buildOpts = SceneBuilderModel.All;
                            _buildOpts.Molds = false;
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
                        _window.SetStatus("Current scene set up with GameManager + DI.", MessageType.Info);
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
                            EditorApplication.delayCall += () => _window.LoadLevelIntoScene(lvl);
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

        public void OnSceneGUI(SceneView sceneView)
        {
            var Molds = Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            
            // Draw floating palette overlay in Scene View
            if (_paintMode)
            {
                Handles.BeginGUI();
                var rect = new Rect(10, 10, 240, 150);
                GUILayout.BeginArea(rect, "Mold Painter Palette", GUI.skin.window);
                using (new GUILayout.VerticalScope())
                {
                    _paintMode = GUILayout.Toggle(_paintMode, " Painter Active", GUI.skin.button);
                    if (!_paintMode) SceneView.RepaintAll();

                    EditorGUILayout.Space(2);
                    _paintColor = EditorGUILayout.ColorField("Color", _paintColor);
                    _paintAmount = GUILayout.HorizontalSlider(_paintAmount, 0.05f, 1f);
                    GUILayout.Label($"Fill: {_paintAmount:F2}", EditorStyles.miniLabel);

                    EditorGUILayout.Space(2);
                    GUILayout.Label("Quick Palette:", EditorStyles.miniBoldLabel);
                    using (new GUILayout.HorizontalScope())
                    {
                        var quickColors = new[] { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
                        foreach (var qc in quickColors)
                        {
                            var oldBg = GUI.backgroundColor;
                            GUI.backgroundColor = qc;
                            if (GUILayout.Button("", GUILayout.Width(22), GUILayout.Height(22)))
                            {
                                _paintColor = qc;
                            }
                            GUI.backgroundColor = oldBg;
                        }
                    }
                }
                GUILayout.EndArea();
                Handles.EndGUI();
            }

            foreach (var Mold in Molds)
            {
                if (Mold == null || Mold.transform == null) continue;

                // Move Handles
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(Mold.transform.position, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Mold.transform, "Move Mold");
                    Mold.transform.position = newPos;
                }

                // Info Label Overlay
                Handles.Label(Mold.transform.position + Vector3.up * 2.2f, 
                    $"{Mold.name}\nLayers: {(Mold.State != null ? Mold.State.LayerCount : 0)}", 
                    EditorStyles.boldLabel);

                // Draw clickable 2D buttons over each Mold in Scene View
                if (_paintMode)
                {
                    Vector2 screenPos = HandleUtility.WorldToGUIPoint(Mold.transform.position + Vector3.up * 1f);
                    Handles.BeginGUI();
                    GUILayout.BeginArea(new Rect(screenPos.x - 45, screenPos.y - 30, 90, 60));
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Mold #{Mold.MoldIndex:D2}", EditorStyles.miniBoldLabel);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                            if (GUILayout.Button("+", GUILayout.Width(22))) { AddLayerToMold(Mold, _paintColor, _paintAmount); }
                            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
                            if (GUILayout.Button("-", GUILayout.Width(22))) { PopLayerFromMold(Mold); }
                            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
                            if (GUILayout.Button("C", GUILayout.Width(22))) { ClearMoldLayers(Mold); }
                            GUI.backgroundColor = Color.white;
                        }
                    }
                    GUILayout.EndArea();
                    Handles.EndGUI();
                }
            }
        }

        private void AddLayerToMold(MoldController mold, Color color, float amount)
        {
            Undo.RecordObject(mold, "Add Layer");
            SerializedObject so = new SerializedObject(mold);
            SerializedProperty layersProp = so.FindProperty("_serializedLayers");
            if (layersProp == null) return;
            
            int nextIndex = layersProp.arraySize;
            if (nextIndex >= 4) return; // cap at 4 layers
            
            layersProp.InsertArrayElementAtIndex(nextIndex);
            SerializedProperty newElement = layersProp.GetArrayElementAtIndex(nextIndex);
            newElement.FindPropertyRelative("color").colorValue = color;
            newElement.FindPropertyRelative("amount").floatValue = amount;
            
            so.ApplyModifiedProperties();
            
            // Auto sync name and dirty
            mold.gameObject.name = $"Mold_{mold.MoldIndex:D2}";
            
            EditorUtility.SetDirty(mold);
            var method = mold.GetType().GetMethod("RestoreStateFromSerialized", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(mold, new object[] { false });
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mold.gameObject.scene);
        }

        private void PopLayerFromMold(MoldController mold)
        {
            Undo.RecordObject(mold, "Pop Layer");
            SerializedObject so = new SerializedObject(mold);
            SerializedProperty layersProp = so.FindProperty("_serializedLayers");
            if (layersProp == null || layersProp.arraySize == 0) return;
            
            layersProp.DeleteArrayElementAtIndex(layersProp.arraySize - 1);
            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(mold);
            var method = mold.GetType().GetMethod("RestoreStateFromSerialized", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(mold, new object[] { false });
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mold.gameObject.scene);
        }

        private void ClearMoldLayers(MoldController mold)
        {
            Undo.RecordObject(mold, "Clear Mold Layers");
            SerializedObject so = new SerializedObject(mold);
            SerializedProperty layersProp = so.FindProperty("_serializedLayers");
            if (layersProp == null) return;
            
            layersProp.ClearArray();
            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(mold);
            var method = mold.GetType().GetMethod("RestoreStateFromSerialized", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(mold, new object[] { false });
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mold.gameObject.scene);
        }

        private void AddMolds(int count, bool firstEmpty)
        {
            try
            {
                EditorUtility.DisplayProgressBar("PuzzleGame", $"Creating {count} Molds...", 0f);
                Vector3 center = new Vector3(0f, 0f, 0f);
                var positions = SceneBuilder.ComputePositions(_MoldLayout, count, center);
                for (int i = 0; i < count; i++)
                {
                    Color[] colors;
                    if (firstEmpty && i == 0)
                        colors = System.Array.Empty<Color>();
                    else
                    {
                        var color = SceneBuilderModel.DefaultPalette[i % SceneBuilderModel.DefaultPalette.Length];
                        colors = new[] { color, color, color, color };
                    }

                    SceneBuilder.CreateMold(SceneBuilderModel.MoldConfig.WithColors(
                        positions[i], colors, _shaderVariant, "Mold"));
                }
                _window.SetStatus($"Added {count} Molds ({(firstEmpty ? "1 empty, " : "")}{count - (firstEmpty ? 1 : 0)} filled).", MessageType.Info);
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
                _window.SetStatus("Scene built. Ctrl+Z to undo.", MessageType.Info);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private void ExportSceneToLevel(LevelData level)
        {
            if (level == null)
            {
                _window.SetStatus("No level selected to export to.", MessageType.Warning);
                return;
            }

            var Molds = Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            if (Molds.Length == 0)
            {
                _window.SetStatus("No Molds found in the scene to export.", MessageType.Warning);
                return;
            }

            var sortedMolds = Molds
                .OrderByDescending(b => b.transform.position.z)
                .ThenBy(b => b.transform.position.x)
                .ToArray();

            level.autoGenerate = false;
            level.MoldCount = sortedMolds.Length;
            if (level.Molds == null)
            {
                level.Molds = new List<LevelMoldData>();
            }
            else
            {
                level.Molds.Clear();
            }

            int emptyCount = 0;

            foreach (var Mold in sortedMolds)
            {
                var MoldData = new LevelMoldData();
                MoldData.isEmpty = Mold.IsEmpty;
                if (MoldData.isEmpty)
                {
                    emptyCount++;
                }

                MoldData.layers = new List<LevelLayerData>();
                if (!Mold.IsEmpty && Mold.State != null && Mold.State.Layers != null)
                {
                    foreach (var layer in Mold.State.Layers)
                    {
                        var layerData = new LevelLayerData();
                        layerData.color = ColorAdapter.ToUnityStatic(layer.Color);
                        layerData.amount = layer.Amount;
                        MoldData.layers.Add(layerData);
                    }
                }
                level.Molds.Add(MoldData);
            }

            level.emptyMoldCount = emptyCount;

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var result = LevelSolverUtility.SolveLevel(level, levelConfig);
            if (result.IsSolvable)
            {
                level.parMoves = result.SolutionPath.Count;
                level.goodMoves = Mathf.RoundToInt(result.SolutionPath.Count * 1.4f);
                if (level.goodMoves < level.parMoves + 2) level.goodMoves = level.parMoves + 2;
                _window.SetStatus($"Exported scene to Level {level.levelNumber} successfully. Solvable in {result.SolutionPath.Count} moves (Par auto-assigned).", MessageType.Info);
            }
            else
            {
                level.parMoves = 10;
                level.goodMoves = 15;
                _window.SetStatus($"Exported scene to Level {level.levelNumber} successfully, but layout is UNSOLVABLE! Reset par to defaults.", MessageType.Warning);
            }

            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _window.RefreshLevelList();
        }
    }
}
