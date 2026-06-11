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

        /// <summary>
        /// Quick-fix action type: what can be done to resolve a failed check.
        /// </summary>
        private enum QuickFixType
        {
            None,
            /// <summary>Ping the failing asset in the Project window.</summary>
            PingAsset,
            /// <summary>Select the failing GameObject(s) in the Hierarchy.</summary>
            SelectObjects,
            /// <summary>Enable GPU Instancing on the named materials.</summary>
            EnableGpuInstancing,
            /// <summary>Create missing data asset(s) via DataAssetCreator.</summary>
            CreateMissingData,
        }

        private struct ValidationResult
        {
            public string label;
            public string detail;
            public bool ok;

            // Quick-fix support
            public QuickFixType fixType;
            /// <summary>Asset path for PingAsset / CreateMissingData.</summary>
            public string fixAssetPath;
            /// <summary>Objects to select for SelectObjects.</summary>
            public Object[] fixObjects;
            /// <summary>Material names for EnableGpuInstancing.</summary>
            public string[] fixMaterialNames;
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
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var badgeColor = result.ok ? new Color(0.12f, 0.75f, 0.12f, 1f) : new Color(0.85f, 0.2f, 0.2f, 1f);
                        var badgeText = result.ok ? "● OK" : "● FAIL";

                        var oldColor = GUI.contentColor;
                        GUI.contentColor = badgeColor;
                        GUILayout.Label(badgeText, EditorStyles.boldLabel, GUILayout.Width(60));
                        GUI.contentColor = oldColor;

                        EditorGUILayout.LabelField(result.label, EditorStyles.boldLabel, GUILayout.MinWidth(160));
                        GUILayout.FlexibleSpace();

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

                    if (!result.ok && result.fixType != QuickFixType.None)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(64);
                            if (GUILayout.Button("⚡ Quick Fix", GUILayout.Width(90), GUILayout.Height(18)))
                                ApplyQuickFix(result);
                            if (result.fixType == QuickFixType.PingAsset && !string.IsNullOrEmpty(result.fixAssetPath))
                            {
                                if (GUILayout.Button("🔍 Ping", GUILayout.Width(60), GUILayout.Height(18)))
                                    PingAsset(result.fixAssetPath);
                            }
                            if (result.fixType == QuickFixType.SelectObjects && result.fixObjects != null)
                            {
                                if (GUILayout.Button("🔍 Select", GUILayout.Width(64), GUILayout.Height(18)))
                                    Selection.objects = result.fixObjects;
                            }
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private void PingAsset(string assetPath)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj != null)
                EditorGUIUtility.PingObject(obj);
            else
                Debug.LogWarning($"[ValidateTab] Asset not found at: {assetPath}");
        }

        private void ApplyQuickFix(ValidationResult result)
        {
            switch (result.fixType)
            {
                case QuickFixType.PingAsset:
                    PingAsset(result.fixAssetPath);
                    break;

                case QuickFixType.SelectObjects:
                    if (result.fixObjects != null)
                        Selection.objects = result.fixObjects;
                    break;

                case QuickFixType.EnableGpuInstancing:
                    if (result.fixMaterialNames != null)
                    {
                        foreach (var matName in result.fixMaterialNames)
                        {
                            var guids = AssetDatabase.FindAssets($"t:Material {matName}");
                            foreach (var guid in guids)
                            {
                                var path = AssetDatabase.GUIDToAssetPath(guid);
                                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                                if (mat != null && !mat.enableInstancing)
                                {
                                    mat.enableInstancing = true;
                                    EditorUtility.SetDirty(mat);
                                }
                            }
                        }
                        AssetDatabase.SaveAssets();
                        _window.SetStatus("GPU Instancing enabled on failing materials.", MessageType.Info);
                        EditorApplication.delayCall += RunValidation;
                    }
                    break;

                case QuickFixType.CreateMissingData:
                    DataAssetCreator.CreateAllDefaults(_ => false);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    _window.SetStatus("Missing data assets created with defaults.", MessageType.Info);
                    EditorApplication.delayCall += RunValidation;
                    break;
            }
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
                ok = missing == 0,
                fixType = missing > 0 ? QuickFixType.CreateMissingData : QuickFixType.None,
            });

            // Premium shaders
            var glassShaderGuids = AssetDatabase.FindAssets("t:Shader PremiumMoldGlass");
            var OreShaderGuids = AssetDatabase.FindAssets("t:Shader PremiumLayeredOre");
            string glassShaderPath = glassShaderGuids.Length > 0 ? AssetDatabase.GUIDToAssetPath(glassShaderGuids[0]) : null;
            string oreShaderPath = OreShaderGuids.Length > 0 ? AssetDatabase.GUIDToAssetPath(OreShaderGuids[0]) : null;
            _validationResults.Add(new ValidationResult
            {
                label = "PremiumMoldGlass",
                detail = glassShaderGuids.Length > 0 ? $"Found: {glassShaderPath}" : "Missing (fallback used)",
                ok = glassShaderGuids.Length > 0,
                fixType = glassShaderGuids.Length > 0 ? QuickFixType.PingAsset : QuickFixType.None,
                fixAssetPath = glassShaderPath,
            });
            _validationResults.Add(new ValidationResult
            {
                label = "PremiumLayeredOre",
                detail = OreShaderGuids.Length > 0 ? $"Found: {oreShaderPath}" : "Missing (fallback used)",
                ok = OreShaderGuids.Length > 0,
                fixType = OreShaderGuids.Length > 0 ? QuickFixType.PingAsset : QuickFixType.None,
                fixAssetPath = oreShaderPath,
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
            string levelCfgPath = $"{DataAssetCreator.DataPath}/LevelConfig.asset";
            var levelCfg = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>(levelCfgPath);
            if (levelCfg != null)
            {
                bool paletteValid = levelCfg.palette != null && levelCfg.palette.Length >= 2;
                _validationResults.Add(new ValidationResult
                {
                    label = "LevelConfig palette",
                    detail = paletteValid ? $"{levelCfg.palette.Length} colors" : "Empty or too few",
                    ok = paletteValid,
                    fixType = paletteValid ? QuickFixType.PingAsset : QuickFixType.None,
                    fixAssetPath = levelCfgPath,
                });
            }
            else
            {
                _validationResults.Add(new ValidationResult
                {
                    label = "LevelConfig palette",
                    detail = "LevelConfig.asset missing — palette not validated",
                    ok = false,
                    fixType = QuickFixType.CreateMissingData,
                });
            }

            // GameManager / MoldController in scene
            var gms = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include);
            var Molds = Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            _validationResults.Add(new ValidationResult
            {
                label = "Scene: GameManager",
                detail = gms.Length == 0 ? "Missing — add GameManager to scene" : $"{gms.Length} found",
                ok = gms.Length > 0,
                fixType = gms.Length > 0 ? QuickFixType.SelectObjects : QuickFixType.None,
                fixObjects = gms.Length > 0 ? System.Array.ConvertAll(gms, g => (Object)g) : null,
            });
            _validationResults.Add(new ValidationResult
            {
                label = "Scene: MoldController",
                detail = Molds.Length == 0 ? "Missing" : $"{Molds.Length} found",
                ok = Molds.Length > 0,
                fixType = Molds.Length > 0 ? QuickFixType.SelectObjects : QuickFixType.None,
                fixObjects = Molds.Length > 0 ? System.Array.ConvertAll(Molds, m => (Object)m) : null,
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
                ok = gpuInstancingOk,
                fixType = gpuInstancingOk ? QuickFixType.None : QuickFixType.EnableGpuInstancing,
                fixMaterialNames = gpuInstancingOk ? null : nonInstancedMats.ToArray(),
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
