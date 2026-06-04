using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleGame.Editor
{
    public class ValidateTab : IEditorTab
    {
        public string TabName => "Validate";
        private ForgeEditorWindow _window;

        private List<ValidationResult> _validationResults = new List<ValidationResult>();
        private Vector2 _validateScroll;

        private struct ValidationResult
        {
            public string label;
            public string detail;
            public bool ok;
        }

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Project Validation", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Checks shader existence, missing references, and color palette health.",
                MessageType.None);

            if (GUILayout.Button("Run Validation", GUILayout.Height(26)))
                EditorApplication.delayCall += RunValidation;

            EditorGUILayout.Space(6);
            if (_validationResults.Count > 0)
            {
                int passed = _validationResults.Count(r => r.ok);
                int failed = _validationResults.Count - passed;

                if (failed == 0)
                {
                    EditorGUILayout.HelpBox($"✓ Bütün Kontroller Başarılı ({passed}/{passed})", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"⚠️ {failed} Kontrol Başarısız! Lütfen aşağıdaki sorunları düzeltin.", MessageType.Error);
                }
                EditorGUILayout.Space(4);
            }

            _validateScroll = EditorGUILayout.BeginScrollView(_validateScroll);

            foreach (var result in _validationResults)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var badgeColor = result.ok ? new Color(0.12f, 0.75f, 0.12f, 1f) : new Color(0.85f, 0.2f, 0.2f, 1f);
                    var badgeText = result.ok ? "● OK" : "● FAIL";

                    var oldColor = GUI.contentColor;
                    GUI.contentColor = badgeColor;
                    GUILayout.Label(badgeText, EditorStyles.boldLabel, GUILayout.Width(60));
                    GUI.contentColor = oldColor;

                    EditorGUILayout.LabelField(result.label, EditorStyles.boldLabel, GUILayout.MinWidth(160));
                    GUILayout.FlexibleSpace();
                    
                    // Display detail with warning coloring if not ok
                    if (!result.ok)
                    {
                        GUI.contentColor = new Color(0.9f, 0.6f, 0.1f, 1f);
                        EditorGUILayout.LabelField(result.detail, EditorStyles.miniBoldLabel);
                        GUI.contentColor = oldColor;
                    }
                    else
                    {
                        EditorGUILayout.LabelField(result.detail, EditorStyles.miniLabel);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
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
            var glassShader = AssetDatabase.FindAssets("t:Shader PremiumMoldGlass");
            var OreShader = AssetDatabase.FindAssets("t:Shader PremiumLayeredOre");
            _validationResults.Add(new ValidationResult
            {
                label = "PremiumMoldGlass",
                detail = glassShader.Length > 0 ? "Found" : "Missing (fallback used)",
                ok = glassShader.Length > 0
            });
            _validationResults.Add(new ValidationResult
            {
                label = "PremiumLayeredOre",
                detail = OreShader.Length > 0 ? "Found" : "Missing (fallback used)",
                ok = OreShader.Length > 0
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
            var levelCfg = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>(
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

            // GameManager / MoldController in scene
            var gms = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include);
            var Molds = Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            _validationResults.Add(new ValidationResult
            {
                label = "Scene: GameManager",
                detail = gms.Length == 0 ? "Missing" : $"{gms.Length} found",
                ok = gms.Length > 0
            });
            _validationResults.Add(new ValidationResult
            {
                label = "Scene: MoldController",
                detail = Molds.Length == 0 ? "Missing" : $"{Molds.Length} found",
                ok = Molds.Length > 0
            });

            // GPU Instancing Validation (AAA Mobile Draw Call optimization)
            bool gpuInstancingOk = true;
            List<string> nonInstancedMats = new List<string>();
            for (int i = 0; i < Molds.Length; i++)
            {
                var mold = Molds[i];
                if (mold.glassMaterial != null && !mold.glassMaterial.enableInstancing)
                {
                    gpuInstancingOk = false;
                    if (!nonInstancedMats.Contains(mold.glassMaterial.name))
                        nonInstancedMats.Add(mold.glassMaterial.name);
                }
                if (mold.OreMaterial != null && !mold.OreMaterial.enableInstancing)
                {
                    gpuInstancingOk = false;
                    if (!nonInstancedMats.Contains(mold.OreMaterial.name))
                        nonInstancedMats.Add(mold.OreMaterial.name);
                }
            }
            _validationResults.Add(new ValidationResult
            {
                label = "Materials GPU Instancing",
                detail = gpuInstancingOk 
                    ? "All materials have GPU Instancing enabled." 
                    : $"Missing on: {string.Join(", ", nonInstancedMats)}",
                ok = gpuInstancingOk
            });

            // Target Frame Rate Validation (Mobile Refresh Rate / Smoothness optimization)
            bool targetFrameRateFound = false;
            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                if (path.StartsWith("Assets/"))
                {
                    var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (monoScript != null && monoScript.text.Contains("targetFrameRate"))
                    {
                        targetFrameRateFound = true;
                        break;
                    }
                }
            }
            _validationResults.Add(new ValidationResult
            {
                label = "Target Frame Rate (FPS) Setup",
                detail = targetFrameRateFound 
                    ? "Application.targetFrameRate found in scripts." 
                    : "Warning: targetFrameRate NOT configured! Game may lock at 30 FPS on mobile.",
                ok = targetFrameRateFound
            });

            int failures = _validationResults.Count(r => !r.ok);
            _window.SetStatus(failures == 0
                ? $"All {_validationResults.Count} checks passed."
                : $"{failures} issue(s) found.",
                failures == 0 ? MessageType.Info : MessageType.Warning);
        }
    }
}
