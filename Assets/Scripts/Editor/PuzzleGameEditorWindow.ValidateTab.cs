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
        // ── Validate tab ────────────────────────────────────────────────────
        private List<ValidationResult> _validationResults = new List<ValidationResult>();
        private Vector2 _validateScroll;

        private struct ValidationResult
        {
            public string label;
            public string detail;
            public bool ok;
        }

        // ── VALIDATE TAB ────────────────────────────────────────────────────

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
    }
}
