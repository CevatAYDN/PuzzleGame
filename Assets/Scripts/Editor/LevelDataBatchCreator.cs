using UnityEditor;
using UnityEngine;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Batch-creates LevelData assets (1-N) with progressive difficulty.
    /// Each level gets a unique seed, increasing color/bottle count, and tighter move thresholds.
    /// All assets land in Assets/Resources/Levels/.
    /// </summary>
    public static class LevelDataBatchCreator
    {
        public const string LevelPath = "Assets/Resources/Levels";

        [MenuItem("PuzzleGame/Levels/Create 100 Levels", false, 110)]
        public static void Create100Levels()
        {
            if (!System.IO.Directory.Exists(LevelPath))
                System.IO.Directory.CreateDirectory(LevelPath);

            var levels = new System.Collections.Generic.List<LevelData>();
            for (int i = 1; i <= 100; i++)
            {
                string fileName = $"Level_{i:D2}";
                string fullPath = $"{LevelPath}/{fileName}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<LevelData>(fullPath);
                if (existing != null)
                {
                    levels.Add(existing);
                    continue;
                }

                var level = ScriptableObject.CreateInstance<LevelData>();
                level.levelNumber = i;
                level.randomSeed = i * 1337;

                // Progressive difficulty
                if (i <= 10)
                {
                    level.difficulty = Difficulty.Trivial;
                    level.bottleCount = 3;
                    level.colorCount = 2;
                    level.emptyBottleCount = 1;
                    level.maxLayersPerBottle = 3;
                    level.parMoves = 5;
                    level.goodMoves = 8;
                }
                else if (i <= 30)
                {
                    level.difficulty = Difficulty.Easy;
                    level.bottleCount = 5;
                    level.colorCount = 3;
                    level.emptyBottleCount = 2;
                    level.maxLayersPerBottle = 4;
                    level.parMoves = 10;
                    level.goodMoves = 15;
                }
                else if (i <= 60)
                {
                    level.difficulty = Difficulty.Medium;
                    level.bottleCount = 7;
                    level.colorCount = 5;
                    level.emptyBottleCount = 2;
                    level.maxLayersPerBottle = 5;
                    level.parMoves = 18;
                    level.goodMoves = 25;
                }
                else if (i <= 85)
                {
                    level.difficulty = Difficulty.Hard;
                    level.bottleCount = 9;
                    level.colorCount = 7;
                    level.emptyBottleCount = 2;
                    level.maxLayersPerBottle = 6;
                    level.parMoves = 28;
                    level.goodMoves = 38;
                }
                else
                {
                    level.difficulty = Difficulty.Expert;
                    level.bottleCount = 12;
                    level.colorCount = 10;
                    level.emptyBottleCount = 2;
                    level.maxLayersPerBottle = 6;
                    level.parMoves = 40;
                    level.goodMoves = 55;
                }

                AssetDatabase.CreateAsset(level, fullPath);
                levels.Add(level);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created/verified {levels.Count} level assets in {LevelPath}.");
        }

        [MenuItem("PuzzleGame/Levels/Create From Seed Range", false, 111)]
        public static void CreateSingleLevel()
        {
            var window = EditorWindow.GetWindow<LevelBatchWindow>();
            window.Show();
        }
    }

    public class LevelBatchWindow : EditorWindow
    {
        private float _start = 1;
        private float _end = 10;
        private int _seedBase = 1337;
        private Difficulty _difficulty = Difficulty.Easy;
        private int _bottleCount = 5;
        private int _colorCount = 3;
        private int _emptyCount = 2;
        private int _maxLayers = 4;
        private int _par = 10;
        private int _good = 15;

        private void OnGUI()
        {
            GUILayout.Label("Batch Level Creator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.MinMaxSlider("Level Range", ref _start, ref _end, 1, 999);
            EditorGUILayout.LabelField($"Start: {(int)_start}  End: {(int)_end}");

            _seedBase = EditorGUILayout.IntField("Seed Base", _seedBase);
            _difficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", _difficulty);
            _bottleCount = EditorGUILayout.IntField("Bottle Count", _bottleCount);
            _colorCount = EditorGUILayout.IntField("Color Count", _colorCount);
            _emptyCount = EditorGUILayout.IntField("Empty Bottles", _emptyCount);
            _maxLayers = EditorGUILayout.IntField("Max Layers", _maxLayers);
            _par = EditorGUILayout.IntField("Par (3★)", _par);
            _good = EditorGUILayout.IntField("Good (2★)", _good);

            if (GUILayout.Button("Create Levels", GUILayout.Height(40)))
            {
                CreateCustomRange();
            }
        }

        private void CreateCustomRange()
        {
            if (!System.IO.Directory.Exists(LevelDataBatchCreator.LevelPath))
                System.IO.Directory.CreateDirectory(LevelDataBatchCreator.LevelPath);

            int count = 0;
            for (int i = (int)_start; i <= (int)_end; i++)
            {
                string fileName = $"Level_{i:D2}";
                string fullPath = $"{LevelDataBatchCreator.LevelPath}/{fileName}.asset";

                if (AssetDatabase.LoadAssetAtPath<LevelData>(fullPath) != null)
                {
                    Debug.LogWarning($"Level {i} already exists, skipping.");
                    continue;
                }

                var level = ScriptableObject.CreateInstance<LevelData>();
                level.levelNumber = i;
                level.randomSeed = i * _seedBase;
                level.difficulty = _difficulty;
                level.bottleCount = _bottleCount;
                level.colorCount = _colorCount;
                level.emptyBottleCount = _emptyCount;
                level.maxLayersPerBottle = _maxLayers;
                level.parMoves = _par;
                level.goodMoves = _good;

                AssetDatabase.CreateAsset(level, fullPath);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {count} level assets.");
        }
    }
}
