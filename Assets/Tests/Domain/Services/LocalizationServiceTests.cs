using NUnit.Framework;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Domain.Services
{
    /// <summary>
    /// Unit tests for LocalizationService.
    /// Fix Test Audit: 0 → coverage for GetString, SetLanguage, provider injection.
    /// </summary>
    [TestFixture]
    public class LocalizationServiceTests
    {
        private LocalizationService _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new LocalizationService(SupportedLanguage.Turkish);
        }

        // ─── GetString ──────────────────────────────────────────────────────────

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

        // ─── SetLanguage ────────────────────────────────────────────────────────

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

        // ─── AddTranslation ─────────────────────────────────────────────────────

        [Test]
        public void AddTranslation_NewKey_IsRetrievable()
        {
            _sut.AddTranslation("test_key", SupportedLanguage.Turkish, "Test Değer");
            _sut.SetLanguage(SupportedLanguage.Turkish);
            Assert.AreEqual("Test Değer", _sut.GetString("test_key"));
        }

        // ─── ITranslationProvider injection ────────────────────────────────────

        [Test]
        public void Constructor_WithProvider_LoadsProviderData()
        {
            var provider = new FakeTranslationProvider();
            var sut = new LocalizationService(SupportedLanguage.English, provider);
            sut.SetLanguage(SupportedLanguage.English);

            Assert.AreEqual("FakeValue", sut.GetString("fake_key"),
                "Provider-loaded translations should be accessible.");
        }

        [Test]
        public void Constructor_NullProvider_UsesDefaultTranslations()
        {
            var sut = new LocalizationService(SupportedLanguage.Turkish, null);
            // Default translations should still be loaded via LoadDefaultTranslations()
            Assert.AreEqual("Hamle", sut.GetString("moves_text"));
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private class FakeTranslationProvider : ITranslationProvider
        {
            public System.Collections.Generic.IReadOnlyDictionary<string,
                System.Collections.Generic.Dictionary<SupportedLanguage, string>> Load()
            {
                return new System.Collections.Generic.Dictionary<string,
                    System.Collections.Generic.Dictionary<SupportedLanguage, string>>
                {
                    ["fake_key"] = new System.Collections.Generic.Dictionary<SupportedLanguage, string>
                    {
                        [SupportedLanguage.English] = "FakeValue"
                    }
                };
            }
        }
    }
}
