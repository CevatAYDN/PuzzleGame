using System;
using System.Collections.Generic;

namespace PuzzleGame.Infrastructure.Models
{
    /// <summary>
    /// JSON DTO for translations.json — one row per key, one column per language.
    /// Field names match the JSON exactly (lowercase language codes) so JsonUtility
    /// can deserialize without custom converters.
    ///
    /// Format choice: flat array (vs nested per-key dictionary) keeps the JSON
    /// greppable and diff-friendly for translators working in a spreadsheet-style flow.
    /// </summary>
    [Serializable]
    public class TranslationFile
    {
        public List<TranslationEntry> entries;
    }

    [Serializable]
    public class TranslationEntry
    {
        public string key;
        public string tr;
        public string en;
        public string de;
        public string es;
        public string fr;
    }
}
