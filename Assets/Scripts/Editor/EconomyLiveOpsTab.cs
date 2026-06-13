using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// 📈 Economy &amp; Live-Ops Simulator Tab
    ///
    /// Soft Launch hazırlığı için gerekli tüm analiz araçlarını barındırır:
    ///   • Retention simülatörü (D1 / D7 / D28 projeksiyonu)
    ///   • Level funnel analizi (kaçıncı levelda oyuncu düşüyor?)
    ///   • Economy health check (coin pace, shop verimi)
    ///   • Remote Config taslak editörü
    ///   • A/B test senaryosu tanımlayıcısı
    ///
    /// NOT: Tüm değerler editörde simüle edilir; gerçek telemetri bağlantısı
    /// bir backend servis katmanı gerektirir (Firebase, Unity Analytics vb.).
    /// </summary>
    public class EconomyLiveOpsTab : IEditorTab
    {
        public string TabName => "Economy";
        public string Category => "LiveOps & Data";

        private ForgeEditorWindow _window;
        private Vector2 _scroll;
        private int _activeSection = 0;

        private static readonly string[] SectionNames =
        {
            "📊 Retention",
            "🔽 Funnel",
            "💰 Economy",
            "🔧 Remote Config",
            "🧪 A/B Tests"
        };

        // ── Retention Simülatörü ─────────────────────────────────────────────
        private float _d1Target  = 40f;
        private float _d7Target  = 20f;
        private float _d28Target = 8f;
        private int   _dau       = 1000;

        // ── Funnel Analizi ───────────────────────────────────────────────────
        private int   _funnelFrom = 1;
        private int   _funnelTo   = 30;
        private List<FunnelEntry> _funnelData = new List<FunnelEntry>();

        private struct FunnelEntry
        {
            public int    LevelNumber;
            public float  EstimatedDropRate;  // 0–1
            public string Risk;               // Low / Medium / High
        }

        // ── Economy ─────────────────────────────────────────────────────────
        private float _coinsPerLevel      = 50f;
        private float _coinsPerAd         = 20f;
        private float _shopItemCost       = 500f;
        private float _hardLevelBonus     = 100f;
        private float _avgLevelsPerDay    = 12f;

        // ── Remote Config ────────────────────────────────────────────────────
        private List<RemoteConfigEntry> _remoteConfigEntries = new List<RemoteConfigEntry>();
        private string _newConfigKey   = "";
        private string _newConfigValue = "";

        [Serializable]
        private class RemoteConfigEntry
        {
            public string Key;
            public string DefaultValue;
            public string OverrideValue;
            public bool   IsOverridden;
        }

        // ── A/B Tests ────────────────────────────────────────────────────────
        private List<ABTestEntry> _abTests = new List<ABTestEntry>();
        private string _newABTestName = "";
        private string _newABVariantA = "Control";
        private string _newABVariantB = "Variant B";

        [Serializable]
        private class ABTestEntry
        {
            public string Name;
            public string VariantA;
            public string VariantB;
            public float  TrafficSplit = 50f;
            public bool   IsActive;
        }

        // ─────────────────────────────────────────────────────────────────────

        public void OnEnable(ForgeEditorWindow window)
        {
            _window = window;
            InitDefaultRemoteConfig();
            InitDefaultABTests();
        }

        public void OnDisable() { }

        public void OnSceneGUI(SceneView sceneView) { }

        public void Refresh()
        {
            // Funnel verisini temizle — bir sonraki hesaplamada yenilenir.
            _funnelData.Clear();
        }

        // ── Ana OnGUI ────────────────────────────────────────────────────────

        public void OnGUI()
        {
            EditorGUILayout.LabelField("📈 Economy & Live-Ops Simulator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Soft Launch hazırlığı için retention, funnel, economy ve A/B test simülatörleri.\n" +
                "Değerler tamamen editör ortamında simüle edilir.",
                MessageType.Info);
            EditorGUILayout.Space(6);

            // Bölüm seçici toolbar
            _activeSection = GUILayout.Toolbar(_activeSection, SectionNames);
            EditorGUILayout.Space(8);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            switch (_activeSection)
            {
                case 0: DrawRetentionSection();    break;
                case 1: DrawFunnelSection();       break;
                case 2: DrawEconomySection();      break;
                case 3: DrawRemoteConfigSection(); break;
                case 4: DrawABTestSection();       break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ── 1. Retention Simülatörü ─────────────────────────────────────────

        private void DrawRetentionSection()
        {
            EditorGUILayout.LabelField("D1 / D7 / D28 Retention Projeksiyonu", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Hedef Metrikler", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                _dau       = EditorGUILayout.IntSlider("Tahmini DAU",    _dau,       100, 100000);
                _d1Target  = EditorGUILayout.Slider("D1 Hedef (%)",  _d1Target,  10f, 70f);
                _d7Target  = EditorGUILayout.Slider("D7 Hedef (%)",  _d7Target,  5f,  40f);
                _d28Target = EditorGUILayout.Slider("D28 Hedef (%)", _d28Target, 2f,  20f);
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Projeksiyon", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                DrawRetentionRow("D1",  _d1Target,  _dau);
                DrawRetentionRow("D7",  _d7Target,  _dau);
                DrawRetentionRow("D28", _d28Target, _dau);

                EditorGUILayout.Space(4);
                float mauEstimate = _dau * (_d28Target / 100f) * 28f;
                EditorGUILayout.LabelField($"Tahmini MAU: {mauEstimate:N0} kullanıcı", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(8);
            DrawRetentionBenchmarks();
        }

        private void DrawRetentionRow(string label, float pct, int dau)
        {
            int retained = Mathf.RoundToInt(dau * pct / 100f);
            var color = pct >= GetBenchmark(label) ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.4f, 0.2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(40));
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Width(200)), pct / 100f,
                    $"{pct:F1}% — {retained:N0} kullanıcı");

                var oldColor = GUI.contentColor;
                GUI.contentColor = color;
                string status = pct >= GetBenchmark(label) ? "✓ Hedef" : "⚠ Düşük";
                EditorGUILayout.LabelField(status, EditorStyles.boldLabel, GUILayout.Width(80));
                GUI.contentColor = oldColor;
            }
        }

        private float GetBenchmark(string label)
        {
            switch (label)
            {
                case "D1":  return 40f;
                case "D7":  return 20f;
                case "D28": return 8f;
                default:    return 10f;
            }
        }

        private void DrawRetentionBenchmarks()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Sektör Kıyaslamaları (Puzzle Oyunları)", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("D1: %40+  |  D7: %20+  |  D28: %8+", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Kaynak: AppsFlyer / GameAnalytics 2024 Mobile Gaming Report", EditorStyles.miniLabel);
            }
        }

        // ── 2. Funnel Analizi ─────────────────────────────────────────────────

        private void DrawFunnelSection()
        {
            EditorGUILayout.LabelField("Level Funnel Analizi", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Analiz Aralığı", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _funnelFrom = EditorGUILayout.IntField("Başlangıç Level", _funnelFrom, GUILayout.Width(220));
                    _funnelTo   = EditorGUILayout.IntField("Bitiş Level",     _funnelTo,   GUILayout.Width(220));
                }

                if (GUILayout.Button("Funnel'ı Hesapla", GUILayout.Height(26)))
                {
                    CalculateFunnel();
                }
            }

            EditorGUILayout.Space(8);

            if (_funnelData.Count > 0)
            {
                DrawFunnelResults();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Funnel'ı hesaplamak için yukarıdaki butona tıklayın.\n" +
                    "Gerçek veriler için Firebase/GameAnalytics entegrasyonu gereklidir — bu değerler simüle edilmiştir.",
                    MessageType.None);
            }
        }

        private void CalculateFunnel()
        {
            _funnelData.Clear();
            var rng = new System.Random(42); // deterministik seed

            float baseDropRate = 0.05f;

            for (int lvl = _funnelFrom; lvl <= _funnelTo; lvl++)
            {
                // Simülasyon: her 5 levelda bir zorluk artışı, boss levellarında spike
                float difficultyMultiplier = 1f + (lvl / 10f) * 0.5f;
                bool  isBoss  = lvl % 10 == 0;
                float drop    = Mathf.Clamp01(baseDropRate * difficultyMultiplier * (isBoss ? 2.5f : 1f)
                                              + (float)(rng.NextDouble() * 0.03));

                string risk = drop < 0.08f ? "Low" : drop < 0.15f ? "Medium" : "High";

                _funnelData.Add(new FunnelEntry
                {
                    LevelNumber       = lvl,
                    EstimatedDropRate = drop,
                    Risk              = risk
                });
            }
        }

        private void DrawFunnelResults()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Funnel Sonuçları", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                var highRisk = _funnelData.Where(f => f.Risk == "High").ToList();
                if (highRisk.Count > 0)
                {
                    string levels = string.Join(", ", highRisk.Select(f => $"L{f.LevelNumber}"));
                    EditorGUILayout.HelpBox(
                        $"⚠ Yüksek drop-rate tespit edildi: {levels}\nBu levelları öncelikli olarak balans edin.",
                        MessageType.Warning);
                    EditorGUILayout.Space(4);
                }

                // En kötü 5 level
                var worst5 = _funnelData.OrderByDescending(f => f.EstimatedDropRate).Take(5).ToList();
                EditorGUILayout.LabelField("En Yüksek Drop-Rate (ilk 5):", EditorStyles.miniBoldLabel);

                foreach (var entry in worst5)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string riskEmoji = entry.Risk == "High" ? "🔴" : entry.Risk == "Medium" ? "🟡" : "🟢";
                        EditorGUILayout.LabelField($"{riskEmoji} Level {entry.LevelNumber}", GUILayout.Width(120));
                        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Width(200)),
                            entry.EstimatedDropRate, $"{entry.EstimatedDropRate * 100f:F1}% drop");
                    }
                }

                EditorGUILayout.Space(4);
                float avgDrop = _funnelData.Average(f => f.EstimatedDropRate);
                EditorGUILayout.LabelField($"Ortalama Drop-Rate: {avgDrop * 100f:F1}%", EditorStyles.boldLabel);
            }
        }

        // ── 3. Economy Health Check ──────────────────────────────────────────

        private void DrawEconomySection()
        {
            EditorGUILayout.LabelField("Economy Health Check", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Gelir Parametreleri", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                _coinsPerLevel   = EditorGUILayout.FloatField("Coin / Level (normal)",  _coinsPerLevel);
                _hardLevelBonus  = EditorGUILayout.FloatField("Coin / Level (zor)",     _hardLevelBonus);
                _coinsPerAd      = EditorGUILayout.FloatField("Coin / Reklam İzleme",   _coinsPerAd);
                _shopItemCost    = EditorGUILayout.FloatField("Ortalama Shop Ürün Fiyatı", _shopItemCost);
                _avgLevelsPerDay = EditorGUILayout.FloatField("Günlük Ortalama Level",  _avgLevelsPerDay);
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Projeksiyon", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                float dailyCoins   = _coinsPerLevel * _avgLevelsPerDay;
                float daysToShop   = _shopItemCost / dailyCoins;
                float weeklyCoins  = dailyCoins * 7f;

                DrawEconomyRow("Günlük Coin Kazancı",     $"{dailyCoins:N0} coin");
                DrawEconomyRow("Haftalık Coin Kazancı",   $"{weeklyCoins:N0} coin");
                DrawEconomyRow("Shop Item İçin Süre",     $"{daysToShop:F1} gün");

                EditorGUILayout.Space(4);

                // Sağlık değerlendirmesi
                string health;
                MessageType msgType;
                if (daysToShop <= 3f)
                {
                    health = "⚠ Ekonomi çok cömert — shop değersizleşir.";
                    msgType = MessageType.Warning;
                }
                else if (daysToShop > 14f)
                {
                    health = "⚠ Ekonomi çok kısıtlayıcı — oyuncu hayal kırıklığı riski.";
                    msgType = MessageType.Warning;
                }
                else
                {
                    health = "✓ Economy sağlıklı görünüyor (3–14 gün aralığı).";
                    msgType = MessageType.Info;
                }
                EditorGUILayout.HelpBox(health, msgType);
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Reklam Entegrasyonu", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);
                float adsNeededForShop = _shopItemCost / _coinsPerAd;
                DrawEconomyRow("Shop Item İçin Reklam",  $"{adsNeededForShop:F1} reklam");

                string adHealth = adsNeededForShop > 20f
                    ? "⚠ Çok fazla reklam gerekiyor — churn artabilir."
                    : "✓ Reklam dengesi makul.";
                EditorGUILayout.LabelField(adHealth, EditorStyles.miniLabel);
            }
        }

        private void DrawEconomyRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(220));
                EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            }
        }

        // ── 4. Remote Config ─────────────────────────────────────────────────

        private void DrawRemoteConfigSection()
        {
            EditorGUILayout.LabelField("Remote Config Taslak Editörü", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Bu editör, Remote Config anahtarlarınızı proje içinde belgeler.\n" +
                "Gerçek değerleri Firebase Remote Config / Unity Remote Config üzerinden yönetin.",
                MessageType.None);

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Mevcut Anahtarlar", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                for (int i = 0; i < _remoteConfigEntries.Count; i++)
                {
                    var entry = _remoteConfigEntries[i];
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(entry.Key, EditorStyles.boldLabel, GUILayout.Width(180));

                        entry.IsOverridden = EditorGUILayout.Toggle("Override", entry.IsOverridden, GUILayout.Width(70));

                        if (entry.IsOverridden)
                        {
                            entry.OverrideValue = EditorGUILayout.TextField(entry.OverrideValue, GUILayout.Width(120));
                            var oldColor = GUI.contentColor;
                            GUI.contentColor = new Color(1f, 0.8f, 0.2f);
                            EditorGUILayout.LabelField("(Local override)", EditorStyles.miniLabel, GUILayout.Width(90));
                            GUI.contentColor = oldColor;
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"Default: {entry.DefaultValue}", EditorStyles.miniLabel, GUILayout.Width(200));
                        }

                        if (GUILayout.Button("X", GUILayout.Width(22)))
                        {
                            _remoteConfigEntries.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Yeni Anahtar Ekle", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                _newConfigKey   = EditorGUILayout.TextField("Key",           _newConfigKey,   GUILayout.Width(300));
                _newConfigValue = EditorGUILayout.TextField("Default Value", _newConfigValue, GUILayout.Width(300));

                using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(_newConfigKey)))
                {
                    if (GUILayout.Button("Ekle", GUILayout.Width(100), GUILayout.Height(24)))
                    {
                        if (!_remoteConfigEntries.Any(e => e.Key == _newConfigKey))
                        {
                            _remoteConfigEntries.Add(new RemoteConfigEntry
                            {
                                Key          = _newConfigKey,
                                DefaultValue = _newConfigValue
                            });
                            _newConfigKey   = "";
                            _newConfigValue = "";
                            _window.SetStatus("Remote Config anahtarı eklendi.", MessageType.Info);
                        }
                        else
                        {
                            _window.SetStatus("Bu anahtar zaten mevcut.", MessageType.Warning);
                        }
                    }
                }
            }

            EditorGUILayout.Space(6);

            if (GUILayout.Button("JSON Olarak Dışa Aktar", GUILayout.Height(26)))
                ExportRemoteConfigToJSON();
        }

        private void ExportRemoteConfigToJSON()
        {
            string path = EditorUtility.SaveFilePanel("Remote Config Dışa Aktar",
                "Assets/", "remote_config.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            for (int i = 0; i < _remoteConfigEntries.Count; i++)
            {
                var e   = _remoteConfigEntries[i];
                string v = e.IsOverridden ? e.OverrideValue : e.DefaultValue;
                sb.Append($"  \"{e.Key}\": \"{v}\"");
                if (i < _remoteConfigEntries.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            _window.SetStatus($"Remote Config JSON dışa aktarıldı: {path}", MessageType.Info);
        }

        // ── 5. A/B Tests ─────────────────────────────────────────────────────

        private void DrawABTestSection()
        {
            EditorGUILayout.LabelField("A/B Test Senaryoları", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Planladığınız A/B testlerini belgeleyin. Gerçek trafik bölümü Firebase A/B Testing\n" +
                "veya Unity Gaming Services üzerinden yönetilmelidir.",
                MessageType.None);

            EditorGUILayout.Space(6);

            for (int i = 0; i < _abTests.Count; i++)
            {
                var test = _abTests[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        test.IsActive = EditorGUILayout.Toggle(test.IsActive, GUILayout.Width(18));
                        EditorGUILayout.LabelField(test.Name, EditorStyles.boldLabel, GUILayout.Width(200));

                        string statusLabel = test.IsActive ? "🟢 Aktif" : "⚫ Pasif";
                        EditorGUILayout.LabelField(statusLabel, GUILayout.Width(80));

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Sil", GUILayout.Width(40)))
                        {
                            _abTests.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                    }

                    EditorGUILayout.Space(2);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("A:", GUILayout.Width(20));
                        test.VariantA = EditorGUILayout.TextField(test.VariantA, GUILayout.Width(180));

                        EditorGUILayout.LabelField("B:", GUILayout.Width(20));
                        test.VariantB = EditorGUILayout.TextField(test.VariantB, GUILayout.Width(180));

                        EditorGUILayout.LabelField($"Trafik: {test.TrafficSplit:F0}% / {100 - test.TrafficSplit:F0}%",
                            EditorStyles.miniLabel, GUILayout.Width(130));
                    }

                    test.TrafficSplit = EditorGUILayout.Slider("A Trafiği (%)", test.TrafficSplit, 10f, 90f);
                }

                EditorGUILayout.Space(2);
            }

            if (_abTests.Count == 0)
            {
                EditorGUILayout.HelpBox("Henüz A/B test tanımlanmadı. Aşağıdan ekleyin.", MessageType.None);
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Yeni A/B Test Ekle", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(4);

                _newABTestName = EditorGUILayout.TextField("Test Adı",  _newABTestName, GUILayout.Width(300));
                _newABVariantA = EditorGUILayout.TextField("Variant A", _newABVariantA, GUILayout.Width(300));
                _newABVariantB = EditorGUILayout.TextField("Variant B", _newABVariantB, GUILayout.Width(300));

                using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(_newABTestName)))
                {
                    if (GUILayout.Button("Test Ekle", GUILayout.Width(120), GUILayout.Height(24)))
                    {
                        _abTests.Add(new ABTestEntry
                        {
                            Name     = _newABTestName,
                            VariantA = _newABVariantA,
                            VariantB = _newABVariantB,
                            IsActive = false
                        });
                        string addedName = _newABTestName;
                        _newABTestName = "";
                        _newABVariantA = "Control";
                        _newABVariantB = "Variant B";
                        _window.SetStatus($"A/B Test eklendi: {addedName}", MessageType.Info);
                    }
                }
            }
        }

        // ── Başlangıç Değerleri ──────────────────────────────────────────────

        private void InitDefaultRemoteConfig()
        {
            if (_remoteConfigEntries.Count > 0) return;

            _remoteConfigEntries = new List<RemoteConfigEntry>
            {
                new RemoteConfigEntry { Key = "coins_per_level_normal",  DefaultValue = "50" },
                new RemoteConfigEntry { Key = "coins_per_level_hard",    DefaultValue = "100" },
                new RemoteConfigEntry { Key = "hint_cost_coins",         DefaultValue = "30" },
                new RemoteConfigEntry { Key = "daily_bonus_coins",       DefaultValue = "100" },
                new RemoteConfigEntry { Key = "max_daily_ads",           DefaultValue = "5" },
                new RemoteConfigEntry { Key = "interstitial_interval",   DefaultValue = "3" },
                new RemoteConfigEntry { Key = "rewarded_ad_cooldown_s",  DefaultValue = "30" },
                new RemoteConfigEntry { Key = "level_difficulty_scale",  DefaultValue = "1.0" },
            };
        }

        private void InitDefaultABTests()
        {
            if (_abTests.Count > 0) return;

            _abTests = new List<ABTestEntry>
            {
                new ABTestEntry
                {
                    Name         = "Onboarding Flow",
                    VariantA     = "Tutorial ekranı (mevcut)",
                    VariantB     = "Direkt oyuna gir",
                    TrafficSplit = 50f,
                    IsActive     = false
                },
                new ABTestEntry
                {
                    Name         = "Interstitial Sıklığı",
                    VariantA     = "Her 3 levelda bir",
                    VariantB     = "Her 5 levelda bir",
                    TrafficSplit = 50f,
                    IsActive     = false
                }
            };
        }
    }
}
