using System.Collections.Generic;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    /// <summary>
    /// Fix #17: Abstracts translation data loading from LocalizationService.
    /// Implementors can load from JSON files, Resources, or Addressables.
    /// Follows OCP — adding a new language source doesn't require modifying LocalizationService.
    /// </summary>
    public interface ITranslationProvider
    {
        /// <summary>
        /// Returns a dictionary of key → (language → translated string).
        /// </summary>
        IReadOnlyDictionary<string, Dictionary<SupportedLanguage, string>> Load();
    }
}
