using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Generic feature flag service interface supporting boolean, string, and integer values.
    /// All methods are safe to call in editor or production.
    /// </summary>
    public interface IFeatureFlagService
    {
        /// <summary>
        /// Gets a boolean feature flag value.
        /// </summary>
        /// <param name="key">The flag key</param>
        /// <param name="defaultValue">Fallback value if key not found</param>
        /// <returns>Flag value or default</returns>
        bool GetBool(string key, bool defaultValue);

        /// <summary>
        /// Gets a string feature flag value.
        /// </summary>
        /// <param name="key">The flag key</param>
        /// <param name="defaultValue">Fallback value if key not found</param>
        /// <returns>Flag value or default</returns>
        string GetString(string key, string defaultValue);

        /// <summary>
        /// Gets an integer feature flag value.
        /// </summary>
        /// <param name="key">The flag key</param>
        /// <param name="defaultValue">Fallback value if key not found</param>
        /// <returns>Flag value or default</returns>
        int GetInt(string key, int defaultValue);
    }
}