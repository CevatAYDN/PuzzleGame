using System;
using System.Collections.Generic;
using System.IO;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Reads translations from a JSON file in StreamingAssets and exposes the
    /// full IReadOnlyDictionary contract required by ITranslationProvider.
    ///
    /// Sprint #12: Externalized 53 keys × 5 languages from LocalizationService.cs
    /// (was 289 LOC of hardcoded AddTranslation calls) into a single JSON file.
    /// Designers/translators can now edit translations without touching C#.
    ///
    /// Android note: StreamingAssets on Android lives inside the APK (jar:// path),
    /// so File.ReadAllText fails. For production Android, load via UnityWebRequest
    /// asynchronously and feed the result through a custom provider implementing
    /// ITranslationProvider. Editor + Standalone paths work as-is.
    /// </summary>
    public class JsonTranslationProvider : ITranslationProvider
    {
        private const string DefaultRelativePath = "Localization/translations.json";
        private readonly string _filePath;

        public JsonTranslationProvider() : this(DefaultRelativePath, useStreamingAssets: true) { }

        public JsonTranslationProvider(string filePath)
            : this(filePath, useStreamingAssets: false) { }

        private JsonTranslationProvider(string filePath, bool useStreamingAssets)
        {
            _filePath = useStreamingAssets
                ? Path.Combine(UnityEngine.Application.streamingAssetsPath, filePath)
                : filePath;
        }

        public IReadOnlyDictionary<string, Dictionary<SupportedLanguage, string>> Load()
        {
            string json = File.ReadAllText(_filePath);
            return Parse(json);
        }

        /// <summary>
        /// Pure-function parser. Public-static so tests can verify deserialization
        /// without touching the filesystem, and so a future async loader (e.g.
        /// UnityWebRequest on Android) can hand the raw JSON text to the same logic.
        /// </summary>
        public static IReadOnlyDictionary<string, Dictionary<SupportedLanguage, string>> Parse(string json)
        {
            var file = JsonUtility.FromJson<TranslationFile>(json);
            if (file?.entries == null)
            {
                return new Dictionary<string, Dictionary<SupportedLanguage, string>>();
            }

            var result = new Dictionary<string, Dictionary<SupportedLanguage, string>>(file.entries.Count);
            foreach (var entry in file.entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                var map = new Dictionary<SupportedLanguage, string>(5);
                TryAdd(map, SupportedLanguage.Turkish, entry.tr);
                TryAdd(map, SupportedLanguage.English, entry.en);
                TryAdd(map, SupportedLanguage.German, entry.de);
                TryAdd(map, SupportedLanguage.Spanish, entry.es);
                TryAdd(map, SupportedLanguage.French, entry.fr);
                result[entry.key] = map;
            }
            return result;
        }

        private static void TryAdd(Dictionary<SupportedLanguage, string> map, SupportedLanguage lang, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                map[lang] = value;
            }
        }
    }
}
