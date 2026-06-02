using System;
using System.Collections.Generic;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Dil desteği için temel enum.
    /// </summary>
    public enum SupportedLanguage
    {
        Turkish,
        English,
        German,
        Spanish,
        French
    }

    /// <summary>
    /// Localization için domain modeli.
    /// Clean Architecture: UnityEngine bağımlılığı yok.
    /// </summary>
    public class LocalizationEntry
    {
        public string Key { get; set; }
        public Dictionary<SupportedLanguage, string> Translations { get; set; } = new Dictionary<SupportedLanguage, string>();
    }
}