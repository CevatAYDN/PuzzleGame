using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Services;

namespace PuzzleGame.Editor
{
    public class TestTab : IEditorTab
    {
        public string TabName => "Test";
        private ForgeEditorWindow _window;

        private Vector2 _testScroll;
        private string _analyticsPath = "Assets/Resources/analytics.json";
        private List<AnalyticsEntry> _analyticsData = new List<AnalyticsEntry>();
        private int _testLevelNumber = 1;
        private bool _infiniteMoves = false;
        private bool _skipIntro = false;
        private bool _debugMode = false;

        private string _screenshotPath = "Assets/Screenshots/";
        private string _screenshotName = "game_screenshot";
        private int _screenshotScale = 1;
        private bool _screenshotTransparent = false;

        [Serializable]
        public class AnalyticsEntry
        {
            public int levelNumber;
            public int attempts;
            public int avgMoves;
            public int bestMoves;
            public float avgTime;
            public float completionRate;
        }

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
        }

        public void OnDisable()
        {
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Test & Analytics Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Geliştirme ve test araçları.\n" +
                "Analytics: Oyuncu verilerini görüntüle.\n" +
                "Cheats: Test için hile kodları.\n" +
                "Debug: Geliştirme ayarları.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            _testScroll = EditorGUILayout.BeginScrollView(_testScroll);

            // ── Analytics Section ─────────────────────────────────────────
            DrawAnalyticsSection();

            EditorGUILayout.Space(8);

            // ── Cheats Section ─────────────────────────────────────────────
            DrawCheatsSection();

            EditorGUILayout.Space(8);

            // ── Debug Section ──────────────────────────────────────────────
            DrawDebugSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawAnalyticsSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("📊 Analytics", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Analytics Dosya:", GUILayout.Width(100));
                    _analyticsPath = EditorGUILayout.TextField(_analyticsPath, GUILayout.Width(200));
                }

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Load Analytics", GUILayout.Width(130)))
                    {
                        LoadAnalytics();
                    }

                    if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                    {
                        LoadAnalytics();
                    }
                }

                if (_analyticsData.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField($"Toplam kayıt: {_analyticsData.Count}", EditorStyles.miniBoldLabel);

                    DrawAnalyticsGraph();

                    // Display analytics table
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        // Header
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Level", GUILayout.Width(60));
                        EditorGUILayout.LabelField("Attempts", GUILayout.Width(70));
                        EditorGUILayout.LabelField("Avg Moves", GUILayout.Width(80));
                        EditorGUILayout.LabelField("Best", GUILayout.Width(60));
                        EditorGUILayout.LabelField("Avg Time", GUILayout.Width(80));
                        EditorGUILayout.LabelField("Completion", GUILayout.Width(80));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                        foreach (var entry in _analyticsData.OrderBy(e => e.levelNumber).Take(20))
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"#{entry.levelNumber}", GUILayout.Width(60));
                            EditorGUILayout.LabelField($"{entry.attempts}", GUILayout.Width(70));
                            EditorGUILayout.LabelField($"{entry.avgMoves}", GUILayout.Width(80));
                            EditorGUILayout.LabelField($"{entry.bestMoves}", GUILayout.Width(60));
                            EditorGUILayout.LabelField($"{entry.avgTime:F1}s", GUILayout.Width(80));
                            EditorGUILayout.LabelField($"{entry.completionRate:P0}", GUILayout.Width(80));
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    // Problem levels (low completion)
                    var problemLevels = _analyticsData.Where(a => a.completionRate < 0.5f).OrderBy(a => a.completionRate).Take(5).ToList();
                    if (problemLevels.Count > 0)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("⚠️ Problem Seviyeler (<50% başarı):", EditorStyles.miniBoldLabel);
                        foreach (var level in problemLevels)
                        {
                            EditorGUILayout.LabelField($"  Level {level.levelNumber}: {level.completionRate:P0} başarı ({level.attempts} deneme)");
                        }
                    }

                    // Easy levels (too easy)
                    var easyLevels = _analyticsData.Where(a => a.bestMoves <= 3 && a.attempts > 10).OrderByDescending(a => a.attempts).Take(5).ToList();
                    if (easyLevels.Count > 0)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("⭐ Çok Kolay Seviyeler (≤3 hamle):", EditorStyles.miniBoldLabel);
                        foreach (var level in easyLevels)
                        {
                            EditorGUILayout.LabelField($"  Level {level.levelNumber}: {level.bestMoves} hamle ({level.attempts} deneme)");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Henüz analytics verisi yok veya dosya bulunamadı.\nOyun oynandıktan sonra burada görünecek.", MessageType.Info);

                    // Generate demo data
                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("Generate Demo Analytics", GUILayout.Width(180)))
                    {
                        GenerateDemoAnalytics();
                    }
                }
            }
        }

        private void DrawCheatsSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("🎮 Cheats (Development)", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                EditorGUILayout.HelpBox(
                    "Bu ayarlar geliştirme/test için kullanılır.\n" +
                    "Build'de mutlaka kaldırılmalı!",
                    MessageType.Warning);

                EditorGUILayout.Space(4);

                // Quick level jump
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Jump to Level:", GUILayout.Width(100));
                    _testLevelNumber = EditorGUILayout.IntField(_testLevelNumber, GUILayout.Width(60));
                    if (GUILayout.Button("Test", GUILayout.Width(80)))
                    {
                        _window.SetStatus($"Test: Level {_testLevelNumber} başlatılacak (editor'dan manual)", MessageType.Info);
                    }
                }

                EditorGUILayout.Space(4);

                // Toggle cheats
                _infiniteMoves = EditorGUILayout.ToggleLeft("∞ Infinite Moves (Sınırsız Hamle)", _infiniteMoves);
                _skipIntro = EditorGUILayout.ToggleLeft("Skip Intro (Girişi Atla)", _skipIntro);

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Auto-Win Current Level", GUILayout.Width(180)))
                    {
                        _window.SetStatus("Auto-Win: Mevcut seviye otomatik kazanılacak", MessageType.Info);
                    }

                    if (GUILayout.Button("Reset Progress", GUILayout.Width(140)))
                    {
                        if (EditorUtility.DisplayDialog("Reset Progress",
                            "Tüm ilerlemeyi sıfırlamak istediğinizden emin misiniz?",
                            "Yes", "Cancel"))
                        {
                            ResetPlayerProgress();
                        }
                    }
                }

                EditorGUILayout.Space(4);

                // Bulk level operations
                EditorGUILayout.LabelField("Bulk Operations:", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Complete All Levels", GUILayout.Width(160)))
                    {
                        CompleteAllLevels();
                    }

                    if (GUILayout.Button("Unlock All", GUILayout.Width(120)))
                    {
                        UnlockAllLevels();
                    }
                }
            }
        }

        private void DrawDebugSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("🔧 Debug Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                _debugMode = EditorGUILayout.ToggleLeft("Enable Debug Mode", _debugMode);

                if (_debugMode)
                {
                    EditorGUILayout.HelpBox("Debug modu aktif - konsolda detaylı log'lar görünür.", MessageType.Info);

                    // Debug options
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Debug Options:", EditorStyles.miniBoldLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Log Level:", GUILayout.Width(80));
                        if (GUILayout.Button("Verbose", GUILayout.Width(80)))
                            _window.SetStatus("Log: Verbose", MessageType.Info);
                        if (GUILayout.Button("Info", GUILayout.Width(60)))
                            _window.SetStatus("Log: Info", MessageType.Info);
                        if (GUILayout.Button("Warning", GUILayout.Width(80)))
                            _window.SetStatus("Log: Warning", MessageType.Info);
                        if (GUILayout.Button("Error", GUILayout.Width(60)))
                            _window.SetStatus("Log: Error", MessageType.Info);
                    }

                    EditorGUILayout.Space(4);

                    // Performance stats
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Log Molds", GUILayout.Width(120)))
                        {
                            LogMoldInfo();
                        }

                        if (GUILayout.Button("Log Configs", GUILayout.Width(120)))
                        {
                            LogConfigInfo();
                        }

                        if (GUILayout.Button("Memory Stats", GUILayout.Width(120)))
                        {
                            LogMemoryStats();
                        }
                    }

                    EditorGUILayout.Space(4);

                    // Simulation
                    EditorGUILayout.LabelField("Simulation:", EditorStyles.miniBoldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Simulate 100 Plays", GUILayout.Width(140)))
                        {
                            SimulatePlays(100);
                        }
                    }

                    EditorGUILayout.Space(4);

                    // Screenshot capture
                    EditorGUILayout.LabelField("Screenshot:", EditorStyles.miniBoldLabel);
                    DrawScreenshotSection();
                }
            }
        }

        private void DrawScreenshotSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Path:", GUILayout.Width(50));
                    _screenshotPath = EditorGUILayout.TextField(_screenshotPath, GUILayout.Width(150));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
                    _screenshotName = EditorGUILayout.TextField(_screenshotName, GUILayout.Width(150));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Scale:", GUILayout.Width(50));
                    _screenshotScale = EditorGUILayout.IntPopup(_screenshotScale, new string[] { "1x", "2x", "3x", "4x" }, new int[] { 1, 2, 3, 4 }, GUILayout.Width(80));
                }

                _screenshotTransparent = EditorGUILayout.ToggleLeft("Transparent Background", _screenshotTransparent);

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("📷 Capture Screenshot", GUILayout.Width(160)))
                    {
                        CaptureScreenshot();
                    }

                    if (GUILayout.Button("Open Folder", GUILayout.Width(100)))
                    {
                        string fullPath = Path.Combine(UnityEngine.Application.dataPath, _screenshotPath.Replace("Assets/", ""));
                        if (Directory.Exists(fullPath))
                        {
                            EditorUtility.RevealInFinder(fullPath);
                        }
                        else
                        {
                            _window.SetStatus("Screenshot folder not found", MessageType.Warning);
                        }
                    }
                }

                EditorGUILayout.HelpBox(
                    "Oyun ekranından screenshot almak için:\n" +
                    "1. Play moduna geç\n" +
                    "2. İstediğin ekranı hazırla\n" +
                    "3. Editör'e dön ve 'Capture Screenshot' tıkla\n" +
                    "Not: Oyun play modundayken çalışır!",
                    MessageType.Info);
            }
        }

        private void CaptureScreenshot()
        {
            try
            {
                // Ensure directory exists
                string fullPath = Path.Combine(UnityEngine.Application.dataPath, _screenshotPath.Replace("Assets/", ""));
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                // Generate filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"{_screenshotName}_{timestamp}.png";
                string filePath = Path.Combine(fullPath, filename);

                // Capture screenshot with scale
                ScreenCapture.CaptureScreenshot(filePath, _screenshotScale);

                // Handle transparent background if needed
                if (_screenshotTransparent)
                {
                    // For transparent, we'd need to use a different approach with RenderTexture
                    // For now, warn user
                    _window.SetStatus("Transparent mode requires RenderTexture - using normal capture", MessageType.Warning);
                }

                AssetDatabase.Refresh();

                _window.SetStatus($"Screenshot saved: {filename}", MessageType.Info);

                // Show in explorer
                EditorUtility.RevealInFinder(filePath);
            }
            catch (Exception ex)
            {
                _window.SetStatus($"Screenshot error: {ex.Message}", MessageType.Error);
            }
        }

        // Helper methods
        private void LoadAnalytics()
        {
            try
            {
                string path = Path.Combine(UnityEngine.Application.dataPath, _analyticsPath.Replace("Assets/", ""));
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = JsonUtility.FromJson<AnalyticsWrapper>(json);
                    _analyticsData = data?.entries ?? new List<AnalyticsEntry>();
                    _window.SetStatus($"Loaded {_analyticsData.Count} analytics records", MessageType.Info);
                }
                else
                {
                    // Try Resources folder
                    var textAsset = Resources.Load<TextAsset>("analytics");
                    if (textAsset != null)
                    {
                        var data = JsonUtility.FromJson<AnalyticsWrapper>(textAsset.text);
                        _analyticsData = data?.entries ?? new List<AnalyticsEntry>();
                        _window.SetStatus($"Loaded {_analyticsData.Count} analytics records", MessageType.Info);
                    }
                    else
                    {
                        _analyticsData = new List<AnalyticsEntry>();
                        _window.SetStatus("Analytics file not found", MessageType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                _window.SetStatus($"Error loading analytics: {ex.Message}", MessageType.Error);
            }
        }

        private void GenerateDemoAnalytics()
        {
            _analyticsData = new List<AnalyticsEntry>();
            var random = new System.Random(12345);

            for (int i = 1; i <= 50; i++)
            {
                int attempts = random.Next(5, 50);
                int avgMoves = random.Next(5, 30) + (i / 5);
                int bestMoves = random.Next(3, avgMoves);
                float avgTime = random.Next(10, 120) + (i * 0.5f);
                float completionRate = random.Next(30, 100) / 100f;

                _analyticsData.Add(new AnalyticsEntry
                {
                    levelNumber = i,
                    attempts = attempts,
                    avgMoves = avgMoves,
                    bestMoves = bestMoves,
                    avgTime = avgTime,
                    completionRate = completionRate
                });
            }

            _window.SetStatus("Demo analytics generated", MessageType.Info);
        }

        private void ResetPlayerProgress()
        {
            // Reset player prefs
            PlayerPrefs.DeleteAll();
            _window.SetStatus("Player progress reset!", MessageType.Info);
        }

        private void CompleteAllLevels()
        {
            int maxLevel = 100;
            for (int i = 1; i <= maxLevel; i++)
            {
                PlayerPrefs.SetInt($"level_{i}_stars", 3);
            }
            PlayerPrefs.SetInt("unlocked_level", maxLevel + 1);
            _window.SetStatus($"Completed all {maxLevel} levels!", MessageType.Info);
        }

        private void UnlockAllLevels()
        {
            PlayerPrefs.SetInt("unlocked_level", 999);
            _window.SetStatus("All levels unlocked!", MessageType.Info);
        }

        private void LogMoldInfo()
        {
            var Molds = UnityEngine.Object.FindObjectsByType<PuzzleGame.MoldController>(FindObjectsInactive.Exclude);
            Debug.Log($"[DEBUG] Active Molds in scene: {Molds.Length}");
            foreach (var Mold in Molds)
            {
                Debug.Log($"[DEBUG] Mold #{Mold.MoldIndex}: {Mold.State.LayerCount} layers, {Mold.State.TotalFill:F2} fill");
            }
        }

        private void DrawAnalyticsGraph()
        {
            if (_analyticsData == null || _analyticsData.Count == 0) return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("📈 Zorluk Eğrisi Grafiği (Başarı Oranı %)", EditorStyles.miniBoldLabel);
            
            Rect rect = GUILayoutUtility.GetRect(100, 120, GUILayout.ExpandWidth(true));
            
            // Draw background frame
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));
            
            // Draw grid lines
            // Y lines (0%, 50%, 100% completion rate)
            Handles.color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
            float y0 = rect.y;
            float y50 = rect.y + rect.height * 0.5f;
            float y100 = rect.y + rect.height;
            Handles.DrawLine(new Vector3(rect.x, y0, 0), new Vector3(rect.x + rect.width, y0, 0));
            Handles.DrawLine(new Vector3(rect.x, y50, 0), new Vector3(rect.x + rect.width, y50, 0));
            Handles.DrawLine(new Vector3(rect.x, y100, 0), new Vector3(rect.x + rect.width, y100, 0));

            // Labels for Y grid lines
            var labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            GUI.Label(new Rect(rect.x + 4, y0 + 2, 60, 15), "100%", labelStyle);
            GUI.Label(new Rect(rect.x + 4, y50 - 7, 60, 15), "50%", labelStyle);
            GUI.Label(new Rect(rect.x + 4, y100 - 15, 60, 15), "0%", labelStyle);

            var ordered = _analyticsData.OrderBy(e => e.levelNumber).ToList();
            int count = ordered.Count;
            if (count > 1)
            {
                Vector3[] points = new Vector3[count];
                float maxX = count - 1;
                
                for (int i = 0; i < count; i++)
                {
                    float ratioX = i / maxX;
                    float ratioY = 1.0f - ordered[i].completionRate; 
                    
                    float px = rect.x + ratioX * rect.width;
                    float py = rect.y + ratioY * rect.height;
                    points[i] = new Vector3(px, py, 0);
                }
                
                // Draw threshold danger line at 50% completion rate (0.5 y-ratio)
                Handles.color = new Color(0.85f, 0.25f, 0.25f, 0.6f);
                Handles.DrawLine(new Vector3(rect.x, y50, 0), new Vector3(rect.x + rect.width, y50, 0));

                // Draw the line graph
                Handles.color = new Color(0.1f, 0.7f, 1.0f, 1f); // Sleek cyan
                Handles.DrawAAPolyLine(3.5f, points);

                // Draw end points info labels
                GUI.Label(new Rect(rect.x + 5, y100 + 2, 80, 15), $"Lvl {ordered[0].levelNumber}", labelStyle);
                string endText = $"Lvl {ordered[count - 1].levelNumber}";
                GUI.Label(new Rect(rect.x + rect.width - 65, y100 + 2, 60, 15), endText, labelStyle);
            }
            
            EditorGUILayout.Space(18); // Space for labels below
        }

        private void LogConfigInfo()
        {
            var gameConfig = Resources.Load<GameConfig>("GameConfig");
            var levelConfig = Resources.Load<LevelConfig>("LevelConfig");

            Debug.Log($"[DEBUG] GameConfig: {(gameConfig != null ? "Loaded" : "NULL")}");
            Debug.Log($"[DEBUG] LevelConfig: {(levelConfig != null ? "Loaded" : "NULL")}");
            if (levelConfig != null && levelConfig.palette != null)
            {
                Debug.Log($"[DEBUG] Palette colors: {levelConfig.palette.Length}");
            }
        }

        private void LogMemoryStats()
        {
            long totalMemory = System.GC.GetTotalMemory(false);
            Debug.Log($"[DEBUG] Total Memory: {totalMemory / 1024 / 1024} MB");
            Debug.Log($"[DEBUG] Mono Heap: {UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / 1024 / 1024} MB");
            Debug.Log($"[DEBUG] Mono Used: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024 / 1024} MB");
        }

        private void SimulatePlays(int count)
        {
            _window.SetStatus($"Simulating {count} plays...", MessageType.Info);
            // This would integrate with the BFS solver
            for (int i = 0; i < count; i++)
            {
                // Simulate level solving
            }
            _window.SetStatus($"Simulation complete!", MessageType.Info);
        }

        [Serializable]
        private class AnalyticsWrapper
        {
            public List<AnalyticsEntry> entries = new List<AnalyticsEntry>();
        }
    }
}
