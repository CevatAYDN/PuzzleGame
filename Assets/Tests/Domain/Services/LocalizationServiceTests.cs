using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Domain.Services
{
    /// <summary>
    /// Unit tests for LocalizationService.
    /// Sprint #12: service no longer embeds data — all tests inject a FakeTranslationProvider.
    /// Coverage focuses on logic (lookup, fallback chain, mutation) rather than data tables.
    /// </summary>
    [TestFixture]
    public class LocalizationServiceTests
    {
        private LocalizationService _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new LocalizationService(SupportedLanguage.Turkish, new FakeTranslationProvider());
        }

        // ─── GetString ─────────────────────────────────────────────────────────

        [Test]
        public void GetString_KnownKey_Turkish_ReturnsCorrectValue()
        {
            string result = _sut.GetString("moves_text");
            Assert.AreEqual("Hamle", result);
        }

        [Test]
        public void GetString_KnownKey_English_ReturnsCorrectValue()
        {
            _sut.SetLanguage(SupportedLanguage.English);
            string result = _sut.GetString("moves_text");
            Assert.AreEqual("Moves", result);
        }

        [Test]
        public void GetString_UnknownKey_ReturnsKeyAsIs()
        {
            string result = _sut.GetString("nonexistent_key_xyz");
            Assert.AreEqual("nonexistent_key_xyz", result, "Missing key should return the key itself as fallback.");
        }

        [Test]
        public void GetString_MissingCurrentLanguage_FallsBackToEnglish()
        {
            // 'german_only' has no Spanish entry — Spanish user gets the English value,
            // not the key. This is the contract: current language → English → key.
            _sut.SetLanguage(SupportedLanguage.Spanish);
            Assert.AreEqual("English fallback", _sut.GetString("german_only"));
        }

        [Test]
        public void GetString_MissingCurrentLanguageAndEnglish_FallsBackToAnyAvailable()
        {
            // 'german_only' has only German + English. If current is Turkish and we ask,
            // the service should fall back to English. This test pins the chain order.
            _sut.SetLanguage(SupportedLanguage.Turkish);
            Assert.AreEqual("English fallback", _sut.GetString("german_only"));
        }

        [TestCase(SupportedLanguage.Turkish, "Hamle")]
        [TestCase(SupportedLanguage.English, "Moves")]
        [TestCase(SupportedLanguage.German, "Züge")]
        [TestCase(SupportedLanguage.Spanish, "Movimientos")]
        [TestCase(SupportedLanguage.French, "Coups")]
        public void GetString_MovesText_AllLanguages(SupportedLanguage lang, string expected)
        {
            _sut.SetLanguage(lang);
            Assert.AreEqual(expected, _sut.GetString("moves_text"));
        }

        // ─── SetLanguage ───────────────────────────────────────────────────────

        [Test]
        public void SetLanguage_ChangesActiveLanguage()
        {
            _sut.SetLanguage(SupportedLanguage.English);
            Assert.AreEqual(SupportedLanguage.English, _sut.CurrentLanguage);
        }

        [Test]
        public void SetLanguage_SameLanguage_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetLanguage(SupportedLanguage.Turkish));
        }

        // ─── AddTranslation ────────────────────────────────────────────────────

        [Test]
        public void AddTranslation_NewKey_IsRetrievable()
        {
            _sut.AddTranslation("test_key", SupportedLanguage.Turkish, "Test Değer");
            _sut.SetLanguage(SupportedLanguage.Turkish);
            Assert.AreEqual("Test Değer", _sut.GetString("test_key"));
        }

        // ─── Lazy load (Sprint #15) ───────────────────────────────────────────

        [Test]
        public void Constructor_DoesNotCallProviderLoad()
        {
            // Lazy-load contract: Load() is deferred to the first GetString so
            // async providers (Android UnityWebRequest) can finish preloading first.
            var provider = new LoadTrackingProvider();
            new LocalizationService(SupportedLanguage.Turkish, provider);

            Assert.AreEqual(0, provider.LoadCallCount,
                "Constructor must not call provider.Load() — that's deferred to first GetString.");
        }

        [Test]
        public void GetString_FirstCall_TriggersProviderLoad()
        {
            var provider = new LoadTrackingProvider();
            var sut = new LocalizationService(SupportedLanguage.English, provider);

            sut.GetString("any_key");

            Assert.AreEqual(1, provider.LoadCallCount, "First GetString should trigger Load exactly once.");
        }

        [Test]
        public void GetString_SubsequentCalls_DoNotReloadProvider()
        {
            var provider = new LoadTrackingProvider();
            var sut = new LocalizationService(SupportedLanguage.English, provider);

            for (int i = 0; i < 5; i++) sut.GetString("any_key");

            Assert.AreEqual(1, provider.LoadCallCount, "After first load, dictionary is cached — no reload.");
        }

        // ─── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// In-memory ITranslationProvider that supplies a small, predictable dataset.
        /// Includes a sparse entry ('german_only') to exercise the fallback chain.
        /// </summary>
        private class FakeTranslationProvider : ITranslationProvider
        {
            public IReadOnlyDictionary<string, Dictionary<SupportedLanguage, string>> Load()
            {
                return new Dictionary<string, Dictionary<SupportedLanguage, string>>
                {
                    ["moves_text"] = new Dictionary<SupportedLanguage, string>
                    {
                        [SupportedLanguage.Turkish] = "Hamle",
                        [SupportedLanguage.English] = "Moves",
                        [SupportedLanguage.German] = "Züge",
                        [SupportedLanguage.Spanish] = "Movimientos",
                        [SupportedLanguage.French] = "Coups"
                    },
                    ["german_only"] = new Dictionary<SupportedLanguage, string>
                    {
                        [SupportedLanguage.German] = "Nur Deutsch",
                        [SupportedLanguage.English] = "English fallback"
                    }
                };
            }
        }

        /// <summary>
        /// Minimal ITranslationProvider that records Load() invocations
        /// for the lazy-load contract tests in Sprint #15.
        /// </summary>
        private class LoadTrackingProvider : ITranslationProvider
        {
            public int LoadCallCount { get; private set; }

            public IReadOnlyDictionary<string, Dictionary<SupportedLanguage, string>> Load()
            {
                LoadCallCount++;
                return new Dictionary<string, Dictionary<SupportedLanguage, string>>();
            }
        }
    }
}
