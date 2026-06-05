using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Infrastructure
{
    /// <summary>
    /// Sprint #12: Externalized translation loader.
    /// Covers JSON parse logic, multi-language integrity, and StreamingAssets fallback.
    /// </summary>
    [TestFixture]
    public class JsonTranslationProviderTests
    {
        private const string MinimalJson = "{\"entries\":[" +
            "{\"key\":\"a\",\"tr\":\"A-tr\",\"en\":\"A-en\"}," +
            "{\"key\":\"b\",\"tr\":\"B-tr\",\"en\":\"B-en\",\"de\":\"B-de\"}" +
        "]}";

        // ─── Parse (pure function) ────────────────────────────────────────────

        [Test]
        public void Parse_MinimalJson_ReturnsTwoKeys()
        {
            var result = JsonTranslationProvider.Parse(MinimalJson);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey("a"));
            Assert.IsTrue(result.ContainsKey("b"));
        }

        [Test]
        public void Parse_TurkishAndEnglish_Populated()
        {
            var result = JsonTranslationProvider.Parse(MinimalJson);

            Assert.AreEqual("A-tr", result["a"][SupportedLanguage.Turkish]);
            Assert.AreEqual("A-en", result["a"][SupportedLanguage.English]);
        }

        [Test]
        public void Parse_MissingLanguage_OmitsKeyNotAddsEmpty()
        {
            // 'a' only has tr+en; 'b' has tr+en+de. Verify sparse entries are not padded.
            var result = JsonTranslationProvider.Parse(MinimalJson);

            Assert.IsFalse(result["a"].ContainsKey(SupportedLanguage.German),
                "Sparse entries should omit the key, not store empty string.");
            Assert.IsTrue(result["b"].ContainsKey(SupportedLanguage.German));
        }

        [Test]
        public void Parse_EmptyEntries_ReturnsEmptyDictionary()
        {
            var result = JsonTranslationProvider.Parse("{\"entries\":[]}");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Parse_NullOrMissingEntries_ReturnsEmptyDictionary()
        {
            // JsonUtility yields an empty entries list for {} — both are safe.
            var empty = JsonTranslationProvider.Parse("{}");
            Assert.IsNotNull(empty);
            Assert.AreEqual(0, empty.Count);
        }

        [Test]
        public void Parse_EmptyKey_Skipped()
        {
            const string json = "{\"entries\":[" +
                "{\"key\":\"\",\"tr\":\"x\",\"en\":\"y\"}," +
                "{\"key\":\"valid\",\"tr\":\"v\",\"en\":\"v\"}" +
            "]}";

            var result = JsonTranslationProvider.Parse(json);

            Assert.AreEqual(1, result.Count, "Empty key rows are dropped.");
            Assert.IsTrue(result.ContainsKey("valid"));
        }

        // ─── File-based load (real StreamingAssets file) ──────────────────────

        [Test]
        public void Parse_StreamingAssetsFile_LoadsAllFiveLanguages()
        {
            // Validates the actual file that ships with the game. Editor builds
            // resolve Application.streamingAssetsPath to <project>/Assets/StreamingAssets.
            var path = Path.Combine(UnityEngine.Application.streamingAssetsPath,
                "Localization/translations.json");
            if (!File.Exists(path))
            {
                Assert.Ignore("translations.json not present in StreamingAssets at '{0}'. " +
                    "Skipping integration test in CI without the asset.", path);
                return;
            }

            string json = File.ReadAllText(path);
            var result = JsonTranslationProvider.Parse(json);

            Assert.GreaterOrEqual(result.Count, 50, "Shipped translations should cover 50+ keys.");
            Assert.IsTrue(result["moves_text"].ContainsKey(SupportedLanguage.Turkish));
            Assert.IsTrue(result["moves_text"].ContainsKey(SupportedLanguage.English));
            Assert.IsTrue(result["moves_text"].ContainsKey(SupportedLanguage.German));
            Assert.IsTrue(result["moves_text"].ContainsKey(SupportedLanguage.Spanish));
            Assert.IsTrue(result["moves_text"].ContainsKey(SupportedLanguage.French));
        }
    }
}
