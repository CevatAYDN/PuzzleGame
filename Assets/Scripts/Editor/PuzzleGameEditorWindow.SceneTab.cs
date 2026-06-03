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
        // ── Scene tab ───────────────────────────────────────────────────────
        private SceneBuilder.BuildOptions _buildOpts = SceneBuilder.All;
        private SceneBuilder.BottleLayout _bottleLayout = SceneBuilder.BottleLayout.Grid;
        private SceneBuilder.ShaderVariant _shaderVariant = SceneBuilder.ShaderVariant.Premium;
        private int _bottleCount = 2;
        private bool _firstEmpty = true;
        private Vector2 _sceneScroll;
        private LevelData _stageLevelAsset;

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
    }
}
