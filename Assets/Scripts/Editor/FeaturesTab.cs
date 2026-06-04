using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;

namespace PuzzleGame.Editor
{
    public class FeaturesTab : IEditorTab
    {
        public string TabName => "Features";
        private ForgeEditorWindow _window;

        private LevelData _selectedLevelForFeatures;
        private int _selectedFeatureTab = 0;
        private string[] _featureTabNames = new string[] { "Multi-Layer Cast", "Reaction System" };

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Level Features Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Enable/disable feature systems per level.\n" +
                "Multi-Layer Cast: Cast multiple layers of matching color at once.\n" +
                "Reaction System: Handles chemical reactions between colors.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // ── Level Selection ─────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Select Level", EditorStyles.miniBoldLabel);

                var levels = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Resources/Levels" });
                var levelOptions = new List<string> { "-- Select Level --" };
                var levelPaths = new List<string> { null };

                foreach (var guid in levels)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                    if (level != null)
                    {
                        levelOptions.Add($"Level {level.levelNumber} - {level.difficulty}");
                        levelPaths.Add(path);
                    }
                }

                int currentIndex = _selectedLevelForFeatures != null ? 
                    levelPaths.FindIndex(p => p != null && AssetDatabase.GetAssetPath(_selectedLevelForFeatures).Contains(p)) : 0;
                
                EditorGUILayout.Space(4);
                int selected = EditorGUILayout.Popup("Level", currentIndex, levelOptions.ToArray());
                
                if (selected >= 0 && selected < levelPaths.Count)
                {
                    if (selected != currentIndex)
                    {
                        _selectedLevelForFeatures = selected > 0 ? 
                            AssetDatabase.LoadAssetAtPath<LevelData>(levelPaths[selected]) : null;
                    }
                }
            }

            if (_selectedLevelForFeatures == null)
            {
                EditorGUILayout.HelpBox("Please select a level to edit.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4);

            _selectedFeatureTab = GUILayout.Toolbar(_selectedFeatureTab, _featureTabNames);
            EditorGUILayout.Space(4);

            switch (_selectedFeatureTab)
            {
                case 0:
                    DrawMultiLayerCastSettings();
                    break;
                case 1:
                    DrawReactionSystemSettings();
                    break;
            }

            // ── Save Button ─────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Changes", GUILayout.Height(28)))
                {
                    Undo.RecordObject(_selectedLevelForFeatures, "Save Features");
                    EditorUtility.SetDirty(_selectedLevelForFeatures);
                    AssetDatabase.SaveAssets();
                    _window.SetStatus("Features saved!", MessageType.Info);
                }

                if (GUILayout.Button("Reset to Default", GUILayout.Height(28)))
                {
                    ResetFeaturesToDefault();
                }
            }
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private void DrawMultiLayerCastSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Multi-Layer Cast Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                Undo.RecordObject(_selectedLevelForFeatures, "MultiLayer Cast");

                _selectedLevelForFeatures.enableMultiLayerCast = EditorGUILayout.ToggleLeft(
                    "Enable Multi-Layer Cast", 
                    _selectedLevelForFeatures.enableMultiLayerCast);

                EditorGUILayout.Space(4);

                if (_selectedLevelForFeatures.enableMultiLayerCast)
                {
                    if (_selectedLevelForFeatures.multiLayerCastConfig == null)
                    {
                        _selectedLevelForFeatures.multiLayerCastConfig = new MultiLayerCastData
                        {
                            isEnabled = true,
                            featureType = LevelFeatureType.MultiLayerCast,
                            CastAllMatching = true,
                            CastConsecutiveOnly = true,
                            minConsecutiveForCast = 2
                        };
                    }

                    var config = _selectedLevelForFeatures.multiLayerCastConfig;

                    EditorGUI.BeginChangeCheck();
                    var newCastAll = EditorGUILayout.ToggleLeft("Cast All Matching Layers", config.CastAllMatching);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Toggle Cast All"); config.CastAllMatching = newCastAll; }

                    EditorGUI.BeginChangeCheck();
                    var newConsecutive = EditorGUILayout.ToggleLeft("Cast Consecutive Only", config.CastConsecutiveOnly);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Toggle Consecutive"); config.CastConsecutiveOnly = newConsecutive; }

                    EditorGUILayout.Space(4);
                    EditorGUI.BeginChangeCheck();
                    var newMin = EditorGUILayout.IntSlider("Min Consecutive for Cast", config.minConsecutiveForCast, 2, _selectedLevelForFeatures.maxLayersPerMold);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Change Min Consecutive"); config.minConsecutiveForCast = newMin; }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        $"Configuration: Cast will occur if there are at least {config.minConsecutiveForCast} consecutive matching layers.",
                        MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("Multi-layer Cast is disabled.", MessageType.Info);
                }
            }
        }

        private void DrawReactionSystemSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Reaction System Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                Undo.RecordObject(_selectedLevelForFeatures, "Reaction System");

                _selectedLevelForFeatures.enableReactionSystem = EditorGUILayout.ToggleLeft(
                    "Enable Reaction System", 
                    _selectedLevelForFeatures.enableReactionSystem);

                EditorGUILayout.Space(4);

                if (_selectedLevelForFeatures.enableReactionSystem)
                {
                    if (_selectedLevelForFeatures.reactionConfig == null)
                    {
                        _selectedLevelForFeatures.reactionConfig = new ReactionSystemData
                        {
                            isEnabled = true,
                            featureType = LevelFeatureType.ReactionSystem,
                            enableReactions = true,
                            reactionRules = new List<ReactionRule>()
                        };
                    }

                    var config = _selectedLevelForFeatures.reactionConfig;

                    EditorGUI.BeginChangeCheck();
                    var newEnableReactions = EditorGUILayout.ToggleLeft("Enable Reactions", config.enableReactions);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Toggle Reactions"); config.enableReactions = newEnableReactions; }

                    EditorGUILayout.Space(4);

                    EditorGUILayout.LabelField("Reaction Rules", EditorStyles.miniBoldLabel);
                    
                    if (config.reactionRules == null)
                        config.reactionRules = new List<ReactionRule>();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+ Add Rule", GUILayout.Width(100)))
                        {
                            Undo.RecordObject(_selectedLevelForFeatures, "Add Reaction Rule");
                            config.reactionRules.Add(new ReactionRule
                            {
                                colorA = OreColor.Red,
                                colorB = OreColor.Blue,
                                resultColor = OreColor.Green,
                                reactionType = ReactionRule.ReactionType.Transform
                            });
                        }
                    }

                    EditorGUILayout.Space(4);

                    for (int i = 0; i < config.reactionRules.Count; i++)
                    {
                        var rule = config.reactionRules[i];
                        
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            EditorGUILayout.LabelField($"Rule {i + 1}", EditorStyles.miniBoldLabel);
                            
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Color A", GUILayout.Width(50));
                                EditorGUI.BeginChangeCheck();
                                var cA = (OreColor)EditorGUILayout.EnumPopup(rule.colorA, GUILayout.Width(80));
                                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Change Rule"); rule.colorA = cA; }

                                EditorGUILayout.LabelField("+", GUILayout.Width(15));
                                EditorGUI.BeginChangeCheck();
                                var cB = (OreColor)EditorGUILayout.EnumPopup(rule.colorB, GUILayout.Width(80));
                                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Change Rule"); rule.colorB = cB; }

                                EditorGUILayout.LabelField("Type", GUILayout.Width(40));
                                EditorGUI.BeginChangeCheck();
                                var type = (ReactionRule.ReactionType)EditorGUILayout.EnumPopup(rule.reactionType, GUILayout.Width(100));
                                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Change Rule"); rule.reactionType = type; }
                            }

                            if (rule.reactionType == ReactionRule.ReactionType.Transform)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField("→ Result:", GUILayout.Width(60));
                                    EditorGUI.BeginChangeCheck();
                                    var res = (OreColor)EditorGUILayout.EnumPopup(rule.resultColor, GUILayout.Width(150));
                                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Change Rule"); rule.resultColor = res; }
                                }
                            }
                            
                            EditorGUI.BeginChangeCheck();
                            var prefab = (GameObject)EditorGUILayout.ObjectField("Effect Prefab", rule.effectPrefab, typeof(GameObject), false);
                            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_selectedLevelForFeatures, "Change Rule Prefab"); rule.effectPrefab = prefab; }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                                {
                                    Undo.RecordObject(_selectedLevelForFeatures, "Remove Rule");
                                    config.reactionRules.RemoveAt(i);
                                    i--;
                                }
                            }
                        }
                    }

                    if (config.reactionRules.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No reaction rules defined.", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Reaction system is disabled.", MessageType.Info);
                }
            }
        }

        private void ResetFeaturesToDefault()
        {
            if (_selectedLevelForFeatures == null) return;

            Undo.RecordObject(_selectedLevelForFeatures, "Reset Features");

            _selectedLevelForFeatures.enableMultiLayerCast = true;
            _selectedLevelForFeatures.enableReactionSystem = false;
            _selectedLevelForFeatures.multiLayerCastConfig = null;
            _selectedLevelForFeatures.reactionConfig = null;

            EditorUtility.SetDirty(_selectedLevelForFeatures);
            _window.SetStatus("Features reset to default.", MessageType.Info);
        }
    }
}
