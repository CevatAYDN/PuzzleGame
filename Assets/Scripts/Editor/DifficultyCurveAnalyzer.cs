using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Analyzes level difficulty progression across the entire catalog.
    /// Detects spikes, gaps, and flat zones in the difficulty curve.
    /// Useful for designers to ensure smooth player progression.
    /// </summary>
    public class DifficultyCurveAnalyzer : IEditorTab
    {
        public string TabName => "Difficulty Curve";
        public string Category => "Game Design";
        private ForgeEditorWindow _window;

        private Vector2 _scrollPos;
        private List<LevelData> _levels = new List<LevelData>();
        private List<DifficultyPoint> _curve = new List<DifficultyPoint>();
        private bool _dataLoaded;

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
            LoadLevels();
        }

        public void OnDisable()
        {
        }

        public void Refresh()
        {
            LoadLevels();
        }

        private void LoadLevels()
        {
            _levels.Clear();
            var guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                    _levels.Add(level);
            }

            _levels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
            _dataLoaded = true;
            AnalyzeCurve();
        }

        private void AnalyzeCurve()
        {
            _curve.Clear();
            if (_levels.Count == 0) return;

            for (int i = 0; i < _levels.Count; i++)
            {
                var level = _levels[i];
                float difficultyScore = CalculateDifficultyScore(level);

                _curve.Add(new DifficultyPoint
                {
                    LevelNumber = level.levelNumber,
                    DifficultyScore = difficultyScore,
                    MoldCount = level.MoldCount,
                    MaxLayers = level.maxLayersPerMold,
                    ColorCount = level.colorCount
                });
            }
        }

        private float CalculateDifficultyScore(LevelData level)
        {
            // Weighted score: mold count + layers + color variety
            float moldFactor = level.MoldCount * 10f;
            float layerFactor = level.maxLayersPerMold * 8f;
            float colorFactor = level.colorCount * 5f;
            float emptyMoldBonus = (level.MoldCount - level.emptyMoldCount) * 3f;

            return moldFactor + layerFactor + colorFactor + emptyMoldBonus;
        }

        public void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Difficulty Curve Analyzer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                LoadLevels();
            }

            EditorGUILayout.Space(8);

            if (!_dataLoaded || _levels.Count == 0)
            {
                EditorGUILayout.HelpBox("No LevelData assets found. Create levels first.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"Total Levels: {_levels.Count}", EditorStyles.miniLabel);

            // ── Summary Stats ────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);

            float minScore = _curve.Min(p => p.DifficultyScore);
            float maxScore = _curve.Max(p => p.DifficultyScore);
            float avgScore = _curve.Average(p => p.DifficultyScore);

            EditorGUILayout.LabelField($"Difficulty Range: {minScore:F0} – {maxScore:F0}");
            EditorGUILayout.LabelField($"Average Difficulty: {avgScore:F0}");

            // ── Spikes Detection ─────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Difficulty Spikes (jump > 30%)", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));

            for (int i = 1; i < _curve.Count; i++)
            {
                var prev = _curve[i - 1];
                var curr = _curve[i];

                if (prev.DifficultyScore > 0)
                {
                    float change = (curr.DifficultyScore - prev.DifficultyScore) / prev.DifficultyScore * 100f;

                    Color rowColor = Color.white;
                    string icon = "  ";
                    if (change > 30f)
                    {
                        rowColor = new Color(1f, 0.7f, 0.7f);
                        icon = "▲ ";
                    }
                    else if (change < -20f)
                    {
                        rowColor = new Color(0.7f, 0.7f, 1f);
                        icon = "▼ ";
                    }

                    var oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = rowColor;

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{icon}Level {curr.LevelNumber}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Score: {curr.DifficultyScore:F0}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Δ: {change:+0.0;-0.0}%", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Molds: {curr.MoldCount}", GUILayout.Width(70));
                    EditorGUILayout.LabelField($"Layers: {curr.MaxLayers}", GUILayout.Width(70));
                    EditorGUILayout.LabelField($"Colors: {curr.ColorCount}", GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();

                    GUI.backgroundColor = oldBg;
                }
            }

            EditorGUILayout.EndScrollView();

            // ── Legend ───────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("▲ = Difficulty spike (>30% jump)   ▼ = Difficulty drop (>20% drop)", EditorStyles.miniLabel);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private struct DifficultyPoint
        {
            public int LevelNumber;
            public float DifficultyScore;
            public int MoldCount;
            public int MaxLayers;
            public int ColorCount;
        }
    }
}
